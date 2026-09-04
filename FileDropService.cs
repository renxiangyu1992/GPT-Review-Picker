using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GPTReviewPicker;

public static class FileDropService
{
    public static string[] ExistingSelectedPaths(IEnumerable<ReviewFileItem> items) =>
        ReviewSelection.ExistingSelected(items).Select(i => i.FullPath).ToArray();

    public static DataObject CreateDataObject(IEnumerable<ReviewFileItem> items) =>
        CreateDataObject(ReviewSelection.ExistingSelected(items).Select(Materialize));

    public static DataObject CreateDataObject(IEnumerable<ReviewTrayItem> items) =>
        CreateDataObject(items.Where(item => item.IsVirtual || File.Exists(item.FullPath)).Select(Materialize));

    public static DataObject CreateDataObject(IEnumerable<string> paths) =>
        new(DataFormats.FileDrop, ExistingPaths(paths));

    public static void CopyToClipboard(IEnumerable<ReviewFileItem> items, int attempts = 3)
        => CopyPathsToClipboard(ReviewSelection.ExistingSelected(items).Select(Materialize), attempts);

    public static void CopyToClipboard(IEnumerable<ReviewTrayItem> items, int attempts = 3)
        => CopyPathsToClipboard(items.Where(item => item.IsVirtual || File.Exists(item.FullPath)).Select(Materialize), attempts);

    public static void CopyPathsToClipboard(IEnumerable<string> paths, int attempts = 3)
    {
        var existingPaths = ExistingPaths(paths);
        if (existingPaths.Length == 0) throw new InvalidOperationException("No existing files in the Review Tray.");
        var collection = new StringCollection();
        collection.AddRange(existingPaths);
        for (var attempt = 1; ; attempt++)
        {
            try { Clipboard.SetFileDropList(collection); return; }
            catch (ExternalException) when (attempt < attempts) { Thread.Sleep(100); }
        }
    }

    private static string[] ExistingPaths(IEnumerable<string> paths) => paths
        .Select(Path.GetFullPath)
        .Where(File.Exists)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static string Materialize(ReviewTrayItem item)
    {
        if (!item.IsVirtual) return Path.GetFullPath(item.FullPath);
        return MaterializeContent(item.DisplayName, item.VirtualContent!);
    }

    public static string Materialize(ReviewFileItem item)
    {
        if (!item.IsVirtual) return Path.GetFullPath(item.FullPath);
        return MaterializeContent(item.RelativePath, item.VirtualContent!);
    }

    private static string MaterializeContent(string fileName, string content)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "CODEX_FINAL_RESPONSE.md";
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..16].ToLowerInvariant();
        var directory = Path.Combine(Path.GetTempPath(), "GPTReviewPicker", "final-response", digest);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, safeName);
        if (!File.Exists(path)) File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }
}
