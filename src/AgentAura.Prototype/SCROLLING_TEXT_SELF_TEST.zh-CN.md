# 长文本悬停滚动自测

这份文档验证 Agent Item 中溢出文本的悬停、截断与滚动行为。请在安装了 .NET 10 SDK 和 Windows Desktop Runtime 的 Windows 开发机上，从仓库根目录执行。

## 自动回归

```powershell
dotnet run --project tests/AgentAura.Prototype.UiTests/AgentAura.Prototype.UiTests.csproj
```

预期输出：

```text
PASS: Overflowing Agent Item text scrolls after a delay, pauses at both ends, and Reduced motion is absent from the prototype.
```

该测试验证同一个真实 `ScrollingText` 控件的状态切换：

1. 文本溢出但未直接悬停该行时，显示 `…`，且不位移。
2. 直接悬停该行后，隐藏省略号，并按完整文本的自然宽度布局。
3. 短暂延迟后开始往返滚动；到达右端后暂停，再反向滚动。
4. 鼠标离开该行后，停止位移并恢复 `…` 截断。

## 人工检查

先启动原型：

```powershell
dotnet run --project src/AgentAura.Prototype/AgentAura.Prototype.csproj
```

然后逐项检查：

1. 调整窗口宽度，确保至少两行文本发生溢出。
2. 鼠标只停在 Agent Item 内、但不直接停在任一溢出文本行时，确认所有溢出行以 `…` 结尾且没有滚动。
3. 将鼠标直接移至第一条溢出文本。确认只有这一行在短暂延迟后滚动，且能看到原先截断位置后的完整内容。
4. 将鼠标移到同一 Agent Item 的另一条溢出文本。确认前一行立即停止并恢复 `…`，只有新悬停的行滚动。
5. 鼠标移出该文本行。确认该行停止滚动并恢复 `…`；不显示工具提示。
6. 重复步骤 3–5，覆盖标题、项目/工作目录和详情行。

通过标准：未直接悬停的溢出行均保持 `…` 截断；每次只有鼠标所在的溢出文本行滚动；滚动能展示完整文本，并在两端停顿。

若失败，请保留完整控制台输出、Windows 版本和 .NET SDK 版本，并录制失败时的窗口画面。
