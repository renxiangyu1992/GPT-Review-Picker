# Manifest schemas

GPT Review Picker Public Protocol v1.0 prefers Manifest `1.2` and continues accepting Manifest `1.0` and `1.1` for compatibility. See [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md) for normative semantics.

## Preferred Manifest 1.2

Conversation-aware public integrations normally submit Producer Request `1.0`; Producer generates Manifest `1.2`. External Agents should not normally hand-author it.

```json
{
  "schema_version": "1.2",
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
  "canonical_response_sha256": "<producer-derived-sha256>",
  "items": [
    {
      "label": "Report",
      "path": "report.md",
      "priority": "MUST",
      "reason": "Primary reviewable deliverable",
      "default_selected": true
    }
  ]
}
```

All example identity and path values are synthetic.

## Manifest 1.2 fields

| Field | Requirement | Meaning |
| --- | --- | --- |
| `schema_version` | Required | Exactly `"1.2"`. |
| `handoff_id` | Required | Current task review-round identity. |
| `conversation_id` | Required | Stable conversation/Tab identity. |
| `project_root` | Required | Existing absolute directory for relative item resolution. |
| `items` | Required | Evidence array, including when empty. |
| `display_name` | Optional | Stable conversation Tab title. |
| `rename_conversation` | Optional | Explicit title-replacement signal. |
| `project_name`, `task_name`, `stage` | Optional | Display/audit metadata. |
| `generated_at` | Optional | Generation timestamp. |
| `final_response` | Optional | Frozen task-result Agent Statement. |
| `canonical_response_sha256` | Optional for external Manifest; Producer-derived when applicable | Integrity value verified by Picker when present. |

Items require `path`, `priority`, and canonical Boolean `default_selected`; `label` and `reason` are recommended. Public priorities are exactly `MUST`, `RECOMMENDED`, and `OPTIONAL`.

## Resolution and selection

- Relative paths resolve against `project_root`; absolute paths, including intentional outside-project paths, are supported.
- Ordinary evidence types are unrestricted; the Manifest itself is JSON.
- Items sort `MUST`, `RECOMMENDED`, `OPTIONAL`, preserving source order within priority.
- Missing items remain visible but are excluded from Clipboard, drag, and Bundle output.
- `default_selected` initializes selection; human selection remains authoritative.
- A non-whitespace `final_response` appears as selected virtual `MUST` source `CODEX_FINAL_RESPONSE.md`.

## Identity behavior

Manifest 1.2 uses `conversation_id` as Tab identity and `handoff_id` as round identity. A new Handoff ID in the same conversation replaces the round and clears prior Manual files. Identical replay retains them. Failed replacement preserves the last-known-good round.

## Compatibility schemas

| Schema | Required identity | Tab identity | Public position |
| --- | --- | --- | --- |
| `1.0` | None | Canonical absolute Manifest path | Accepted legacy compatibility |
| `1.1` | `handoff_id` | Handoff ID | Accepted legacy Handoff compatibility |
| `1.2` | `conversation_id` + `handoff_id` | Conversation ID | Preferred public format |

Compatibility schemas retain their historical identity behavior. They are not removed by Public Protocol v1.0.
