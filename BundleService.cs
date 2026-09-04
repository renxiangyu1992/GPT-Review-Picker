using System.Security.Cryptography;
using System.Text;

namespace GPTReviewPicker;

public static class BundleService
{
    public static string CreateSelectedBundle(string manifestPath, IEnumerable<ReviewFileItem> items)
        => CreateReviewBundle(manifestPath, ReviewSelection.ExistingSelected(items).Select(FileDropService.Materialize));

    public static string CreateSelectedBundle(string manifestPath, IEnumerable<ReviewTrayItem> items)
        => CreateReviewBundle(manifestPath, items.Select(FileDropService.Materialize));

    public static string CreateReviewBundle(string manifestPath, IEnumerable<string> paths)
        => CreateReviewBundleInDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!, paths);

    public static string CreateReviewBundleInDirectory(string baseDirectory, IEnumerable<string> paths)
    {
        var manifestDirectory = new DirectoryInfo(Path.GetFullPath(baseDirectory));
        if (!manifestDirectory.Exists) manifestDirectory.Create();
        var gptReview = FindGptReviewDirectory(manifestDirectory) ?? Directory.CreateDirectory(Path.Combine(manifestDirectory.FullName, ".gpt-review"));
        var selectedDirectory = Path.Combine(gptReview.FullName, "selected");
        if (Directory.Exists(selectedDirectory)) Directory.Delete(selectedDirectory, true);
        Directory.CreateDirectory(selectedDirectory);

        CopyPaths(selectedDirectory, paths);
        return selectedDirectory;
    }

    public static string CreateHandoffBundle(LoadedReview loaded, IEnumerable<string> paths)
    {
        var gptReview = Directory.CreateDirectory(Path.Combine(loaded.Manifest.ProjectRoot!, ".gpt-review"));
        var selectedRoot = Directory.CreateDirectory(Path.Combine(gptReview.FullName, "selected"));
        var selectedDirectory = Path.Combine(selectedRoot.FullName, SessionDirectoryName(loaded));
        if (Directory.Exists(selectedDirectory)) Directory.Delete(selectedDirectory, true);
        Directory.CreateDirectory(selectedDirectory);
        CopyPaths(selectedDirectory, paths);
        return selectedDirectory;
    }

    public static string CreateHandoffBundle(LoadedReview loaded, IEnumerable<ReviewTrayItem> items)
        => CreateHandoffBundle(loaded, items.Select(FileDropService.Materialize));

    private static DirectoryInfo? FindGptReviewDirectory(DirectoryInfo directory)
    {
        for (var current = directory; current is not null; current = current.Parent)
            if (string.Equals(current.Name, ".gpt-review", StringComparison.OrdinalIgnoreCase)) return current;
        return null;
    }

    private static string UniqueName(string original, HashSet<string> used)
    {
        if (used.Add(original)) return original;
        var stem = Path.GetFileNameWithoutExtension(original);
        var ext = Path.GetExtension(original);
        for (var n = 2; ; n++) { var candidate = $"{stem} ({n}){ext}"; if (used.Add(candidate)) return candidate; }
    }

    private static void CopyPaths(string destinationDirectory, IEnumerable<string> paths)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourcePath in paths.Select(Path.GetFullPath).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var destinationName = UniqueName(Path.GetFileName(sourcePath), usedNames);
            File.Copy(sourcePath, Path.Combine(destinationDirectory, destinationName), false);
        }
    }

    private static string SessionDirectoryName(LoadedReview loaded)
    {
        var raw = loaded.IsSchema11 ? loaded.Manifest.HandoffId! : Path.GetFileNameWithoutExtension(loaded.ManifestPath);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(raw.Trim().Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim('.', ' ');
        if (string.IsNullOrWhiteSpace(safe)) safe = "handoff";
        if (safe.Length > 64) safe = safe[..64];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(loaded.SessionIdentity)))[..10].ToLowerInvariant();
        return $"{safe}-{hash}";
    }
}
