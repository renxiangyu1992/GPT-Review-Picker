using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GPTReviewPicker;

public sealed class HandoffRequest
{
    [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }
    [JsonPropertyName("handoff_id")] public string? HandoffId { get; set; }
    [JsonPropertyName("conversation_id")] public string? ConversationId { get; set; }
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("rename_conversation")] public bool RenameConversation { get; set; }
    [JsonPropertyName("project_name")] public string? ProjectName { get; set; }
    [JsonPropertyName("task_name")] public string? TaskName { get; set; }
    [JsonPropertyName("stage")] public string? Stage { get; set; }
    [JsonPropertyName("project_root")] public string? ProjectRoot { get; set; }
    [JsonPropertyName("generated_at")] public string? GeneratedAt { get; set; }
    [JsonPropertyName("final_response")] public string? FinalResponse { get; set; }
    [JsonPropertyName("canonical_response_sha256")] public string? CanonicalResponseSha256 { get; set; }
    [JsonPropertyName("items")] public List<HandoffRequestItem>? Items { get; set; }
    [JsonPropertyName("evidence")] public List<HandoffRequestItem>? LegacyEvidence { get; set; }
}

public static class HandoffRequestContract
{
    public const string SchemaVersion = "1.0";
}

public sealed class HandoffRequestItem
{
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("priority")] public string? Priority { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("default_selected")] public bool DefaultSelected { get; set; }
}

public sealed class HandoffProducerResult
{
    [JsonPropertyName("status")] public string Status { get; set; } = HandoffProducerStatuses.Blocked;
    [JsonPropertyName("handoff_id")] public string? HandoffId { get; set; }
    [JsonPropertyName("conversation_id")] public string? ConversationId { get; set; }
    [JsonPropertyName("manifest_path")] public string? ManifestPath { get; set; }
    [JsonPropertyName("request_path")] public string? RequestPath { get; set; }
    [JsonPropertyName("result_path")] public string? ResultPath { get; set; }
    [JsonPropertyName("final_response")] public string? FinalResponse { get; set; }
    [JsonPropertyName("canonical_response_sha256")] public string? CanonicalResponseSha256 { get; set; }
    [JsonPropertyName("picker_delivery")] public string? PickerDelivery { get; set; }
    [JsonPropertyName("replayed")] public bool Replayed { get; set; }
    [JsonPropertyName("warnings")] public List<string> Warnings { get; set; } = [];
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = [];
}

public static class HandoffProducerStatuses
{
    public const string ManifestCreated = "manifest_created";
    public const string Delivered = "delivered";
    public const string ManifestCreatedDeliveryFailed = "manifest_created_delivery_failed";
    public const string Blocked = "blocked";
}

public static class HandoffProducerExitCodes
{
    public const int Delivered = 0;
    public const int ValidationBlocked = 2;
    public const int DeliveryFailed = 3;
    public const int UnexpectedError = 4;
}

public static class PickerDeliveryModes
{
    public const string IpcExistingInstance = "ipc_existing_instance";
    public const string StartedPrimary = "started_primary";
    public const string Unavailable = "unavailable";
}

public sealed record PickerDeliveryOutcome(bool Delivered, string Mode, string? Error = null)
{
    public static PickerDeliveryOutcome ExistingInstance() => new(true, PickerDeliveryModes.IpcExistingInstance);
    public static PickerDeliveryOutcome StartedPrimary() => new(true, PickerDeliveryModes.StartedPrimary);
    public static PickerDeliveryOutcome Failed(string error) => new(false, PickerDeliveryModes.Unavailable, error);
}

public sealed record HandoffGeneration(HandoffProducerResult Result, bool CanDeliver);

public static class HandoffProducer
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly Regex SafeHandoffId = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant);

    public static HandoffGeneration Generate(string requestFilePath)
        => GenerateCore(CaptureRequest(requestFilePath), lockAlreadyHeld: false);

    internal static HandoffGeneration GenerateFromIntake(RequestIntake intake)
        => GenerateCore(intake, lockAlreadyHeld: false);

    internal static HandoffGeneration GenerateUnderDeliveryLock(string requestFilePath)
        => GenerateUnderDeliveryLock(CaptureRequest(requestFilePath));

    internal static HandoffGeneration GenerateUnderDeliveryLock(RequestIntake intake)
        => GenerateCore(intake, lockAlreadyHeld: true);

    private static HandoffGeneration GenerateCore(RequestIntake intake, bool lockAlreadyHeld)
    {
        var sourcePath = intake.SourcePath;
        if (intake.ReadError is not null)
        {
            var unreadable = NewBlockedResult(null, sourcePath, FallbackResultPath(sourcePath), intake.ReadError);
            WriteResultBestEffort(unreadable);
            return new HandoffGeneration(unreadable, false);
        }

        if (intake.ParseError is not null)
        {
            var invalidJson = NewBlockedResult(null, sourcePath, FallbackResultPath(sourcePath), intake.ParseError);
            WriteResultBestEffort(invalidJson);
            return new HandoffGeneration(invalidJson, false);
        }
        var request = intake.Request;
        if (request is null)
        {
            var empty = NewBlockedResult(null, sourcePath, FallbackResultPath(sourcePath), "Request is empty.");
            WriteResultBestEffort(empty);
            return new HandoffGeneration(empty, false);
        }

        request.HandoffId = request.HandoffId?.Trim();
        request.ConversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? null : request.ConversationId.Trim();
        request.CanonicalResponseSha256 = request.FinalResponse is null ? null : CanonicalResponseHash.Compute(request.FinalResponse);

        var errors = new List<string>();
        var warnings = new List<string>();
        if (intake.CompatibilityWarning is not null) warnings.Add(intake.CompatibilityWarning);
        ValidateRequestHeader(request, errors);
        var handoffId = request.HandoffId;
        var conversationId = request.ConversationId;
        var root = ValidateProjectRoot(request.ProjectRoot, errors);
        var outputDirectory = root is not null && IsSafeHandoffId(handoffId) && (conversationId is null || IsSafeHandoffId(conversationId))
            ? conversationId is null
                ? Path.Combine(root, ".gpt-review", "handoffs", handoffId!)
                : Path.Combine(root, ".gpt-review", "conversations", conversationId)
            : null;
        var resultPath = outputDirectory is null ? FallbackResultPath(sourcePath) : Path.Combine(outputDirectory, "result.json");
        var requestSnapshotPath = outputDirectory is null ? sourcePath : Path.Combine(outputDirectory, "request.json");
        var manifestPath = outputDirectory is null ? null : Path.Combine(outputDirectory, "manifest.json");

        if (outputDirectory is not null)
        {
            Directory.CreateDirectory(outputDirectory);
            Mutex? mutex = null;
            if (!lockAlreadyHeld)
            {
                mutex = CreateProducerMutex(conversationId is null ? $"handoff:{handoffId}" : $"conversation:{conversationId}");
                WaitForMutex(mutex);
            }
            try
            {
                var normalizedItems = NormalizeItems(request.Items, root!, errors, warnings);
                var existingRequest = InspectExistingRequest(requestSnapshotPath, request);
                if (existingRequest == ExistingRequestDisposition.Conflict)
                {
                    errors.Add($"HANDOFF_REUSE_WITH_CHANGED_CONTENT: handoff_id '{handoffId}' already exists with a different request.");
                }

                if (errors.Count > 0)
                {
                    var preserveExistingSlot = existingRequest != ExistingRequestDisposition.None;
                    var reportedRequestPath = preserveExistingSlot
                        ? sourcePath
                        : File.Exists(requestSnapshotPath) ? requestSnapshotPath : sourcePath;
                    var blockedResultPath = preserveExistingSlot ? FallbackResultPath(sourcePath) : resultPath;
                    var blocked = CreateResult(HandoffProducerStatuses.Blocked, handoffId, conversationId, manifestPath, reportedRequestPath, blockedResultPath, false, warnings, errors);
                    AtomicWriteJson(blockedResultPath, blocked);
                    return new HandoffGeneration(blocked, false);
                }

                var replayed = existingRequest == ExistingRequestDisposition.Replay;
                HandoffRequest frozenRequest = request;
                ReviewManifest frozenManifest;
                if (replayed)
                {
                    var persistedRequest = ReadJson<HandoffRequest>(requestSnapshotPath);
                    var persistedManifest = ReadJson<ReviewManifest>(manifestPath!);
                    if (persistedRequest is null || persistedManifest is null)
                    {
                        errors.Add("HANDOFF_CANONICAL_MISMATCH: persisted Request or Manifest is empty.");
                        frozenManifest = persistedManifest ?? new ReviewManifest();
                    }
                    else
                    {
                        frozenRequest = persistedRequest;
                        frozenManifest = persistedManifest;
                        errors.AddRange(ValidateCandidateSnapshot(frozenRequest, frozenManifest));
                    }
                }
                else
                {
                    var displayName = ResolveDisplayName(request, conversationId, existingRequest, manifestPath, warnings);
                    frozenManifest = new ReviewManifest {
                        SchemaVersion = string.IsNullOrWhiteSpace(conversationId) ? "1.1" : "1.2",
                        HandoffId = handoffId,
                        ConversationId = conversationId,
                        DisplayName = displayName,
                        RenameConversation = request.RenameConversation ? true : null,
                        ProjectName = request.ProjectName?.Trim(),
                        TaskName = request.TaskName?.Trim(),
                        Stage = request.Stage?.Trim(),
                        ProjectRoot = root,
                        GeneratedAt = string.IsNullOrWhiteSpace(request.GeneratedAt)
                            ? DateTimeOffset.Now.ToString("O")
                            : request.GeneratedAt.Trim(),
                        FinalResponse = request.FinalResponse,
                        CanonicalResponseSha256 = request.CanonicalResponseSha256,
                        Items = normalizedItems
                    };
                    errors.AddRange(ValidateCandidateSnapshot(frozenRequest, frozenManifest));
                }

                if (errors.Count > 0)
                {
                    var preserveExistingSlot = existingRequest != ExistingRequestDisposition.None;
                    var blockedResultPath = preserveExistingSlot ? FallbackResultPath(sourcePath) : resultPath;
                    var blocked = CreateResult(HandoffProducerStatuses.Blocked, handoffId, conversationId, manifestPath,
                        preserveExistingSlot ? sourcePath : requestSnapshotPath, blockedResultPath, false, warnings, errors);
                    blocked.FinalResponse = request.FinalResponse;
                    blocked.CanonicalResponseSha256 = request.CanonicalResponseSha256;
                    AtomicWriteJson(blockedResultPath, blocked);
                    return new HandoffGeneration(blocked, false);
                }

                if (!replayed)
                {
                    AtomicWriteJson(requestSnapshotPath, frozenRequest);
                    AtomicWriteJson(manifestPath!, frozenManifest);
                }
                _ = ManifestLoader.Load(manifestPath!);

                var generated = CreateResult(HandoffProducerStatuses.ManifestCreated, handoffId, conversationId, manifestPath, requestSnapshotPath, resultPath, replayed, warnings, []);
                generated.FinalResponse = frozenRequest.FinalResponse;
                generated.CanonicalResponseSha256 = frozenRequest.CanonicalResponseSha256;
                AtomicWriteJson(resultPath, generated);
                return new HandoffGeneration(generated, true);
            }
            finally
            {
                if (mutex is not null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }

        var invalid = CreateResult(HandoffProducerStatuses.Blocked, handoffId, conversationId, manifestPath, requestSnapshotPath, resultPath, false, warnings, errors);
        WriteResultBestEffort(invalid);
        return new HandoffGeneration(invalid, false);
    }

    public static void WriteResult(HandoffProducerResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ResultPath)) throw new InvalidOperationException("Result path is unavailable.");
        AtomicWriteJson(result.ResultPath, result);
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, WriteOptions);

    public static IReadOnlyList<string> ValidateCandidateSnapshot(HandoffRequest request, ReviewManifest manifest)
    {
        var errors = new List<string>();
        if (!string.Equals(request.ConversationId?.Trim(), manifest.ConversationId?.Trim(), StringComparison.Ordinal))
            errors.Add($"CONVERSATION_ID_MISMATCH: Request '{request.ConversationId ?? "(null)"}', Manifest '{manifest.ConversationId ?? "(null)"}'.");
        if (!string.Equals(request.HandoffId?.Trim(), manifest.HandoffId?.Trim(), StringComparison.Ordinal))
            errors.Add($"HANDOFF_ID_MISMATCH: Request '{request.HandoffId ?? "(null)"}', Manifest '{manifest.HandoffId ?? "(null)"}'.");
        if (!string.Equals(request.FinalResponse, manifest.FinalResponse, StringComparison.Ordinal))
            errors.Add($"HANDOFF_CANONICAL_MISMATCH: Request {ResponseFingerprint(request.FinalResponse)}, Manifest {ResponseFingerprint(manifest.FinalResponse)}.");

        var requestComputed = request.FinalResponse is null ? null : CanonicalResponseHash.Compute(request.FinalResponse);
        var manifestComputed = manifest.FinalResponse is null ? null : CanonicalResponseHash.Compute(manifest.FinalResponse);
        if (!string.Equals(requestComputed, request.CanonicalResponseSha256, StringComparison.Ordinal) ||
            !string.Equals(manifestComputed, manifest.CanonicalResponseSha256, StringComparison.Ordinal) ||
            !string.Equals(request.CanonicalResponseSha256, manifest.CanonicalResponseSha256, StringComparison.Ordinal))
            errors.Add($"CANONICAL_RESPONSE_HASH_MISMATCH: Request declared '{request.CanonicalResponseSha256 ?? "(null)"}', computed '{requestComputed ?? "(null)"}'; Manifest declared '{manifest.CanonicalResponseSha256 ?? "(null)"}', computed '{manifestComputed ?? "(null)"}'.");
        return errors;
    }

    private static void ValidateRequestHeader(HandoffRequest request, List<string> errors)
    {
        if (!string.Equals(request.SchemaVersion, HandoffRequestContract.SchemaVersion, StringComparison.Ordinal))
            errors.Add($"Unsupported request schema_version: {request.SchemaVersion ?? "(missing)"}. Expected {HandoffRequestContract.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(request.HandoffId)) errors.Add("handoff_id is required.");
        else if (!IsSafeHandoffId(request.HandoffId.Trim())) errors.Add("handoff_id must use 1-128 letters, digits, dots, underscores, or hyphens and start with a letter or digit.");
        if (!string.IsNullOrWhiteSpace(request.ConversationId) && !IsSafeHandoffId(request.ConversationId.Trim()))
            errors.Add("conversation_id must use 1-128 letters, digits, dots, underscores, or hyphens and start with a letter or digit.");
        if (request.RenameConversation && string.IsNullOrWhiteSpace(request.ConversationId))
            errors.Add("rename_conversation requires conversation_id.");
        if (request.RenameConversation && string.IsNullOrWhiteSpace(request.DisplayName))
            errors.Add("rename_conversation requires display_name.");
        if (FirstNonBlank(request.DisplayName, request.TaskName, request.Stage, request.ProjectName) is null)
            errors.Add("At least one of display_name, task_name, stage, or project_name is required.");
        if ((request.Items is null || request.Items.Count == 0) && string.IsNullOrWhiteSpace(request.FinalResponse))
            errors.Add("items must contain at least one review file unless final_response is provided.");
    }

    private static string? ValidateProjectRoot(string? rawRoot, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(rawRoot)) { errors.Add("project_root is required."); return null; }
        if (!Path.IsPathFullyQualified(rawRoot)) { errors.Add("project_root must be an absolute path."); return null; }
        try
        {
            var root = Path.GetFullPath(rawRoot);
            if (!Directory.Exists(root)) { errors.Add($"Project root not found: {root}"); return null; }
            return root;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errors.Add($"Invalid project_root: {ex.Message}");
            return null;
        }
    }

    private static List<ReviewManifestItem> NormalizeItems(
        IReadOnlyList<HandoffRequestItem>? rawItems,
        string root,
        List<string> errors,
        List<string> warnings)
    {
        if (rawItems is null) return [];
        var unique = new Dictionary<string, NormalizedItem>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rawItems.Count; index++)
        {
            var raw = rawItems[index];
            var itemLabel = $"items[{index}]";
            if (string.IsNullOrWhiteSpace(raw.Path)) { errors.Add($"{itemLabel}.path is required."); continue; }
            if (!Enum.TryParse<ReviewPriority>(raw.Priority, true, out var priority))
            {
                errors.Add($"{itemLabel}.priority is invalid: {raw.Priority ?? "(missing)"}.");
                continue;
            }

            string fullPath;
            try
            {
                fullPath = Path.IsPathFullyQualified(raw.Path)
                    ? Path.GetFullPath(raw.Path)
                    : Path.GetFullPath(Path.Combine(root, raw.Path));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                errors.Add($"{itemLabel}.path is invalid: {ex.Message}");
                continue;
            }

            var manifestPath = Path.IsPathFullyQualified(raw.Path)
                ? fullPath
                : Path.GetRelativePath(root, fullPath);
            var candidate = new NormalizedItem(
                fullPath,
                manifestPath,
                string.IsNullOrWhiteSpace(raw.Label) ? Path.GetFileName(fullPath) : raw.Label.Trim(),
                priority,
                raw.Reason?.Trim() ?? string.Empty,
                raw.DefaultSelected);
            if (unique.TryGetValue(fullPath, out var existing)) unique[fullPath] = MergeDuplicate(existing, candidate);
            else unique.Add(fullPath, candidate);
        }

        foreach (var item in unique.Values)
        {
            if (File.Exists(item.FullPath)) continue;
            var message = $"Missing {item.Priority} file: {item.FullPath}";
            if (item.Priority == ReviewPriority.MUST) errors.Add(message);
            else warnings.Add(message);
        }

        return unique.Values
            .OrderBy(item => item.Priority)
            .Select(item => new ReviewManifestItem {
                Label = item.Label,
                Path = item.ManifestPath,
                Priority = item.Priority.ToString(),
                Reason = item.Reason,
                DefaultSelected = item.DefaultSelected
            })
            .ToList();
    }

    private static NormalizedItem MergeDuplicate(NormalizedItem existing, NormalizedItem candidate)
    {
        if (candidate.Priority < existing.Priority)
        {
            return candidate with {
                Label = string.IsNullOrWhiteSpace(candidate.Label) ? existing.Label : candidate.Label,
                Reason = string.IsNullOrWhiteSpace(candidate.Reason) ? existing.Reason : candidate.Reason
            };
        }
        if (candidate.Priority == existing.Priority)
        {
            return existing with {
                Label = string.IsNullOrWhiteSpace(existing.Label) ? candidate.Label : existing.Label,
                Reason = string.IsNullOrWhiteSpace(existing.Reason) ? candidate.Reason : existing.Reason,
                DefaultSelected = existing.DefaultSelected || candidate.DefaultSelected
            };
        }
        return existing with {
            Label = string.IsNullOrWhiteSpace(existing.Label) ? candidate.Label : existing.Label,
            Reason = string.IsNullOrWhiteSpace(existing.Reason) ? candidate.Reason : existing.Reason
        };
    }

    private static ExistingRequestDisposition InspectExistingRequest(string snapshotPath, HandoffRequest incoming)
    {
        if (!File.Exists(snapshotPath)) return ExistingRequestDisposition.None;
        try
        {
            var existing = JsonSerializer.Deserialize<HandoffRequest>(File.ReadAllText(snapshotPath), ReadOptions);
            if (existing is null) return ExistingRequestDisposition.Conflict;
            if (string.Equals(CanonicalRequest(existing), CanonicalRequest(incoming), StringComparison.Ordinal))
                return ExistingRequestDisposition.Replay;
            var sameConversation = !string.IsNullOrWhiteSpace(incoming.ConversationId)
                && string.Equals(existing.ConversationId?.Trim(), incoming.ConversationId.Trim(), StringComparison.OrdinalIgnoreCase);
            var newHandoff = !string.Equals(existing.HandoffId?.Trim(), incoming.HandoffId?.Trim(), StringComparison.OrdinalIgnoreCase);
            return sameConversation && newHandoff
                ? ExistingRequestDisposition.ReplaceConversation
                : ExistingRequestDisposition.Conflict;
        }
        catch (Exception) { return ExistingRequestDisposition.Conflict; }
    }

    private static bool IsReusableManifest(string manifestPath, string handoffId)
    {
        if (!File.Exists(manifestPath)) return false;
        try
        {
            var loaded = ManifestLoader.Load(manifestPath);
            return loaded.IsHandoffSchema && string.Equals(loaded.Manifest.HandoffId?.Trim(), handoffId, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) { return false; }
    }

    private static string ResolveDisplayName(
        HandoffRequest request,
        string? conversationId,
        ExistingRequestDisposition existingRequest,
        string? manifestPath,
        List<string> warnings)
    {
        if (conversationId is null)
            return FirstNonBlank(request.DisplayName, request.TaskName, request.Stage, request.ProjectName)!;

        if (existingRequest == ExistingRequestDisposition.ReplaceConversation && !request.RenameConversation &&
            manifestPath is not null && File.Exists(manifestPath))
        {
            var existingName = ManifestLoader.Load(manifestPath).DisplayName;
            var requestedName = FirstNonBlank(request.DisplayName);
            if (requestedName is not null && !string.Equals(requestedName, existingName, StringComparison.Ordinal))
                warnings.Add("display_name was ignored because this conversation already has a stable tab title; set rename_conversation to true to rename it explicitly.");
            return existingName;
        }

        var explicitName = FirstNonBlank(request.DisplayName);
        if (explicitName is not null) return explicitName;

        var projectName = FirstNonBlank(request.ProjectName);
        var suffixLength = Math.Min(8, conversationId.Length);
        var stableSuffix = conversationId[..suffixLength];
        return projectName is null ? conversationId : $"{projectName} [{stableSuffix}]";
    }

    private static string CanonicalRequest(HandoffRequest request) => JsonSerializer.Serialize(request);

    private static T? ReadJson<T>(string path) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), ReadOptions); }
        catch (Exception) { return null; }
    }

    private static string ResponseFingerprint(string? response)
        => response is null
            ? "(null)"
            : $"length {response.Length}, sha256 {CanonicalResponseHash.Compute(response)}";

    private static HandoffProducerResult NewBlockedResult(string? handoffId, string requestPath, string resultPath, string error)
        => CreateResult(HandoffProducerStatuses.Blocked, handoffId, null, null, requestPath, resultPath, false, [], [error]);

    private static HandoffProducerResult CreateResult(
        string status,
        string? handoffId,
        string? conversationId,
        string? manifestPath,
        string requestPath,
        string resultPath,
        bool replayed,
        IEnumerable<string> warnings,
        IEnumerable<string> errors)
        => new() {
            Status = status,
            HandoffId = handoffId,
            ConversationId = conversationId,
            ManifestPath = manifestPath,
            RequestPath = requestPath,
            ResultPath = resultPath,
            Replayed = replayed,
            Warnings = warnings.ToList(),
            Errors = errors.ToList()
        };

    private static string CanonicalRequestPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Request path is required.", nameof(path));
        return Path.GetFullPath(path);
    }

    private static string FallbackResultPath(string requestPath)
    {
        var directory = Path.GetDirectoryName(requestPath);
        if (string.IsNullOrWhiteSpace(directory)) directory = Environment.CurrentDirectory;
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(requestPath)}.result.json");
    }

    private static bool IsSafeHandoffId(string? handoffId)
        => !string.IsNullOrWhiteSpace(handoffId) && SafeHandoffId.IsMatch(handoffId) && handoffId is not "." and not "..";

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static Mutex CreateProducerMutex(string identity)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToUpperInvariant())));
        return new Mutex(false, $@"Local\GPTReviewPicker.HandoffProducer.{hash}");
    }

    internal static Mutex? AcquireConversationDeliveryMutex(string requestFilePath)
        => AcquireConversationDeliveryMutex(CaptureRequest(requestFilePath));

    internal static Mutex? AcquireConversationDeliveryMutex(RequestIntake intake)
    {
        var conversationId = intake.Request?.ConversationId?.Trim();
        if (string.IsNullOrWhiteSpace(conversationId) || !IsSafeHandoffId(conversationId)) return null;
        var mutex = CreateProducerMutex($"conversation:{conversationId}");
        WaitForMutex(mutex);
        return mutex;
    }

    internal sealed class RequestIntake
    {
        public required string SourcePath { get; init; }
        public string? ReadError { get; init; }
        public string? ParseError { get; init; }
        public HandoffRequest? Request { get; init; }
        public string? CompatibilityWarning { get; init; }
    }

    internal static RequestIntake CaptureRequest(string requestFilePath)
    {
        var sourcePath = CanonicalRequestPath(requestFilePath);
        string rawJson;
        try { rawJson = File.ReadAllText(sourcePath); }
        catch (Exception ex) { return new RequestIntake { SourcePath = sourcePath, ReadError = $"Request could not be read: {ex.Message}" }; }
        try
        {
            var request = JsonSerializer.Deserialize<HandoffRequest>(rawJson, ReadOptions);
            var compatibilityWarning = NormalizeLegacyRequest(request);
            return new RequestIntake { SourcePath = sourcePath, Request = request, CompatibilityWarning = compatibilityWarning };
        }
        catch (JsonException ex)
        {
            return new RequestIntake { SourcePath = sourcePath, ParseError = $"Request JSON parse failed: {ex.Message}" };
        }
    }

    private static string? NormalizeLegacyRequest(HandoffRequest? request)
    {
        if (request is null || request.SchemaVersion is not null || request.Items is not null || request.LegacyEvidence is null) return null;
        request.SchemaVersion = HandoffRequestContract.SchemaVersion;
        request.Items = request.LegacyEvidence.Select(item => new HandoffRequestItem {
            Label = item.Label,
            Path = item.Path,
            Priority = item.Priority?.Trim().ToUpperInvariant() switch {
                "SHOULD" => "RECOMMENDED",
                "MAY" => "OPTIONAL",
                _ => item.Priority
            },
            Reason = item.Reason,
            DefaultSelected = item.DefaultSelected
        }).ToList();
        request.LegacyEvidence = null;
        return "LEGACY_REQUEST_NORMALIZED: schema-less evidence request was normalized to Producer Request 1.0.";
    }

    private static void WaitForMutex(Mutex mutex)
    {
        try { mutex.WaitOne(); }
        catch (AbandonedMutexException) { }
    }

    private static void WriteResultBestEffort(HandoffProducerResult result)
    {
        try { AtomicWriteJson(result.ResultPath!, result); }
        catch (Exception) { }
    }

    private static void AtomicWriteJson<T>(string path, T value) => AtomicWriteText(path, Serialize(value));

    private static void AtomicWriteText(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = $"{fullPath}.tmp.{Environment.ProcessId}.{Guid.NewGuid():N}";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private sealed record NormalizedItem(
        string FullPath,
        string ManifestPath,
        string Label,
        ReviewPriority Priority,
        string Reason,
        bool DefaultSelected);

    private enum ExistingRequestDisposition { None, Replay, ReplaceConversation, Conflict }
}

public static class PickerDeliveryService
{
    public static PickerDeliveryOutcome Deliver(string manifestPath, string executablePath)
        => DeliverArtifact(PickerIpcMessage.OpenManifest(manifestPath), executablePath, manifestPath, false);

    public static PickerDeliveryOutcome DeliverResult(string resultPath, string executablePath)
        => DeliverArtifact(PickerIpcMessage.OpenResult(resultPath), executablePath, resultPath, true);

    private static PickerDeliveryOutcome DeliverArtifact(PickerIpcMessage message, string executablePath, string artifactPath, bool isResult)
    {
        if (PickerIpcClient.Send(message, attempts: 2, retryDelayMilliseconds: 50))
            return PickerDeliveryOutcome.ExistingInstance();
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return PickerDeliveryOutcome.Failed($"Picker executable not found: {executablePath}");

        try
        {
            var startInfo = new ProcessStartInfo(executablePath) { UseShellExecute = false };
            if (isResult) startInfo.ArgumentList.Add("--handoff-result");
            startInfo.ArgumentList.Add(Path.GetFullPath(artifactPath));
            _ = Process.Start(startInfo) ?? throw new InvalidOperationException("Picker process did not start.");
            return PickerIpcClient.Send(message)
                ? PickerDeliveryOutcome.StartedPrimary()
                : PickerDeliveryOutcome.Failed("Picker started but its IPC endpoint could not be reached.");
        }
        catch (Exception ex)
        {
            return PickerDeliveryOutcome.Failed($"Picker delivery failed: {ex.Message}");
        }
    }
}

public static class HandoffProducerCommand
{
    internal static Action? IntakeCapturedHook { get; set; }

    public static int Run(string requestPath, Func<string, PickerDeliveryOutcome>? delivery = null, string? executablePath = null)
    {
        try
        {
            var intake = HandoffProducer.CaptureRequest(requestPath);
            IntakeCapturedHook?.Invoke();
            using var conversationLock = HandoffProducer.AcquireConversationDeliveryMutex(intake);
            var generation = conversationLock is null
                ? HandoffProducer.GenerateFromIntake(intake)
                : HandoffProducer.GenerateUnderDeliveryLock(intake);
            if (!generation.CanDeliver)
            {
                var blockedResult = generation.Result;
                var blockedDeliver = delivery ?? (result => PickerDeliveryService.DeliverResult(result, executablePath ?? Environment.ProcessPath ?? string.Empty));
                var blockedOutcome = blockedDeliver(blockedResult.ResultPath!);
                blockedResult.PickerDelivery = blockedOutcome.Mode;
                if (!blockedOutcome.Delivered) blockedResult.Warnings.Add(blockedOutcome.Error ?? "Picker failure delivery failed.");
                HandoffProducer.WriteResult(blockedResult);
                return HandoffProducerExitCodes.ValidationBlocked;
            }

            var result = generation.Result;
            var deliver = delivery ?? (manifest => PickerDeliveryService.Deliver(manifest, executablePath ?? Environment.ProcessPath ?? string.Empty));
            var outcome = deliver(result.ManifestPath!);
            result.PickerDelivery = outcome.Mode;
            if (outcome.Delivered)
            {
                result.Status = HandoffProducerStatuses.Delivered;
                HandoffProducer.WriteResult(result);
                return HandoffProducerExitCodes.Delivered;
            }

            result.Status = HandoffProducerStatuses.ManifestCreatedDeliveryFailed;
            result.Warnings.Add(outcome.Error ?? "Picker delivery failed.");
            HandoffProducer.WriteResult(result);
            return HandoffProducerExitCodes.DeliveryFailed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected Handoff Producer error: {ex.Message}");
            return HandoffProducerExitCodes.UnexpectedError;
        }
    }
}
