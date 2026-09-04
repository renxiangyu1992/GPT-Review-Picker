# GPT Review Picker
# Public Protocol v1.0

GPT Review Picker by Xiangyu Ren separates task execution from independent review:

```text
Executor completes a task
  -> freezes a task-result Agent Statement
  -> selects minimal sufficient review evidence
  -> submits Producer Request 1.0
  -> Producer writes Manifest and Result
  -> Picker receives the Handoff
  -> human selects sources
  -> independent Reviewer reviews them
```

Picker is a post-completion review handoff tool. It is not the task executor, business-logic authority, final correctness judge, cloud upload service, repository watcher, or independent Reviewer.

This document is the normative public protocol for the first public release. It deliberately distinguishes canonical public authoring, compatibility behavior, and implementation detail.

## 1. Version Map

The version numbers identify different surfaces and do not need to match:

| Surface | Public v1 position |
| --- | --- |
| Application release | `0.1.0` |
| Public protocol edition | `1.0` |
| Producer Request schema | `1.0` |
| Preferred Manifest schema | `1.2` |
| Accepted Manifest compatibility schemas | `1.0`, `1.1` |
| Result contract | Result v1, documented here without a JSON `schema_version` field |

Readers should ignore unknown additive fields. Canonical writers should emit only documented fields. Changing a required field, identity rule, enum meaning, terminal status, or accepted schema requires an explicit version and compatibility decision.

## 2. Normative Public Workflow

For a normal conversation-aware Agent integration:

1. Complete the substantive task and its normal verification.
2. Decide whether a meaningful result exists for independent review.
3. Freeze the user-facing task-result statement. This becomes `final_response`.
4. Select the smallest sufficient evidence set.
5. Resolve the Picker executable using the bounded discovery strategy in section 12.
6. Write and preflight one Producer Request with `schema_version: "1.0"` and `items`.
7. Invoke `GPTReviewPicker.exe --handoff-request <request.json>` once and wait for process exit.
8. Read the terminal Result.
9. Report the Task Result and Review Handoff Result separately. The transport receipt is not part of `final_response`.

Agents should author Producer Requests. They should not normally hand-author Manifest 1.2 or Result files.

## 3. Producer Request 1.0

### 3.1 Canonical shape

```json
{
  "schema_version": "1.0",
  "handoff_id": "handoff-example-01",
  "conversation_id": "conversation-example-01",
  "display_name": "Example conversation",
  "rename_conversation": false,
  "project_name": "Example Project",
  "task_name": "Create reviewable report",
  "stage": "DOCUMENTATION",
  "project_root": "C:\\Example\\ReviewableProject",
  "generated_at": "2030-01-02T03:04:05Z",
  "final_response": "The requested report is complete and ready for independent review.",
  "items": [
    {
      "label": "Report",
      "path": "report.md",
      "priority": "MUST",
      "reason": "Primary deliverable",
      "default_selected": true
    },
    {
      "label": "Focused verification",
      "path": "verification.txt",
      "priority": "RECOMMENDED",
      "reason": "Concise verification evidence",
      "default_selected": false
    }
  ]
}
```

The path, names, timestamp, and identifiers above are synthetic.

### 3.2 Request fields

| Field | Status | Meaning and canonical writer behavior |
| --- | --- | --- |
| `schema_version` | Required | String exactly `"1.0"`. This is the Request schema, not a Manifest or application version. |
| `handoff_id` | Required | Opaque identity for one task Handoff/review round. Use a fresh GUID-N or equally safe opaque identifier for every substantive new round. |
| `conversation_id` | Required for conversation-aware Codex; compatibility-optional | Stable conversation identity. Source from `CODEX_THREAD_ID`, then `CODEX_SESSION_ID`. Never invent it from names, paths, timestamps, or task data. |
| `display_name` | Optional | Exact stable conversation title only if explicitly available. Do not substitute `task_name` or `stage`. |
| `rename_conversation` | Optional, default `false` | Set `true` only for an intentional title correction/rename; requires non-empty `conversation_id` and `display_name`. |
| `project_name` | Optional | Human-readable project name. Used in the first conversation title fallback when `display_name` is absent. |
| `task_name` | Optional, recommended | Human-readable name of the completed task. It is replaceable round metadata, not conversation identity. |
| `stage` | Optional | Human-readable stage or task code. It is not a conversation title. |
| `project_root` | Required | Existing absolute local directory against which relative item paths resolve. |
| `generated_at` | Optional | ISO 8601 when authored. Omit to let Producer supply the current local timestamp. |
| `final_response` | Optional | Frozen user-facing task-result Agent Statement. A non-whitespace value permits `items` to be empty. |
| `items` | Required canonical field | Evidence descriptor array. May be empty only with a non-whitespace `final_response`. New writers never use `evidence`. |

At least one of `display_name`, `task_name`, `stage`, or `project_name` must be non-empty. A normal public Agent integration should provide `project_name` and `task_name`.

### 3.3 Item fields

| Field | Status | Meaning and canonical writer behavior |
| --- | --- | --- |
| `path` | Required | Non-empty local file path, relative to `project_root` or absolute. |
| `priority` | Required | Exactly `MUST`, `RECOMMENDED`, or `OPTIONAL`. |
| `default_selected` | Required for canonical writers | Boolean. Normally `true` for `MUST` and `false` otherwise. |
| `label` | Optional, recommended | Short human-readable source name; filename is the fallback. |
| `reason` | Optional, recommended | Why the independent reviewer may need this source. |

Canonical relative paths should resolve inside `project_root`. Use an absolute path when intentionally referencing an outside-project file. Do not rely on relative `..` traversal as an authoring convention.

### 3.4 Header and identity validation

`handoff_id` and, when present, `conversation_id` must contain 1–128 ASCII letters, digits, dots, underscores, or hyphens, beginning with a letter or digit. Identifiers are opaque. Preserve exact original spelling and never intentionally create identities that differ only by casing.

If neither `CODEX_THREAD_ID` nor `CODEX_SESSION_ID` exists, do not invent a conversation identity. Omit `conversation_id` and explicitly report that the Handoff uses non-conversation-aware compatibility mode. Producer then emits Manifest 1.1 and uses `handoff_id` as the Picker Tab identity, so later tasks do not replace one stable conversation Tab.

For the first conversation-aware Handoff, omit `display_name` when the real title is unavailable. Producer uses `<project_name> [<first 8 conversation-id characters>]`, or the full `conversation_id` when `project_name` is absent. That fallback is display metadata, not a new identity. In non-conversation-aware compatibility mode, the first non-empty value among `display_name`, `task_name`, `stage`, and `project_name` supplies the display label.

### 3.5 Request validation and normalization

Producer applies these public behaviors:

- Missing or unsupported Request schema is blocked.
- Unsafe identity, invalid/missing project root, empty payload, missing item path, or invalid priority is blocked.
- Missing `MUST` evidence blocks Manifest generation and delivery.
- Missing `RECOMMENDED` or `OPTIONAL` evidence produces warnings and remains represented as Missing.
- Relative paths resolve against `project_root`; absolute paths are supported.
- Windows canonical paths are compared case-insensitively and duplicate evidence is collapsed.
- Duplicate priority conflict resolves as `MUST` over `RECOMMENDED` over `OPTIONAL`.
- The Request is data. It cannot declare commands, executable actions, scripts, or network endpoints.

The current implementation accepts priority spelling case-insensitively and defaults an omitted Boolean to `false`. Those are implementation tolerances, not canonical authoring. Public writers use the exact uppercase values and always emit `default_selected`.

## 4. Evidence Priority Vocabulary

The only public v1 authoring values are:

| Priority | Meaning | Missing-file behavior | Canonical default |
| --- | --- | --- | --- |
| `MUST` | Without this source, the independent reviewer cannot reliably judge the core task. | Blocks Manifest generation/delivery. | `default_selected: true` |
| `RECOMMENDED` | Helpful for deeper verification or diagnosis, but not essential to the core judgment. | Warning; visible as Missing. | `default_selected: false` |
| `OPTIONAL` | Auxiliary context such as broad logs, raw datasets, supplementary screenshots, or debug artifacts. | Warning; visible as Missing. | `default_selected: false` |

Priority expresses review importance, not access control. Picker sorts and initializes selection, but the human's final selection remains authoritative. A `MUST` source is not locked against human deselection.

Legacy priority aliases are documented only in section 16 and are not public v1 authoring choices.

## 5. Minimal Sufficient Review Evidence

The public rule is:

> Select the smallest set of sources that lets an independent reviewer judge the main claim and, when needed, diagnose a failure.

Use three passes:

1. What decision must the reviewer make?
2. Which sources are truly required to judge it?
3. Which small additional set would help diagnose uncertainty or failure?

Prefer the actual deliverable, directly governing specification, focused test/result evidence, and small diagnostic evidence. Do not attach the whole repository, `bin`/`obj`, dependency caches, full raw logs, entire datasets, unrelated screenshots, private data, secrets, or the same source through redundant paths by default.

A final-response-only Handoff is valid when the Agent Statement itself is the substantive reviewable result. Files are not mandatory when `final_response` is sufficient.

Handoff preparation is a lightweight deterministic close-out, not a second execution of the task. Reuse already-known task context. Do not solely for Handoff preparation rescan the whole repository without need, redo business analysis, rerun already-adequate expensive tests, create duplicate summaries or unnecessary review-only artifacts, or repeat Git operations. The Handoff should remain materially cheaper than the task it closes.

## 6. Canonical Final Response and Handoff Receipt

### 6.1 Agent Statement

`final_response` is the frozen user-facing **task-result statement** prepared before Producer invocation. It contains the substantive result of the completed task. It does not contain the later Review Handoff transport receipt.

Producer preserves the exact string in the Request snapshot, Manifest, and terminal Result. It computes SHA-256 over the exact UTF-8 bytes with no assumed newline or Unicode normalization. `canonical_response_sha256` is Producer-derived; Agent authors should omit it from Producer Requests and must not attempt to predict it.

Picker presents `final_response` as the selected virtual `MUST` source `CODEX_FINAL_RESPONSE.md`, labeled `Codex Final Response / Agent Statement`. It is materialized on demand for Clipboard, drag, or Bundle output and is not a permanent project file. It is an Agent Statement, not independent evidence. Agents must not create a duplicate physical `CODEX_FINAL_RESPONSE.md` merely to populate `items`.

If a Manifest declares `canonical_response_sha256`, Picker verifies it before replacing the current review round. A mismatch is rejected.

### 6.2 Transport receipt

After Producer exits, the Agent may report a separate receipt:

```text
Task Result: SUCCESS
Review Handoff: DELIVERED
Handoff ID: handoff-example-01
Result: <result path>
```

The receipt is not part of the hashed Agent Statement. A blocked or failed Review Handoff never retroactively changes the completed business task's result.

## 7. Manifest Contract

### 7.1 Version policy

Picker accepts Manifest `1.0`, `1.1`, and `1.2`. Manifest `1.2` is the preferred public v1 output for conversation-aware integrations. `1.0` and `1.1` remain compatibility formats and are not removed.

Normal public flow:

```text
Producer Request 1.0 with conversation_id
  -> Producer
  -> Manifest 1.2
```

### 7.2 Manifest 1.2 fields

| Field | Status | Meaning |
| --- | --- | --- |
| `schema_version` | Required | Exactly `"1.2"`. |
| `handoff_id` | Required | Current task review-round identity. |
| `conversation_id` | Required | Stable conversation/Tab identity. |
| `project_root` | Required | Existing absolute directory for relative evidence paths. |
| `items` | Required | Evidence descriptor array, including when empty. |
| `display_name` | Optional | Stable conversation Tab title. |
| `rename_conversation` | Optional | Explicit title-replacement signal. |
| `project_name` | Optional | Human-readable project metadata. |
| `task_name` | Optional | Current completed-task metadata. |
| `stage` | Optional | Stage/task-code metadata. |
| `generated_at` | Optional | Generation timestamp. |
| `final_response` | Optional | Exact task-result Agent Statement. |
| `canonical_response_sha256` | Optional for external Manifests; Producer-derived when applicable | Integrity value for `final_response`. |

Manifest items use the fields and priorities described in sections 3.3 and 4.

### 7.3 Path, order, missing, and selection behavior

- Relative paths resolve against `project_root`; absolute paths are supported, including outside-project files.
- Ordinary evidence file types are unrestricted. The Manifest itself must be JSON.
- Picker checks existence and orders items by `MUST`, `RECOMMENDED`, then `OPTIONAL`, preserving source order within a priority.
- Missing items remain visible but are excluded from Clipboard, drag, and Bundle output.
- `default_selected` controls initial selection. Priority does not force a checkbox to remain selected.
- `Only MUST` selects all existing `MUST` items and clears other Manifest selections.

External Agents should normally submit Producer Request 1.0 instead of manually authoring Manifest 1.2.

### 7.4 Compatibility Manifest identity

| Schema | Required identity | Picker Tab identity |
| --- | --- | --- |
| `1.0` | No Handoff identity field | Canonical absolute Manifest path |
| `1.1` | `handoff_id` | Handoff ID |
| `1.2` | `conversation_id` and `handoff_id` | Conversation ID |

## 8. Result v1

Result v1 is defined within Public Protocol v1.0 and deliberately has no JSON `schema_version` field.

### 8.1 Terminal statuses and process exits

| `status` | Meaning | Reviewable Manifest | Exit code |
| --- | --- | --- | --- |
| `delivered` | Manifest was generated and accepted by an existing or newly started Picker. | Yes | `0` |
| `blocked` | Intake, Request, evidence, identity, replay, or consistency validation blocked the Handoff. | No new reviewable Manifest | `2` |
| `manifest_created_delivery_failed` | Manifest was generated, but Picker delivery was not completed. | Yes, at `manifest_path`; not confirmed open | `3` |

Exit `4` means unexpected Producer failure. A Result file is not guaranteed if the error prevents safe persistence.

`manifest_created` is an internal persistence transition, not a terminal public Result. Integrations wait for process exit before interpreting Result.

### 8.2 Result fields

| Field | Status | Meaning |
| --- | --- | --- |
| `status` | Required terminal field | One of the three terminal statuses above. |
| `handoff_id` | Conditional | Recovered task Handoff identity. |
| `conversation_id` | Conditional | Recovered conversation identity. |
| `manifest_path` | Conditional | Generated or target Manifest path. On `blocked`, the path may be absent or may name a target that was not created. |
| `request_path` | Conditional | Frozen Request snapshot when one exists; otherwise the input Request path. |
| `result_path` | Required when Result persisted | Location of this Result. |
| `picker_delivery` | Conditional | `ipc_existing_instance`, `started_primary`, or `unavailable`. May be absent before transport was possible. |
| `replayed` | Required in normal persisted Results | Whether the same canonical Request/Handoff was replayed. |
| `warnings` | Required array in normal persisted Results | Non-blocking and transport diagnostics. |
| `errors` | Required array in normal persisted Results | Blocking validation/consistency diagnostics. |
| `final_response` | Conditional | Agent Statement when the generation stage persisted it. |
| `canonical_response_sha256` | Conditional | Producer-derived hash when a final response was persisted. |

### 8.3 Delivery values

- `ipc_existing_instance`: an already-running Picker accepted the artifact through current-user IPC.
- `started_primary`: Producer started the primary Picker and it accepted the artifact.
- `unavailable`: delivery was not completed.

Status, exit code, and `picker_delivery` are related but distinct. The terminal Result after process exit is authoritative.

### 8.4 Result locations and visible failures

Conversation-aware success uses the fixed Result path under `.gpt-review/conversations/<conversation_id>/result.json`. When invalid intake cannot safely determine that location, Producer writes `<request-name>.result.json` beside the input when possible. A failed replacement of an existing valid round also uses this fallback so it does not overwrite the last-known-good conversation slot.

When Picker is reachable, a `blocked` Result can be delivered through `open_result` as a de-duplicated, non-reviewable failure entry. A `manifest_created_delivery_failed` Result is also a valid failure artifact Picker can load, but automatic visibility is not guaranteed because transport itself failed.

The Result examples are in [RESULT_EXAMPLES.md](RESULT_EXAMPLES.md).

## 9. Conversation, Round, Replacement, and Replay

### 9.1 Identities

- `conversation_id` is stable conversation identity and owns one schema 1.2 Picker Tab.
- `handoff_id` identifies one task Handoff/review round.
- Names are display metadata and never identity sources.

### 9.2 State transitions

| Input | Public behavior |
| --- | --- |
| First `conversation_id` + `handoff_id` | Creates one conversation-aware round and Tab. |
| Same conversation, new Handoff ID | Replaces the current round in the same Tab. |
| Same conversation, same Handoff ID, same canonical Request | Idempotent replay; `replayed: true`. |
| Same Handoff ID, changed canonical Request | Conflicting reuse; blocked. |
| Invalid replacement | Preserves the last-known-good successful round. |
| Different conversation ID | Isolated persistence slot and Tab. |

On a valid new round, Manifest, terminal Result, task metadata, final response, and evidence are replaced. Manual review files from the prior Workspace round are cleared. An identical replay retains Manual files. A failed replacement never replaces the Workspace round and therefore preserves its Manual files.

`display_name` remains stable across ordinary same-conversation replacement. A different incoming name is ignored with a warning unless `rename_conversation: true` explicitly requests a correction.

### 9.3 Persistence and serialization

Conversation-aware storage is bounded to:

```text
<project_root>/.gpt-review/conversations/<conversation_id>/
  request.json
  manifest.json
  result.json
```

Same-conversation generation and delivery are serialized. Individual files are replaced atomically where implemented. The three files are not one filesystem transaction. After normal Producer CLI completion, the terminal snapshot is coherent. Candidate/replay fences detect mismatched persisted Request, Manifest, response, hash, or identity. Failed replacement preserves the last-known-good valid round and records failure separately when possible.

Evidence is referenced in place and is not copied into this metadata slot.

## 10. Public Handoff Trigger Policy

Do not hand off every Agent interaction. Trigger only after a substantive reviewable result exists, unless the user explicitly overrides the default.

Typical triggers include meaningful code changes, document creation/editing, spreadsheet or data work, image/design deliverables, formal analysis/reports, build/release changes, and configuration changes with substantive effect.

Do not trigger by default for ordinary questions, casual discussion, tiny operational answers, status checks, simple navigation, read-only exploration, or trivial actions with no meaningful review claim.

An explicit request to review triggers a Handoff; an explicit request not to hand off suppresses it. Public v1 does not require creating or updating `GPT_REVIEW_HANDOFF.md`.

Review-Handoff infrastructure/meta work is not itself an automatic trigger. Discussing or diagnosing a prior Handoff, editing this protocol or the Setup Prompt, changing Handoff rules, and installing/updating integration instructions must not recursively create another Handoff merely because they concern Picker. An explicit request for independent review of substantive Picker implementation changes remains subject to the normal reviewability rule.

## 11. Deterministic Preflight

Before Producer invocation, check:

- Request `schema_version` is exactly `"1.0"`.
- The descriptor field is `items`, never legacy `evidence`.
- Every priority is exactly `MUST`, `RECOMMENDED`, or `OPTIONAL`.
- `project_root` is an existing absolute directory.
- Every item has a non-empty path and explicit Boolean `default_selected`.
- Every `MUST` file exists now.
- A non-whitespace `final_response` or at least one item exists.
- `conversation_id`, when used, came from the supported environment identity order.
- Identifiers use safe characters, exact stable casing, and the correct new-round/replay identity.
- The task-result Agent Statement and evidence set are frozen for the attempt.
- Picker executable was resolved before making transport claims.

Preflight prevents avoidable blocked/failure entries. It does not replace Producer validation.

A reusable path such as `.gpt-review/producer-request.json` is an input slot, not permission to retain stale Request semantics. Every substantive new round completely rewrites the current Request fields and items before preflight; it must not partially patch an old Request in a way that carries prior-round data forward.

Each formal submission attempt is immutable. Once Producer invocation begins, do not change the Request, `final_response`, `items`, `handoff_id`, or conversation identity during that attempt. The lifecycle is `freeze -> invoke -> validate/generate -> deliver -> Result`; never allow a process to read Request A and then continue the same logical submission after the shared path has been mutated into Request B.

## 12. Bounded Picker Discovery

Resolve the executable in this order and stop at the first valid result:

1. `GPT_REVIEW_PICKER_EXE`, if defined and pointing to an existing file named `GPTReviewPicker.exe`.
2. `%LOCALAPPDATA%\GPTReviewPicker\GPTReviewPicker.exe`, if it exists.
3. One exact current-user-accessible running-process lookup for `GPTReviewPicker`; accept a unique valid executable path whose filename is `GPTReviewPicker.exe`.
4. An explicit portable executable path supplied by the user, after the same validation.
5. Otherwise declare Picker unavailable and stop discovery.

Do not recursively search drives or scan Downloads, Desktop, AppData, or other personal directories. Do not guess a username, `C:\GPTReviewPicker`, a developer checkout, or a ZIP extraction path.

Running-process discovery is an acceptable bounded fallback for an already-running portable Picker, not a durable installation record. If multiple plausible running paths remain, report ambiguity rather than guessing.

When a portable Picker is not running and no configured/explicit path exists, its arbitrary extraction directory cannot be inferred reliably. Ask the user to launch Picker once, provide the executable path, or configure `GPT_REVIEW_PICKER_EXE`.

The stable installed-mode location is `%LOCALAPPDATA%\GPTReviewPicker\GPTReviewPicker.exe`. Installation is a distribution concern, not a protocol operation.

## 13. Producer Invocation

Invoke the resolved executable with exactly one Request path and wait for process exit:

```powershell
& $pickerPath --handoff-request $requestPath
$producerExit = $LASTEXITCODE
```

If Picker is running, the transient Producer process delivers to it through current-user IPC. If Picker is not running but the EXE is known, Producer starts the primary Picker and delivers the artifact.

Always read the terminal Result rather than inferring delivery only from window state or process exit.

## 14. Retry and Error Recovery

Blind or open-ended retries are prohibited by public policy.

| Condition | Public recovery |
| --- | --- |
| Invalid schema, priority, or deterministic Request error | Correct once, rerun preflight, then submit. |
| Missing `MUST` | Fix the artifact/path or correct a genuinely mistaken evidence judgment. Do not downgrade merely to force success. |
| Missing lower-priority source | Delivery may continue with warning. Report it when material. |
| Picker not found before invocation | Stop bounded discovery. Ask the user to launch, configure, or provide Picker. Do not fabricate artifacts/status. |
| EXE known, Picker not running | Invoke normally; Producer may start the primary Picker. |
| IPC failure after Manifest creation | Preserve Manifest and Result, resolve availability, then permit at most one identical transport retry. |
| Same Handoff ID with changed content | Do not reuse it. A changed substantive result/evidence set is a new round and needs a new Handoff ID. |
| Unexpected failure/no Result | Report the Producer error and preserve the core-task conclusion. Do not loop. |

An identical retry uses the same `handoff_id`, the same canonical Request, the same Agent Statement, and the same evidence set. A substantive change requires a new Handoff ID.

Once the terminal Result is `delivered`, the current Review Round is complete. Report the delivery receipt and stop the Handoff workflow. Wording, Markdown, spacing, prettier paths, timestamp formatting, nonessential evidence, package/release metadata, Handoff-document polish, or administrative Git changes do not justify modifying or resubmitting the delivered round. Only a genuinely new substantive review snapshot may later start a new round with a new Handoff ID.

If Handoff protocol or transport fails during an unrelated substantive task, preserve and report the business result separately. Do not automatically modify successful business work or opportunistically patch Picker, Producer, this protocol, or Agent Setup rules. Infrastructure repair requires an explicit user request or a task whose scope is infrastructure maintenance.

When Git provenance is useful review evidence or metadata, reference a stable reviewed implementation commit. Do not require a tracked Handoff document to name the commit that contains that same document, and do not commit or amend solely to satisfy Handoff metadata; that creates a self-referential changing-HEAD loop.

Task and Handoff outcomes remain separate:

```text
Task Result: SUCCESS
Review Handoff: FAILED
Reason: Picker unavailable
```

## 15. Security and Privacy Contract

- Producer and Picker operate on local paths and do not upload evidence.
- IPC is local and restricted to the current Windows user by the implementation.
- Evidence is referenced in place until the user chooses Clipboard, drag, or Bundle output.
- The Agent must not attach secrets, credentials, unrelated personal data, or out-of-scope files.
- Bounded discovery must never become a broad personal-directory scan.
- `.gpt-review` contains task metadata, paths, Agent Statements, and identities; treat it as local task state and do not publish it by default.
- `CODEX_FINAL_RESPONSE.md` materializations and selected Bundles may contain sensitive task content and should be handled like the original evidence.

See [SECURITY_AND_PRIVACY.md](SECURITY_AND_PRIVACY.md) for user-facing guidance.

## 16. Compatibility Behavior

Compatibility exists to read historical data, not to teach new authoring.

### 16.1 Legacy Request normalization

Producer narrowly recognizes a historical Request only when it has no `schema_version`, has an `evidence` array, and has no `items` array. It normalizes that shape to Request 1.0, maps legacy `SHOULD` to `RECOMMENDED` and legacy `MAY` to `OPTIONAL`, and emits `LEGACY_REQUEST_NORMALIZED`.

New writers never emit the historical descriptor field or historical priority aliases. A schema-less Request using `items`, an unknown schema, malformed JSON, or other arbitrary shape remains blocked.

### 16.2 Manifest compatibility

Manifest 1.0 and 1.1 remain accepted with their historical identity rules. Compatibility acceptance does not make them preferred for new conversation-aware Agent integrations.

## 17. Implementation Detail Boundary

The following are intentionally not frozen as public protocol obligations:

- C# class/type names and serializer property order;
- mutex and pipe names;
- retry delay values and IPC polling counts;
- temporary filenames, write-through flags, and internal file-replacement mechanics;
- WinForms control structure, visual Tab implementation, and UI layout;
- virtual-source temporary directory names and Bundle naming;
- the internal `manifest_created` transition;
- exact PowerShell syntax used for a bounded process lookup.

Implementations may change these details while preserving the public behaviors in this document.

## 18. Public/Private Boundary

Public setup requires only this protocol, the concise [CODEX_SETUP.md](CODEX_SETUP.md), the executable, and the user's own task context. It does not require the creator's private AGENTS rules, private Handoff history, internal phase notes, project-specific stage codes, real conversation identities, real machine paths, or private development conventions.

The public product preserves the semantics of executor/reviewer separation, deterministic identity, minimal evidence, and independent Handoff outcome without exposing private workflow material.
