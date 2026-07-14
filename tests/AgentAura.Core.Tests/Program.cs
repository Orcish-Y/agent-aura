using AgentAura.Core.Items;
using AgentAura.Core.Settings;

var store = new AgentMessageItemStore(attentionPinSpan: AttentionPinSpan.ForUpdates(10));
var observedAt = new DateTimeOffset(2026, 7, 14, 9, 30, 0, TimeSpan.FromHours(8));

var result = store.Apply(new ThreadObserved(
    Epoch: 1,
    ThreadId: "thread-1",
    WorkingDirectory: @"C:\code\agent-aura",
    CodexThreadTitle: null,
    OccurredAt: observedAt));

Assert(result.Applied, "The first observation must create an Agent Message Item.");
Assert(!result.IsSignificantUpdate, "The first observation is not a Significant Update.");
var item = store.Items.Single();
Assert(item.ThreadId == "thread-1", "The stable Codex Thread ID must be item identity.");
Assert(item.State == AgentMessageItemState.Observed, "An observed Thread starts in observed state.");
Assert(item.DisplayTitle == "agent-aura · 2026-07-14 09:30", "A missing title uses the per-lifecycle fallback title.");

store.Apply(new TurnStarted(1, 2, "thread-1", "turn-1", observedAt.AddMinutes(1)));
store.Apply(new AttentionRequested(1, 3, "thread-1", "turn-1", "Approval required", observedAt.AddMinutes(2)));
Assert(store.Items.Single().RemainingAttentionPinUpdates == 10, "Entering Attention State resets the Attention Pin Span.");
store.Apply(new TurnStarted(1, 1, "thread-2", "turn-2", observedAt.AddMinutes(3)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").RemainingAttentionPinUpdates == 9, "A Significant Update from another Thread decrements an attention pin exactly once.");
store.Apply(new TurnCompleted(1, 4, "thread-1", "turn-1", TurnOutcomeState.Failed, "Authoritative failure", observedAt.AddMinutes(4)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").State == AgentMessageItemState.Failed, "A terminal event preserves its exact authoritative outcome.");
store.Apply(new TurnCompleted(1, 5, "thread-1", "turn-1", TurnOutcomeState.Completed, null, observedAt.AddMinutes(5)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").State == AgentMessageItemState.Failed, "Duplicate terminal results are rejected.");
store.Apply(new TransportDisconnected(1, 6, "thread-1", observedAt.AddMinutes(6)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1") is { State: AgentMessageItemState.ConnectionDisconnected, LatestTurnOutcome: TurnOutcomeState.Failed }, "Transport loss marks disconnection without inventing or discarding an outcome.");
store.Apply(new TurnStarted(2, 1, "thread-1", "turn-2", observedAt.AddMinutes(7)));
store.Apply(new TurnCompleted(1, 7, "thread-1", "turn-2", TurnOutcomeState.Completed, null, observedAt.AddMinutes(8)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").State == AgentMessageItemState.Running, "Late events from an older connection epoch are rejected.");
Assert(!store.Apply(new TurnCompleted(2, 2, "thread-1", "another-turn", TurnOutcomeState.Completed, null, observedAt.AddMinutes(8))).Applied, "A terminal event for a non-current Turn is rejected.");
store.SetThreadAlias("thread-1", "Saved alias");
store.Apply(new ThreadTitleChanged(2, 2, "thread-1", "Codex title", observedAt.AddMinutes(9)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").DisplayTitle == "Saved alias", "A Thread Alias takes precedence over a Codex Thread Title.");
Assert(store.Dismiss("thread-1"), "Dismiss removes a runtime item.");
store.Apply(new TurnCompleted(2, 3, "thread-1", "turn-2", TurnOutcomeState.Completed, null, observedAt.AddMinutes(10)));
Assert(store.Items.Single(item => item.ThreadId == "thread-1").DisplayTitle == "Saved alias", "A valid later event recreates a dismissed item without losing its alias.");
store.Clear();
store.Apply(new TurnStarted(2, 4, "thread-1", "turn-3", observedAt.AddMinutes(11)));
Assert(store.Items.Single().ThreadId == "thread-1", "Clear removes only runtime items; a later valid event recreates one.");

var settingsPath = Path.Combine(Path.GetTempPath(), $"agent-aura-{Guid.NewGuid():N}.json");
try
{
    var settings = new DurableSettings(AuraSettings.Default, new Dictionary<string, string> { ["thread-1"] = "Saved alias" }, new WslConnectionCredentials(new string('x', 32), "Ubuntu"));
    var settingsStore = new JsonSettingsStore(settingsPath);
    await settingsStore.SaveAsync(settings);
    var loaded = await settingsStore.LoadAsync();
    Assert(loaded.ThreadAliases["thread-1"] == "Saved alias" && loaded.WslConnection?.DefaultDistribution == "Ubuntu", "Validated durable settings persist atomically without runtime items.");
}
finally
{
    if (File.Exists(settingsPath)) File.Delete(settingsPath);
}

Console.WriteLine("PASS: first observations create an observed Agent Message Item.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
