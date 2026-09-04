# Security and privacy

GPT Review Picker handles paths and task material on the local Windows computer. The authoritative behavioral contract is [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md).

## Local processing

- Producer reads a local JSON Request and local evidence metadata.
- Picker/Producer do not upload evidence, call an AI model, or expose a network service.
- Delivery to an existing Picker uses implementation-restricted current-user local IPC.
- Evidence remains at its source path until the human chooses Clipboard, drag, or Bundle output.
- A Review Bundle is a deliberate local copy; it does not modify the source files.

The destination application chosen by the user may have its own upload or retention behavior. Picker cannot control that external application after Clipboard or drag transfer.

## Sensitive local state

`.gpt-review` can contain:

- conversation and Handoff identities;
- project and evidence paths;
- task metadata;
- the task-result Agent Statement;
- warnings, errors, and delivery status.

Treat this directory as local task state. Do not commit, publish, attach, or synchronize it by default. A materialized `CODEX_FINAL_RESPONSE.md` and a selected Review Bundle can contain the same sensitive content as the original task and deserve the same handling.

## Evidence minimization

Agents and users should select the smallest sufficient review set. Do not include secrets, credentials, tokens, private keys, unrelated personal data, entire repositories, dependency caches, broad logs, raw datasets, or screenshots containing unrelated applications/accounts unless the user intentionally puts them in review scope.

Absolute outside-project evidence is supported, but should be intentional and explained. The tool does not establish that a selected file is safe to disclose.

## Executable discovery

Bounded discovery protects user privacy and reduces accidental execution risk:

1. Validate configured `GPT_REVIEW_PICKER_EXE`.
2. Check the stable per-user installed path.
3. Perform at most one exact current-user-accessible lookup for an already-running `GPTReviewPicker` process.
4. Validate an explicit portable path supplied by the user.
5. Otherwise stop and report unavailable.

Every accepted candidate must exist and be named `GPTReviewPicker.exe`. Do not recursively search drives or scan Downloads, Desktop, AppData, or arbitrary personal directories. Do not guess usernames, extraction paths, or developer paths. If process lookup returns multiple plausible executable locations, report ambiguity.

Executable signing and release checksum verification are distribution concerns. Users should obtain Picker from the official release location and verify published integrity information when available.

## Request safety

Producer Request is data and cannot specify a shell command, script, executable action, network endpoint, or arbitrary IPC target. It can reference local files, including outside the project when an absolute path is intentionally supplied.

Missing `MUST` evidence blocks a reviewable Manifest. Missing lower-priority evidence is visibly warned and excluded from output when absent.

## Identity privacy

Conversation and Handoff IDs are not authentication secrets, but they are operational metadata. Public examples use synthetic identities. Do not publish real Request, Manifest, Result, Agent Statement, path, or screenshot data merely because it contains no password.

If Codex does not expose `CODEX_THREAD_ID` or `CODEX_SESSION_ID`, omit `conversation_id`. Never derive a false identity from user/project names, paths, timestamps, or conversation content.

## Failure safety

A failed or blocked Handoff cannot redefine the business task's outcome. Preserve the successful task result, report transport/review failure separately, and avoid blind retry loops. Never fabricate delivery status, paths, Result, Manifest, or hashes.
