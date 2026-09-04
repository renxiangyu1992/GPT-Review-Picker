namespace GPTReviewPicker;

public enum ReviewTraySource { MUST, RECOMMENDED, OPTIONAL, MANUAL }

public sealed class ReviewTrayItem
{
    public required string FullPath { get; init; }
    public required string DisplayName { get; init; }
    public string? VirtualContent { get; init; }
    public required ReviewTraySource Source { get; init; }
    public ReviewFileItem? ManifestItem { get; init; }
    public bool IsVirtual => VirtualContent is not null;
    public bool IsManual => Source == ReviewTraySource.MANUAL;
}

public sealed record ManualAddResult(int Added, int AlreadyPresent, int DirectoriesIgnored, int InvalidIgnored)
{
    public bool HasWarnings => DirectoriesIgnored > 0 || InvalidIgnored > 0;

    public string ToStatusMessage(int visibleAdded)
    {
        var messages = new List<string>();
        if (visibleAdded > 0) messages.Add($"{visibleAdded} file{(visibleAdded == 1 ? "" : "s")} added");
        var manifestDuplicates = Added - visibleAdded;
        if (manifestDuplicates > 0) messages.Add($"{manifestDuplicates} kept under Manifest source");
        if (AlreadyPresent > 0) messages.Add($"{AlreadyPresent} duplicate ignored");
        if (DirectoriesIgnored > 0) messages.Add($"{DirectoriesIgnored} folder{(DirectoriesIgnored == 1 ? "" : "s")} ignored (folders are not supported)");
        if (InvalidIgnored > 0) messages.Add($"{InvalidIgnored} invalid path{(InvalidIgnored == 1 ? "" : "s")} ignored");
        return messages.Count == 0 ? "No files were added." : string.Join("; ", messages);
    }
}

public sealed class ReviewTray
{
    private IReadOnlyList<ReviewFileItem> _manifestItems;
    private readonly List<string> _manualPaths = [];
    private readonly HashSet<string> _manualPathSet = new(StringComparer.OrdinalIgnoreCase);

    public ReviewTray(IReadOnlyList<ReviewFileItem>? manifestItems = null) => _manifestItems = manifestItems ?? [];

    public void SetManifestItems(IReadOnlyList<ReviewFileItem> manifestItems) => _manifestItems = manifestItems;

    public void ClearManifestItems()
    {
        foreach (var item in _manifestItems) item.Selected = false;
        _manifestItems = [];
    }

    public IReadOnlyList<ReviewTrayItem> Items
    {
        get
        {
            var items = new List<ReviewTrayItem>();
            var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var manifest in _manifestItems.Where(item => item.Selected && item.Exists))
            {
                var path = manifest.IsVirtual ? string.Empty : NormalizePath(manifest.FullPath);
                var key = manifest.IsVirtual ? $"virtual:{manifest.Label}" : path;
                if (!visible.Add(key)) continue;
                items.Add(new ReviewTrayItem {
                    FullPath = path,
                    DisplayName = manifest.IsVirtual ? manifest.RelativePath : Path.GetFileName(path),
                    VirtualContent = manifest.VirtualContent,
                    Source = Enum.Parse<ReviewTraySource>(manifest.Priority.ToString()),
                    ManifestItem = manifest
                });
            }
            foreach (var path in _manualPaths)
            {
                if (!File.Exists(path) || !visible.Add(path)) continue;
                items.Add(new ReviewTrayItem { FullPath = path, DisplayName = Path.GetFileName(path), Source = ReviewTraySource.MANUAL });
            }
            return items;
        }
    }

    public IReadOnlyList<string> Paths => Items.Where(item => !item.IsVirtual).Select(item => item.FullPath).ToList();
    public int ManualCount => _manualPaths.Count(path => File.Exists(path));

    public ManualAddResult AddManualPaths(IEnumerable<string> droppedPaths)
    {
        var added = 0;
        var existing = 0;
        var directories = 0;
        var invalid = 0;
        foreach (var rawPath in droppedPaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) { invalid++; continue; }
            string path;
            try { path = NormalizePath(rawPath); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { invalid++; continue; }
            if (Directory.Exists(path)) { directories++; continue; }
            if (!File.Exists(path)) { invalid++; continue; }
            if (!_manualPathSet.Add(path)) { existing++; continue; }
            _manualPaths.Add(path);
            added++;
        }
        return new ManualAddResult(added, existing, directories, invalid);
    }

    public bool RemoveManual(string path)
    {
        var normalized = NormalizePath(path);
        if (!_manualPathSet.Remove(normalized)) return false;
        _manualPaths.RemoveAll(item => StringComparer.OrdinalIgnoreCase.Equals(item, normalized));
        return true;
    }

    public void Remove(ReviewTrayItem item)
    {
        if (item.ManifestItem is not null) item.ManifestItem.Selected = false;
        else RemoveManual(item.FullPath);
    }

    public void ClearManual()
    {
        _manualPaths.Clear();
        _manualPathSet.Clear();
    }

    public static string NormalizePath(string path) => Path.GetFullPath(path);
}

public sealed class ReviewSession
{
    public ReviewTray Tray { get; } = new();
    public LoadedReview? LoadedReview { get; private set; }
    public FailedHandoff? FailedHandoff { get; private set; }
    public bool HasManifest => LoadedReview is not null;
    public bool IsFailure => FailedHandoff is not null;
    public bool IsQuickTray { get; private init; } = true;
    public string Identity { get; private set; } = "quick-tray";
    public string DisplayName { get; private set; } = "Quick Tray";
    public string ProjectName { get; private set; } = string.Empty;
    public string TaskName { get; private set; } = string.Empty;
    public string GeneratedAt { get; private set; } = string.Empty;
    public string ManifestPath { get; private set; } = string.Empty;
    public bool Unread { get; internal set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public bool StatusIsError { get; private set; }

    public static ReviewSession CreateHandoff(LoadedReview loaded, bool unread)
    {
        var session = new ReviewSession { IsQuickTray = false, Unread = unread };
        session.ApplyLoadedReview(loaded);
        return session;
    }

    public static ReviewSession CreateFailure(FailedHandoff failure, bool unread)
    {
        var session = new ReviewSession { IsQuickTray = false, Unread = unread };
        session.ApplyFailure(failure);
        return session;
    }

    public void ApplyFailure(FailedHandoff failure)
    {
        LoadedReview = null;
        FailedHandoff = failure;
        Tray.ClearManifestItems();
        Identity = failure.Identity;
        DisplayName = failure.DisplayName;
        ProjectName = failure.ProjectName;
        TaskName = failure.TaskName;
        GeneratedAt = failure.GeneratedAt;
        ManifestPath = failure.ResultPath;
        SetStatus(failure.Reason, true);
    }

    public void LoadManifest(string path)
    {
        var loaded = ManifestLoader.Load(path);
        ApplyLoadedReview(loaded);
    }

    public void ApplyLoadedReview(LoadedReview loaded)
    {
        var replacesReviewRound = !IsQuickTray && LoadedReview is { } current &&
            current.IsSchema12 && loaded.IsSchema12 &&
            string.Equals(current.SessionIdentity, loaded.SessionIdentity, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(current.Manifest.HandoffId?.Trim(), loaded.Manifest.HandoffId?.Trim(), StringComparison.OrdinalIgnoreCase);
        var preserveConversationTitle = !IsQuickTray && loaded.IsSchema12 &&
            string.Equals(Identity, loaded.SessionIdentity, StringComparison.OrdinalIgnoreCase) &&
            loaded.Manifest.RenameConversation is not true;
        var currentDisplayName = DisplayName;
        LoadedReview = loaded;
        FailedHandoff = null;
        Tray.SetManifestItems(loaded.Items);
        if (replacesReviewRound) Tray.ClearManual();
        if (IsQuickTray) return;
        Identity = loaded.SessionIdentity;
        DisplayName = preserveConversationTitle ? currentDisplayName : loaded.DisplayName;
        ProjectName = loaded.ProjectName;
        TaskName = loaded.TaskName;
        GeneratedAt = loaded.Manifest.GeneratedAt?.Trim() ?? string.Empty;
        ManifestPath = loaded.ManifestPath;
    }

    public void UnloadManifest()
    {
        Tray.ClearManifestItems();
        LoadedReview = null;
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }
}

public sealed record WorkspaceAddResult(ReviewSession Session, bool Created);

public sealed class ReviewWorkspace
{
    private readonly List<ReviewSession> _handoffs = [];
    private readonly Dictionary<string, ReviewSession> _byIdentity = new(StringComparer.OrdinalIgnoreCase);

    public ReviewSession QuickTray { get; } = new();
    public ReviewSession ActiveSession { get; private set; }
    public IReadOnlyList<ReviewSession> Handoffs => _handoffs;
    public IReadOnlyList<ReviewSession> Sessions => [QuickTray, .. _handoffs];
    public int HandoffCount => _handoffs.Count;
    public int UnreadCount => _handoffs.Count(session => session.Unread);

    public ReviewWorkspace() => ActiveSession = QuickTray;

    public WorkspaceAddResult AddOrUpdateManifest(string path, bool activate, bool markUnread)
        => AddOrUpdateLoadedReview(ManifestLoader.Load(path), activate, markUnread);

    public WorkspaceAddResult AddOrUpdateFailure(string path, bool activate, bool markUnread)
    {
        var failure = FailedHandoffLoader.Load(path);
        if (_byIdentity.TryGetValue(failure.Identity, out var existing))
        {
            existing.ApplyFailure(failure);
            if (markUnread && !ReferenceEquals(existing, ActiveSession)) existing.Unread = true;
            if (activate) Activate(existing);
            return new WorkspaceAddResult(existing, false);
        }
        var session = ReviewSession.CreateFailure(failure, markUnread);
        _handoffs.Add(session);
        _byIdentity.Add(session.Identity, session);
        if (activate) Activate(session);
        return new WorkspaceAddResult(session, true);
    }

    public WorkspaceAddResult AddOrUpdateLoadedReview(LoadedReview loaded, bool activate, bool markUnread)
    {
        if (_byIdentity.TryGetValue(loaded.SessionIdentity, out var existing))
        {
            existing.ApplyLoadedReview(loaded);
            if (markUnread && !ReferenceEquals(existing, ActiveSession)) existing.Unread = true;
            if (activate) Activate(existing);
            return new WorkspaceAddResult(existing, false);
        }

        var session = ReviewSession.CreateHandoff(loaded, markUnread);
        _handoffs.Add(session);
        _byIdentity.Add(session.Identity, session);
        if (activate) Activate(session);
        return new WorkspaceAddResult(session, true);
    }

    public void Activate(ReviewSession session)
    {
        if (!Sessions.Contains(session)) throw new InvalidOperationException("Session does not belong to this workspace.");
        ActiveSession = session;
        session.Unread = false;
    }

    public bool CloseHandoff(ReviewSession session)
    {
        if (session.IsQuickTray || !_handoffs.Remove(session)) return false;
        _byIdentity.Remove(session.Identity);
        if (ReferenceEquals(ActiveSession, session)) Activate(QuickTray);
        return true;
    }
}
