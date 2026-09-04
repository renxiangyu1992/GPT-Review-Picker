# Agent integration

This guide summarizes the operational path for GPT Review Picker Public Protocol v1.0. The normative contract is [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md); the one-time Codex prompt is [CODEX_SETUP.md](CODEX_SETUP.md).

## Integration flow

```text
complete and verify substantive task
  -> freeze task-result Agent Statement
  -> choose minimal sufficient evidence
  -> resolve GPTReviewPicker.exe
  -> preflight Producer Request 1.0
  -> run --handoff-request and wait
  -> read terminal Result v1
  -> report Task Result and Review Handoff separately
```

This is an explicit Producer push model. There is no repository/directory watcher, Codex database reader, cloud service, or automatic disk search.

## Trigger

Create a Handoff only after a substantive reviewable result exists, unless the user explicitly requests or suppresses review. Meaningful code, documents, data/spreadsheets, images/design, reports, releases, and consequential configuration are typical triggers. Ordinary questions, status checks, navigation, read-only exploration, and trivial actions are not default triggers.

Public integration does not require `GPT_REVIEW_HANDOFF.md`. A final-response-only Handoff is valid.

Review-Handoff meta work does not automatically recurse. Discussion/diagnosis of Handoffs, protocol or Setup edits, Handoff-rule changes, and integration-instruction maintenance need no new Handoff unless the user explicitly requests review of substantive Picker implementation changes.

## Prepare the Request

Canonical writers use Producer Request schema `1.0`, `items`, and only `MUST`, `RECOMMENDED`, or `OPTIONAL`. See [PRODUCER_REQUEST_SCHEMA.md](PRODUCER_REQUEST_SCHEMA.md) and the [synthetic Request](../samples/producer-request-v1.json).

`final_response` is the exact user-facing task-result Agent Statement frozen before transport. Do not include the later delivery receipt and do not supply `canonical_response_sha256`; Producer derives it.

For Codex, source `conversation_id` from `CODEX_THREAD_ID`, falling back to `CODEX_SESSION_ID`. If neither exists, omit it and report non-conversation-aware compatibility mode. Never invent identity or a conversation title. Each new substantive round gets a fresh GUID-N `handoff_id`; only an identical retry reuses it.

Keep preparation materially cheaper than the completed task: reuse known context and minimum sufficient evidence rather than rescanning, re-analyzing, rerunning adequate expensive tests, duplicating summaries, creating review-only artifacts, or repeating Git work solely for Handoff.

A reusable `.gpt-review/producer-request.json` is only an input slot. Completely rewrite the current Request for each substantive round so no stale fields survive. Freeze the complete Request, Agent Statement, items, and identities before invocation, and do not mutate that logical submission until its Result is terminal.

## Resolve the executable

Use this bounded order:

| Order | Locator | Acceptance |
| --- | --- | --- |
| 1 | `GPT_REVIEW_PICKER_EXE` | Existing file named `GPTReviewPicker.exe` |
| 2 | `%LOCALAPPDATA%\GPTReviewPicker\GPTReviewPicker.exe` | Existing file |
| 3 | One exact running-process lookup for `GPTReviewPicker` | Unique current-user-accessible existing executable path with exact filename |
| 4 | Explicit portable path from user | Existing file with exact filename |
| 5 | None | Report unavailable and stop |

Running-process lookup is a bounded fallback for portable mode while Picker is running. It is not an installation record. If multiple valid executable paths remain, report ambiguity rather than guessing.

An arbitrary portable extraction directory cannot be inferred when Picker is not running. Ask the user to launch Picker once, provide the path, or configure `GPT_REVIEW_PICKER_EXE`.

Never recursively search a drive or scan Downloads, Desktop, AppData, or other personal directories. Never guess a username, `C:\GPTReviewPicker`, a developer checkout, or ZIP extraction path.

## Preflight

Before invocation verify:

- Request schema is exactly `1.0`, the descriptor field is `items`, and every priority is one of the three exact uppercase values.
- `project_root` exists and is absolute.
- Every item has a path and Boolean `default_selected`; every `MUST` file exists.
- A non-whitespace `final_response` or at least one item exists.
- Conversation identity came from the supported environment source, if used.
- Handoff identity is safe and correct for new-round versus replay behavior.
- Agent Statement, evidence set, and executable path are frozen for the attempt.

## Invoke and interpret

```powershell
& $pickerPath --handoff-request $requestPath
$producerExit = $LASTEXITCODE
```

Wait for exit and then read the Result at its actual persisted path.

| Exit | Terminal status | Meaning |
| --- | --- | --- |
| `0` | `delivered` | Picker accepted the Manifest. |
| `2` | `blocked` | No new reviewable Manifest was produced. |
| `3` | `manifest_created_delivery_failed` | Manifest exists, but delivery was not completed. |
| `4` | — | Unexpected Producer failure; Result may be unavailable. |

`picker_delivery` is `ipc_existing_instance`, `started_primary`, or `unavailable`. The internal `manifest_created` transition is not terminal.

If Picker is already running, Producer sends the artifact through current-user IPC. If the EXE is known but Picker is not running, Producer starts the primary Picker and delivers the artifact.

`delivered` is terminal for the current round. Report it and stop Handoff processing. Cosmetic wording/format/path/timestamp changes, nonessential evidence, package metadata, or administrative Git updates do not justify resubmission. A later new round requires a genuinely substantive changed snapshot and a new Handoff ID.

## Identity and Workspace result

A conversation-aware Request produces Manifest 1.2. Same conversation plus a new Handoff ID replaces the current review round in one Tab and clears prior Manual files for that round. Identical replay reports `replayed: true` and retains Manual files. Conflicting Handoff ID reuse is blocked. Failed replacement preserves the last-known-good round. Different conversation IDs remain isolated.

## Recovery

- Correct a deterministic Request error once and rerun preflight.
- Fix missing `MUST` evidence honestly; do not downgrade it merely to force success.
- When Picker cannot be resolved before invocation, stop and ask the user to launch/configure/provide it.
- After delivery failure with a durable Manifest, resolve availability and permit at most one identical retry with the same Request and Handoff ID.
- Changed substantive result/evidence is a new round and requires a new Handoff ID.
- Never loop indefinitely or fabricate Result fields.

If Handoff infrastructure fails during an unrelated business task, report it separately without altering successful business work or opportunistically patching Picker, Producer, protocol, or Setup rules. Repair infrastructure only when the user requests it or the task itself is infrastructure maintenance.

## Git provenance

When useful, include the stable reviewed implementation commit as provenance. Do not require a tracked Handoff document to contain the hash of the commit that contains that same document, do not repeatedly rewrite “Final HEAD,” and do not create or amend a commit solely to satisfy a Handoff template.

Always keep outcomes separate:

```text
Task Result: SUCCESS
Review Handoff: FAILED
Reason: Picker unavailable
```
