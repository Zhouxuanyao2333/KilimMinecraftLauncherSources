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
