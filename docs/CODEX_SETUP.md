# Agent Setup for GPT Review Picker

**Copy once. Send to your Agent. Then work normally.**

Copy the entire single prompt block below and send it once to the AI Agent you use for your work. The Agent should integrate these rules into the persistent, global, or project instruction mechanism appropriate for its environment when supported. If it cannot do that safely or automatically, it should tell you only the minimum exact setup step you need to perform.

You do **not** need to open, copy, download, or paste [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md) for normal setup. The Public Protocol is detailed reference documentation for integration, troubleshooting, compatibility, developers, and advanced Agent implementation.

Once your Agent has integrated this prompt persistently, you do **not** need to send it again for every task.

## Copy this one complete prompt

```text
Integrate the following GPT Review Picker instructions into the persistent instruction mechanism appropriate for your current Agent environment. Prefer persistent, global, or project instructions that apply to future work over requiring the user to repeat these rules in every task.

If you can safely update the appropriate instruction mechanism yourself, do so. If user action is required, tell the user only the minimum exact step needed. Do not claim that setup is persistent unless it actually is, and do not repeatedly ask the user to paste these instructions once persistent setup is complete.

Use GPT Review Picker only as a post-completion review handoff. Executor and Reviewer are separate roles: complete the substantive task and normal verification first; Picker does not execute the task or decide whether it succeeded.

Trigger a Review Handoff only when a substantive reviewable result exists (for example meaningful code, document, spreadsheet/data, image/design, analysis/report, build/release, or consequential configuration work), unless the user explicitly requests or suppresses review. Do not trigger by default for ordinary questions, casual discussion, tiny operational answers, status checks, simple navigation, read-only exploration, or trivial actions. Do not require GPT_REVIEW_HANDOFF.md.

Review-Handoff infrastructure/meta work—such as discussing or diagnosing Handoff behavior, editing the public protocol, Setup Prompt, or Handoff rules, or installing/updating integration instructions—is not by itself an automatic Handoff trigger. Do not create recursive Handoffs about Handoffs. If the user explicitly requests independent review of substantive Picker implementation changes, apply the normal reviewability rule.

Before handoff, freeze the user-facing task-result Agent Statement. Put that exact substantive statement in final_response; do not include the later transport receipt. Select the smallest evidence set that lets an independent reviewer judge the main claim and, when useful, diagnose failure. Do not dump the entire repository, bin/obj, caches, broad logs, full datasets, unrelated/private files, secrets, or redundant paths.

Keep Handoff preparation lightweight: reuse known task context; do not re-analyze solely for Handoff. Do not rescan the repository without need, redo business analysis, rerun already-adequate expensive tests, create duplicate summaries/review-only artifacts, or repeat Git operations. The Handoff should remain materially cheaper than the task it closes, governed by minimum sufficient evidence.

Author only Producer Request schema_version "1.0". Use the field items, never legacy evidence. The only priority values are exactly MUST, RECOMMENDED, and OPTIONAL. MUST means review cannot reliably judge the core claim without the source and normally has default_selected true; RECOMMENDED and OPTIONAL normally have default_selected false. A final-response-only Handoff is valid.

Set conversation_id from CODEX_THREAD_ID, falling back to CODEX_SESSION_ID. Never invent it from a name, path, time, or task. If neither exists, omit conversation_id and explicitly report non-conversation-aware compatibility mode. Use display_name only when the real conversation title is explicitly available; otherwise omit it. Use a fresh GUID-N handoff_id for every substantive new review round. Reuse the same ID only for one identical transport retry with the same canonical Request. Changed substantive result or evidence requires a new ID; never reuse an ID with changed canonical content. Preserve identifier spelling and do not vary only casing.

Resolve GPTReviewPicker.exe with bounded discovery: (1) valid GPT_REVIEW_PICKER_EXE, (2) %LOCALAPPDATA%\GPTReviewPicker\GPTReviewPicker.exe, (3) one exact current-user-accessible running-process lookup for GPTReviewPicker with an existing path and exact filename, (4) an explicit user-provided portable path. Otherwise report Picker unavailable and stop. Never recursively search drives or scan Downloads, Desktop, AppData, or arbitrary personal directories; never guess usernames, extraction folders, C:\GPTReviewPicker, or developer paths.

Before invocation, preflight that schema_version is exactly "1.0"; items and priorities are canonical; project_root is an existing absolute directory; every item has a non-empty path and Boolean default_selected; every MUST file exists; final_response is non-whitespace or at least one item exists; identities are legitimate and safe; Agent Statement and evidence are frozen; and the executable is resolved.

Treat .gpt-review\producer-request.json or another reusable Request path only as an input slot. For each substantive new round, completely rewrite its current Request; do not partially patch stale prior-round semantics. Before formal invocation, freeze the complete Request, final_response, items, handoff_id, and conversation identity. Once invocation begins, do not mutate that logical submission: freeze -> invoke -> validate/generate -> deliver -> Result.

Invoke the resolved executable once as: GPTReviewPicker.exe --handoff-request <request.json>. Wait for process exit, then read the terminal Result. Treat delivered/exit 0, blocked/exit 2, manifest_created_delivery_failed/exit 3, and unexpected failure/exit 4 according to docs/PUBLIC_PROTOCOL_V1.md. Do not treat internal manifest_created as terminal.

Once the terminal Result is delivered, that Review Round is complete: report the receipt and stop the Handoff workflow. Do not modify or resubmit the delivered round for wording, Markdown/spacing/path/timestamp polish, nonessential evidence or package metadata, or administrative Git changes. Only a genuinely new substantive review snapshot may later use a new handoff_id; cosmetic or administrative changes do not justify a new round.

Report Task Result and Review Handoff Result separately. Handoff blockage or delivery failure never rewrites a successfully completed core task. Correct deterministic Request errors once and rerun preflight. Fix missing MUST evidence honestly. If delivery failed after Manifest creation, preserve Manifest/Result and allow at most one identical retry after resolving Picker availability. Never loop indefinitely or fabricate Manifest, Result, hash, path, or delivery status.

If Handoff protocol or transport fails during an unrelated substantive task, report or diagnose that failure separately. Do not automatically modify successful business work or opportunistically patch Picker, Producer, protocol, or Setup rules; infrastructure repair requires an explicit user request or an infrastructure-maintenance task.

Use only the public product protocol. Do not depend on creator-specific private rules, private Handoff history, private paths, or project-specific conventions. Consult docs/PUBLIC_PROTOCOL_V1.md when full integration details are required.
```
