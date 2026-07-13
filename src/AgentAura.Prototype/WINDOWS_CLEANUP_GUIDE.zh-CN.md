# Agent Aura WPF 原型：Windows 验证后清理手册

本手册适用于当前 `AgentAura.Prototype` 验证原型。它不是正式安装版：不会创建开机启动项、Windows 服务、计划任务、注册表配置或 Codex 集成。

先从系统托盘图标的菜单选择 **Exit**，确认 `AgentAura.Prototype.exe` 已退出，再进行清理。

## 仅运行框架依赖发布包

如果你只是从 `publish` 目录运行 `Start-AgentAura.cmd`，且没有安装 .NET SDK，清理下面两项即可：

1. 删除保存窗口位置、尺寸和置顶状态的文件：

   ```powershell
   Remove-Item -LiteralPath "$env:LOCALAPPDATA\AgentAura\Prototype" -Recurse -Force
   ```

2. 删除你复制到验证机上的整个发布目录（包含 `Start-AgentAura.cmd` 和 `AgentAura.Prototype.exe` 的目录）。

这样不会影响系统环境变量、注册表、启动项或其他应用。运行应用所需的 .NET 10 Windows Desktop Runtime 是机器级共享先决条件，不是 Agent Aura 的文件；仅当没有其他应用需要它时，才按 Windows 的卸载流程移除它。

## 从源码运行或发布

若运行过下面任一命令：

```powershell
dotnet run --project src/AgentAura.Prototype/AgentAura.Prototype.csproj
.\scripts\publish-framework-dependent.ps1
```

除上述状态文件外，项目目录中还会产生编译和发布输出。进入仓库根目录后执行：

```powershell
Remove-Item -LiteralPath '.\src\AgentAura.Prototype\bin' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath '.\src\AgentAura.Prototype\obj' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath "$env:LOCALAPPDATA\AgentAura\Prototype" -Recurse -Force -ErrorAction SilentlyContinue
```

若这台机器只用于本次验证，也可在确认不再需要源码、截图和验证记录后，直接删除整个仓库目录。

## 如安装过 .NET 10 SDK

为了使用 `dotnet run`，开发验证机需要 .NET 10 SDK。若该 SDK 不再被任何其他项目使用，请通过 Windows 的 **设置 → 应用 → 已安装的应用** 卸载对应的 **Microsoft .NET SDK 10.x** 项目。

不要手动删除 `C:\Program Files\dotnet` 或修改注册表；Windows 卸载程序会处理 SDK 的安装登记和它创建的环境配置。若电脑也安装了其他版本的 .NET，卸载时请只选择 `10.x SDK`。

## 可选：释放 NuGet 缓存空间

`dotnet` 可能把构建所需包缓存到 `%USERPROFILE%\.nuget\packages`。这个缓存由所有 .NET 项目共享，删除后将迫使其他项目下次构建时重新下载包；它不是 Agent Aura 专属数据。

若确认不需要保留缓存，可运行：

```powershell
dotnet nuget locals global-packages --clear
```

这一步是可选的。不要因为本原型而删除不属于自己的整个 `%USERPROFILE%\.nuget\packages` 目录。

## 不需要清理的项目

- **注册表**：原型没有读写注册表，不需要手动清理。
- **环境变量**：原型不会设置环境变量；若卸载 .NET SDK，请让 Windows 卸载程序管理 SDK 相关配置。
- **开机启动、服务、计划任务、Codex 配置**：当前原型不会创建或修改它们。
- **托盘图标**：程序退出后会自动消失；如果图标短暂残留，重启 Windows Explorer 或注销后会恢复，不必删除任何系统文件。

Windows 仍可能保留常规系统痕迹，例如下载文件的“来自 Internet”标记、Defender 扫描记录或异常时的事件查看器日志。这些由 Windows 管理，通常不需要也不建议为了本次验证手动清除。

## 清理完成检查

以下命令若没有输出，表示 Agent Aura 原型自己的持久化状态和构建输出都已清理：

```powershell
Get-ChildItem -LiteralPath "$env:LOCALAPPDATA\AgentAura" -Force -ErrorAction SilentlyContinue
Get-ChildItem -LiteralPath '.\src\AgentAura.Prototype\bin' -Force -ErrorAction SilentlyContinue
Get-ChildItem -LiteralPath '.\src\AgentAura.Prototype\obj' -Force -ErrorAction SilentlyContinue
```

如安装了 .NET SDK，另在“已安装的应用”中确认不再存在不需要的 **Microsoft .NET SDK 10.x** 即可。
