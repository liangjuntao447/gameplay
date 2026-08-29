# TouchCloudPad 触控云手柄

一个轻量级的 Windows 10 触屏工具：在触屏电脑上浮出一个**玻璃质感、置顶**的模拟手机手柄面板，
把手指在屏幕上的操作转换成 **虚拟 Xbox 360 手柄（XInput）** 信号，发给正在运行云游戏的
浏览器（Chrome）或桌面应用（绝区零）。

**零依赖、单 exe**：只用 Windows 10 自带的 .NET Framework 4.8 编译，无需安装任何运行库、SDK、
Python、Electron 等，体积仅约 44 KB，非常适合配置较低、支持库较少的旧 Win10 电脑。

---

## 一、功能

- **三种皮肤（一键切换）**：鸣潮（WuWa，Chrome）、崩铁（HSR，Chrome）、绝区零（ZZZ，桌面应用）。
- **玻璃透明感 + 窗口置顶**：半透明磨砂玻璃面板，可拖拽、可缩放、可调透明度、可随时置顶。
- **模拟手机界面**：左侧虚拟摇杆（移动）、中部视角拖拽区（转视角）、右侧动作按钮组。
- **多账号自动切换**：每个账号对应一个独立 Chrome 配置目录，登录态/Cookie 互相隔离，
  选账号后“打开浏览器”即可一键以该账号进入云游戏。
- **多指触控**：可同时按摇杆 + 按钮（真实多点触控）。
- **输出 = 虚拟手柄**：通过 **ViGEmBus** 虚拟出 Xbox 360 手柄（XInput），
  左摇杆=移动、右视角区=右摇杆、动作按钮=手柄按键。**不再使用键鼠模拟。**

---

## 二、运行要求

- Windows 10（含较旧版本）。
- 自带 .NET Framework 4.8（Win10 默认已包含，无需安装）。
- **ViGEmBus 虚拟手柄驱动**（需安装一次，见 `drivers` 文件夹）。
- 云游戏端：鸣潮/崩铁 → 本机已安装 Chrome；绝区零 → 本机云游戏桌面客户端。

---

## 三、编译（可选）

已经提供了编译好的 `TouchCloudPad.exe`，直接运行即可。
如需重新编译：双击 `build.bat`，会调用系统自带 `csc.exe` 编译 `src\*.cs`，
输出到同目录 `TouchCloudPad.exe`。

> 说明：为了兼容旧 Win10，代码按 C# 5 编写，用系统自带编译器即可，无需联网、无需 NuGet。

### 一键 GitHub Actions 构建（推荐，把 ViGEmClient 也一起编译）
项目内置了 `.github/workflows/build.yml`。把它推到 GitHub 后，Actions 会在
Windows 运行器上自动完成全部工作并产出可直接用的发布包：

1. 用 CMake/vcpkg **从源码编译官方的 `ViGEmClient.dll`**（不再需要你自己装编译环境）；
2. 用系统编译器编译 `TouchCloudPad.exe`；
3. 自动下载 **ViGEmBus 驱动安装包**；
4. 打包成 `TouchCloudPad-release.zip`（含 exe + ViGEmClient.dll + 驱动 + 说明），
   上传为构建产物，并可在打 `v*` 标签时自动发布 GitHub Release。

使用方式（在你电脑上执行一次）：
```bash
git init
git add .
git commit -m "init"
git branch -M main
git remote add origin https://github.com/<你的用户名>/<仓库名>.git
git push -u origin main
# 需要发布版时，打标签：
git tag v1.0.0
git push origin v1.0.0
```
然后在 GitHub 仓库的 **Actions** 页查看构建；从该次运行的 **Artifacts**（或
`v1.0.0` 的 **Release**）下载 `TouchCloudPad-release.zip`，解压后里面自带
`ViGEmClient.dll` 和 `ViGEmBus_Setup.*`，装上驱动即可运行。

---

## 四、使用方法

0. **先安装手柄驱动**：双击 `drivers\install.bat` 安装 ViGEmBus（或按 `drivers\README.txt` 手动装）。
1. 运行 `TouchCloudPad.exe`，浮出玻璃手柄面板；状态栏显示“手柄:已连接”即正常。
2. **先点击一下游戏窗口**，让浏览器/桌面客户端获得焦点。
3. 左手在**左侧摇杆**拖动 → 角色移动（映射为手柄左摇杆）。
4. 在**中间视角区**拖动 → 转动视角（映射为手柄右摇杆）。
5. 点按**右侧动作按钮** → 触发对应手柄按键（可长按，如普攻/闪避/扳机）。
6. 顶部：切换皮肤、置顶开关、账号、设置、最小化、关闭。
7. 拖动顶部标题栏可移动面板；**缩放**滑杆或直接拖拽面板边缘可整体按比例放大/缩小。
8. **调整布局**：点“布局”进入编辑模式，直接拖动摇杆、视角区或任意按键到新位置，
   松开即自动保存；再次点“布局”退出编辑、恢复正常操作。

### 账号（多账号切换）
1. 点“管理” → “账号/浏览器”页。
2. “添加账号”，填名称、对应游戏、云游戏网址；点“保存修改”。
3. 在主页“账号”下拉选一个账号 → 点“打开浏览器”，即用该账号的独立配置目录打开云游戏。
4. 换号：切到另一个账号再“打开浏览器”。每个账号的 Cookie/登录态互不干扰。

### 按键自定义
“设置 → 按键设置”里，可为当前皮肤重新指定每个按钮发送的按键，点“保存按键”。

---

## 五、默认手柄按键（可在“手柄按键”设置中改）

> 移动摇杆 → 手柄左摇杆；视角区 → 手柄右摇杆（转视角）。下表为动作按钮对应的手柄按键。

### 鸣潮 WuWa（Chrome）
| 控件 | 手柄按键 |
|---|---|
| 跳跃 | A |
| 冲刺（长按） | LB |
| 普攻（长按） | X |
| 共鸣技能 | Y |
| 共鸣解放 | B |
| 声骸 | RB |
| 交互 | Start |
| 切换角色 1/2/3 | 十字键 ← / ↑ / → |

### 崩铁 HSR（Chrome）
| 控件 | 手柄按键 |
|---|---|
| 交互 / 确定 | A |
| 对话 | B |
| 技能 | X |
| 地图 | Y |
| 菜单 | Start |
| 镜头 | Back |

### 绝区零 ZZZ（桌面应用）
| 控件 | 手柄按键 |
|---|---|
| 普通攻击（长按） | X |
| 特殊攻击 | Y |
| 闪避（长按） | LB |
| 连携 | RB |
| 终结 | B |
| 交互 | A |
| 切换角色 | 十字键 ↑ |

> 云游戏客户端需把“外设/输入”设为“手柄”。若个别键位不一致，可在“设置 → 手柄按键”里为任意按钮改绑。

---

## 六、虚拟手柄驱动（ViGEmBus）

工具通过 **ViGEmBus** 虚拟出一个 Xbox 360 手柄（XInput），这是 Windows 上最通用、
云游戏客户端最容易识别成“手柄”的方案。需要先安装一次驱动：
- 双击 `drivers\install.bat`（自动请求管理员并从 GitHub 官方下载安装），
  或按 `drivers\README.txt` 手动安装。装完**重启电脑**。
- 注意：winget 仓库没有 ViGEmBus，不要用 winget 装。
- 装好后重启本工具，状态栏显示“手柄:已连接”。

**未安装驱动时，工具会提示“手柄:未连接 · 请安装 ViGEmBus 驱动”，不会崩溃。**

---

## 七、文件结构

```
TouchCloudPad/
├─ build.bat            # 一键编译脚本（系统自带 csc.exe）
├─ TouchCloudPad.exe    # 已编译好的可执行文件
├─ drivers/
│  ├─ install.bat       # 一键安装 ViGEmBus 驱动
│  └─ README.txt        # 驱动安装说明
└─ src/
   ├─ Program.cs        # 入口 + 单实例 + 异常保护
   ├─ MainWindow.cs     # 玻璃面板 UI + 多点触控 + 手柄映射
   ├─ XInputGamepad.cs  # ViGEmBus 虚拟 Xbox 手柄（含按键名映射）
   ├─ Native.cs         # Win32 窗口/玻璃样式
   ├─ Skins.cs          # 三套皮肤与手柄按键定义
   ├─ Config.cs         # 配置持久化（JSON）
   ├─ Browser.cs        # 查找 Chrome + 按配置目录启动
   ├─ CrashLog.cs       # 异常记录（crash.log）
   └─ SettingsDialog.cs # 账号管理 + 手柄按键自定义
```

配置保存于 `%LocalAppData%\TouchCloudPad\config.json`（不可写时回退到 exe 旁）。
浏览器配置目录默认在 `%LocalAppData%\TouchCloudPad\Profiles\游戏_账号名\`。
如果程序异常，会在 exe 旁生成 `crash.log`，内含错误详情，可据此排查。

---

## 八、常见问题

- **状态栏显示“手柄:未连接”？** 程序会自动区分两种情况：
  ①“缺少 ViGEmClient.dll”→ 程序目录丢了该文件，把它恢复（它已随 exe 一起提供）。
  ②“ViGEmBus 驱动未安装或未加载”→ 驱动没真正加载。必须**以管理员身份**运行
  `drivers\install.bat`（或手动安装），装完**重启电脑**，再用 `drivers\check.bat` 确认
  “driver service is present”。注意：只把 .inf 放进 DriverStore 不算装好。
- **云游戏识别不到手柄？** 在云客户端“设置/外设”里把输入切到“手柄”，确认该游戏支持手柄。
- **点了没反应？** 先点击一下游戏窗口让它获得焦点；再到“设置 → 手柄按键”确认各按钮绑定的键位。
- **找不到 Chrome？** 在“设置 → 账号”里为该账号手动填 Chrome.exe 路径。
- **突然崩溃？** 程序有异常保护（不会崩，并把详情写入 exe 旁的 `crash.log`）。若仍异常，
  把 `crash.log` 发回即可定位。
