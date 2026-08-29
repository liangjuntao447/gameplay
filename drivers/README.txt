TouchCloudPad 依赖的虚拟手柄 — ViGEmBus
=============================================

本工具通过 ViGEmBus 虚拟出一个“Xbox 360 手柄”（XInput），让云游戏客户端
把它当真实手柄识别。它由两部分组成，缺一不可：

  ① ViGEmClient.dll   —— 用户态客户端库（程序要能加载它）
  ② ViGEmBus 驱动     —— 内核驱动，负责真正创建虚拟手柄

重点说明：
- ① ViGEmClient.dll【没有官方预编译下载，需要自己从源码编译】。
  GitHub 仓库 nefarius/ViGEmClient 只有源码，要用 CMake/vcpkg 构建（见下方“编译”）。
  编译好之后，把 x64 的 ViGEmClient.dll 放到 TouchCloudPad.exe 旁即可，程序会自动加载。
  不要用别处复制来的改版 DLL（函数接口不同，会“ViGEm 初始化失败”）。
- ② ViGEmBus 驱动必须“真正安装并加载”。只把 .inf 放进 DriverStore 不算装好，
  需管理员安装，通常要【重启电脑】。

一、编译 ViGEmClient.dll（在你自己的电脑上做，需联网）
---------------------------------------------------------
方式一：vcpkg（推荐，规范）
  1. 安装：Visual Studio 2019/2022 生成工具（含 MSVC C++）、CMake、Git。
  2. 用作者维护的 vcpkg fork：
       git clone https://github.com/nefarius/vcpkg
       cd vcpkg
       .\bootstrap-vcpkg.bat
       .\vcpkg install vigemclient:x64-windows
  3. 编译好的 DLL 在：
       vcpkg\installed\x64-windows\bin\ViGEmClient.dll
  4. 把这个 DLL 复制到 TouchCloudPad.exe 旁。

方式二：直接构建源码
  1. git clone --recurse-submodules https://github.com/nefarius/ViGEmClient
  2. cmake -B build -S . -A x64
  3. cmake --build build --config Release
  4. 到 build\Release\ 取 ViGEmClient.dll，复制到 TouchCloudPad.exe 旁。

二、安装 ViGEmBus 驱动（若还没装）
-----------------------------------
以管理员身份运行本文件夹里的 install.bat（它会从 GitHub 下载并安装驱动），
装完【重启电脑】。（驱动安装不提供 ViGEmClient.dll，上面第一步要单独编译。）

三、确认
---------
双击 check.bat：
- ViGEmClient.dll 一项为 [OK]；
- ViGEmBus driver service 一项为 [OK] + STATE RUNNING。
然后重启 TouchCloudPad.exe，状态栏应显示“手柄:已连接”。

四、常见状态栏提示
-------------------
- “缺少 ViGEmClient.dll” → 程序目录没放编译好的 ViGEmClient.dll。
- “ViGEm 初始化失败” → 放了非官方/改版 DLL，请换成官方源码编译版。
- “ViGEmBus 驱动未安装或未加载” → 驱动没装好，看“二”并重启。
- “手柄:已连接” → 正常。
