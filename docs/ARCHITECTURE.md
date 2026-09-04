# Architecture

GPT Review Picker is a local post-completion bridge between an executing Agent, a human evidence-selection step, and an independent Reviewer. Public semantics are defined in [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md).

## Components

```text
Executing Agent
  |
  | Producer Request 1.0
  v
Handoff Producer (GPTReviewPicker.exe --handoff-request)
  |-- validates identity, paths, priorities, and evidence
  |-- derives Agent Statement hash
  |-- writes Request / Manifest / Result metadata
  `-- delivers Manifest or failure Result
          |
          | current-user local IPC or primary-process startup
          v
Picker Workspace
  |-- one Quick Tray
  |-- Handoff/failure Tabs
  |-- human evidence selection
  `-- Clipboard / drag / local Bundle
          |
          v
Independent Reviewer
```

The same executable hosts both Producer CLI mode and the WinForms Picker. Only one Picker UI instance runs for the current Windows user. A transient Producer invocation can deliver to the existing process or start the primary Picker when the executable path is known.

## Contract boundaries

- Producer Request `1.0` is the Agent-authored input.
- Manifest `1.2` is the preferred conversation-aware Picker input; `1.0` and `1.1` remain compatible.
- Result v1 is the terminal machine-readable Producer receipt.
- `final_response` is the task-result Agent Statement. The later Handoff receipt is outside that hash boundary.
- Picker controls review-source selection and transport to the Reviewer; it does not establish business-task correctness.

## Identity and state

Manifest `1.2` maps `conversation_id` to one Tab and uses `handoff_id` for one review round. Same-conversation new-round replacement clears prior Manual files. Identical replay retains them. Failed replacement preserves the last-known-good round. Different conversations remain isolated.

Manifest `1.1` uses Handoff identity; Manifest `1.0` uses canonical Manifest path.

## Persistence

Conversation-aware metadata is bounded to three files per conversation:

```text
<project_root>/.gpt-review/conversations/<conversation_id>/
  request.json
  manifest.json
  result.json
```

Same-conversation generation and delivery are serialized. Individual files are atomically replaced where implemented; the three-file set is not one filesystem transaction. After normal Producer CLI completion, the terminal snapshot is coherent. Candidate/replay fences detect persisted identity, response, or hash mismatch. A failed replacement records failure separately when possible and leaves the valid slot intact.

Evidence is referenced in place. It is copied only when a user explicitly creates a Review Bundle or when a virtual Agent Statement must be materialized for output.

## Delivery

Producer uses a current-user local IPC channel to send `open_manifest` or `open_result` to an existing Picker. If no Picker process is available but the executable is known, Producer starts the primary Picker and attempts delivery.

The public contract freezes delivery outcomes, not internal pipe names, mutex names, retry intervals, UI controls, or temporary filenames.

## Non-components

The architecture contains no cloud upload, AI Reviewer, API server, network listener, repository watcher, folder watcher, recursive evidence collector, Codex UI/database reader, history database, or cross-device synchronization.
