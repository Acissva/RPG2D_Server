# 公告数据库脚本

本目录保存本机开发数据库 rpg2d_dev 的初始化脚本，目标 MySQL Community Server 9.7.2。由管理账号手工执行，C# 服务启动时不自动执行。数据库实际执行进展单独记录；文件存在本身不代表初始化成功。

## 执行前

用 DBeaver 连接 127.0.0.1:3306，使用 sslMode=REQUIRED。先确认目标库尚不存在、会话为自动提交、会话 SQL 模式含 STRICT_TRANS_TABLES 或 STRICT_ALL_TABLES：

```sql
SELECT @@session.autocommit AS autocommit_enabled, @@session.sql_mode AS session_sql_mode;
SHOW DATABASES;
```

已有同名数据库时先核对用途和结构，不使用删除重建处理。脚本不包含数据库账号密码。

## 执行顺序与重跑影响

1. 001_create_notice_schema.sql：首次建立数据库和公告表；没有 IF NOT EXISTS，已有库 / 表会明确报错。中途失败时检查哪一步已成功，逐条继续尚未完成的步骤，不盲目重跑或忽略错误。结构采用 InnoDB、utf8mb4、utf8mb4_0900_ai_ci；字段仅 id / title / content。
2. 确认公告表为空后，手工执行一次 002_seed_notice.sql：插入一条非敏感示例公告；每次再次执行都会增加记录，不会覆盖已有公告。服务启动时不自动执行。
3. 确认 rpg2d_app 尚不存在后，逐条执行 003_create_notice_app_user.sql：创建 'rpg2d_app'@'127.0.0.1' 并要求加密连接，仅授权 rpg2d_dev.notices 的 SELECT / INSERT / UPDATE / DELETE。CREATE USER 会返回生成密码，用户先在本机保存密码，再执行 GRANT；实际密码不写入脚本或仓库。重跑会报账号已存在，不用于重置密码或修改已有授权。

在 DBeaver 中每次只选中一条完整语句按 Ctrl+Enter；执行整份脚本使用 Alt+X。001 与 002 分开执行并检查结果，不把示例数据插入混入重复运行的只读查询中。

## 执行后查询

```sql
DESCRIBE rpg2d_dev.notices;
SHOW CREATE TABLE rpg2d_dev.notices;
SELECT id, title, content FROM rpg2d_dev.notices;
```

期望三个字段，id 为有符号 BIGINT 主键并自增，标题 / 正文非 NULL，数据库长度上限分别为 64 / 512 个字符；示例数据首次执行后有一行公告。C# 的 UTF-16 长度与空白校验在应用层单独实现。

数据库运行数据保存在 MySQL 数据目录；保存 / 发布 SQL 文件不等于备份运行数据。当前脚本不执行自动迁移、回滚或删除；003 的账号初始化独立于结构与数据脚本。初始化脚本和账号准备不是复制服务端发布目录就会自动完成的步骤。

## 应用账号核对

应用连接使用 Host=127.0.0.1、Port=3306、Database=rpg2d_dev、User=rpg2d_app、sslMode=REQUIRED。本机密码由用户单独保存；后续服务端 Development 通过 User Secrets 配置，不使用 root 运行公告业务。

```sql
SELECT USER() AS connection_identity, CURRENT_USER() AS matched_account;
SHOW GRANTS;
SHOW SESSION STATUS LIKE 'Ssl_cipher';
SELECT id, title, content FROM rpg2d_dev.notices;
```

matched_account 应为 rpg2d_app@127.0.0.1。授权结果应只含 USAGE / REQUIRE SSL 与项目公告表四项权限；不授予 CREATE / DROP / GRANT OPTION 等管理权限。开发验收只按实际 id 操作自己的样本，不重复执行 002 作为清理手段。

## 独立边界验收

本目录 001～003 仍只初始化 rpg2d_dev，不加入故障注入或自动切库步骤。独立 rpg2d_acceptance_v0017 的手工准备方法见工程根 README；该表通过 LIKE 复制开发表结构，初始无数据。按需准备后，同一应用账号可同时拥有两个明确 notices 表的四项 CRUD，其他权限仍不扩展。

临时坏数据、列名调整与行锁只在独立验收表进行；检查结束后恢复列名、结束事务、按本次样本 id 清理，再切回开发库。原开发公告与持久化验收记录保留，实际执行结果在版本开发记录中单独核查。
