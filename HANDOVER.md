# Kilim Minecraft Launcher — 交接文档

> 本文档描述项目架构与现状，**不包含源码**。源码请直接阅读仓库。
>
> - 分支：`master`
> - 技术栈：C# / .NET 10 + Avalonia 12，无 WebView，自绘液态玻璃 UI
> - 账号模型：**仅 Microsoft（Xbox Live）账号**

---

## 目录

- [一、项目概况](#一项目概况)
- [二、技术栈与构建](#二技术栈与构建)
- [三、目录结构与模块职责](#三目录结构与模块职责)
- [四、账号认证链](#四账号认证链)
- [五、游戏启动链](#五游戏启动链)
- [六、实例与目录分析](#六实例与目录分析)
- [七、界面结构](#七界面结构)
- [八、数据持久化](#八数据持久化)
- [九、已知限制与待办](#九已知限制与待办)
- [十、开发注意事项](#十开发注意事项)

---

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

---

## 二、技术栈与构建

| 项 | 内容 |
|---|---|
| 运行时 | .NET 10（`net10.0`），RID `win-x64` |
| UI 框架 | Avalonia 12.1.2 + Fluent 主题 + Inter 字体 |
| 图形 | SkiaSharp 3.119.4（3D 皮肤投影、玻璃着色器运行时效果） |
| 音频 | NAudio 2.2.1 |
| 发布形态 | 单文件自包含、开启压缩、未裁剪 |
| 子项目 | `src/LiquidGlassAvaloniaUI`（独立 csproj，Avalonia 12.0.1） |

构建：

```bash
dotnet build Project.Launch.csproj -c Debug
```

发布：

```bash
dotnet publish Project.Launch.csproj -c Release -r win-x64
```

---

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

---

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

---

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

---

## 六、实例与目录分析

`MinecraftDirectoryAnalyzer.Analyze(path)` 自动识别两种布局：

| 形态 | 判定条件 | 游戏目录 |
|---|---|---|
| 标准 | 选定目录下直接存在 `versions/` | 选定目录本身 |
| 隔离实例 | 选定目录下存在 `.minecraft/versions/` | 选定目录下的 `.minecraft/` |

扫描规则：`versions/<版本号>/` 下必须存在同名 `<版本号>.json` 才算一个可用版本。

---

## 七、界面结构

- **主窗**：五个页签（下载 / 启动 / 主页 / 设置 / 工具），底部玻璃滑块移动动画（缓入缓出 + 梯形缩放脉冲 + 模糊），页签内容淡入淡出。
- **自绘标题栏**：`WindowDecorations="None"` 配合自绘标题栏，两态切换——弹出模态窗时为渐变底 + 浅色文字，登录完成后切为透明底 + 深色文字，图标随之黑白切换。
- **登录窗**：直接呈现 Microsoft 设备代码登录流程，三种界面状态（初始 / 等待授权 / 认证推进）。
- **账户管理窗**：账户列表按最近使用排序，点击切换，删除带确认弹窗，`ShouldOpenLogin` 通知主窗是否需要拉起登录窗。
- **3D 皮肤**：启动页显示可拖拽旋转的 3D 人物，皮肤类型（Classic / Slim）自动检测，交互后暂停自动旋转。
- **音效与 Toast**：音效在后台线程播放以避免阻塞 UI；提示信息以右下角 Toast 呈现。

---

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

---

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

---

## 十、开发注意事项

1. **编译前先关闭正在运行的启动器**，否则输出文件被占用会报 `MSB3027 / MSB3021`；这是文件占用，不是代码错误。
2. **不要在仓库根目录创建临时 csproj**：主项目使用默认 `**/*.cs` 通配，临时项目在 `obj/` 下生成的 `AssemblyInfo.cs` 会被吸入主项目编译，导致 `CS0579` 程序集特性重复。
3. **`Border` 是 `Decorator`，只能有一个子元素**；多个子元素需要套一层 `Grid`，否则报 `AVLN3000`。
4. **新增窗口必须在 csproj 的 `<AvaloniaXaml Update="...">` 中登记**，否则不参与 XAML 编译。
5. **数据目录不在 AppData**，而在可执行文件同级的 `KilimLauncher/`。
6. 涉及像素坐标计算时注意用户系统的 DPI 缩放。
7. 3D 光照已在 `Pivot3DRenderer` 中修正过面片法线方向（法线朝外 + 光源变换回模型空间），调整渲染时不要改回。
