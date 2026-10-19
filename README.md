# RPG2D_Server

RPG2D v0.0.16 的独立 ASP.NET Core 业务服务端，当前提供健康检查和只读内存公告。Unity 客户端通过 HTTP / UTF-8 JSON 访问，不引用本项目 DLL。MySQL、Redis 与 Python AI 服务在后续版本引入。

## 开发环境

- Visual Studio 2022 17.14.13，安装“ASP.NET 和 Web 开发”工作负载。
- 项目目标框架 net9.0；本机已验证 SDK 9.0.304、ASP.NET Core / .NET Runtime 9.0.8。
- global.json 选择 SDK 9.0.304，允许同一 9.0.3xx 功能区段中不低于该版本的已安装补丁；不选择预览 SDK。它不负责安装或固定部署机器的运行时。
- 构建需要 NuGet 包 Microsoft.AspNetCore.OpenApi 9.0.8，首次还原需要对应包源或本地缓存。

## 开发启动

VS2022 打开 RPG2D_Server.sln，选择 `http` 配置运行。默认地址为 `http://127.0.0.1:5080`，环境 Development。

命令行等价入口，在项目根目录执行：

```powershell
dotnet run --project .\RPG2D_Server.csproj --launch-profile http
```

修改公告的位置是 Services/NoticeService.cs 中的标题与正文。内存公告在服务实例创建时校验；修改代码并重启服务后，原客户端点击刷新即可读取新内容。

## HTTP 协议

| 请求 | 成功状态与字段 |
| --- | --- |
| GET /api/health | 200；status = "ok"、service = "RPG2D_Server"、protocolVersion = 1。 |
| GET /api/notice | 200；protocolVersion = 1、title、content。 |

响应为 application/json，字段使用 camelCase，文本编码 UTF-8。公告标题 1～64、正文 1～512 个 UTF-16 代码单元，不能仅为空白；与 C# string.Length 的计数规则一致。客户端还限制完整响应正文不超过 16 KiB，且把公告当作普通文本显示。

错误路径返回 404，不支持的方法返回 405。RPG2D_Server.http 保存开发请求示例。Development 环境提供 `/openapi/v1.json`，Production 环境不映射 OpenAPI 文档。

## 发布与独立启动

本版采用 framework-dependent DLL 发布：发布目录不携带 .NET 运行时。运行机器需要安装兼容的 ASP.NET Core 9 运行时及 .NET 9 运行时；无需 Visual Studio、Unity 或 SDK。以下步骤在 PowerShell 中执行。

先停止 VS 调试实例和占用 5080 的测试夹具。在项目根目录发布：

```powershell
dotnet publish .\RPG2D_Server.csproj -c Release --self-contained false -p:UseAppHost=false -o .\bin\publish\v0.0.16
```

进入产物目录，明确指定地址和环境：

```powershell
Set-Location .\bin\publish\v0.0.16
dotnet .\RPG2D_Server.dll --urls http://127.0.0.1:5080 --environment Production
```

启动后请求两个 API，或在 Unity / Windows Player 标题菜单刷新公告。终端 Ctrl+C 停止当前服务实例。

部署时复制整个发布目录，包括业务 DLL、依赖 DLL、deps.json、runtimeconfig.json 和 appsettings 配置；不能只拷贝 RPG2D_Server.dll。这里显式关闭 AppHost，所以没有独立服务端 exe 是预期结果。

launchSettings.json 用于开发启动，不是发布产物的地址配置。本版独立启动命令显式使用 Production；Release 是编译配置，两者含义独立。环境名不决定监听范围，绑定到 127.0.0.1 才限制为当前机器。本版验收仅在本机回环地址运行。

发布目录位于 bin/，已经由 .gitignore 排除。Git 保存源码、项目文件、global.json 与本说明，不提交 .vs/、bin/、obj/ 或本地敏感配置。

## 客户端与版本对应

Unity 的 GameServerSettings 资产使用相同根地址与 5 秒超时。单次有效请求、失败保留本次运行的上一份公告、离开标题菜单取消请求，均由客户端负责；没有磁盘缓存或离线持久化。

当前对应客户端开发版本 v0.0.16、协议版本 1。双方提交哈希与正式客户端标签在最终发布记录中登记；本说明不把未完成的发布写成已完成。

官方参考：[global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)、[.NET 发布方式](https://learn.microsoft.com/en-us/dotnet/core/deploying/)、[ASP.NET Core 环境](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-9.0)。
