# 无 Hook 的 launcher 终端恢复：Windows 平台调查

## 结论

**Windows 没有把 Codex Thread ID、控制台客户端 PID 与可见终端 `HWND` 连接起来的通用 API。** 因此，单凭 Agent Aura 启动命令、`Process.MainWindowHandle`、`EnumWindows` 和进程/PID，不能可靠地从稳定 Thread ID 找回正确的可见终端窗口。它们只能在已经持有可信 `HWND` 的前提下，发现、核验和尽力激活该窗口。

无生命周期 Hook 的 MVP 应把「线程到窗口」视为**未绑定**，除非启动路径额外提供了一个可验证、排他的 host 级关联（例如启动器自己在 classic Console Host 内运行的协作 helper，在创建时报告 `HWND` 和 Thread ID）。这种 helper 是新的启动器/宿主契约，**不是** Windows 自动推导出的关联，且不适用于伪控制台。没有这个契约时，不能以标题、cwd、终端名、时间、`MainWindowHandle` 或进程树来猜测；点击后的确定回退是选中项目并提供「复制连接到该 Thread 的 resume 命令」，不启动或激活一个猜测的窗口。

## Win32 能做什么，以及不能做什么

- [`EnumWindows`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows) 枚举的是顶级桌面应用窗口（不含普通子窗口）；配合 [`GetWindowThreadProcessId`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid) 可以从一个 `HWND` 得到**创建该窗口**的 PID。这是「窗口 → owner PID」的核验路径，不是「任意子进程 PID → 它显示在哪个终端」的反向映射。
- [`.NET Process.MainWindowHandle`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.mainwindowhandle?view=net-9.0) 仅适用于本机进程，是缓存值（应先 `Refresh()`）；无 GUI/main window 或窗口隐藏时返回零。它描述进程的图形顶级窗口，不能把 CLI 客户端的 PID 绑定到 Console Host、Windows Terminal 标签页或编辑器内嵌 terminal。
- `HWND` 不能当永久身份：[`IsWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindow) 明确警告窗口可能在检查后销毁，且句柄会被回收并指向另一个窗口。因此即使未来有启动时捕获的 `HWND`，每次使用前仍须重新核对存在性、`GetWindowThreadProcessId` 的 owner PID、会话和创建时间；[`GetProcessTimes`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes) 提供 creation time，[`ProcessIdToSessionId`](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-processidtosessionid) 提供 RDS session ID（调用者须有查询权限）。

## 宿主边界

| 宿主 | 无 Hook 的可靠 Thread → 可见终端绑定 | 根据官方文档的原因 |
| --- | --- | --- |
| Classic Console Host | 否；仅在启动时另有同一控制台内的协作 helper 回报 `HWND` 时，才能作为候选并反复核验 | [`GetConsoleWindow`](https://learn.microsoft.com/en-us/windows/console/getconsolewindow) 只返回**调用进程所关联控制台**的窗口；它不接受另一个 PID，也没有 Thread ID 参数。[`GetConsoleProcessList`](https://learn.microsoft.com/en-us/windows/console/getconsoleprocesslist) 同样仅列出**当前控制台**的客户端 PID。|
| Windows Terminal | 不能取得通用 `HWND` 绑定；可选的 host 专用控制名并非安全恢复协议 | [`wt -w <window-id>`](https://learn.microsoft.com/en-us/windows/terminal/command-line-arguments) 可向指定整数/名称窗口发送命令，名称不存在时会**新建**该窗口；因此它不能安全地区分「已失效」和「应恢复」。文档的 `focus-tab` 使用随布局变化的 tab index。Windows Terminal 的 [`globalSummon`](https://learn.microsoft.com/en-us/windows/terminal/customize-settings/actions#global-summon) 能按名称召回窗口，但这是 Terminal 设置中的全局按键动作，名称不存在时同样会创建窗口；并无该页所述的外部 HWND/PID 查询或 Thread 绑定 API。|
| VS Code integrated terminal / 其他 ConPTY host | 否 | VS Code 官方文档说明 integrated terminal 是编辑器内的 terminal，且 Windows 使用 ConPTY 的 emulated pty backend。[`EnumWindows`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows) 不枚举普通子窗口，而 [`GetConsoleWindow`](https://learn.microsoft.com/en-us/windows/console/getconsolewindow) 对 pseudoconsole 只给消息队列用、不会本地显示的窗口；故这些 API 无法从 shell/Codex PID 找到正确的 editor terminal pane。[VS Code shell integration](https://code.visualstudio.com/docs/terminal/shell-integration) |

伪控制台是决定性边界：Microsoft 把它定义为由宿主负责显示输出和收集输入的模型，系统不创建传统 hosting window；[`GetConsoleWindow`](https://learn.microsoft.com/en-us/windows/console/getconsolewindow) 对 pseudoconsole 返回的窗口也只用于 message queue、不在本地显示。因此不应把它当作 Windows Terminal 或 VS Code 的可见窗口句柄。[Pseudoconsoles](https://learn.microsoft.com/en-us/windows/console/pseudoconsoles)

## 可见窗口已被可信捕获时的激活

1. 仅在用户点击后，对记录的候选执行重新核验；若无效、PID/creation time/session 不匹配、不可见或权限查询失败，即删除映射并走回退。`IsWindowVisible` 只说明 `WS_VISIBLE` 样式，窗口仍可能被遮挡，不能作为归属证明。[IsWindowVisible](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible)
2. 若窗口已最小化，可用 [`ShowWindow(SW_RESTORE)`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow) 恢复；再调用 [`SetForegroundWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow) 作为**尽力而为**。其返回零就是未前置；即使满足列出的桌面应用、前台锁、菜单和最近输入等资格条件，Windows 仍可拒绝，且文档明确禁止在用户正在使用别的窗口时强制抢前台。
3. 若已核验但前置被拒绝，可用 [`FlashWindowEx`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-flashwindowex) 提醒用户；它不会改变 active state。绝不可对未绑定窗口 flash。

## 权限与过期

- 运行于普通 medium integrity 的 Agent Aura 不能访问 elevated UI；跨越更高完整性需要满足 UIAccess 的签名、受保护安装位置与 manifest 条件，且 UIAccess 也不能访问 system IL。[UI Automation security](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview) 这是一般 UIPI/完整性边界；`SetForegroundWindow` 文档本身列的是前台锁与用户输入限制，不能把 UIPI 表述成该 API 的已证明专属失败理由。
- 不使用 `AllowSetForegroundWindow`、`AttachThreadInput`、合成输入、提升权限或 UIAccess 绕过用户的前台选择。特别是 [`AllowSetForegroundWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow) 只能由本已获准设置前台的进程调用，且该许可会在后续用户输入或另一授权后失效。
- 记录若存在的启动期绑定时，最少保存 Thread ID、`HWND`、owner PID、owner creation time、Windows session ID 和捕获时间；进程退出、核验失败、会话变化或 TTL 到期即失效。TTL 是产品策略，而非 Windows 所保证的生命周期信号。

## 对当前工单的决策含义

针对「managed App Server + 独立 `codex --remote` TUI」：若 App Server/CLI 协议没有提供 Thread 到 terminal-host 标识，Windows API 不能补齐这条缺失边。因此没有经启动期显式回报并实际验证的 host 契约时，所有新建、resume 和并发 Thread 都一律视为**不可准确激活**；使用安全回退，不猜测、不自动启动/恢复 Codex。
