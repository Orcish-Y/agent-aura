# Agent Aura WPF 原型：Windows 11 验证操作手册

这份手册用于完成 `Validate the Windows observation shell` 票据的实机验证。完成后，原型才可以给出 **继续使用 WPF** 或 **进行同等 Tauri 对比** 的结论。

对应的验收结果记录在 [prototype-validation.md](prototype-validation.md)。每项完成后都应把“Not measured”替换为实际结果、日期和证据位置。

## 1. 验证范围与通过规则

本次验证的是一次性的 WPF 观察窗口原型，而不是 Codex 集成或生产版 Agent Aura。它要证明以下体验和分发形态可行：

- 仅驻留系统托盘，不显示任务栏按钮。
- 可显示、隐藏、最小化、恢复和获得焦点。
- 无边框、半透明、可置顶；置顶时自动隐藏 Header，非置顶时 Header 保持可见。
- 示例 Agent Item 可从一行展开为四行，提供状态图形、低饱和颜色、编辑/删除、长文本悬停后延迟内部滚动和详情淡入。
- 窗口位置、尺寸和置顶状态可恢复；失去显示器后回到可见屏幕。
- 可发布为体积较小的、依赖预装 .NET 10 Windows Desktop Runtime 的 `win-x64` 程序；缺少运行时时由用户选择是否打开 Microsoft 官方安装页。

所有“核心能力”门槛必须通过。若任一核心能力失败，或资源/稳定性门槛失败，不直接否定 WPF；应记录证据并启动同等范围的 Tauri 对比。

## 2. 环境准备

准备两套 Windows 11 环境：

1. **开发验证机**：Windows 11、.NET 10 SDK、可从仓库运行源码。
2. **运行时存在环境**：Windows 11，安装 x64 .NET 10 Windows Desktop Runtime，用于验证正常启动。
3. **运行时缺失环境**：Windows 11 虚拟机、Windows Sandbox 或另一台机器；不能安装 .NET 10 Windows Desktop Runtime 或 SDK，用于验证先决条件提示。

在开发验证机的仓库根目录运行：

```powershell
dotnet --info
dotnet run --project src/AgentAura.Prototype/AgentAura.Prototype.csproj
```

记录 Windows 版本、系统架构、CPU、内存、磁盘类型、.NET SDK 版本和提交号。验证时建议关闭不相关的重负载程序，并使用同一台机器完成五次冷启动测量。

## 3. 核心体验验证

每个步骤通过后，保存截图或录屏，并在结果表写下通过/失败和证据文件名。

### 3.1 托盘优先生命周期

1. 启动程序，确认任务栏没有 `Agent Aura prototype` 按钮；若图标位于通知区域折叠菜单，也确认其存在。
2. 右键托盘图标，确认菜单有 **Show**、**Hide**、**Pin / unpin**、**Settings** 和 **Exit**。
3. 点击 **Hide**，确认窗口消失但托盘图标保留。
4. 左键托盘图标，确认窗口重新出现、恢复普通状态并获得焦点。
5. 在窗口 Header 中点击 **Minimise**，确认窗口隐藏到托盘；再次左键托盘图标，确认可恢复。

通过标准：没有任务栏按钮，且显示、隐藏、最小化、恢复和聚焦均可用。

### 3.2 窗口形态和置顶

1. 确认窗口无系统标题栏和边框，表面半透明且文字仍清晰。
2. 用 Header 左侧的标题区域拖动窗口，再拖动窗口边缘改变尺寸。
3. 点击 **Pin**，再切换到另一个普通窗口，确认原型保持在最前面。
4. 将鼠标移出已置顶窗口，确认 Header 隐藏；再移回窗口，确认 Header 重新出现。
5. 再次点击 **Pin** 取消置顶，确认 Header 即使鼠标离开也持续可见，并且窗口不再总在最前。

通过标准：无边框、半透明、置顶和 Header 行为均符合预期，且 Header 可用于拖动。

### 3.3 Agent Item、状态和文本

窗口中的每个示例 Agent Item 是一个 Codex Thread 的视觉替身。逐项检查：

1. 未悬停时，每项只有一行：状态图形、标题和颜色可见，编辑/删除按钮不显示。
2. 将鼠标移到某一项上，确认它显示四行：标题/操作、项目与工作目录、状态与时间、详情。
3. 检查状态不只依赖颜色：Attention 显示 `!`，Running 显示 `▶`，Succeeded 显示 `✓`；颜色应为低饱和色。
4. 点击 **Edit**，确认示例标题变为以 `Alias:` 开头；点击 **Delete**，确认该示例从当前列表移除。
5. 检查较长的标题或详情文本。鼠标未直接悬停在该文本行时，确认它以省略号截断。将鼠标直接移到该文本行，确认省略号隐藏、完整内容先短暂停留，然后在可用宽度内平滑往返滚动，并在两端停顿；不显示工具提示。逐行重复此检查，确认同一 Agent Item 中只有鼠标所在的文本行滚动。

通过标准：悬停从一行变为四行，状态有形状/文字与颜色双重表达；长文本未直接悬停时以省略号截断，仅在直接悬停对应行后延迟滚动、在两端停顿且没有工具提示。

### 3.3.1 长文本滚动回归（宿主机自动测试）

按 [长文本悬停滚动自测](SCROLLING_TEXT_SELF_TEST.zh-CN.md) 执行自动回归和逐行人工检查。

### 3.4 托盘闪烁与确认

1. 点击 **Flash tray**。
2. 随即点击 **Hide**，观察通知区域图标在信息和警告图标之间持续切换。
3. 左键托盘图标，确认窗口恢复并获得焦点，图标停止切换。
4. 重复点击 **Flash tray** 两次，确认不会叠加出多个独立警报，而是保持一个连续的闪烁状态。

通过标准：窗口隐藏时可看到一个持续警报；左键恢复窗口即确认并停止警报。

## 4. 窗口状态持久化与失屏恢复

状态文件位于：

```text
%LOCALAPPDATA%\AgentAura\Prototype\window-state.json
```

1. 将窗口拖到非默认位置，改变尺寸并开启置顶。
2. 通过托盘菜单的 **Exit** 退出程序，然后重新启动。
3. 确认位置、尺寸和置顶状态都恢复。
4. 再次退出，使用文本编辑器把状态文件改成屏幕外的位置，例如：

```json
{
  "Left": 99999,
  "Top": 99999,
  "Width": 520,
  "Height": 320,
  "IsPinned": true
}
```

5. 重启程序，确认它出现在主显示器的可见区域中间，而非停留在屏幕外。
6. 可选：把 JSON 改成无效文本后启动，确认程序使用默认状态而不崩溃。

通过标准：有效状态会恢复；不可见坐标和损坏 JSON 都不会让用户失去窗口。

## 5. 框架依赖发布和运行时先决条件

在开发验证机从仓库根目录发布框架依赖包：

```powershell
.\scripts\publish-framework-dependent.ps1
```

发布目录是：

```text
src\AgentAura.Prototype\bin\Release\net10.0-windows\win-x64\publish\
```

将该目录的**全部内容**复制到两套 Windows 11 环境。发布目录不应包含 `coreclr.dll`、`hostfxr.dll` 或 Windows Desktop/WPF 的运行时程序集；它们属于机器级 .NET Runtime，而不是 Agent Aura 包。

在运行时存在环境中：

1. 确认 `dotnet --list-runtimes` 包含 `Microsoft.WindowsDesktop.App 10.x`。
2. 双击 `Start-AgentAura.cmd`，确认程序可启动、托盘图标出现，且完成第 3 节的最小生命周期检查。
3. 运行自动外部启动检查：

   ```powershell
   .\scripts\check-framework-dependent-startup.ps1 -Scenario RuntimePresent
   ```

在运行时缺失环境中：

1. 确认 `dotnet` 不存在，或 `dotnet --list-runtimes` 不包含 `Microsoft.WindowsDesktop.App 10.x`。
2. 双击 `Start-AgentAura.cmd`。确认程序不会启动，窗口会清楚说明需要 x64 .NET 10 Windows Desktop Runtime，并询问是否打开 Microsoft 官方下载页。
3. 选择 `y`，确认浏览器才会打开 `https://dotnet.microsoft.com/download/dotnet/10.0/runtime`；选择其他值或直接关闭窗口时不打开浏览器、不下载、不安装任何内容。
4. 安装运行时后，重新执行同一个 `Start-AgentAura.cmd`，确认应用正常启动。
5. 在该运行时缺失环境执行：

   ```powershell
   .\scripts\check-framework-dependent-startup.ps1 -Scenario RuntimeMissing
   ```

通过标准：运行时存在时应用正常启动；运行时缺失时应用停止在明确的先决条件提示，只有用户确认才会打开官方安装页；安装后重启成功。仅完成 publish 不算通过；必须完成两套环境的外部启动检查。

## 6. 性能、包体和稳定性测量

### 6.1 冷启动

1. 退出程序，等待至少 10 秒。
2. 用秒表从双击 EXE 开始计时，到托盘图标可用且观察窗口显示为止。
3. 重复五次，记录每次秒数和中位数。

通过标准：中位数不高于 1.5 秒。

### 6.2 空闲内存和 CPU

1. 显示窗口后不要操作，等待五分钟。
2. 使用任务管理器“详细信息”页，或性能监视器采集 `AgentAura.Prototype` 的 Private Working Set 和 `% Processor Time`。
3. 每 30 秒记录一次，至少记录十个样本；计算平均 CPU 和最高/平均内存。

通过标准：平均 CPU 不高于 1%，工作集不高于 100 MiB。

### 6.3 发布包大小

在开发验证机运行：

```powershell
$publish = 'src\AgentAura.Prototype\bin\Release\net10.0-windows\win-x64\publish'
[math]::Round(((Get-ChildItem $publish -Recurse | Measure-Object Length -Sum).Sum / 1MB), 1)
```

通过标准：发布目录总大小不高于 200 MiB。

### 6.4 稳定性

连续运行至少八小时。每 30 分钟执行一次：显示/隐藏、置顶/取消置顶、悬停展开、减弱动画开关、托盘恢复和退出后重启。记录每次故障、卡死、托盘图标残留或窗口无法找回的情况。

通过标准：无崩溃、无卡死、无卡住的托盘图标，也没有无法恢复的屏幕外窗口。

## 7. 结果记录模板

将下表复制到 [prototype-validation.md](prototype-validation.md) 的 `Result` 列或追加到其末尾：

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| Windows 版本、硬件、.NET SDK、提交号 |  |  |
| 托盘优先生命周期 | Pass / Fail |  |
| 窗口形态与置顶 | Pass / Fail |  |
| Agent Item 与文本 | Pass / Fail |  |
| 减弱动画 | Pass / Fail |  |
| 托盘闪烁与确认 | Pass / Fail |  |
| 位置、尺寸、置顶恢复 | Pass / Fail |  |
| 屏幕外与损坏状态恢复 | Pass / Fail |  |
| 运行时存在环境启动 | Pass / Fail |  |
| 运行时缺失环境恢复路径 | Pass / Fail |  |
| 冷启动（五次与中位数） |  |  |
| 空闲内存与 CPU |  |  |
| 发布目录大小 |  |  |
| 八小时稳定性 | Pass / Fail |  |

证据至少包括：每个关键行为的截图或短录屏、干净机启动截图、性能原始数字、异常日志，以及发布命令输出。

查看 src\AgentAura.Prototype\prototype-validation.md 已经记录，虽然不是table的形式，但是内容都记录了

## 8. 最终决策

在验证表的 `Decision record` 中写出以下两种结论之一：

- **推荐 WPF**：全部核心能力通过，性能和稳定性均未超出门槛。写明实际测量结果和已知限制。
- **启动 Tauri 对比**：列出每个失败门槛、可复现步骤、环境、证据和失败影响。对比必须覆盖同一套托盘、窗口、无障碍和自包含分发能力，不能只比较包体大小。

没有实机数据时，结论必须保持“undecided”，不能仅因项目能够编译或发布就推荐 WPF。
