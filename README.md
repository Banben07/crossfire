# CustomCrosshair (自定义准星)

这是一个专为 Windows 设计的桌面准星叠加层（Overlay），适用于那些不提供自定义准星设置的 FPS 游戏。

## 核心特性

* **透明置顶显示**：采用点击穿透技术，不抢占窗口焦点，不影响游戏操作。
* **配置文件系统**：支持添加、克隆、删除配置文件，并可随时重置为默认设置。
* **内置预设**：提供灵感源自 `CS` 和 `VALORANT` 的经典准星预设。
* **实时预览**：调节参数时准星实时更新，无需保存即可看到效果。
* **系统托盘集成**：可最小化至托盘，支持通过托盘菜单进行显示/隐藏、切换及退出操作。
* **自动启动**：可选随 Windows 启动（写入注册表 `HKCU` 路径）。
* **配置分享**：支持通过“分享代码”快速导入或导出准星配置。
* **深度自定义**：
* 线条长度、粗细、中心间隙。
* 中心点开关及大小调节。
* T 型准星切换。
* 外边框开关及粗细调节。
* 不透明度（Opacity）调节。
* 屏幕位置偏移（Offset）。
* **动态扩散**：支持手动设置扩散值，或在按下 `WASD`、方向键、`空格` 时触发临时扩散。
* 颜色支持 Hex 色值输入及可视化颜色选择器。


* **全局热键**：
* 开关准星显示。
* 循环切换配置文件。


* **本地存储**：设置以 JSON 格式持久化存储于：
* `%APPDATA%\CrossfireCrosshair\settings.json`



---

## 构建与运行

### 环境要求

* Windows 10/11
* .NET 8 SDK

### 快速开始

在项目根目录下执行以下命令：

```powershell
dotnet build
dotnet run

```

---

## 分享代码 (Share Code)

* **导出**：在应用中点击 `Copy current code`（复制当前代码）。
* **导入**：点击 `Import from clipboard`（从剪贴板导入）或 `Paste code...`（粘贴代码）。
* **安全性**：所有导入的配置都会经过清洗和数值范围限制，确保其处于安全区间内。

---

## 发布 (单文件 EXE)

您可以构建一个独立运行的单文件可执行程序：

```powershell
dotnet publish CrossfireCrosshair.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -o artifacts/win-x64

```

或直接使用预设脚本：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1

```

---

## CI/CD 自动化 (GitHub Actions)

工作流文件：`.github/workflows/windows-build.yml`

该流水线会自动执行以下任务：

1. 在 `push`、`PR` 或手动触发时进行代码恢复（Restore）与构建。
2. 发布 `win-x64` 架构的单文件 Release 包。
3. 上传 `.zip` 压缩包及 `.sha256` 校验文件作为构建产物。
4. 当检测到符合 `v*` 格式的 Tag 时，自动创建 GitHub Release。
5. （可选）如果配置了证书密钥，则会自动对 EXE 进行数字签名。

### 代码签名配置

在 GitHub 仓库的 **Secrets** 中设置以下变量以启用 CI 签名：

* `WINDOWS_PFX_BASE64`：Base64 编码后的 `.pfx` 证书内容。
* `WINDOWS_PFX_PASSWORD`：证书密码。

---

## 安全与合规说明

> [!IMPORTANT]
> 没有任何工具可以保证在所有游戏中绝对不被封禁。请务必遵守各游戏的最终用户许可协议（EULA）和反作弊政策。

* **非侵入性**：本程序仅在桌面层绘制叠加图层并监听全局热键。
* **不注入**：不向游戏进程注入任何代码。
* **不读取**：不读取任何游戏内存数据。
* **无驱动**：不安装任何内核级驱动程序。
* **低权限**：不需要管理员权限即可运行。

### 如何减少误报（False Positives）

为了降低被反作弊系统误判的风险，建议：

1. **自行从源码构建**程序。
2. 保持依赖项最简化。
3. **避免**使用任何加壳工具（Obfuscators/Packers）。
4. 使用可信的数字证书对生成的二进制文件进行签名。
5. 发布每个版本时同时公开 Checksum 校验和及更新日志。
