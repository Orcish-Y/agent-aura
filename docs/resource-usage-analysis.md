# Agent Aura 原型资源占用分析

> 本文保留早期 15 秒启动诊断。完成 Window Pin State、Agent Message Item 动画和框架依赖分发后的正式长时结果见 [变更后资源基线](resource-baseline.md)。

## 结论

当前约 170 MB 的磁盘与内存占用并非由 Agent Aura 的业务代码或样例数据造成，而是 .NET 10 WPF 原型的发布模式和运行时启动基线所致：

| 项目 | 实测结果 | 结论 |
| --- | ---: | --- |
| 仓库（不含 `.git`） | 176 MB | 几乎全部是发布产物。 |
| `src/AgentAura.Prototype/bin/Release/.../publish` | 172 MB | 自包含发布复制了 .NET Core 与 Windows Desktop/WPF 运行时。 |
| 调试输出目录 | 约 0.24 MB | 应用自身及其直接输出很小。 |
| 启动 15 秒后的进程工作集 | 212.3 MB | Windows 当前驻留在物理内存中的页。 |
| 启动 15 秒后的进程私有内存 | 151.7 MB | 更接近任务管理器中约 170 MB 的观察值。 |

## 磁盘占用

发布目录的 `AgentAura.Prototype.runtimeconfig.json` 记录了 `Microsoft.NETCore.App` 与 `Microsoft.WindowsDesktop.App` 的内置运行时。这表示它是自包含发布：目标机器不需要预先安装 .NET Desktop Runtime，但发布目录必须携带完整运行时。

其中包括 `System.Private.CoreLib`、`PresentationFramework`、`PresentationCore`、`System.Windows.Forms`、CLR/JIT/图形组件，以及约 18 MB 的多语言卫星资源。项目文件本身没有 `PackageReference`，也没有将这 172 MB 的运行时声明为业务依赖；该体积来自发布时选择的自包含部署方式。

用同一项目实测框架依赖发布：

```powershell
dotnet publish src/AgentAura.Prototype/AgentAura.Prototype.csproj `
  -c Release -r win-x64 --self-contained false -o publish
```

结果为 **0.22 MB、5 个文件**。此方案要求目标机器已安装匹配的 .NET 10 Windows Desktop Runtime。

## 内存占用

在未进行任何交互、只启动原型窗口的情况下，进程采样结果如下：

| 启动后时间 | 工作集 | 私有内存 | 句柄数 |
| ---: | ---: | ---: | ---: |
| 1 秒 | 78.0 MB | 41.1 MB | 388 |
| 5 秒 | 217.6 MB | 157.1 MB | 616 |
| 15 秒 | 212.3 MB | 151.7 MB | 613 |

内存在 WPF 延迟初始化后回落并保持稳定；这次采样中没有持续增长的泄漏信号。

该启动基线主要包含：

- .NET CLR、GC、JIT 与基础类库；
- WPF 的 XAML、文本/字体、渲染和透明窗口合成；
- 托盘功能显式使用的 Windows Forms `NotifyIcon`、菜单和计时器；
- Windows 为进程保留或映射的运行时代码和图形资源。

应用自身目前仅创建三个 `AgentItemSample` 样例项，没有大量数据、网络缓存或无限增长的 UI 集合，因此不能解释约 150 MB 的私有内存。

注意：工作集不等于进程永远独占的内存；系统有内存压力时可以回收其中可换出的页。任务管理器显示的数值也会随其列类型（工作集、专用工作集或提交大小）而不同。

## 取舍与建议

1. 若优先保证用户机器开箱即用，保留自包含发布；约 172 MB 是携带 .NET 10 WPF 运行时的代价。
2. 若优先减小安装包，使用框架依赖发布，并在安装器或先决条件检查中要求 .NET 10 Windows Desktop Runtime。
3. 单文件发布主要减少文件数量，不会显著降低运行时内存，也通常不会消除运行时本身的总磁盘成本。
4. 不应仅为减小体积而直接启用 WPF 裁剪；XAML 与反射路径需要完整的回归验证。
5. 若将来有明确的低内存目标，应先用 .NET 性能分析器拆分托管堆、模块映射和图形资源，再决定是否替换 Windows Forms 托盘实现或调整 UI 技术栈。

## 测量范围

本报告的结论仅覆盖原型启动后的前 15 秒。它足以区分启动基线与立即发生的泄漏；若出现长时间运行后内存持续上涨，应针对实际交互与事件流进行更长时间的性能跟踪。
