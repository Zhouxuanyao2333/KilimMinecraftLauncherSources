# Kilim Minecraft Launcher

**中文** | [English](#english)

---

<a id="中文"></a>

# 中文

一个使用 C# 与 Avalonia 编写的 Minecraft Java 版启动器：无 WebView、原生渲染性能、自绘液态玻璃界面。

## 账号支持

本启动器**仅支持 Microsoft（Xbox Live）账号登录**。

- 通过微软 OAuth 2.0 **设备代码流**完成授权，用户全程在浏览器中操作，启动器不接触、不存储用户密码。
- 认证链完整走官方接口：Microsoft OAuth → Xbox Live → XSTS → Minecraft 认证 → 玩家档案。
- 启动游戏时使用 Minecraft 官方签发的 `access_token` 与 `user_type=msa`，以正版身份进入服务器。
- 皮肤取自 Mojang 官方纹理服务；账号未设置自定义皮肤时回退官方默认皮肤（按 UUID 判定 Steve / Alex）。
- 不提供、也不计划提供任何绕过官方验证的登录方式。

启动器需要配置自己的 Azure 应用 `ClientId`，见 `src/Tools/Auth/DeviceCodeAuthService.cs`。

## 功能

| 模块 | 说明 |
|---|---|
| 账号 | Microsoft 账号登录、多账号管理（切换 / 删除）、账户持久化 |
| 实例 | 导入本机已有的 Minecraft 目录，自动识别标准 `.minecraft` 与隔离实例两种布局 |
| 版本 | 解析版本 JSON，兼容新版 `arguments.jvm` / `arguments.game` 数组与旧版 `minecraftArguments` 字符串 |
| 启动 | 拼装 classpath、解压 natives、全量占位符替换、rules 规则过滤、进程日志重定向 |
| Java | 从 `JAVA_HOME`、`PATH`、常见 JDK 厂商目录检测 Java，并按版本 JSON 要求匹配 |
| 界面 | 五页签主窗、自绘标题栏、液态玻璃滑块动画、3D 皮肤预览、音效与 Toast |

## 技术栈

| 项 | 内容 |
|---|---|
| 运行时 | .NET 10（`net10.0`），win-x64 |
| UI | Avalonia 12.1.2 + Fluent 主题 + Inter 字体 |
| 渲染 | SkiaSharp（3D 皮肤投影、玻璃着色器） |
| 音频 | NAudio |
| 发布 | 单文件自包含（`PublishSingleFile` + 压缩），未裁剪 |
| 子项目 | `src/LiquidGlassAvaloniaUI`（独立 csproj，主项目引用编译产物） |

## 构建

```bash
dotnet build Project.Launch.csproj -c Debug
```

发布单文件：

```bash
dotnet publish Project.Launch.csproj -c Release -r win-x64
```

> 编译前请先关闭正在运行的启动器，否则会因输出文件被占用报 `MSB3027 / MSB3021`。

## 数据目录

所有运行数据位于可执行文件同级的 `KilimLauncher/` 目录（不使用 AppData）：

```
KilimLauncher/
├── accounts.json      账户与令牌
├── instances.json     实例列表
├── app.log            应用日志
├── MainWindowLog.txt  主窗口日志
├── startup.log        启动日志
└── crash.log          崩溃日志
```

## 说明

- 本项目**不捆绑、不分发任何 Minecraft 游戏文件**，需要用户自行提供本机已安装的游戏目录。
- 本项目与 Mojang Studios、Microsoft 无隶属关系。Minecraft 是 Mojang Studios 的商标。

## 许可

见仓库根目录许可证文件。

---

<a id="english"></a>

# English

A Minecraft: Java Edition launcher written in C# and Avalonia — no WebView, native rendering performance, and a self-rendered liquid-glass UI.

## Account Support

This launcher **supports Microsoft (Xbox Live) accounts only**.

- Authorization uses the Microsoft OAuth 2.0 **device code flow**. Users complete sign-in entirely in their browser; the launcher never touches or stores user passwords.
- The authentication chain uses official endpoints throughout: Microsoft OAuth → Xbox Live → XSTS → Minecraft authentication → player profile.
- Game launch uses the official Minecraft `access_token` together with `user_type=msa`, so sessions are authenticated as a genuine (premium) account.
- Skins are fetched from Mojang's official texture service. If an account has no custom skin, the launcher falls back to the official default skin (Steve / Alex, selected by UUID).
- No method of bypassing official authentication is provided, nor is one planned.

You must configure your own Azure application `ClientId`; see `src/Tools/Auth/DeviceCodeAuthService.cs`.

## Features

| Area | Description |
|---|---|
| Accounts | Microsoft account sign-in, multi-account management (switch / remove), account persistence |
| Instances | Import an existing local Minecraft directory; automatically detects the standard `.minecraft` layout and the isolated-instance layout |
| Versions | Parses version JSON, supporting both the modern `arguments.jvm` / `arguments.game` arrays and the legacy `minecraftArguments` string |
| Launching | Builds the classpath, extracts natives, performs full placeholder substitution, applies `rules` filtering, redirects process logs |
| Java | Detects Java from `JAVA_HOME`, `PATH` and common JDK vendor directories, then matches the version required by the version JSON |
| Interface | Five-tab main window, custom title bar, liquid-glass slider animations, 3D skin preview, sound effects and toasts |

## Tech Stack

| Item | Details |
|---|---|
| Runtime | .NET 10 (`net10.0`), win-x64 |
| UI | Avalonia 12.1.2 + Fluent theme + Inter font |
| Rendering | SkiaSharp (3D skin projection, glass shaders) |
| Audio | NAudio |
| Distribution | Single-file, self-contained (`PublishSingleFile` + compression), untrimmed |
| Subproject | `src/LiquidGlassAvaloniaUI` (separate csproj; the main project references its build output) |

## Build

```bash
dotnet build Project.Launch.csproj -c Debug
```

Publish a single file:

```bash
dotnet publish Project.Launch.csproj -c Release -r win-x64
```

> Close any running instance of the launcher before building, otherwise the output file will be locked and the build will fail with `MSB3027 / MSB3021`.

## Data Directory

All runtime data lives in a `KilimLauncher/` directory next to the executable (not in AppData):

```
KilimLauncher/
├── accounts.json      Accounts and tokens
├── instances.json     Instance list
├── app.log            Application log
├── MainWindowLog.txt  Main window log
├── startup.log        Startup log
└── crash.log          Crash log
```

## Notes

- This project **does not bundle or redistribute any Minecraft game files**. Users must supply their own local, already-installed game directory.
- This project is not affiliated with Mojang Studios or Microsoft. Minecraft is a trademark of Mojang Studios.

## License

See the license file in the repository root.


# 中文版
## 隐私政策
Kilim Minecraft Launcher（以下简称"本启动器"）尊重并保护用户隐私。
我们收集的信息
本启动器在运行过程中可能接触以下信息：
微软账户认证信息：通过微软官方 OAuth 2.0 设备代码流（Device Code Flow）获取的访问令牌（Access Token）与刷新令牌（Refresh Token），仅用于验证您的正版游戏所有权。
本地配置数据：游戏安装路径、Java 路径、内存分配设置、版本列表等，仅存储在您的计算机本地。
日志文件：运行日志可能包含系统版本、错误堆栈等信息，用于排查问题。
信息存储与传输
所有认证令牌与配置文件仅保存在您的本地设备，我们不会将您的账户信息上传至任何第三方服务器。
本启动器仅与以下官方服务器通信：
微软认证服务（login.microsoftonline.com）
Xbox Live / XSTS 服务（user.auth.xboxlive.com, xsts.auth.xboxlive.com）
Mojang 官方 API（api.minecraftservices.com）
Minecraft 官方资源服务器（piston-meta.mojang.com, resources.download.minecraft.net）
我们不做的事
我们不会收集、存储或分享您的微软账户密码。
我们不会将您的任何个人数据用于广告、分析或商业用途。
我们不会将数据出售给第三方。
第三方服务
本启动器使用微软与 Mojang 的官方 API 进行身份验证与游戏资源下载。这些服务的隐私实践受其各自政策约束：
微软隐私声明
Mojang 隐私政策
免责声明
本启动器为非官方开源项目，与 Mojang Studios、Microsoft 或其关联公司无任何隶属关系。本启动器不对因使用本软件导致的任何账户问题、数据丢失或系统损坏承担责任。使用本启动器即表示您同意自行承担风险。
政策更新
我们可能随时更新本政策，更新后将在本页面公布。继续使用本启动器即视为接受更新后的政策。

# English Version
## Privacy Policy
Kilim Minecraft Launcher (the "Launcher") respects and protects user privacy.
Information We Collect
The Launcher may access the following information during operation:
Microsoft Account Authentication Data: Access tokens and refresh tokens obtained via Microsoft's official OAuth 2.0 Device Code Flow, used solely to verify your ownership of a legitimate Minecraft license.
Local Configuration Data: Game installation paths, Java paths, memory allocation settings, and version lists, stored exclusively on your local machine.
Log Files: Runtime logs may include system version and error stack traces for troubleshooting purposes.
Data Storage & Transmission
All authentication tokens and configuration files are stored locally on your device. We do not upload your account information to any third-party servers.
The Launcher only communicates with the following official servers:
Microsoft Authentication Service (login.microsoftonline.com)
Xbox Live / XSTS Services (user.auth.xboxlive.com, xsts.auth.xboxlive.com)
Mojang Official API (api.minecraftservices.com)
Minecraft Official Resource Servers (piston-meta.mojang.com, resources.download.minecraft.net)
What We Do NOT Do
We do not collect, store, or share your Microsoft account password.
We do not use your personal data for advertising, analytics, or commercial purposes.
We do not sell data to third parties.
Third-Party Services
This Launcher utilizes official APIs from Microsoft and Mojang for authentication and game asset downloads. Their privacy practices are governed by their respective policies:
Microsoft Privacy Statement
Mojang Privacy Policy
Disclaimer
This Launcher is an unofficial open-source project and is not affiliated with, endorsed by, or connected to Mojang Studios, Microsoft, or their affiliates. The Launcher assumes no liability for any account issues, data loss, or system damage arising from the use of this software. Use at your own risk.
Policy Updates
We may update this policy at any time. Continued use of the Launcher constitutes acceptance of the updated policy.
