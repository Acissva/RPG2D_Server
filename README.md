# RPG2D_Server

RPG2D 独立 ASP.NET Core 业务服务端。v0.0.18 在 MySQL 公告持久化与受限开发管理 CRUD 的基础上接入 Redis Cache-Aside，保留 protocolVersion=1，与已有 v0.0.16 Unity / Windows Player 兼容。正式发布节点以服务端 main 提交与附注标签为准。

数据库执行包装位于 Infrastructure/Database/MySqlOperationExecutor，复用数据源并为每次操作独立获取连接；公告 SQL、参数绑定和记录映射由 MySqlNoticeRepository 负责。公共 ICacheStore / RedisCacheStore 处理 Redis 命令与有限等待，NoticeCache 负责公告缓存格式、TTL 和降级策略。已识别的存储不可用使用 DataStoreUnavailableException，输入字段错误使用 InputValidationException；公告规则由 NoticeContentRules 定义，记录公有属性使用 Id，对外 JSON 维持 camelCase。

## 开发环境

- Visual Studio 2022 17.14.13，ASP.NET 和 Web 开发工作负载。
- net9.0；global.json 选择 SDK 9.0.304，允许同一功能区段的较新补丁，不使用预览 SDK。
- Microsoft.AspNetCore.OpenApi 9.0.8、MySqlConnector 2.6.2、StackExchange.Redis 3.4.5。
- 用户级 NUGET_PACKAGES=E:\NuGet\Packages、NUGET_HTTP_CACHE_PATH=E:\NuGet\HttpCache；修改后由新启动的工具继承，已核对实际包与还原路径。
- 本机 MySQL Community Server 9.7.2，127.0.0.1:3306。
- 所有新增或本版修改的文本文件保存为 UTF-8。

## 数据库准备与本机配置

Database/Scripts/README.md 说明手工初始化步骤、执行账号与重跑影响。程序启动不会建表、种数据或迁移。

连接使用已有 rpg2d_dev 库及 rpg2d_app 应用账号，要求加密，只具有 notices 表的 SELECT / INSERT / UPDATE / DELETE 权限。管理账号仅用于初始化。

在 VS2022 中右键项目 → 管理用户机密，使用以下占位结构，实际密码只填在本机机密文件中；不要把示例文件放入项目根目录作为真实配置：

```json
{
  "ConnectionStrings": {
    "Rpg2D": "Server=127.0.0.1;Port=3306;Database=rpg2d_dev;User ID=rpg2d_app;Password='本机应用账号密码';SslMode=Required;Pooling=True;Connection Timeout=2;Default Command Timeout=2;Cancellation Timeout=1;UseAffectedRows=False;",
    "Redis": "127.0.0.1:6379,defaultDatabase=0"
  }
}
```

密码中的单引号在连接串内写成两个单引号；JSON 中双引号和反斜杠还需进行 JSON 转义。UserSecretsId 是关联标识，可以保存在项目文件中；实际机密不进入源码、日志或 Git。User Secrets 是开发配置存储，不是加密保险箱。

## Redis 共享连接

本机 Redis 8.2.10 运行于 WSL2 的 Ubuntu-24.04，供 Windows 127.0.0.1:6379 访问；练习期间保留 Ubuntu 终端。Redis Insight 实际程序为 D:\RedisInsight\Redis Insight\Redis Insight.exe，主要应用数据在 E:\RedisInsight\Data，使用数据库 0。

基础 appsettings 的 Redis:Enabled=false，Development 覆盖为 true；Redis:OperationTimeoutMilliseconds=500，启用时校验范围为 100～2000。连接地址来自 ConnectionStrings:Redis，使用 StackExchange.Redis 格式。修改后重启 C# 服务。

RedisConnection 是 Singleton，同一对象注册为 IHostedService，在启动时异步初始化一个 ConnectionMultiplexer，停止时释放。初始 ConnectTimeout=1000、ConnectRetry=1、AbortOnConnectFail=false；命令 AsyncTimeout 取上述配置，心跳间隔 100ms，BacklogPolicy.FailFast。配置有效但 Redis 暂未连接时仍可保留重连对象；缺少 / 格式错误的必需配置会报告启动错误。具体故障耗时及恢复结果在后续缓存批次记录，配置值不代表完整 HTTP 耗时上限。

真实运行已观察到 name=RPG2D_Server、SE.Redis 3.4.5.54687 客户端，原健康 / 公告 GET 均为 200。一个 Multiplexer 可管理多条 TCP 连接；公告已使用缓存，读取来源结合策略日志、请求代码和缓存状态判断，不能仅凭客户端连接判断命中。ICacheStore / RedisCacheStore 已提供字符串读取、强制带 TTL 写入和删除，复用 RedisConnection。缺键返回 null，空字符串保持原值，预期连接 / 超时故障转换为 CacheUnavailableException，请求取消继续传播，其他命令 / 程序错误不伪装成缺键。WaitAsync 结束调用者等待，原命令仍可能执行，收尾任务观察其最终异常。用户独立观察入口已完成实际值 / 过期 / 重复删除练习，结果的五个源文件哈希与当前代码吻合。公告读取策略及管理写后失效均已接入；真实停服与恢复按 5B 分批观察。

## 公告读取缓存

公开查询通过 NoticeCache 实现 Cache-Aside。启用时先读取 / 校验缓存，缺键、无效缓存或预期缓存故障再查询 MySQL；有效数据库结果尽力回填，缓存回填故障不丢弃数据库结果。管理按编号查询继续直接读 MySQL，公开成功 DTO 仍为 protocolVersion / title / content。

缓存使用 Redis String 保存 schemaVersion=1、id、title、content 的 JSON。NoticeCache:TtlSeconds 默认 30、启动校验 1～300；命中不续期，空表不做负缓存。Key 为 rpg2d:{environment}:{database}:notice:current:v1，环境转小写、数据库保持大小写，两段经转义；CurrentKey 由 Program 根据实际配置派生。当前开发键为 rpg2d:development:rpg2d_dev:notice:current:v1。无效 JSON、格式版本或内容被拒绝并尽力失效，不修改数据库来修补缓存内容。

管理 POST / PUT / DELETE 校验通过后，由 NoticeService 的私有包装执行数据库操作，finally 统一尝试失效当前公告缓存；有效操作返回 false 或抛数据库异常也尝试清理。预先取消或输入无效时不开始该次写入 / 清理。写后清理使用独立 1 秒取消期限，底层继续有限等待；记录预期缓存故障、清理期限或非预期清理异常，保留原数据库返回 / 异常。只有删除正常完成才记录成功失效，缺键删除也满足目标。

正常顺序写入成功且失效成功后，下次公开读取回源。数据库已提交但 Redis 删除失败时，写入成功仍保留，旧缓存可能暂留；写入超时 / 断连不能推断一定未提交，先确认事实，不自动重试 POST。DBeaver 直接修改会绕过应用失效，并发读取也可能回填旧快照。TTL 从每次缓存写入计时，不构成严格实时一致性或提交后精确 30 秒可见的承诺。

## 开发启动与协议

打开现有解决方案，选择 http 配置启动，环境为 Development，地址为 http://127.0.0.1:5080。Development 默认加载项目关联的 User Secrets。MySQL 连接串缺失、格式无效或 UseAffectedRows=True 会在启动时报告配置错误；配置有效时不会在构造函数或启动阶段连接 MySQL。启用 Redis 时，宿主在 HTTP 就绪前异步初始化共享缓存连接。

| 请求 | 当前行为 |
| --- | --- |
| GET /api/health | 200，status / service / protocolVersion；只判断 HTTP 服务存活。 |
| GET /api/notice | 启用缓存时先读有效副本，未命中再查询 notices 中现存最大 id 并回填；200 保留 protocolVersion=1、title、content。 |

公告标题 1～64、正文 1～512 个 UTF-16 代码单元，不能只含空白。回源读到无效数据库记录时报告 500；缓存缺失且数据库无记录时返回 404，已识别的 MySQL 连接 / 权限 / 超时故障返回 503；其他程序或结构错误返回 500。有效缓存可以暂时满足公开读取，不代表数据库当前健康。错误响应为简短 ProblemDetails，不含密码、连接串或异常堆栈。

v0.0.18 的 Redis 运行期停服 / 恢复已观察：最新完整五阶段快照中 C# PID 与启动时间保持一致，Redis inactive 时公开公告仍为 200，恢复 active / PONG 后重新建立有限 TTL 缓存，最终删除本次临时样本并回落原公告。首次中断轮次单独保留，没有将不同进程拼作一次恢复；离线 PUT 的 204 / Warning 依据用户实操反馈，快照本身只采集读取。MySQL 停服后的六阶段也已核查：同一 C# 进程中，有效缓存仍可提供公开 200、管理 GET 为 503；PTTL=-2 后公开 / 管理均为 503；双依赖停服时 health 仍为 200，依次恢复后读取及缓存回填恢复正常。Redis 离线冷启动也已通过三阶段核查：启动前无 HTTP 监听且 Redis inactive，新 C# 进程在该时刻之后创建并以 MySQL 提供公告 200；Redis 恢复后保持该进程并回填原公告、有限 TTL。旧副本 / 并发回填也已通过独立验收库及专用 Redis 键的受控观察：删除失败时数据库 B / 缓存 A，正常失效后仍可能由晚到任务回填 B 而数据库已为 C；副本过期后重新回源，8 次少量并发读一致。该观察调用真实 Service / 存储并控制故障及时序，没有调用正式 HTTP；样本和键已清理。

Repository 通过公共 MySqlOperationExecutor 每次异步获取连接；操作内释放 Reader / 命令，执行器负责释放本次连接，数据源由 DI 容器管理。请求取消令牌传到数据库操作，并设置约 3 秒的访问期限。连接等待、命令超时和取消清理有不同阶段，实际 HTTP 耗时在集中故障验收中记录。

RPG2D_Server.http 保留健康、公告及原有方法 / 路由检查请求；DevelopmentNotices.http 提供管理写入练习。Development 提供 /openapi/v1.json，Production 不映射。

v0.0.18 前置整理的正常 CRUD 回归使用 PreCacheRegression.http：POST 只发送一次，将实际编号保存为 SampleId，再验证相同内容 PUT、更新、输入拒绝和按编号清理。本轮已保存的临时样本编号为 6，清理后 GET 为 404，原公告保持不变；重新实操应以新 POST 返回的编号为准。

PreCacheBoundary.http 用于前置边界观察，只含健康、当前公告和原样本的 GET。空表 / 坏存量 / MySQL97 停服与同进程恢复已由用户完成观察；最终只读核查确认 rpg2d_dev 配置及原公告恢复，独立验收表为空。中间观察与最终直接核查的证据边界见 v0.0.18 开发记录。

在 VS 调试中测试数据库停服时，调试器可能在 DataStoreUnavailableException 抛出处暂停，尚未执行到统一异常处理器；暂停期间其他请求也可能等待。在“调试 → 窗口 → 异常设置”中取消该预期异常的“引发时中断”，必要时启用 Continue When Unhandled in User Code，然后按 F5 继续。保留业务异常和统一处理代码；正常执行时公告返回 503，health 不访问数据库、仍为 200。每次只发送当前选定的 .http 请求，避免执行整份文件中的 POST。

## 数据与发布边界

数据由 MySQL 独立保存，关闭 DBeaver 或 C# 服务不会主动清除它。本机开发验收已由 API 写入公告，并分别经过 C# / MySQL 重启后查回；停库时健康仍可用、公告失败，MySQL 恢复后同一 C# 进程可继续读取。旧客户端兼容及独立 Production 产物的实际数据库读取已通过验收。

User Secrets 不会自动随发布产物携带，也不是 Production 默认配置来源。独立发布通过本机临时进程环境变量 ConnectionStrings__Rpg2D 注入连接配置；方法见下文。当前仍复用 v0.0.16 Unity 客户端及 Windows Player，协议版本为 1。

Git 保存源码、依赖版本、结构脚本和配置方法；不保存数据库数据目录、真实机密、.vs、bin、obj 或管理工具凭据。版本内不逐批提交，验收后集中收尾。

## 本机开发管理接口

管理响应包含数据库 id；客户端 GET /api/notice 仍使用 protocolVersion / title / content。

| 方法与路径 | 成功结果 | 主要输入 / 存在性错误 |
| --- | --- | --- |
| POST /api/dev/notices | 201，返回 id / title / content；Location 指向新记录的管理 GET。 | 无效输入为 400。 |
| GET /api/dev/notices/{id:long} | 200，返回指定记录的 id / title / content。 | 非正数为 400，不存在为 404。 |
| PUT /api/dev/notices/{id:long} | 204，完整替换 title / content；内容相同也成功。 | 无效输入 / 非正数为 400，不存在为 404。 |
| DELETE /api/dev/notices/{id:long} | 204，只删除指定记录。 | 非正数为 400，不存在或重复删除为 404。 |

管理入口同时要求 Development、DevelopmentNoticeApi:Enabled=true、服务端看到的 RemoteIpAddress 为回环地址。基础 appsettings 默认关闭，开发配置显式开启；Production 即使误设为 true 也拒绝。请求不满足条件时返回 404，并在模型绑定及业务执行前结束。

所有管理动作共用资源过滤器，不依赖 Host、Origin 或转发头证明本机来源。启用时，格式不符合 long 的路由返回 404；已识别的存储故障沿用 503。

POST / PUT 共用 NoticeWriteRequest 和 NoticeContentRules。缺字段、null、全空白、超长、不合法 JSON、错误字段类型及未声明字段（包括 id）返回 400 ValidationProblemDetails，无效输入不写入数据库。标题和正文按 UTF-16 代码单元计数，保留原文与首尾空白，不裁剪。直接调用业务方法也执行相同的写入规则。

外部 id、title、content 全部通过驱动参数传入，含引号与 SQL 片段的文本仍作为普通公告保存。INSERT 从同一命令 LastInsertedId 取得生成编号；UPDATE / DELETE 使用主键限制为 0 或 1 行。程序要求 UseAffectedRows=False，以匹配行数判断记录是否存在，避免把相同内容 PUT 误判为不存在。

每次 CRUD 是一条 SQL，沿用 InnoDB 自动提交，不自动重试写请求。写入超时或连接中断后的结果可能不确定，先查询数据库再决定后续操作，不直接重复 POST。

DevelopmentNotices.http 每次只发送当前选定的请求。只新增一次，将响应实际 id 保存到 LearningNoticeId；后续读、改、删只使用该编号。删除前先 GET 确认是自己的学习样本，不清空表、不重置自增编号，保留其他公告。新增记录成为当前最大 id，删除后公开公告回落到仍存在的最大 id。

关闭开发配置中的 Enabled 并重启调试实例后，管理 GET 返回 404，公开健康及当前公告接口继续按原规则工作。开发配置保留 true；已通过实际 Production 请求与隔离访问条件检查确认管理入口被拒绝，非回环等上下文覆盖使用隔离检查，未搭建外部网络访问环境。

## 独立边界验收库的准备与恢复

默认开发库为 rpg2d_dev。空表、结构错误和锁等待等检查使用独立 rpg2d_acceptance_v0017，首次由管理账号手工准备，不在程序启动时执行：

```sql
CREATE DATABASE rpg2d_acceptance_v0017 CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE TABLE rpg2d_acceptance_v0017.notices LIKE rpg2d_dev.notices;
GRANT SELECT, INSERT, UPDATE, DELETE ON rpg2d_acceptance_v0017.notices TO 'rpg2d_app'@'127.0.0.1';
```

LIKE 复制定义而不复制数据。已有同名库时先核对用途，不删除重建；本节是可选验收准备方法，不代表脚本已经执行。该账号在验收库也只获指定表 CRUD，不获得 DDL 权限。

停止开发服务后，在本机 User Secrets 的现有连接串中临时修改 Database 选项，仍用原账号、TLS 和有限超时；边界请求保存在 BoundaryAcceptance.http，每次只发送选中的请求。成功响应的实际 id 保存到各样本变量，删除只使用本次创建的编号。

验收分别观察输入拒绝 400、空表 404、坏存量内容 / 列结构错误 500、不可用身份 / 锁等待超时 503。结构故障只在验收表中临时改列名并立即恢复；FOR UPDATE 观察结束必须在原连接 COMMIT / ROLLBACK 释放锁。写入超时后查询实际结果，不用错误状态推断一定未提交。

完成后恢复列名、结束事务、按 id 清理自己的验收样本，再将本机连接配置恢复为 rpg2d_dev / rpg2d_app；原开发库的持久化验收记录保留。独立库和空表可继续保留，后续重用时先确认现状，不重跑首次 CREATE 或重置自增编号。

## 客户端兼容验收

客户端继续复用 v0.0.16 源码与已有 Windows Player。实际入口为客户端 Assets/Scenes/StartScene.scene；连接资产已配置 http://127.0.0.1:5080、5 秒超时，经 ConfigSystem 读取，不包含数据库配置。旧 Player 位于客户端工程 Builds/v0.0.16/RPG2D.exe。

ClientAcceptance.http 保留 v0.0.17 的原样本联调请求；v0.0.18 的 6A 材料使用一次 POST 创建独立临时公告，按命名响应编号更新 / 查询 / 删除，原 id=5 保留。观察编辑器与 Player 同步更新、Redis 停服时回源成功、双依赖故障时旧内容 / 首次无内容、请求中开始游戏或关闭 Player，以及恢复后重新显示。

ServerInfoView 关闭时取消请求，GameServerSystem 在请求取消 / 应用退出时 Abort 并释放；客户端检查涵盖请求中开始游戏或退出，以及公告失败时仍可完成现有单机流程。正常状态文案为“服务可用”，失败时可能显示“显示上次内容”；该内容只是当前客户端进程的内存快照。

v0.0.18 的 6A 已通过：客户端源码 / Player 哈希未变、工作区干净；新日志有 503、开始 / 返回游戏场景、Callback aborted 和 Shutdown。临时公告已清理，原 id=5 及正式缓存已独立读回。菜单画面、初次失败与操作时序依据完成反馈，不从日志推断全部界面结果。本版继续复用旧 Player。

## 本机 Release / Production

Run-LocalProduction.ps1 默认运行 bin/publish/v0.0.18。功能分支的实际 Release / Production 候选已通过公开接口、环境访问限制及 Redis 停服 / 恢复验收；正式发布仍按下文 main 构建与标签流程执行。

在服务端工程根目录的 PowerShell 终端执行：

```powershell
dotnet publish .\RPG2D_Server.csproj -c Release --self-contained false -p:UseAppHost=false -o .\bin\publish\v0.0.18
```

发布保留默认依赖还原，按当前分支的 csproj 核对 NuGet 结果；切换分支后不要默认用 --no-restore 复用 obj。还原不会执行数据库脚本。

采用依赖框架的目录发布，需要 ASP.NET Core 9 运行时。整个发布目录包含主 DLL、依赖与配置，数据和 MySQL 服务由外部独立提供；复制 DLL 或整个目录不会复制数据库与机密。Release 编译配置和 Production 运行环境分别指定。发布目录可能包含非敏感 appsettings.Development.json，但 Production 不加载该文件。

Run-LocalProduction.ps1 仅保存本机非敏感连接选项：127.0.0.1:3306、rpg2d_dev、rpg2d_app、SslMode=Required 及既定有限超时 / UseAffectedRows=False。脚本在正文构造默认发布路径；通过 Read-Host -AsSecureString 隐藏输入密码，连接串构造器处理转义，设置 Process 范围的 ConnectionStrings__Rpg2D，并设置 ConnectionStrings__Redis=127.0.0.1:6379,defaultDatabase=0、Redis__Enabled=true，再从发布目录运行：

```powershell
dotnet .\RPG2D_Server.dll --environment Production --urls http://127.0.0.1:5080 --DevelopmentNoticeApi:Enabled true
```

上述命令由启动脚本在注入配置后执行。实际操作前保持 MySQL97 运行、停止占用 5080 的开发服务；在工程根目录启动：

```powershell
.\Run-LocalProduction.ps1
```

仅在隐藏提示中输入原 rpg2d_app 密码，不把密码写进命令、脚本或发布 JSON。脚本 finally 恢复上述三项原进程变量与目录、清理输入对象；Process 配置不持久写入用户 / 系统环境。环境变量仍会在进程内以明文使用，这种本机学习方式不是机密保险箱。直接运行 DLL 不使用 VS launchSettings.json，Production 不默认加载 User Secrets。

启动日志应显示 Production、127.0.0.1:5080 及本版发布目录。PublishAcceptance.http 逐条检查健康 / 真实公告 200、OpenAPI 404、管理 GET / POST / PUT / DELETE 全部 404。特意开启 Enabled 验证环境限制；POST / PUT 使用不完整 JSON，PUT / DELETE 使用 id=0，避免构成有效数据修改。不要改成合法新增或删除已有公告的请求。

实际候选已确认公开 200、OpenAPI / 四种管理动作 404；真实 Redis 停服时日志显示回源数据库，恢复后重新回填 / 命中。独立复核时 Production 进程运行本版发布 DLL，缓存 rpg2d:production:rpg2d_dev:notice:current:v1 为 schemaVersion=1 / id=5，连续命中 PTTL=29999 → 29997ms；该键与 Development Key 隔离。旧 Player 已完成用户画面观察，日志正常退出；数据库仅保留原公告。隔离预检另覆盖 Redis 不可达、禁用 / 无 Redis 配置和缺 MySQL 配置拒绝启动。具体证据与限制见设计目录 v0.0.18 开发记录。结束服务时在原终端 Ctrl+C。

版本发布流程为推送来源分支、合并 develop / main，在 main 重新发布并核对接口成功后，再创建和推送服务端附注标签；保留远端来源分支。客户端无改动时继续保留 v0.0.16 基线与 Player，不创建空提交或同名新客户端标签。
