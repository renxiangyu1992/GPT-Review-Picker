using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GPTReviewPicker;

public static class HandoffTabClose
{
    public static bool CanClose(ReviewSession? session) => session is { IsQuickTray: false };

    public static Rectangle GetCloseBounds(ReviewSession? session, Rectangle tabBounds, int deviceDpi)
    {
        if (!CanClose(session) || tabBounds.Width <= 0 || tabBounds.Height <= 0) return Rectangle.Empty;
        var scale = Math.Max(deviceDpi, 96) / 96f;
        var size = Math.Max(12, (int)Math.Round(16 * scale));
        var margin = Math.Max(3, (int)Math.Round(4 * scale));
        return new Rectangle(tabBounds.Right - margin - size, tabBounds.Top + (tabBounds.Height - size) / 2, size, size);
    }

    public static bool IsCloseHit(ReviewSession? session, Rectangle tabBounds, Point location, int deviceDpi)
        => GetCloseBounds(session, tabBounds, deviceDpi).Contains(location);
}

public static class ReviewTabLabel
{
    public static string Format(ReviewSession session)
    {
        var title = BaseTitle(session);
        if (session.IsQuickTray) return title;
        return AppendTime(title, session.GeneratedAt);
    }

    public static string BaseTitle(ReviewSession session)
    {
        var conversationId = session.LoadedReview?.Manifest.ConversationId?.Trim();
        var title = RemoveFallbackSuffix(session.DisplayName, conversationId);
        if (!string.IsNullOrWhiteSpace(conversationId) &&
            string.Equals(title, conversationId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(session.ProjectName)) return session.ProjectName.Trim();
            if (!string.IsNullOrWhiteSpace(session.TaskName)) return session.TaskName.Trim();
            return conversationId[..Math.Min(8, conversationId.Length)];
        }
        return title;
    }

    public static string FormatHeaderTime(string generatedAt)
        => DateTimeOffset.TryParse(generatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : generatedAt;

    private static string AppendTime(string title, string generatedAt)
        => DateTimeOffset.TryParse(generatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? $"{title} [{parsed:HH\\:mm}]"
            : title;

    private static string RemoveFallbackSuffix(string title, string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return title;
        var prefix = conversationId.Trim();
        var suffix = prefix[..Math.Min(8, prefix.Length)];
        var marker = $" [{suffix}]";
        return title.EndsWith(marker, StringComparison.OrdinalIgnoreCase)
            ? title[..^marker.Length].TrimEnd()
            : title;
    }
}

public sealed class MainForm : Form
{
    private readonly ReviewWorkspace _workspace;
    private readonly Label _sessionTitle = new(), _sessionDetails = new(), _count = new(), _status = new();
    private readonly TabControl _sessionTabs = new();
    private readonly TableLayoutPanel _manifestRows = new(), _trayRows = new();
    private readonly Label _trayCount = new(), _dropHint = new();
    private readonly Button _clearManual = new(), _unloadManifest = new(), _closeHandoff = new();
    private readonly List<Button> _manifestSelectionButtons = [];
    private readonly Panel _dragOutPanel = new();
    private readonly ToolTip _toolTip = new();
    private bool _syncingTabs;
    private Point _dragStart;
    private bool _dragArmed;
    private ReviewTrayItem? _rowDragItem;
    private Point _rowDragStart;
    private bool _rowDragArmed;

    private ReviewSession Session => _workspace.ActiveSession;

    public MainForm(ReviewWorkspace? workspace = null)
    {
        _workspace = workspace ?? new ReviewWorkspace();
        Text = "GPT Review Picker"; Width = 1240; Height = 920; MinimumSize = new Size(980, 720); TopMost = false;
        StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Font;
        BuildUi(); RebuildSessionTabs(); RefreshManifestRows();
    }

    public void LoadManifestPath(string path) => OpenManifest(path, true);

    public PickerIpcResponse HandleIpcMessage(PickerIpcMessage message)
    {
        if (InvokeRequired) return (PickerIpcResponse)Invoke(() => HandleIpcMessage(message));
        if (message.Type == PickerIpcMessage.ActivateType) { ActivateWindow(); return PickerIpcResponse.Accept(); }

        try
        {
            var result = string.Equals(message.Type, PickerIpcMessage.OpenResultType, StringComparison.Ordinal)
                ? _workspace.AddOrUpdateFailure(message.Path!, activate: false, markUnread: true)
                : _workspace.AddOrUpdateManifest(message.Path!, activate: false, markUnread: true);
            if (!result.Session.IsFailure)
                result.Session.SetStatus(result.Created ? "Handoff received." : "Handoff refreshed; Manual files retained.");
            RebuildSessionTabs(); RefreshManifestRows();
            return PickerIpcResponse.Accept();
        }
        catch (Exception ex)
        {
            SetStatus($"Incoming Handoff artifact rejected: {ex.Message}", true);
            return PickerIpcResponse.Reject(ex.Message);
        }
    }

    public void HandleIpcError(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => HandleIpcError(message)); return; }
        SetStatus($"IPC error: {message}", true);
    }

    public void ActivateWindow()
    {
        if (InvokeRequired) { BeginInvoke(ActivateWindow); return; }
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Show(); BringToFront(); Activate(); NativeMethods.SetForegroundWindow(Handle);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(14) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 330));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 610));
        var avatar = new PictureBox { Image = LoadBrandAvatar(), SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, Margin = new Padding(2, 8, 8, 8), AccessibleName = "Xiangyu Ren avatar" };
        var branding = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 8, 0, 8) };
        branding.RowStyles.Add(new RowStyle(SizeType.Percent, 62)); branding.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        var title = new Label { Text = "GPT Review Picker", Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        var byline = new Label { Text = "by Xiangyu Ren", Font = new Font(Font.FontFamily, 9, FontStyle.Regular), ForeColor = Color.DimGray, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
        branding.Controls.Add(title, 0, 0); branding.Controls.Add(byline, 0, 1);
        var meta = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        meta.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); meta.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); meta.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
        foreach (var label in new[] { _sessionTitle, _sessionDetails, _count }) { label.AutoEllipsis = true; label.Dock = DockStyle.Fill; label.TextAlign = ContentAlignment.MiddleRight; }
        _count.Font = new Font(Font, FontStyle.Bold);
        meta.Controls.Add(_sessionTitle, 0, 0); meta.Controls.Add(_sessionDetails, 0, 1); meta.Controls.Add(_count, 0, 2);
        header.Controls.Add(avatar, 0, 0); header.Controls.Add(branding, 1, 0); header.Controls.Add(meta, 2, 0); root.Controls.Add(header, 0, 0);

        _sessionTabs.Dock = DockStyle.Fill; _sessionTabs.HotTrack = true; _sessionTabs.ShowToolTips = true;
        _sessionTabs.DrawMode = TabDrawMode.OwnerDrawFixed; _sessionTabs.Padding = new Point(20, 3);
        _sessionTabs.DrawItem += DrawSessionTab; _sessionTabs.MouseDown += SessionTabMouseDown;
        _sessionTabs.SelectedIndexChanged += (_, _) => SessionTabChanged();
        root.Controls.Add(_sessionTabs, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        buttons.Controls.Add(CommandButton("Open Manifest", (_, _) => OpenManifestDialog()));
        ConfigureButton(_unloadManifest, "Unload Manifest", (_, _) => UnloadManifest()); buttons.Controls.Add(_unloadManifest);
        ConfigureButton(_closeHandoff, "Close Handoff", (_, _) => CloseHandoff()); buttons.Controls.Add(_closeHandoff);
        _manifestSelectionButtons.Add(CommandButton("Only MUST", (_, _) => { var loaded = Session.LoadedReview; if (loaded != null) { ReviewSelection.SelectOnlyMust(loaded.Items); RefreshManifestRows(); } }));
        _manifestSelectionButtons.Add(CommandButton("Select All", (_, _) => { var loaded = Session.LoadedReview; if (loaded != null) { ReviewSelection.SelectAllExisting(loaded.Items); RefreshManifestRows(); } }));
        _manifestSelectionButtons.Add(CommandButton("Clear", (_, _) => { var loaded = Session.LoadedReview; if (loaded != null) { ReviewSelection.Clear(loaded.Items); RefreshManifestRows(); } }));
        foreach (var button in _manifestSelectionButtons) buttons.Controls.Add(button);
        buttons.Controls.Add(CommandButton("Copy Review Tray", CopyTray));
        buttons.Controls.Add(CommandButton("Open Review Bundle", OpenBundle));
        root.Controls.Add(buttons, 0, 2);

        _manifestRows.Dock = DockStyle.Fill; _manifestRows.AutoScroll = true; _manifestRows.ColumnCount = 1; _manifestRows.RowCount = 0; _manifestRows.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
        _manifestRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(_manifestRows, 0, 3);
        root.Controls.Add(CreateTrayPanel(), 0, 4);

        _status.Dock = DockStyle.Bottom; _status.Height = 22; _status.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(_status); _status.BringToFront();
    }

    private static Image LoadBrandAvatar()
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("GPTReviewPicker.Assets.XiangyuRen_avatar.png")
            ?? throw new InvalidOperationException("Embedded branding avatar resource is missing.");
        using var source = Image.FromStream(stream);
        return new Bitmap(source);
    }

    private Control CreateTrayPanel()
    {
        var tray = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6) };
        tray.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); tray.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tray.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); tray.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var trayHeader = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        trayHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); trayHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); trayHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        trayHeader.Controls.Add(new Label { Text = "Review Tray", Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _trayCount.Dock = DockStyle.Fill; _trayCount.TextAlign = ContentAlignment.MiddleRight; _trayCount.Font = new Font(Font, FontStyle.Bold); trayHeader.Controls.Add(_trayCount, 1, 0);
        _clearManual.Text = "Clear Manual"; _clearManual.Dock = DockStyle.Fill; _clearManual.Margin = new Padding(8, 3, 0, 3);
        _clearManual.Click += (_, _) => { Session.Tray.ClearManual(); RefreshTray(); SetStatus("Manual files cleared; Manifest selection retained."); };
        trayHeader.Controls.Add(_clearManual, 2, 0); tray.Controls.Add(trayHeader, 0, 0);

        _trayRows.Dock = DockStyle.Fill; _trayRows.AutoScroll = true; _trayRows.ColumnCount = 1; _trayRows.RowCount = 0; _trayRows.BackColor = Color.White;
        _trayRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); tray.Controls.Add(_trayRows, 0, 1);
        _dropHint.Text = "Drop files here to add"; _dropHint.Dock = DockStyle.Fill; _dropHint.TextAlign = ContentAlignment.MiddleCenter; _dropHint.ForeColor = Color.DimGray; _dropHint.BackColor = Color.FromArgb(248, 249, 250); _dropHint.BorderStyle = BorderStyle.FixedSingle;
        tray.Controls.Add(_dropHint, 0, 2);

        _dragOutPanel.Dock = DockStyle.Fill; _dragOutPanel.BackColor = Color.FromArgb(218, 234, 252); _dragOutPanel.Cursor = Cursors.Hand;
        var dragText = new Label { Text = "Drag all files to ChatGPT", Font = new Font(Font.FontFamily, 12, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
        _dragOutPanel.Controls.Add(dragText); tray.Controls.Add(_dragOutPanel, 0, 3);

        AttachDropEvents(_trayRows); AttachDropEvents(_dropHint); AttachDropEvents(tray);
        AttachDragOutEvents(_dragOutPanel); AttachDragOutEvents(dragText);
        return tray;
    }

    private static Button CommandButton(string text, EventHandler click)
    {
        var button = new Button(); ConfigureButton(button, text, click); return button;
    }

    private static void ConfigureButton(Button button, string text, EventHandler click)
    {
        button.Text = text; button.AutoSize = true; button.Height = 32; button.Margin = new Padding(0, 0, 8, 0); button.Click += click;
    }

    private void RebuildSessionTabs()
    {
        _syncingTabs = true;
        _sessionTabs.TabPages.Clear();
        foreach (var session in _workspace.Sessions)
        {
            var page = new TabPage(TabText(session)) { Tag = session, ToolTipText = SessionToolTip(session) };
            _sessionTabs.TabPages.Add(page);
            if (ReferenceEquals(session, Session)) _sessionTabs.SelectedTab = page;
        }
        _syncingTabs = false;
        UpdateSessionHeader();
    }

    private static string TabText(ReviewSession session) => session.Unread ? $"● {ReviewTabLabel.Format(session)}" : ReviewTabLabel.Format(session);

    private void DrawSessionTab(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _sessionTabs.TabPages.Count) return;
        var page = _sessionTabs.TabPages[e.Index];
        var session = page.Tag as ReviewSession;
        var tabBounds = _sessionTabs.GetTabRect(e.Index);
        var selected = (e.State & DrawItemState.Selected) != 0;
        e.Graphics.FillRectangle(selected ? SystemBrushes.Window : SystemBrushes.Control, tabBounds);

        var closeBounds = HandoffTabClose.GetCloseBounds(session, tabBounds, DeviceDpi);
        var textRight = closeBounds.IsEmpty ? tabBounds.Right - 8 : closeBounds.Left - 4;
        var textBounds = Rectangle.FromLTRB(tabBounds.Left + 8, tabBounds.Top, Math.Max(tabBounds.Left + 8, textRight), tabBounds.Bottom);
        TextRenderer.DrawText(e.Graphics, page.Text, Font, textBounds, SystemColors.ControlText,
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);

        if (!closeBounds.IsEmpty)
            TextRenderer.DrawText(e.Graphics, "×", Font, closeBounds, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
        if (selected && Focused) e.DrawFocusRectangle();
    }

    private void SessionTabMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        for (var index = 0; index < _sessionTabs.TabPages.Count; index++)
        {
            var page = _sessionTabs.TabPages[index];
            if (page.Tag is ReviewSession session &&
                HandoffTabClose.IsCloseHit(session, _sessionTabs.GetTabRect(index), e.Location, DeviceDpi))
            {
                CloseHandoff(session);
                return;
            }
        }
    }

    private static string SessionToolTip(ReviewSession session)
    {
        if (session.IsQuickTray) return "Quick Tray - standalone Manual files";
        return string.Join(Environment.NewLine, new[] {
            $"Display Name: {session.DisplayName}", $"Project: {session.ProjectName}", $"Task: {session.TaskName}",
            $"Generated At: {ReviewTabLabel.FormatHeaderTime(session.GeneratedAt)}", $"Manifest: {session.ManifestPath}"
        });
    }

    private void SessionTabChanged()
    {
        if (_syncingTabs || _sessionTabs.SelectedTab?.Tag is not ReviewSession session) return;
        _workspace.Activate(session);
        _sessionTabs.SelectedTab.Text = TabText(session);
        RefreshManifestRows();
    }

    private void OpenManifestDialog()
    {
        using var dialog = new OpenFileDialog { Filter = "Manifest JSON (*.json)|*.json", Title = "Open GPT review manifest" };
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenManifest(dialog.FileName, true);
    }

    private void OpenManifest(string path, bool activate)
    {
        try
        {
            var result = _workspace.AddOrUpdateManifest(path, activate, markUnread: !activate);
            result.Session.SetStatus(result.Created ? $"Loaded {Path.GetFileName(path)}" : "Handoff refreshed; Manual files retained.");
            RebuildSessionTabs(); RefreshManifestRows();
        }
        catch (Exception ex)
        {
            SetStatus($"Invalid Manifest: {ex.Message}", true);
            MessageBox.Show(this, ex.Message, "GPT Review Picker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UnloadManifest()
    {
        if (!Session.HasManifest) return;
        Session.UnloadManifest(); RefreshManifestRows(); SetStatus("Manifest unloaded; Manual files retained.");
    }

    private void CloseHandoff() => CloseHandoff(Session);

    private void CloseHandoff(ReviewSession closing)
    {
        if (!_workspace.CloseHandoff(closing)) return;
        RebuildSessionTabs(); RefreshManifestRows(); SetStatus($"Closed {closing.DisplayName}; source files were not changed.");
    }

    private void RefreshManifestRows()
    {
        var loaded = Session.LoadedReview;
        _manifestRows.SuspendLayout(); _manifestRows.Controls.Clear(); _manifestRows.RowStyles.Clear(); _manifestRows.RowCount = loaded?.Items.Count ?? 0;
        if (loaded != null)
            foreach (var item in loaded.Items) { _manifestRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); _manifestRows.Controls.Add(CreateManifestRow(item)); }
        _manifestRows.ResumeLayout(); UpdateSessionHeader(); RefreshTray(); DisplayStatus();
    }

    private Control CreateManifestRow(ReviewFileItem item)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(6, 3, 6, 3) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        var check = new CheckBox { Checked = item.Selected, Anchor = AnchorStyles.None, Tag = item };
        check.CheckedChanged += (_, _) => { item.Selected = check.Checked; UpdateSessionHeader(); RefreshTray(); };
        var priority = new Label { Text = item.Priority.ToString(), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SourceColor(item.Priority.ToString()) };
        var label = new Label { Text = $"{item.Label}\n{item.RelativePath}", Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        var reason = new Label { Text = item.Reason, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray };
        var status = new Label { Text = item.Exists ? "Exists" : "Missing", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = item.Exists ? Color.ForestGreen : Color.Firebrick };
        row.Controls.Add(check, 0, 0); row.Controls.Add(priority, 1, 0); row.Controls.Add(label, 2, 0); row.Controls.Add(reason, 3, 0); row.Controls.Add(status, 4, 0);
        return row;
    }

    private void RefreshTray()
    {
        var items = Session.Tray.Items;
        _trayRows.SuspendLayout(); _trayRows.Controls.Clear(); _trayRows.RowStyles.Clear(); _trayRows.RowCount = items.Count;
        foreach (var item in items)
        {
            _trayRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            var row = CreateTrayRow(item); _trayRows.Controls.Add(row); AttachDropEventsRecursive(row); AttachRowDragOutEventsRecursive(row, item);
        }
        _trayRows.ResumeLayout();
        _trayCount.Text = $"{items.Count} files ready"; _clearManual.Enabled = Session.Tray.ManualCount > 0;
        _dropHint.Text = items.Count == 0 ? "Drop files here to add" : "Drop more files here to add";
    }

    private Control CreateTrayRow(ReviewTrayItem item)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = new Padding(0), Padding = new Padding(4, 1, 2, 1), BackColor = Color.White };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        var source = new Label { Text = item.Source.ToString(), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = SourceColor(item.Source.ToString()) };
        var name = new Label { Text = item.DisplayName, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(Font, FontStyle.Bold), AutoEllipsis = true };
        var path = new Label { Text = item.IsVirtual ? "Codex Final Response / Agent Statement" : item.FullPath, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.DimGray, AutoEllipsis = true };
        var remove = new Button { Text = "X", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 3, 2, 3), AccessibleName = item.IsManual ? "Remove manual file" : "Unselect manifest file" };
        remove.FlatAppearance.BorderSize = 0; _toolTip.SetToolTip(path, item.IsVirtual ? "Exact canonical final text" : item.FullPath); _toolTip.SetToolTip(remove, remove.AccessibleName);
        remove.Click += (_, _) => { Session.Tray.Remove(item); RefreshManifestRows(); SetStatus(item.IsManual ? "Manual file removed." : "Manifest file unselected."); };
        row.Controls.Add(source, 0, 0); row.Controls.Add(name, 1, 0); row.Controls.Add(path, 2, 0); row.Controls.Add(remove, 3, 0);
        return row;
    }

    private static Color SourceColor(string source) => source switch
    {
        "MUST" => Color.Firebrick,
        "RECOMMENDED" => Color.DarkGoldenrod,
        "MANUAL" => Color.RoyalBlue,
        _ => Color.DimGray
    };

    private void UpdateSessionHeader()
    {
        var loaded = Session.LoadedReview;
        var items = loaded?.Items ?? Array.Empty<ReviewFileItem>();
        _sessionTitle.Text = Session.IsFailure ? $"Producer Blocked: {ReviewTabLabel.Format(Session)}" : $"Current: {ReviewTabLabel.Format(Session)}";
        _sessionDetails.Text = Session.IsQuickTray
            ? "Quick Tray - standalone Manual files"
            : Session.IsFailure
                ? $"Project: {Session.ProjectName} | Task: {(string.IsNullOrWhiteSpace(Session.TaskName) ? "(not provided)" : Session.TaskName)} | Handoff: {Session.FailedHandoff?.Result.HandoffId}"
                : $"Project: {Session.ProjectName} | Task: {(string.IsNullOrWhiteSpace(Session.TaskName) ? "(not provided)" : Session.TaskName)} | {ReviewTabLabel.FormatHeaderTime(Session.GeneratedAt)}";
        _toolTip.SetToolTip(_sessionDetails, Session.ManifestPath);
        _count.Text = $"{_workspace.HandoffCount} handoffs / {_workspace.UnreadCount} unread | {items.Count(item => item.Selected)} selected / {items.Count} files";
        _unloadManifest.Enabled = loaded is not null;
        _closeHandoff.Enabled = !Session.IsQuickTray;
        foreach (var button in _manifestSelectionButtons) button.Enabled = loaded is not null;
    }

    private void CopyTray(object? sender, EventArgs e)
    {
        var items = Session.Tray.Items;
        if (items.Count == 0) { SetStatus("The Review Tray is empty.", true); return; }
        try { FileDropService.CopyToClipboard(items); SetStatus($"{items.Count} Review Tray files copied"); }
        catch (ExternalException) { SetStatus("Clipboard is busy. Close other clipboard tools and try again.", true); }
    }

    private void OpenBundle(object? sender, EventArgs e)
    {
        var items = Session.Tray.Items; if (items.Count == 0) { SetStatus("The Review Tray is empty.", true); return; }
        try
        {
            string directory;
            if (Session.LoadedReview is not null) directory = BundleService.CreateHandoffBundle(Session.LoadedReview, items);
            else
            {
                var sessionDirectory = Session.IsQuickTray ? "QuickTray" : Uri.EscapeDataString(Session.Identity);
                directory = BundleService.CreateReviewBundleInDirectory(Path.Combine(Path.GetTempPath(), "GPTReviewPicker", sessionDirectory), items.Select(FileDropService.Materialize));
            }
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{directory}\"") { UseShellExecute = true });
            SetStatus($"Review Bundle ready: {items.Count} files");
        }
        catch (Exception ex) { SetStatus($"Bundle failed: {ex.Message}", true); }
    }

    private void AttachDropEvents(Control control)
    {
        control.AllowDrop = true; control.DragEnter += TrayDragEnter; control.DragDrop += TrayDragDrop;
    }

    private void AttachDropEventsRecursive(Control control)
    {
        AttachDropEvents(control); foreach (Control child in control.Controls) AttachDropEventsRecursive(child);
    }

    private void AttachRowDragOutEventsRecursive(Control control, ReviewTrayItem item)
    {
        if (control is Button) return;
        control.Cursor = Cursors.Hand;
        control.MouseDown += (_, e) => RowDragOutMouseDown(item, e);
        control.MouseMove += RowDragOutMouseMove;
        control.MouseUp += (_, _) => ResetRowDrag();
        foreach (Control child in control.Controls) AttachRowDragOutEventsRecursive(child, item);
    }

    private void TrayDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private void TrayDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths) return;
        var before = Session.Tray.Items.Count; var result = Session.Tray.AddManualPaths(paths); var visibleAdded = Session.Tray.Items.Count - before;
        RefreshTray(); SetStatus(result.ToStatusMessage(visibleAdded), result.HasWarnings);
    }

    private void RowDragOutMouseDown(ReviewTrayItem item, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _rowDragItem = item; _rowDragStart = e.Location; _rowDragArmed = true;
    }

    private void RowDragOutMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_rowDragArmed || _rowDragItem is null || e.Button != MouseButtons.Left) return;
        if (Math.Abs(e.X - _rowDragStart.X) < SystemInformation.DragSize.Width / 2 && Math.Abs(e.Y - _rowDragStart.Y) < SystemInformation.DragSize.Height / 2) return;
        var item = _rowDragItem; ResetRowDrag();
        if (!item.IsVirtual && !File.Exists(item.FullPath)) { RefreshTray(); SetStatus("The file no longer exists.", true); return; }
        var effect = DoDragDrop(FileDropService.CreateDataObject([item]), DragDropEffects.Copy);
        SetStatus(effect == DragDropEffects.None ? "Drag canceled" : $"{item.DisplayName} dragged");
    }

    private void ResetRowDrag() { _rowDragArmed = false; _rowDragItem = null; }

    private void AttachDragOutEvents(Control control)
    {
        control.MouseDown += DragOutMouseDown; control.MouseMove += DragOutMouseMove; control.MouseUp += (_, _) => _dragArmed = false;
    }

    private void DragOutMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { _dragStart = e.Location; _dragArmed = true; }
    }

    private void DragOutMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragArmed || e.Button != MouseButtons.Left) return;
        if (Math.Abs(e.X - _dragStart.X) < SystemInformation.DragSize.Width / 2 && Math.Abs(e.Y - _dragStart.Y) < SystemInformation.DragSize.Height / 2) return;
        _dragArmed = false; var items = Session.Tray.Items;
        if (items.Count == 0) { SetStatus("The Review Tray is empty.", true); return; }
        var effect = DoDragDrop(FileDropService.CreateDataObject(items), DragDropEffects.Copy);
        SetStatus(effect == DragDropEffects.None ? "Drag canceled" : $"{items.Count} Review Tray files dragged");
    }

    private void SetStatus(string message, bool error = false)
    {
        Session.SetStatus(message, error); DisplayStatus();
    }

    private void DisplayStatus()
    {
        _status.Text = Session.StatusMessage; _status.ForeColor = Session.StatusIsError ? Color.Firebrick : Color.DarkSlateGray;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
    }
}
