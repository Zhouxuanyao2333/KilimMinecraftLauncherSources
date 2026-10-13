# Kilim Minecraft Launcher — 交接文档 / Handover Document

**中文** | [English](#english)

> 本文档描述项目架构与现状，**不包含源码**。源码请直接阅读仓库。
> This document describes the project architecture and current state and **contains no source code**. Please read the repository for the code itself.

---

<a id="中文"></a>

# 中文

- 分支：`master`
- 技术栈：C# / .NET 10 + Avalonia 12，无 WebView，自绘液态玻璃 UI
- 账号模型：**仅 Microsoft（Xbox Live）账号**

## 目录

- [一、项目概况](#cn-1)
- [二、技术栈与构建](#cn-2)
- [三、目录结构与模块职责](#cn-3)
- [四、账号认证链](#cn-4)
- [五、游戏启动链](#cn-5)
- [六、实例与目录分析](#cn-6)
- [七、界面结构](#cn-7)
- [八、数据持久化](#cn-8)
- [九、已知限制与待办](#cn-9)
- [十、开发注意事项](#cn-10)

<a id="cn-1"></a>
## 一、项目概况

Kilim 是一个 Minecraft Java 版启动器桌面客户端，职责是：

1. 通过 Microsoft 账号登录并取得 Minecraft 官方令牌；
2. 识别本机已有的 Minecraft 安装目录，解析版本元数据；
3. 检测并匹配合适的 Java 运行时；
4. 组装 JVM 启动参数并拉起游戏进程。

当前形态是**导入式启动器**：使用用户本机已安装的 Minecraft 目录，不从网络下载游戏文件。

设计取向：

- **不引入 WebView**，界面全部由 Avalonia 原生控件 + Skia 自绘完成；
- 账号体系只走官方渠道，不存在任何绕过官方验证的登录方式；
- 所有运行数据集中放在可执行文件同级目录，便于排查。

<a id="cn-2"></a>
## 二、技术栈与构建

| 项 | 内容 |
|---|---|
| 运行时 | .NET 10（`net10.0`），RID `win-x64` |
| UI 框架 | Avalonia 12.1.2 + Fluent 主题 + Inter 字体 |
| 图形 | SkiaSharp 3.119.4（3D 皮肤投影、玻璃着色器运行时效果） |
| 音频 | NAudio 2.2.1 |
| 发布形态 | 单文件自包含、开启压缩、未裁剪 |
| 子项目 | `src/LiquidGlassAvaloniaUI`（独立 csproj，Avalonia 12.0.1） |

构建：`dotnet build Project.Launch.csproj -c Debug`

发布：`dotnet publish Project.Launch.csproj -c Release -r win-x64`

<a id="cn-3"></a>
## 三、目录结构与模块职责

```
Project.Launch/
├── Program.cs                 程序入口，注册全局崩溃日志
├── App.axaml(.cs)             应用初始化、启动时恢复上次账户、创建主窗
├── Project.Launch.csproj      依赖、资源登记、发布配置
├── app.manifest               Windows 清单（DPI 感知等）
└── src/
    ├── Tools/                 内核逻辑层
    ├── Tools/Auth/            账号认证链
    ├── Views/                 界面层
    ├── ViewModel/             预留的 MVVM 协调层
    └── LiquidGlassAvaloniaUI/ 液态玻璃 UI 子系统
```

### `src/Tools/` — 内核逻辑

| 文件 | 职责 |
|---|---|
| `LauncherPaths.cs` | 集中管理全部路径（数据根目录、配置文件、日志文件） |
| `LogHelper.cs` | 统一日志写入（中英双语，带锁） |
| `AccountData.cs` | 账户数据模型（玩家名、UUID、令牌、皮肤 URL） |
| `AccountService.cs` | 账户持久化：原子写（临时文件 + Move 覆盖）、读改写收口在单一锁内 |
| `InstanceData.cs` / `InstanceService.cs` | 实例数据模型与持久化 |
| `MinecraftDirectoryAnalyzer.cs` | 目录布局识别与版本扫描 |
| `MinecraftVersionJson.cs` | 版本 JSON 数据模型 |
| `GameLauncher.cs` | 组装参数、解压 natives、拉起游戏进程 |
| `JavaDetector.cs` | Java 探测与版本匹配 |
| `DefaultSkinSelector.cs` | 按 UUID 判定官方默认皮肤（Steve / Alex） |
| `SkinCropper.cs` | 从皮肤 PNG 裁出头像与帽子层 |
| `MinecraftSkinModel.cs` | 皮肤立方体模型的顶点与 UV 定义（Classic / Slim） |
| `Pivot3DRenderer.cs` | 伪 3D 投影、背面剔除、画家算法排序、光照 |
| `Pivot3DControl.cs` | 3D 皮肤控件（自定义绘制 + 拖拽 + 自动旋转） |
| `SoundService.cs` / `SoundConfig.cs` | 音效播放与音量配置 |
| `AnimationConfig.cs` | 全局动画参数（时长、缩放峰值、模糊峰值） |
| `InteractionConfig.cs` | 交互参数（拖拽方向、灵敏度） |

### `src/Tools/Auth/` — 认证链

| 文件 | 职责 |
|---|---|
| `MicrosoftAuthFlow.cs` | 全流程编排器，通过事件向 UI 回报进度与设备代码 |
| `DeviceCodeAuthService.cs` | 微软 OAuth 2.0 设备代码流（申请代码 + 轮询取令牌） |
| `XboxAuthService.cs` | Xbox Live 认证 + XSTS 授权，含 XErr 错误码中文映射 |
| `MinecraftAuthService.cs` | Minecraft 认证 + 拉取玩家档案 |
| `AuthModels.cs` | 各阶段请求 / 响应的 JSON 模型 |

### `src/Views/` — 界面层

| 文件 | 职责 |
|---|---|
| `MainWindow.axaml(.cs)` | 主窗：五页签、自绘标题栏、滑块动画、实例导入、启动游戏 |
| `LoginWindow.axaml(.cs)` | 登录窗：Microsoft 设备代码登录 |
| `AccountManagerWindow.axaml(.cs)` | 账户管理：列表、切换、删除、添加 |
| `Styles.axaml` / `Glass.axaml` | 全局样式与玻璃按钮样式 |
| `imgs/` / `Sound/` | 图标、皮肤、音效资源 |

### `src/LiquidGlassAvaloniaUI/`

独立的液态玻璃渲染子系统（主项目引用其编译产物，源码不参与主项目编译）：

- 从 `TopLevel` 抓取背景快照，发布给所有 `LiquidGlassSurface`；
- 6 个 SKSL 运行时着色器：透镜折射、高光、交互高光、伽马、渐进遮罩、背景变换；
- 快照缓存与节流（哈希去重、脏矩形、滤镜结果 LRU）。

<a id="cn-4"></a>
## 四、账号认证链

仅 Microsoft 账号，共 7 个阶段，全部走官方接口：

| 阶段 | 说明 |
|---|---|
| 1. 申请设备代码 | 向微软 OAuth 端点申请 `device_code`，拿到 `user_code` 与验证网址 |
| 2. 用户浏览器授权 | 启动器自动打开浏览器并带上 `otc` 参数，或由用户手动打开并输入代码 |
| 3. 轮询换取令牌 | 按 `interval` 轮询，处理 `authorization_pending` / `slow_down` / `access_denied` / `expired_token`，支持取消 |
| 4. Xbox Live 认证 | 用微软 access_token 换取 XBL token（`RpsTicket` 需加 `d=` 前缀） |
| 5. XSTS 授权 | 用 XBL token 换取 XSTS token，失败时按 XErr 错误码给出中文原因 |
| 6. Minecraft 认证 | 用 `XBL3.0 x=<uhs>;<xsts>` 换取 Minecraft access_token |
| 7. 拉取档案 | 获取玩家名、UUID 与皮肤 URL；账号未拥有游戏时明确报错 |

得到的账户信息包含玩家名、UUID、微软 refresh_token、Minecraft access_token 及其过期时间、皮肤 URL。

`MicrosoftAuthFlow` 通过两个事件与界面解耦：`DeviceCodeReady`（用户代码就绪）与 `Progress`（阶段提示）。

<a id="cn-5"></a>
## 五、游戏启动链

启动前的校验顺序：

1. 必须已登录 Microsoft 账号（否则提示先登录）；
2. 所选账户必须存在且 `access_token` 未过期（否则提示重新登录）；
3. 版本 JSON 必须存在且可解析；
4. 必须能检测到满足版本要求的 Java。

参数组装要点：

- **兼容两代参数格式**：新版 `arguments.jvm` / `arguments.game` 数组，与旧版 `minecraftArguments` 字符串；
- **rules 规则过滤**：库与参数都做操作系统判定（当前仅实现 Windows 判定）；
- **natives 解压**：跳过 `META-INF/`，处理 `${arch}` 的 64 / 32 位分支；
- **占位符替换**：`${classpath}`、`${natives_directory}`、`${library_directory}`、`${assets_root}`、`${assets_index_name}`、`${game_directory}`、`${classpath_separator}` 等；
- **正版身份**：`${auth_player_name}` 取玩家名，`${auth_uuid}` 取 UUID，`${auth_access_token}` / `${auth_session}` 取真实 Minecraft access_token，`${user_type}` 为 `msa`。

进程启动后，标准输出与标准错误被重定向到 `instances/{实例}/game.log`。

<a id="cn-6"></a>
## 六、实例与目录分析

`MinecraftDirectoryAnalyzer.Analyze(path)` 自动识别两种布局：

| 形态 | 判定条件 | 游戏目录 |
|---|---|---|
| 标准 | 选定目录下直接存在 `versions/` | 选定目录本身 |
| 隔离实例 | 选定目录下存在 `.minecraft/versions/` | 选定目录下的 `.minecraft/` |

扫描规则：`versions/<版本号>/` 下必须存在同名 `<版本号>.json` 才算一个可用版本。

<a id="cn-7"></a>
## 七、界面结构

- **主窗**：五个页签（下载 / 启动 / 主页 / 设置 / 工具），底部玻璃滑块移动动画（缓入缓出 + 梯形缩放脉冲 + 模糊），页签内容淡入淡出。
- **自绘标题栏**：`WindowDecorations="None"` 配合自绘标题栏，两态切换——弹出模态窗时为渐变底 + 浅色文字，登录完成后切为透明底 + 深色文字，图标随之黑白切换。
- **登录窗**：直接呈现 Microsoft 设备代码登录流程，三种界面状态（初始 / 等待授权 / 认证推进）。
- **账户管理窗**：账户列表按最近使用排序，点击切换，删除带确认弹窗，`ShouldOpenLogin` 通知主窗是否需要拉起登录窗。
- **3D 皮肤**：启动页显示可拖拽旋转的 3D 人物，皮肤类型（Classic / Slim）自动检测，交互后暂停自动旋转。
- **音效与 Toast**：音效在后台线程播放以避免阻塞 UI；提示信息以右下角 Toast 呈现。

<a id="cn-8"></a>
## 八、数据持久化

数据根目录为可执行文件同级的 `KilimLauncher/`：

| 文件 | 内容 |
|---|---|
| `accounts.json` | 账户字典、当前选中账户、启动时是否跳过登录窗 |
| `instances.json` | 实例列表与当前选中实例 |
| `app.log` | 应用级日志 |
| `MainWindowLog.txt` | 主窗口日志 |
| `startup.log` | 进程启动日志 |
| `crash.log` | 未捕获异常堆栈 |

写入策略：`accounts.json` 与 `instances.json` 均采用**原子写**（先写 `.tmp`，再 `File.Move` 覆盖），避免写入中断导致配置损坏。

<a id="cn-9"></a>
## 九、已知限制与待办

**认证相关**

- 需要把 `src/Tools/Auth/DeviceCodeAuthService.cs` 中的 `ClientId` 替换为已在 Azure 注册的应用 ID。
- 微软 refresh_token 已持久化，但**尚未实现静默续期**；access_token 过期（约 24 小时）后需要用户重新登录。
- `${auth_xuid}` 暂固定为 `0`，尚未从 XSTS 声明中提取真实 XUID。

**启动相关**

- 尚无 `-Xmx` / `-Xms` 内存参数设置界面。
- 窗口分辨率在占位符中固定为 925×530。
- 启动前不做完整性校验，缺失的库会被静默跳过。
- 拉起进程后立即返回，未监听游戏退出事件。
- 尚未支持 Forge / Fabric 等模组加载器。

**功能相关**

- 下载页仅有界面骨架，尚未实现版本下载能力。
- `src/ViewModel/` 为预留的 MVVM 协调层，尚未接入。

<a id="cn-10"></a>
## 十、开发注意事项

1. **编译前先关闭正在运行的启动器**，否则输出文件被占用会报 `MSB3027 / MSB3021`；这是文件占用，不是代码错误。
2. **不要在仓库根目录创建临时 csproj**：主项目使用默认 `**/*.cs` 通配，临时项目在 `obj/` 下生成的 `AssemblyInfo.cs` 会被吸入主项目编译，导致 `CS0579` 程序集特性重复。
3. **`Border` 是 `Decorator`，只能有一个子元素**；多个子元素需要套一层 `Grid`，否则报 `AVLN3000`。
4. **新增窗口必须在 csproj 的 `<AvaloniaXaml Update="...">` 中登记**，否则不参与 XAML 编译。
5. **数据目录不在 AppData**，而在可执行文件同级的 `KilimLauncher/`。
6. 涉及像素坐标计算时注意用户系统的 DPI 缩放。
7. 3D 光照已在 `Pivot3DRenderer` 中修正过面片法线方向（法线朝外 + 光源变换回模型空间），调整渲染时不要改回。

---

<a id="english"></a>

# English

- Branch: `master`
- Stack: C# / .NET 10 + Avalonia 12, no WebView, self-rendered liquid-glass UI
- Account model: **Microsoft (Xbox Live) accounts only**

## Contents

- [1. Overview](#en-1)
- [2. Tech Stack and Build](#en-2)
- [3. Directory Structure and Module Responsibilities](#en-3)
- [4. Account Authentication Chain](#en-4)
- [5. Game Launch Chain](#en-5)
- [6. Instances and Directory Analysis](#en-6)
- [7. Interface Structure](#en-7)
- [8. Data Persistence](#en-8)
- [9. Known Limitations and TODO](#en-9)
- [10. Development Notes](#en-10)

<a id="en-1"></a>
## 1. Overview

Kilim is a Minecraft: Java Edition launcher desktop client. Its responsibilities are:

1. Sign in with a Microsoft account and obtain official Minecraft tokens;
2. Detect an existing local Minecraft installation and parse its version metadata;
3. Detect and match a suitable Java runtime;
4. Assemble JVM launch arguments and start the game process.

The current form is an **import-based launcher**: it uses a Minecraft directory already installed on the user's machine and does not download game files from the network.

Design choices:

- **No WebView is used.** The entire interface is built from native Avalonia controls plus custom Skia rendering.
- The account system uses official channels only. No method of bypassing official authentication exists.
- All runtime data is kept in a single directory next to the executable, which makes troubleshooting straightforward.

<a id="en-2"></a>
## 2. Tech Stack and Build

| Item | Details |
|---|---|
| Runtime | .NET 10 (`net10.0`), RID `win-x64` |
| UI framework | Avalonia 12.1.2 + Fluent theme + Inter font |
| Graphics | SkiaSharp 3.119.4 (3D skin projection, runtime glass shaders) |
| Audio | NAudio 2.2.1 |
| Distribution | Single-file, self-contained, compression enabled, untrimmed |
| Subproject | `src/LiquidGlassAvaloniaUI` (separate csproj, Avalonia 12.0.1) |

Build: `dotnet build Project.Launch.csproj -c Debug`

Publish: `dotnet publish Project.Launch.csproj -c Release -r win-x64`

<a id="en-3"></a>
## 3. Directory Structure and Module Responsibilities

```
Project.Launch/
├── Program.cs                 Entry point; registers the global crash log
├── App.axaml(.cs)             App initialization, restores the last account, creates the main window
├── Project.Launch.csproj      Dependencies, resource registration, publish configuration
├── app.manifest               Windows manifest (DPI awareness, etc.)
└── src/
    ├── Tools/                 Core logic layer
    ├── Tools/Auth/            Account authentication chain
    ├── Views/                 UI layer
    ├── ViewModel/             Reserved MVVM coordination layer
    └── LiquidGlassAvaloniaUI/ Liquid-glass UI subsystem
```

### `src/Tools/` — Core Logic

| File | Responsibility |
|---|---|
| `LauncherPaths.cs` | Centralizes all paths (data root, config files, log files) |
| `LogHelper.cs` | Unified logging (bilingual, lock-protected) |
| `AccountData.cs` | Account model (player name, UUID, tokens, skin URL) |
| `AccountService.cs` | Account persistence: atomic writes (temp file + move-overwrite); read-modify-write confined to a single lock |
| `InstanceData.cs` / `InstanceService.cs` | Instance model and persistence |
| `MinecraftDirectoryAnalyzer.cs` | Directory layout detection and version scanning |
| `MinecraftVersionJson.cs` | Version JSON data model |
| `GameLauncher.cs` | Argument assembly, natives extraction, game process startup |
| `JavaDetector.cs` | Java discovery and version matching |
| `DefaultSkinSelector.cs` | Determines the official default skin (Steve / Alex) from the UUID |
| `SkinCropper.cs` | Crops the avatar and hat layer out of a skin PNG |
| `MinecraftSkinModel.cs` | Vertex and UV definitions for the skin cube model (Classic / Slim) |
| `Pivot3DRenderer.cs` | Pseudo-3D projection, back-face culling, painter's algorithm sorting, lighting |
| `Pivot3DControl.cs` | 3D skin control (custom drawing + drag + auto-rotate) |
| `SoundService.cs` / `SoundConfig.cs` | Sound playback and volume configuration |
| `AnimationConfig.cs` | Global animation parameters (durations, peak scale, peak blur) |
| `InteractionConfig.cs` | Interaction parameters (drag direction, sensitivity) |

### `src/Tools/Auth/` — Authentication Chain

| File | Responsibility |
|---|---|
| `MicrosoftAuthFlow.cs` | End-to-end orchestrator; reports progress and device code to the UI via events |
| `DeviceCodeAuthService.cs` | Microsoft OAuth 2.0 device code flow (request code + poll for token) |
| `XboxAuthService.cs` | Xbox Live authentication + XSTS authorization, with XErr code mapping |
| `MinecraftAuthService.cs` | Minecraft authentication + player profile retrieval |
| `AuthModels.cs` | JSON models for each stage's requests / responses |

### `src/Views/` — UI Layer

| File | Responsibility |
|---|---|
| `MainWindow.axaml(.cs)` | Main window: five tabs, custom title bar, slider animations, instance import, game launch |
| `LoginWindow.axaml(.cs)` | Login window: Microsoft device code sign-in |
| `AccountManagerWindow.axaml(.cs)` | Account management: list, switch, remove, add |
| `Styles.axaml` / `Glass.axaml` | Global styles and glass button styles |
| `imgs/` / `Sound/` | Icons, skins, sound resources |

### `src/LiquidGlassAvaloniaUI/`

A standalone liquid-glass rendering subsystem (the main project references its build output; its source is excluded from the main project's compilation):

- Captures a background snapshot from the `TopLevel` and publishes it to every `LiquidGlassSurface`;
- Six SKSL runtime shaders: lens refraction, highlight, interactive highlight, gamma, progressive mask, backdrop transform;
- Snapshot caching and throttling (hash deduplication, dirty rects, LRU of filter results).

<a id="en-4"></a>
## 4. Account Authentication Chain

Microsoft accounts only, across 7 stages, all using official endpoints:

| Stage | Description |
|---|---|
| 1. Request device code | Requests a `device_code` from the Microsoft OAuth endpoint, receiving a `user_code` and verification URI |
| 2. User authorizes in browser | The launcher opens the browser automatically with the `otc` parameter, or the user opens it manually and enters the code |
| 3. Poll for token | Polls at `interval`, handling `authorization_pending` / `slow_down` / `access_denied` / `expired_token`, and supports cancellation |
| 4. Xbox Live authentication | Exchanges the Microsoft access token for an XBL token (the `RpsTicket` requires a `d=` prefix) |
| 5. XSTS authorization | Exchanges the XBL token for an XSTS token; on failure, gives a human-readable reason based on the XErr code |
| 6. Minecraft authentication | Exchanges `XBL3.0 x=<uhs>;<xsts>` for a Minecraft access token |
| 7. Profile retrieval | Obtains player name, UUID and skin URL; reports a clear error if the account does not own the game |

The resulting account record contains the player name, UUID, Microsoft refresh token, Minecraft access token with its expiry, and the skin URL.

`MicrosoftAuthFlow` is decoupled from the UI via two events: `DeviceCodeReady` (the user code is available) and `Progress` (stage notifications).

<a id="en-5"></a>
## 5. Game Launch Chain

Pre-launch validation order:

1. A Microsoft account must be signed in (otherwise the user is prompted to sign in);
2. The selected account must exist and its `access_token` must not be expired (otherwise the user is prompted to sign in again);
3. The version JSON must exist and be parseable;
4. A Java runtime satisfying the version requirement must be detected.

Argument assembly highlights:

- **Supports both argument generations**: the modern `arguments.jvm` / `arguments.game` arrays and the legacy `minecraftArguments` string;
- **`rules` filtering**: both libraries and arguments are filtered by operating system (currently Windows detection only);
- **Natives extraction**: skips `META-INF/` and resolves the 64 / 32-bit branch of `${arch}`;
- **Placeholder substitution**: `${classpath}`, `${natives_directory}`, `${library_directory}`, `${assets_root}`, `${assets_index_name}`, `${game_directory}`, `${classpath_separator}`, and more;
- **Genuine account identity**: `${auth_player_name}` is the player name, `${auth_uuid}` is the UUID, `${auth_access_token}` / `${auth_session}` is the real Minecraft access token, and `${user_type}` is `msa`.

After the process starts, stdout and stderr are redirected to `instances/{instance}/game.log`.

<a id="en-6"></a>
## 6. Instances and Directory Analysis

`MinecraftDirectoryAnalyzer.Analyze(path)` automatically detects two layouts:

| Layout | Detection condition | Game directory |
|---|---|---|
| Standard | A `versions/` directory exists directly under the selected folder | The selected folder itself |
| Isolated instance | `.minecraft/versions/` exists under the selected folder | `.minecraft/` under the selected folder |

Scanning rule: a version counts as usable only if `versions/<version>/` contains a matching `<version>.json`.

<a id="en-7"></a>
## 7. Interface Structure

- **Main window**: five tabs (Download / Launch / Home / Settings / Tools) with a bottom glass slider animation (ease-in-out + trapezoidal scale pulse + blur) and cross-fading tab content.
- **Custom title bar**: `WindowDecorations="None"` with a self-drawn title bar that has two states — a gradient background with light text while a modal window is open, and a transparent background with dark text once sign-in completes, with the icons switching between white and black accordingly.
- **Login window**: presents the Microsoft device code sign-in flow directly, with three UI states (initial / waiting for authorization / authentication chain progressing).
- **Account manager window**: lists accounts ordered by most recent use, switches on click, confirms before deleting, and exposes `ShouldOpenLogin` so the main window knows whether to open the login window.
- **3D skin**: the launch page shows a draggable, rotatable 3D character; the skin type (Classic / Slim) is auto-detected and auto-rotation pauses after interaction.
- **Sound and toasts**: sounds are played on a background thread to avoid blocking the UI, and messages are shown as a bottom-right toast.

<a id="en-8"></a>
## 8. Data Persistence

The data root is the `KilimLauncher/` directory next to the executable:

| File | Contents |
|---|---|
| `accounts.json` | Account dictionary, currently selected account, whether to skip the login window at startup |
| `instances.json` | Instance list and currently selected instance |
| `app.log` | Application-level log |
| `MainWindowLog.txt` | Main window log |
| `startup.log` | Process startup log |
| `crash.log` | Unhandled exception stack traces |

Write strategy: both `accounts.json` and `instances.json` use **atomic writes** (write `.tmp` first, then `File.Move` over the target) so that an interrupted write cannot corrupt the configuration.

<a id="en-9"></a>
## 9. Known Limitations and TODO

**Authentication**

- The `ClientId` in `src/Tools/Auth/DeviceCodeAuthService.cs` must be replaced with an application ID registered in Azure.
- The Microsoft refresh token is persisted, but **silent renewal is not implemented yet**; once the access token expires (about 24 hours), the user must sign in again.
- `${auth_xuid}` is currently hard-coded to `0`; the real XUID is not yet extracted from the XSTS claims.

**Launching**

- No UI for `-Xmx` / `-Xms` memory settings yet.
- The window resolution is fixed at 925×530 in the placeholders.
- No integrity check before launch; missing libraries are skipped silently.
- Returns immediately after starting the process and does not listen for the game's exit event.
- No support for mod loaders such as Forge / Fabric yet.

**Features**

- The download page is only a UI skeleton; version downloading is not implemented yet.
- `src/ViewModel/` is a reserved MVVM coordination layer that is not wired up yet.

<a id="en-10"></a>
## 10. Development Notes

1. **Close any running instance of the launcher before building**, otherwise the output file will be locked and the build will fail with `MSB3027 / MSB3021`. This is file locking, not a code error.
2. **Do not create temporary csproj files in the repository root**: the main project uses the default `**/*.cs` glob, so an `AssemblyInfo.cs` generated under a temporary project's `obj/` would be pulled into the main compilation, causing duplicated assembly attributes (`CS0579`).
3. **`Border` is a `Decorator` and accepts only one child**; multiple children require wrapping them in a `Grid`, otherwise `AVLN3000` is raised.
4. **New windows must be registered in the csproj under `<AvaloniaXaml Update="...">`**, otherwise they are not included in XAML compilation.
5. **The data directory is not in AppData**; it is `KilimLauncher/` next to the executable.
6. Keep the user's system DPI scaling in mind when computing pixel coordinates.
7. The 3D lighting in `Pivot3DRenderer` has already been corrected once for face normal direction (normals facing outward, with the light direction transformed back into model space). Do not revert this when adjusting rendering.
