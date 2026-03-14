# LLM-Driven UI Testing Framework for Spire Client

## Context

The project has no automated testing. The goal is to enable LLM agents (Claude Code) to do TDD on the Godot game client by inspecting UI state as structured data — no screenshots or vision processing. The key insight from web research: **Playwright's accessibility tree pattern** (semantic tree of roles, labels, states) is the gold standard for LLM UI testing, and we can replicate it for Godot's scene tree.

## Architecture Decision

**Custom Autoload + file-based protocol.** Not GodotTestDriver (it's in-process only — the LLM runs outside Godot). Not HTTP server (heavyweight dependency, threading complexity with Godot's main thread). File-based is zero-dependency, trivially debuggable, and maps naturally to CLI tool workflows.

```
Godot (headless)                         LLM Agent (terminal)
┌─────────────────────┐                  ┌──────────────────┐
│ TestHarness (auto)  │  state.json ──►  │ reads state.json │
│  ├─ Serializer      │                  │ writes cmds.json │
│  └─ Executor        │  ◄── cmds.json   │ reads response   │
│     writes response │  response.json►  │                  │
└─────────────────────┘                  └──────────────────┘
```

## Phase 1: SceneTreeSerializer

**New file: `Engine/Test/SceneTreeSerializer.cs`**

Recursive `Node → JSON` using `System.Text.Json` (already in .NET 10, no new deps).

Output format — compact, LLM-friendly:
```json
{
  "timestamp": "2026-03-14T12:00:00Z",
  "tree": {
    "name": "Lobby", "type": "Control", "path": "Lobby", "visible": true,
    "children": [
      {
        "name": "AccountLayer", "type": "MarginContainer", "path": "Lobby/AccountLayer", "visible": true,
        "children": [
          { "name": "DevAccountInput", "type": "LineEdit", "path": "...", "text": "", "placeholder": "Dev Account", "editable": true },
          { "name": "ConnectButton", "type": "Button", "path": "...", "text": "CONNECT", "disabled": false }
        ]
      },
      {
        "name": "CharacterLayer", "type": "MarginContainer", "path": "...", "visible": false,
        "children": [
          { "name": "RaceSelect", "type": "OptionButton", "path": "...", "selected": 0, "items": ["Human", "Orc"] }
        ]
      }
    ]
  }
}
```

Design rules:
- **Type-specific extractors**: `Dictionary<string, Func<Node, Dictionary<string, object>>>` mapping Godot class names to property extractors. Extensible — add new node types by registering an extractor.
  - `Button` → `text`, `disabled`
  - `LineEdit` → `text`, `placeholder_text`, `editable`
  - `OptionButton` → `selected`, `items` (list of item texts)
  - `Label` → `text`
  - `Control` → `visible`
  - `TextureRect` → (just name/type/visible)
- **Every node gets a `path` field** (e.g. `"Lobby/CharacterLayer/CreateContainer/NameInput"`) so the LLM can address nodes in commands
- **Skip invisible subtrees** by default (configurable flag) — the LLM only cares about what a user would see
- Container nodes (VBox, HBox, Margin) are kept in the tree for hierarchy context but only emit `visible` + `children`

## Phase 2: CommandExecutor

**New file: `Engine/Test/CommandExecutor.cs`**

Reads parsed commands and executes actions on the Godot main thread.

Command types:
| Action | Params | Behavior |
|--------|--------|----------|
| `click` | `path` | Sets `ButtonPressed = true` on BaseButton (triggers Pressed signal) |
| `type` | `path`, `text` | Sets `LineEdit.Text`, emits `TextChanged` |
| `select` | `path`, `index` | Sets `OptionButton.Selected`, emits `ItemSelected` |
| `wait` | `path`, `condition`, `timeout_ms` | Polls per-frame until condition met (visible/hidden/text_equals/exists) |
| `snapshot` | — | Triggers immediate state.json write |
| `assert` | `path`, `property`, `expected` | Checks condition, returns pass/fail |

Commands file format:
```json
{ "commands": [
    {"action": "type", "path": "Lobby/.../DevAccountInput", "text": "test_001"},
    {"action": "click", "path": "Lobby/.../ConnectButton"},
    {"action": "wait", "path": "Lobby/CharacterLayer", "condition": "visible", "timeout_ms": 5000}
]}
```

Response file format:
```json
{ "results": [
    {"action": "type", "status": "ok"},
    {"action": "click", "status": "ok"},
    {"action": "wait", "status": "ok", "elapsed_ms": 230}
]}
```

## Phase 3: TestHarness Autoload

**New file: `Engine/Test/TestHarness.cs`**

```csharp
public partial class TestHarness : Node
```

- On `_Ready()`: check `OS.GetCmdlineArgs()` for `--test-harness`. If absent, `QueueFree()` (inert in normal runs).
- Configure harness dir: `user://test_harness/`
- `_Process()` every frame: check if `commands.json` exists → parse → execute via CommandExecutor → write `response.json` + `state.json` → delete `commands.json`
- Atomic writes: write to `.tmp` then rename to avoid partial reads
- Register in `project.godot` `[autoload]`: `TestHarness="*res://Test/TestHarness.cs"`

## Phase 4: Test Project (xUnit)

**New project: `Test/Test.csproj`** — console/xUnit project (follows Bot project pattern)

Key classes:
- **`TestHarnessClient.cs`** — manages Godot process lifecycle, writes commands, polls responses
  - `StartAsync()` — launches `godot --headless --test-harness`, waits for initial `state.json`
  - `SendCommandsAsync(commands)` — writes `commands.json`, polls for `response.json`
  - `GetStateAsync()` — reads latest `state.json`
  - `StopAsync()` — kills Godot process
- **`SceneState.cs`** — deserialized state with query methods: `FindNode(path)`, `FindByType(type)`, `FindByText(text)`

Example test:
```csharp
[Fact]
public async Task Login_ShowsCharacterLayer()
{
    await _harness.Type("Lobby/.../DevAccountInput", "test_001");
    await _harness.Click("Lobby/.../ConnectButton");
    await _harness.WaitFor("Lobby/CharacterLayer", visible: true, timeout: 5000);

    var state = await _harness.GetStateAsync();
    Assert.True(state.FindNode("Lobby/CharacterLayer")!.Visible);
    Assert.False(state.FindNode("Lobby/AccountLayer")!.Visible);
}
```

## Phase 5: LLM Agent Direct Usage (the TDD loop)

For Claude Code agents, the xUnit project is optional. The agent can work directly:

1. Run `godot --headless --test-harness` in background
2. Read `state.json` with Read tool → understand current UI
3. Write `commands.json` with Write tool → interact with UI
4. Read `response.json` → check results
5. Read updated `state.json` → verify new state

For formal TDD: agent writes xUnit test → `dotnet test` → red → implement → green.

## Implementation Order

1. `Engine/Test/SceneTreeSerializer.cs` — no dependencies, testable in isolation
2. `Engine/Test/CommandExecutor.cs` — depends on Godot node types
3. `Engine/Test/TestHarness.cs` — combines above, adds file I/O
4. `Engine/project.godot` — register TestHarness autoload
5. `Test/Test.csproj` + `TestHarnessClient.cs` + `SceneState.cs` — out-of-process client
6. `Test/Tests/LobbyTests.cs` — first real tests

## Critical Files to Modify/Reference

- `Engine/Core/Main.cs` — autoload pattern to follow
- `Engine/project.godot` — add TestHarness autoload registration
- `Engine/Lobby/Lobby.cs` — primary UI under test, has `[Export]` node bindings
- `Engine/Lobby/Lobby.tscn` — scene tree structure the serializer must handle
- `Engine/Lobby/CharacterSlot.cs` — dynamic child nodes to serialize
- `Engine/Engine.csproj` — may need System.Text.Json reference (likely already implicit)
- `Spire.slnx` — add Test project

## Verification Plan

1. **Unit**: Run Godot with `--headless --test-harness`, check that `state.json` appears with correct Lobby scene tree
2. **Command round-trip**: Write a `commands.json` with `{"commands": [{"action": "snapshot"}]}`, verify `response.json` appears with `"ok"`
3. **UI interaction**: Type into DevAccountInput via command, verify `state.json` shows updated text
4. **End-to-end**: Run `dotnet test Test/Test.csproj` with a lobby server running, verify login flow test passes
5. **LLM workflow**: Have Claude Code read `state.json`, reason about the UI, write commands, and verify state changes
