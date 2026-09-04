# Result v1 examples

These synthetic examples illustrate terminal Producer outcomes. Result v1 is part of [GPT Review Picker Public Protocol v1.0](PUBLIC_PROTOCOL_V1.md) and has no JSON `schema_version` field.

Paths use unmistakable placeholders. The hash placeholder represents a Producer-derived value; Agent authors do not calculate or predict it.

## Delivered

```json
{
  "status": "delivered",
  "handoff_id": "handoff-example-01",
  "conversation_id": "conversation-example-01",
  "manifest_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\manifest.json",
  "request_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\request.json",
  "result_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\result.json",
  "final_response": "The requested report is complete and ready for independent review.",
  "canonical_response_sha256": "<producer-derived-sha256>",
  "picker_delivery": "ipc_existing_instance",
  "replayed": false,
  "warnings": [],
  "errors": []
}
```

```text
Task Result: SUCCESS
Review Handoff: DELIVERED
Delivery: ipc_existing_instance
```

## Blocked

```json
{
  "status": "blocked",
  "handoff_id": "handoff-example-02",
  "conversation_id": "conversation-example-01",
  "manifest_path": "<target-manifest-path-not-created>",
  "request_path": "<input-request-path>",
  "result_path": "<fallback-result-path>",
  "picker_delivery": "ipc_existing_instance",
  "replayed": false,
  "warnings": [],
  "errors": [
    "items[0].priority is invalid: <invalid-value>."
  ]
}
```

`manifest_path` on a blocked Result can be absent or can name the intended target without implying that a Manifest exists. `picker_delivery: ipc_existing_instance` here means Picker received the non-reviewable failure Result; it does not mean a reviewable Manifest was delivered.

```text
Task Result: SUCCESS
Review Handoff: BLOCKED
Reason: Producer Request validation failed
```

## Manifest created, delivery failed

```json
{
  "status": "manifest_created_delivery_failed",
  "handoff_id": "handoff-example-03",
  "conversation_id": "conversation-example-01",
  "manifest_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\manifest.json",
  "request_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\request.json",
  "result_path": "<project-root>\\.gpt-review\\conversations\\conversation-example-01\\result.json",
  "final_response": "The requested report is complete and ready for independent review.",
  "canonical_response_sha256": "<producer-derived-sha256>",
  "picker_delivery": "unavailable",
  "replayed": false,
  "warnings": [
    "Picker delivery was not completed."
  ],
  "errors": []
}
```

```text
Task Result: SUCCESS
Review Handoff: FAILED
Reason: Picker unavailable
Manifest: <durable manifest path>
```

The final example preserves the successful business result. The Agent may perform at most one identical transport retry after resolving Picker availability.
