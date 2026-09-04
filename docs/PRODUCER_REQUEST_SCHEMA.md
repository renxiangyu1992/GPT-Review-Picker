# Producer Request schema 1.0

Producer Request `1.0` is the only canonical public Agent authoring format for GPT Review Picker Public Protocol v1.0. The complete normative contract is [PUBLIC_PROTOCOL_V1.md](PUBLIC_PROTOCOL_V1.md).

## Canonical authoring

New writers emit:

- top-level `schema_version: "1.0"`;
- the evidence descriptor array `items`;
- only `MUST`, `RECOMMENDED`, and `OPTIONAL` priorities;
- explicit Boolean `default_selected` for every item.

See [`samples/producer-request-v1.json`](../samples/producer-request-v1.json) for a sanitized example.

## Fields

| Field | Requirement | Canonical rule |
| --- | --- | --- |
| `schema_version` | Required | Exactly `"1.0"`. |
| `handoff_id` | Required | Fresh safe opaque identity for each substantive round; GUID-N recommended. |
| `conversation_id` | Required for conversation-aware Codex; otherwise optional | Use `CODEX_THREAD_ID`, then `CODEX_SESSION_ID`; never invent. |
| `display_name` | Optional | Real explicitly available conversation title only. |
| `rename_conversation` | Optional | Default `false`; `true` requires conversation identity and title. |
| `project_name` | Optional | Human-readable project metadata. |
| `task_name` | Optional, recommended | Current completed task metadata. |
| `stage` | Optional | Stage/task-code metadata, not conversation title. |
| `project_root` | Required | Existing absolute directory. |
| `generated_at` | Optional | ISO 8601 if authored; Producer supplies when omitted. |
| `final_response` | Optional | Frozen task-result Agent Statement, excluding transport receipt. |
| `items` | Required canonical field | May be empty only when `final_response` is non-whitespace. |

At least one of `display_name`, `task_name`, `stage`, or `project_name` must be non-empty.

## Item fields

| Field | Requirement | Rule |
| --- | --- | --- |
| `path` | Required | Relative to `project_root` or absolute. |
| `priority` | Required | Exact uppercase public value. |
| `default_selected` | Required for canonical writers | Normally `true` for `MUST`, `false` otherwise. |
| `label` | Recommended | Filename is fallback. |
| `reason` | Recommended | Short review relevance statement. |

Missing `MUST` blocks. Missing lower-priority items warn. Duplicate Windows paths are collapsed case-insensitively and the strongest priority wins.

## Identity fallback

If neither supported Codex environment identity exists, omit `conversation_id` rather than inventing it. Producer emits Manifest 1.1 compatibility mode, keyed by `handoff_id`, and the Agent reports that conversation-aware replacement is unavailable.

Identifiers use 1–128 letters, digits, dots, underscores, or hyphens, beginning with a letter or digit. Preserve exact casing.

When a first conversation-aware Request omits an unavailable `display_name`, Producer uses `<project_name> [<first 8 conversation-id characters>]`, or the full conversation ID when no project name exists. Later ordinary replacements retain the stored title unless `rename_conversation: true` explicitly requests a correction.

## Agent Statement

Agent authors provide `final_response` but omit `canonical_response_sha256`. Producer derives SHA-256 from the exact UTF-8 string, with no assumed newline or Unicode normalization. The subsequent delivery receipt is separate.

## Legacy compatibility

The implementation narrowly normalizes a historical schema-less Request only when it contains an `evidence` array and no `items`. Legacy `SHOULD` maps to `RECOMMENDED`, legacy `MAY` maps to `OPTIONAL`, and `LEGACY_REQUEST_NORMALIZED` is emitted.

That intake path is compatibility behavior only. New examples and integrations never use the historical array, historical priorities, a missing schema, or an inferred version. Schema-less `items`, unknown schemas, and malformed JSON are blocked.
