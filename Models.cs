using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GPTReviewPicker;

public enum ReviewPriority { MUST, RECOMMENDED, OPTIONAL }

public sealed class ReviewManifest
{
    [JsonPropertyName("schema_version")] public string? SchemaVersion { get; set; }
    [JsonPropertyName("stage")] public string? Stage { get; set; }
    [JsonPropertyName("handoff_id")] public string? HandoffId { get; set; }
    [JsonPropertyName("conversation_id")] public string? ConversationId { get; set; }
    [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
    [JsonPropertyName("rename_conversation")] public bool? RenameConversation { get; set; }
    [JsonPropertyName("project_name")] public string? ProjectName { get; set; }
    [JsonPropertyName("task_name")] public string? TaskName { get; set; }
    [JsonPropertyName("project_root")] public string? ProjectRoot { get; set; }
    [JsonPropertyName("generated_at")] public string? GeneratedAt { get; set; }
    [JsonPropertyName("final_response")] public string? FinalResponse { get; set; }
    [JsonPropertyName("canonical_response_sha256")] public string? CanonicalResponseSha256 { get; set; }
    [JsonPropertyName("items")] public List<ReviewManifestItem>? Items { get; set; }
}

public static class CanonicalResponseHash
{
    public static string Compute(string response)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(response)));
}

public sealed class ReviewManifestItem
{
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("priority")] public string? Priority { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("default_selected")] public bool DefaultSelected { get; set; }
}

public sealed class ReviewFileItem
{
    public required string Label { get; init; }
    public required string RelativePath { get; init; }
    public required string FullPath { get; init; }
    public required ReviewPriority Priority { get; init; }
    public required string Reason { get; init; }
    public required bool Exists { get; init; }
    public bool IsVirtual { get; init; }
    public string? VirtualContent { get; init; }
    public bool Selected { get; set; }
}

public sealed class LoadedReview
{
    public required string ManifestPath { get; init; }
    public required ReviewManifest Manifest { get; init; }
    public required IReadOnlyList<ReviewFileItem> Items { get; init; }

    public bool IsSchema11 => string.Equals(Manifest.SchemaVersion, "1.1", StringComparison.OrdinalIgnoreCase);
    public bool IsSchema12 => string.Equals(Manifest.SchemaVersion, "1.2", StringComparison.OrdinalIgnoreCase);
    public bool IsHandoffSchema => IsSchema11 || IsSchema12;
    public string SessionIdentity => IsSchema12
        ? $"conversation:{Manifest.ConversationId!.Trim()}"
        : IsSchema11
            ? $"handoff:{Manifest.HandoffId!.Trim()}"
            : $"manifest:{Path.GetFullPath(ManifestPath)}";
    public string DisplayName
    {
        get
        {
            if (IsHandoffSchema && !string.IsNullOrWhiteSpace(Manifest.DisplayName)) return Manifest.DisplayName.Trim();
            if (!string.IsNullOrWhiteSpace(Manifest.Stage)) return Manifest.Stage.Trim();
            var projectName = new DirectoryInfo(Manifest.ProjectRoot!).Name;
            return string.IsNullOrWhiteSpace(projectName) ? Path.GetFileName(ManifestPath) : projectName;
        }
    }
    public string ProjectName => !string.IsNullOrWhiteSpace(Manifest.ProjectName)
        ? Manifest.ProjectName.Trim()
        : new DirectoryInfo(Manifest.ProjectRoot!).Name;
    public string TaskName => Manifest.TaskName?.Trim() ?? string.Empty;
}

public sealed class FailedHandoff
{
    public required string ResultPath { get; init; }
    public required HandoffProducerResult Result { get; init; }
    public HandoffRequest? Request { get; init; }
    public string Identity => $"failure:{Result.ConversationId ?? "no-conversation"}:{Result.HandoffId ?? Path.GetFileName(ResultPath)}";
    public string DisplayName => !string.IsNullOrWhiteSpace(Request?.DisplayName)
        ? Request.DisplayName.Trim()
        : !string.IsNullOrWhiteSpace(Request?.ProjectName) ? Request.ProjectName.Trim() : "Handoff Failure";
    public string ProjectName => Request?.ProjectName?.Trim() ?? string.Empty;
    public string TaskName => Request?.TaskName?.Trim() ?? string.Empty;
    public string GeneratedAt => Request?.GeneratedAt?.Trim() ?? string.Empty;
    public string Reason => string.Join(Environment.NewLine, Result.Errors.Concat(Result.Warnings));
}

public static class FailedHandoffLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static FailedHandoff Load(string resultPath)
    {
        var fullPath = Path.GetFullPath(resultPath);
        HandoffProducerResult result;
        try { result = JsonSerializer.Deserialize<HandoffProducerResult>(File.ReadAllText(fullPath), Options) ?? throw new InvalidDataException("Result is empty."); }
        catch (JsonException ex) { throw new InvalidDataException($"Result JSON parse failed: {ex.Message}", ex); }
        if (result.Status is not HandoffProducerStatuses.Blocked and not HandoffProducerStatuses.ManifestCreatedDeliveryFailed)
            throw new InvalidDataException($"Result status is not a visible failure: {result.Status}.");
        HandoffRequest? request = null;
        if (!string.IsNullOrWhiteSpace(result.RequestPath) && File.Exists(result.RequestPath))
        {
            try { request = JsonSerializer.Deserialize<HandoffRequest>(File.ReadAllText(result.RequestPath), Options); }
            catch (JsonException) { }
        }
        return new FailedHandoff { ResultPath = fullPath, Result = result, Request = request };
    }
}

public static class ManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex SafeConversationId = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant);

    public static LoadedReview Load(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath)) throw new InvalidDataException("Manifest path is empty.");
        var fullManifestPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(fullManifestPath)) throw new FileNotFoundException("Manifest not found.", fullManifestPath);

        ReviewManifest? manifest;
        try { manifest = JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(fullManifestPath), JsonOptions); }
        catch (JsonException ex) { throw new InvalidDataException($"JSON parse failed: {ex.Message}", ex); }
        if (manifest is null) throw new InvalidDataException("Manifest is empty.");
        var is10 = string.Equals(manifest.SchemaVersion, "1.0", StringComparison.OrdinalIgnoreCase);
        var is11 = string.Equals(manifest.SchemaVersion, "1.1", StringComparison.OrdinalIgnoreCase);
        var is12 = string.Equals(manifest.SchemaVersion, "1.2", StringComparison.OrdinalIgnoreCase);
        if (!is10 && !is11 && !is12)
            throw new InvalidDataException($"Unsupported schema_version: {manifest.SchemaVersion ?? "(missing)"}");
        if ((is11 || is12) && string.IsNullOrWhiteSpace(manifest.HandoffId))
            throw new InvalidDataException($"handoff_id is required for schema {manifest.SchemaVersion}.");
        if (is12 && (string.IsNullOrWhiteSpace(manifest.ConversationId) || !SafeConversationId.IsMatch(manifest.ConversationId.Trim())))
            throw new InvalidDataException("conversation_id is required for schema 1.2 and must use 1-128 letters, digits, dots, underscores, or hyphens.");
        if (string.IsNullOrWhiteSpace(manifest.ProjectRoot)) throw new InvalidDataException("project_root is required.");
        if (!Path.IsPathFullyQualified(manifest.ProjectRoot)) throw new InvalidDataException("project_root must be an absolute path.");
        var root = Path.GetFullPath(manifest.ProjectRoot);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Project root not found: {root}");
        manifest.ProjectRoot = root;
        if (manifest.Items is null) throw new InvalidDataException("items is required.");
        if (manifest.CanonicalResponseSha256 is not null)
        {
            var computedHash = manifest.FinalResponse is null ? null : CanonicalResponseHash.Compute(manifest.FinalResponse);
            if (!string.Equals(manifest.CanonicalResponseSha256, computedHash, StringComparison.Ordinal))
                throw new InvalidDataException($"CANONICAL_RESPONSE_HASH_MISMATCH: manifest declared '{manifest.CanonicalResponseSha256}', computed '{computedHash ?? "(no final_response)"}'.");
        }

        var items = new List<ReviewFileItem>();
        if (!string.IsNullOrWhiteSpace(manifest.FinalResponse))
        {
            items.Add(new ReviewFileItem {
                Label = "Codex Final Response / Agent Statement",
                RelativePath = "CODEX_FINAL_RESPONSE.md",
                FullPath = string.Empty,
                Priority = ReviewPriority.MUST,
                Reason = "Exact canonical final text shown to the user; Agent Statement, not independent evidence.",
                Exists = true,
                IsVirtual = true,
                VirtualContent = manifest.FinalResponse,
                Selected = true
            });
        }
        foreach (var raw in manifest.Items)
        {
            if (string.IsNullOrWhiteSpace(raw.Path)) throw new InvalidDataException("Every item requires path.");
            if (!Enum.TryParse<ReviewPriority>(raw.Priority, true, out var priority))
                throw new InvalidDataException($"Unsupported priority: {raw.Priority ?? "(missing)"}");
            var relative = raw.Path!;
            var fullPath = Path.IsPathRooted(relative) ? Path.GetFullPath(relative) : Path.GetFullPath(Path.Combine(root, relative));
            items.Add(new ReviewFileItem {
                Label = string.IsNullOrWhiteSpace(raw.Label) ? Path.GetFileName(relative) : raw.Label!,
                RelativePath = Path.IsPathRooted(relative) ? fullPath : relative,
                FullPath = fullPath,
                Priority = priority,
                Reason = raw.Reason ?? string.Empty,
                Exists = File.Exists(fullPath),
                Selected = raw.DefaultSelected
            });
        }
        var sorted = items.OrderBy(i => i.Priority).ToList();
        return new LoadedReview { ManifestPath = fullManifestPath, Manifest = manifest, Items = sorted };
    }
}

public static class ReviewSelection
{
    public static IReadOnlyList<ReviewFileItem> ExistingSelected(IEnumerable<ReviewFileItem> items) => items.Where(i => i.Selected && i.Exists).ToList();
    public static int MissingSelected(IEnumerable<ReviewFileItem> items) => items.Count(i => i.Selected && !i.Exists);
    public static void SelectOnlyMust(IEnumerable<ReviewFileItem> items)
    { foreach (var item in items) item.Selected = item.Exists && item.Priority == ReviewPriority.MUST; }
    public static void SelectAllExisting(IEnumerable<ReviewFileItem> items)
    { foreach (var item in items) item.Selected = item.Exists; }
    public static void Clear(IEnumerable<ReviewFileItem> items)
    { foreach (var item in items) item.Selected = false; }
}
