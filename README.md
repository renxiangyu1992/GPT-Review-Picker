# GPT Review Picker

**GPT Review Picker by Xiangyu Ren** is a local Windows 10/11 utility for handing completed AI Agent work to an independent Reviewer. The executor finishes the task first, freezes its task-result statement, and selects a minimal evidence set. Picker lets a human choose which local sources enter the Review Tray.

```text
Agent task result
  -> Producer Request 1.0
  -> Manifest 1.2 + Result v1
  -> local current-user delivery
  -> Picker Handoff Tab
  -> human-selected Review Tray
  -> independent Reviewer
```

Picker is not the task executor, correctness judge, repository watcher, cloud upload service, or independent Reviewer.

## Start here

- [Public Protocol v1.0](docs/PUBLIC_PROTOCOL_V1.md) — authoritative public contract
- [Codex Setup](docs/CODEX_SETUP.md) — one compact, copyable setup prompt
- [Integration guide](docs/INTEGRATION.md) — operational flow and executable discovery
- [Producer Request schema](docs/PRODUCER_REQUEST_SCHEMA.md)
- [Manifest schema](docs/MANIFEST_SCHEMA.md)
- [Result examples](docs/RESULT_EXAMPLES.md)
- [Security and privacy](docs/SECURITY_AND_PRIVACY.md)
- [Synthetic samples](samples/README.md)

## Run

Open the standalone Quick Tray:

```powershell
GPTReviewPicker.exe
```

Open a Manifest directly:

```powershell
GPTReviewPicker.exe "C:\Example\ReviewableProject\manifest.json"
```

Generate and deliver a Handoff from a canonical Producer Request:

```powershell
GPTReviewPicker.exe --handoff-request "C:\Example\ReviewableProject\producer-request.json"
```

The canonical Agent workflow authors Request schema `1.0` using `items` and priorities `MUST`, `RECOMMENDED`, and `OPTIONAL`. A Request with `conversation_id` produces preferred Manifest schema `1.2`. Picker continues to accept Manifest `1.0` and `1.1` for compatibility.

## When to hand off

Hand off only after a substantive reviewable result exists, unless the user explicitly requests or suppresses review. Typical candidates include meaningful code changes, documents, spreadsheets/data work, image/design deliverables, formal reports, releases, and consequential configuration changes.

Do not hand off ordinary questions, casual discussion, status checks, read-only exploration, or trivial actions by default. Public use does not require a `GPT_REVIEW_HANDOFF.md` file. A final-response-only Handoff is valid when the Agent Statement itself is the reviewable result.

## Minimal sufficient evidence

Select the smallest set that lets an independent Reviewer judge the main claim and, when useful, diagnose failure:

- `MUST`: required for reliable core judgment; missing evidence blocks generation.
- `RECOMMENDED`: useful for deeper verification; missing evidence warns.
- `OPTIONAL`: auxiliary context such as broad logs or supplementary screenshots; missing evidence warns.

Do not attach an entire repository, build output, dependency caches, full raw logs, private data, secrets, or redundant paths by default. Priority expresses review importance; the human retains final selection control.

## Workspace behavior

`Quick Tray` is fixed and accepts Manual files without a Manifest. Each Handoff identity has its own selection, Manual files, Review Tray, status, and output state. New background Handoffs are marked unread without interrupting the active Tab.

For Manifest `1.2`, `conversation_id` owns one Tab and `handoff_id` identifies one task review round:

- same conversation plus a new Handoff ID replaces the current round and clears prior Manual files for that round;
- an identical replay reuses the round, reports `replayed: true`, and retains Manual files;
- conflicting reuse of a Handoff ID with changed canonical content is blocked;
- failed replacement preserves the last-known-good round and its Manual files;
- different conversation IDs remain isolated;
- the stable Tab title changes only through explicit `rename_conversation: true`.

Manifest `1.1` uses `handoff_id` as Tab identity. Manifest `1.0` uses the canonical Manifest path.

Closing a Handoff removes only in-memory Workspace state. It never deletes the Request, Manifest, Result, evidence, or source files.

## Agent Statement and receipt

`final_response` is the frozen, user-facing task-result Agent Statement prepared before Producer invocation. Producer hashes and preserves its exact UTF-8 content. Picker exposes it as a virtual, selected `MUST` source named `CODEX_FINAL_RESPONSE.md`; it is materialized only for output and is not independent evidence.

The later transport receipt is separate. A correct report can say:

```text
Task Result: SUCCESS
Review Handoff: FAILED
Reason: Picker unavailable
```

A blocked or failed Handoff never retroactively changes a successfully completed business task.

## Executable discovery

Agent integrations use a bounded lookup:

1. Valid `GPT_REVIEW_PICKER_EXE`.
2. `%LOCALAPPDATA%\GPTReviewPicker\GPTReviewPicker.exe`.
3. One exact lookup for an already-running `GPTReviewPicker` process and its valid executable path.
4. An explicit user-provided portable path.
5. Otherwise report Picker unavailable and stop.

Do not recursively scan drives or personal directories and do not guess an extraction, username, developer, or `C:\GPTReviewPicker` path. An arbitrary portable ZIP location cannot be inferred while Picker is not running.

The optional local integration helper installs a formal publish at the stable per-user path:

```powershell
.\Tools\Install-LocalIntegration.ps1 -SetUserEnvironmentVariable
```

Installation and release packaging are distribution operations, not protocol requirements.

## Local files and outputs

Producer validates local paths, collapses duplicates, references evidence in place, and never uploads it. Picker accepts ordinary local file types. Missing Manifest items remain visible but are excluded from Clipboard, drag, and Bundle output.

Every output action applies only to the active Tab:

- `Copy Review Tray` writes a Windows multi-file Clipboard FileDrop.
- Dragging one row sends one local file.
- `Drag all files to ChatGPT` sends the active tray in one FileDrop operation.
- `Open Review Bundle` copies selected sources into an isolated local bundle without modifying originals.

Folders are ignored without recursion. When the same path is selected from a Manifest and added manually, the Manifest source wins.

## Persistence and Result

Conversation-aware metadata uses a bounded slot:

```text
<project_root>\.gpt-review\conversations\<conversation_id>\
  request.json
  manifest.json
  result.json
```

Same-conversation generation/delivery is serialized. Individual files are atomically replaced where implemented; the three files are not one filesystem transaction. After normal Producer CLI completion, the terminal snapshot is coherent, and failed replacement preserves the last-known-good valid round.

Terminal Result statuses are `delivered` (exit `0`), `blocked` (exit `2`), and `manifest_created_delivery_failed` (exit `3`). Exit `4` is unexpected Producer failure. The internal `manifest_created` transition is not a terminal public outcome.

## Build and test

The project targets .NET 8 WinForms. Build and run the regression suite with:

```powershell
dotnet run --project .\Tests\GPTReviewPicker.Tests.csproj -c Debug
dotnet run --project .\Tests\GPTReviewPicker.Tests.csproj -c Release
```

Publish a self-contained single-file Windows x64 executable with:

```powershell
dotnet publish .\GPTReviewPicker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Limits

There is no directory recursion, content preview, AI/API upload, folder watcher, conversation watcher, Codex UI/database reader, history database, cloud sync, Git integration, full installer, or auto-update. Only one Picker window runs for the current Windows user, and local IPC is restricted to that user by the implementation.
