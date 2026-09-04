using System.Text.Json;
using GPTReviewPicker;

internal static class Program
{
    private static readonly List<string> Failures = [];
    private static int TestsRun;

    [STAThread]
    private static int Main()
    {
        Run("Manifest and default selection", TestManifestAndDefaults);
        Run("Only MUST", TestOnlyMust);
        Run("Selected bundle and collisions", TestBundle);
        Run("Invalid schema", TestInvalidSchema);
        Run("Relative project root rejected", TestRelativeRoot);
        Run("OLE FileDrop data object", TestFileDropDataObject);
        Run("Windows FileDrop clipboard", TestClipboard);
        Run("GUI entry point is STA", TestStaEntryPoint);
        Run("Main window is not always on top", TestMainWindowIsNotTopMost);
        Run("Manual drag in multiple files", TestManualDragIn);
        Run("Cross-project manual file", TestCrossProjectFile);
        Run("Manifest source wins de-duplication", TestManifestManualDeduplication);
        Run("Repeated manual drop de-duplicates", TestRepeatedManualDrop);
        Run("Manifest toggle syncs tray", TestManifestToggleSync);
        Run("Mixed Review Tray clipboard", TestMixedClipboard);
        Run("Mixed Review Tray drag payload", TestMixedDragOut);
        Run("Mixed Review Bundle", TestMixedBundle);
        Run("Remove one manual file", TestRemoveManual);
        Run("Clear Manual preserves Manifest", TestClearManual);
        Run("Directory drop is rejected", TestDirectoryRejection);
        Run("Empty standalone session", TestEmptyStandaloneSession);
        Run("Standalone manual tray", TestStandaloneManualTray);
        Run("Manifest load preserves manual", TestManifestLoadPreservesManual);
        Run("Unload Manifest preserves manual", TestUnloadManifestPreservesManual);
        Run("Invalid Manifest preserves session", TestInvalidManifestPreservesSession);
        Run("Single-row drag payload", TestSingleRowDragPayload);
        Run("Standalone Review Bundle", TestStandaloneBundle);
        Run("Workspace Quick Tray plus three handoffs", TestWorkspace);
        Run("Manifest 1.1 handoff identity", TestHandoffIdentity11);
        Run("Same project different conversations", TestSameProjectDifferentConversations);
        Run("Manifest 1.2 conversation identity", TestManifest12ConversationIdentity);
        Run("Same conversation latest task wins", TestSameConversationLatestWins);
        Run("Conversation replacement preserves tab title", TestConversationReplacementPreservesTitle);
        Run("Conversation explicit rename updates tab title", TestConversationExplicitRename);
        Run("Different conversation IDs stay separate", TestDifferentConversationIdsStaySeparate);
        Run("Conversation replacement resets Manual files", TestConversationReplacementResetsManual);
        Run("Failed conversation replacement retains Manual files", TestFailedConversationReplacementRetainsManual);
        Run("Conversation replay retains Manual files", TestConversationReplayRetainsManual);
        Run("Conversation replacement isolates Manual files", TestConversationReplacementIsolatesManual);
        Run("Conversation replacement updates unread", TestConversationReplacementUpdatesUnread);
        Run("Tab label strips fallback suffix and shows time", TestTabLabelFallbackAndTime);
        Run("Tab label replaces raw conversation ID with project", TestTabLabelRawConversationIdUsesProject);
        Run("Tab label retains human display name", TestTabLabelHumanName);
        Run("Tab label updates on replacement", TestTabLabelReplacementTime);
        Run("Tab label preserves old time on failed replacement", TestTabLabelFailedReplacementTime);
        Run("Tab label invalid time falls back", TestTabLabelInvalidTime);
        Run("Tab label preserves user brackets", TestTabLabelUserBrackets);
        Run("Closed conversation can reappear", TestClosedConversationCanReappear);
        Run("Manifest 1.0 workspace compatibility", TestManifest10WorkspaceCompatibility);
        Run("Duplicate 1.1 does not add a tab", TestDuplicate11);
        Run("Duplicate 1.0 does not add a tab", TestDuplicate10);
        Run("Workspace session isolation", TestWorkspaceSessionIsolation);
        Run("Quick Tray isolation", TestQuickTrayIsolation);
        Run("Unread lifecycle", TestUnreadLifecycle);
        Run("Close Handoff preserves workspace", TestCloseHandoff);
        Run("Tab close protects Quick Tray", TestTabCloseProtectsQuickTray);
        Run("Tab close removes exact Handoff", TestTabCloseRemovesExactHandoff);
        Run("Tab close preserves active Handoff", TestTabClosePreservesActiveHandoff);
        Run("Tab close decrements unread", TestTabCloseDecrementsUnread);
        Run("Tab close active leaves valid session", TestTabCloseActiveLeavesValidSession);
        Run("Tab close allows new project Handoff", TestTabCloseAllowsNewProjectHandoff);
        Run("Tab close allows same identity again", TestTabCloseAllowsSameIdentityAgain);
        Run("Invalid incoming Manifest preserves workspace", TestInvalidIncomingManifest);
        Run("IPC message parsing", TestIpcMessageParsing);
        Run("Handoff Bundle isolation", TestHandoffBundleIsolation);
        Run("Active-session output isolation", TestActiveSessionOutput);
        Run("Request schema validation", TestRequestSchemaValidation);
        Run("Canonical writer emits Request schema 1.0", TestCanonicalWriterSchema);
        Run("Canonical V1 handoff reaches Picker workspace", TestCanonicalV1EndToEnd);
        Run("Legacy evidence request normalizes to V1", TestLegacyEvidenceRequestNormalization);
        Run("Missing schema is blocked and visible", TestMissingSchemaBlockedVisible);
        Run("Unsupported schema is blocked and visible", TestUnsupportedSchemaBlockedVisible);
        Run("Failed Handoff refresh de-duplicates", TestFailedHandoffRefreshDeduplicates);
        Run("Valid Producer Request", TestValidProducerRequest);
        Run("Unique Handoff path", TestUniqueHandoffPath);
        Run("Manifest 1.1 generation", TestProducerManifest11);
        Run("Producer conversation slot stays fixed", TestProducerConversationSlot);
        Run("Producer conversation title stays stable", TestProducerConversationTitleStaysStable);
        Run("Producer explicit conversation rename", TestProducerExplicitConversationRename);
        Run("Producer conversation fallback avoids task title", TestProducerConversationFallbackAvoidsTaskTitle);
        Run("Conversation rename request validation", TestConversationRenameRequestValidation);
        Run("Producer conversation replay", TestProducerConversationReplay);
        Run("Blocked conversation task preserves current slot", TestBlockedConversationTaskPreservesCurrentSlot);
        Run("Invalid conversation ID blocks", TestInvalidConversationIdBlocks);
        Run("Producer relative path resolution", TestProducerRelativePath);
        Run("Producer absolute outside-project path", TestProducerOutsidePath);
        Run("Missing MUST blocks", TestMissingMustBlocks);
        Run("Missing optional warns", TestMissingOptionalWarns);
        Run("Producer path de-duplication", TestProducerPathDeduplication);
        Run("Producer priority conflict resolution", TestProducerPriorityConflict);
        Run("Atomic Manifest write", TestAtomicManifestWrite);
        Run("Producer result success", TestProducerResultSuccess);
        Run("Producer result blocked", TestProducerResultBlocked);
        Run("Producer exit semantics", TestProducerExitSemantics);
        Run("Same Handoff retry", TestSameHandoffRetry);
        Run("Same conversation command replay retains receipt", TestSameConversationCommandReplay);
        Run("Immutable intake survives fixed request replacement", TestImmutableIntakeSurvivesFixedRequestReplacement);
        Run("Conflicting Handoff retry blocks", TestConflictingHandoffRetry);
        Run("Concurrent independent Handoffs", TestConcurrentIndependentHandoffs);
        Run("Same conversation delivery is serialized", TestSameConversationDeliverySerialization);
        Run("Different conversations deliver concurrently", TestDifferentConversationDeliveryConcurrency);
        Run("Failed delivery releases conversation lock", TestFailedDeliveryReleasesConversationLock);
        Run("Picker-existing delivery result", TestPickerExistingDeliveryResult);
        Run("Picker-not-running startup result", TestPickerNotRunningDeliveryResult);
        Run("Canonical handoff hash persists across Request Manifest Result", TestCanonicalHandoffHashPersistence);
        Run("Candidate consistency fence reports deterministic mismatches", TestCandidateConsistencyFence);
        Run("Manifest hash mismatch preserves workspace round", TestManifestHashMismatchPreservesWorkspaceRound);
        Run("Replay validates persisted Request Manifest pair", TestReplayValidatesPersistedPair);
        Run("New Handoff ID replaces round and resets Manual", TestNewHandoffIdReplacesRoundAndResetsManual);
        Run("Final response only", TestFinalResponseOnly);
        Run("Final response plus evidence", TestFinalResponsePlusEvidence);
        Run("Final response conversation replacement", TestFinalResponseConversationReplacement);
        Run("Failed final response replacement preserves current", TestFailedFinalResponseReplacementPreservesCurrent);
        Run("Final response stays isolated by conversation", TestFinalResponseConversationIsolation);
        Run("Final response materializes on demand", TestFinalResponseMaterialization);
        Run("Final response Clipboard drag and bundle", TestFinalResponseClipboardDragAndBundle);
        if (Failures.Count > 0)
        {
            Console.Error.WriteLine($"FAILED: {Failures.Count}");
            foreach (var failure in Failures) Console.Error.WriteLine(failure);
            return 1;
        }
        Console.WriteLine($"ALL TESTS PASSED ({TestsRun}/{TestsRun})");
        return 0;
    }

    private static void Run(string name, Action test)
    {
        TestsRun++;
        try { test(); Console.WriteLine($"PASS {name}"); }
        catch (Exception ex) { Failures.Add($"FAIL {name}: {ex.Message}"); }
    }

    private static void TestManifestAndDefaults()
    {
        var loaded = ManifestLoader.Load(SampleManifest());
        Equal(6, loaded.Items.Count, "item count");
        Equal(3, loaded.Items.Count(i => i.Selected), "default_selected count, including Missing");
        Equal(2, ReviewSelection.ExistingSelected(loaded.Items).Count, "existing default selection count");
        Equal(1, ReviewSelection.MissingSelected(loaded.Items), "missing selected count");
        True(loaded.Items.Take(2).All(i => i.Priority == ReviewPriority.MUST), "MUST sorts first");
        True(!loaded.Items.Single(i => i.RelativePath == "missing.md").Exists, "Missing detected");
    }

    private static void TestOnlyMust()
    {
        var loaded = ManifestLoader.Load(SampleManifest());
        ReviewSelection.SelectOnlyMust(loaded.Items);
        Equal(2, ReviewSelection.ExistingSelected(loaded.Items).Count, "all existing MUST selected");
        True(loaded.Items.Where(i => i.Priority != ReviewPriority.MUST).All(i => !i.Selected), "non-MUST cleared");
    }

    private static void TestBundle()
    {
        var temp = Path.Combine(Path.GetTempPath(), "GPTReviewPickerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(temp, "project", "a")); Directory.CreateDirectory(Path.Combine(temp, "project", "b"));
        File.WriteAllText(Path.Combine(temp, "project", "a", "same.txt"), "A"); File.WriteAllText(Path.Combine(temp, "project", "b", "same.txt"), "B");
        var manifestPath = Path.Combine(temp, "manifest.json");
        var manifest = new { schema_version = "1.0", stage = "TEST", project_root = Path.Combine(temp, "project"), items = new[] {
            new { label="A", path="a/same.txt", priority="MUST", reason="", default_selected=true },
            new { label="B", path="b/same.txt", priority="MUST", reason="", default_selected=true },
            new { label="Missing", path="missing.txt", priority="OPTIONAL", reason="", default_selected=true }
        }};
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
        var loaded = ManifestLoader.Load(manifestPath); var selected = BundleService.CreateSelectedBundle(manifestPath, loaded.Items);
        Equal(2, Directory.GetFiles(selected).Length, "only existing files copied");
        True(File.Exists(Path.Combine(selected, "same.txt")), "first name retained");
        True(File.Exists(Path.Combine(selected, "same (2).txt")), "collision renamed deterministically");
        Directory.Delete(temp, true);
    }

    private static void TestInvalidSchema() => WithManifest(new { schema_version = "2.0", stage = "TEST", project_root = Path.GetTempPath(), items = Array.Empty<object>() }, path => Throws<InvalidDataException>(() => ManifestLoader.Load(path)));
    private static void TestRelativeRoot() => WithManifest(new { schema_version = "1.0", stage = "TEST", project_root = ".", items = Array.Empty<object>() }, path => Throws<InvalidDataException>(() => ManifestLoader.Load(path)));

    private static void TestFileDropDataObject()
    {
        var loaded = ManifestLoader.Load(SampleManifest()); var data = FileDropService.CreateDataObject(loaded.Items);
        True(data.GetDataPresent(DataFormats.FileDrop), "FileDrop format present");
        var paths = (string[])data.GetData(DataFormats.FileDrop)!;
        Equal(2, paths.Length, "two existing defaults in drag payload");
        True(paths.All(Path.IsPathFullyQualified), "drag paths are absolute");
    }

    private static void TestClipboard()
    {
        var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.SelectAllExisting(loaded.Items); FileDropService.CopyToClipboard(loaded.Items);
        var paths = Clipboard.GetFileDropList().Cast<string>().ToArray();
        Equal(5, paths.Length, "five existing files on clipboard"); True(paths.All(File.Exists), "clipboard contains real files");
        True(paths.Any(p => p.EndsWith(".md")) && paths.Any(p => p.EndsWith(".json")) && paths.Any(p => p.EndsWith(".csv")), "clipboard contains different file types");
    }

    private static void TestStaEntryPoint()
    {
        var entryPoint = typeof(MainForm).Assembly.EntryPoint;
        True(entryPoint?.GetCustomAttributes(typeof(STAThreadAttribute), false).Length == 1, "entry point requires STAThread for Clipboard and OLE drag/drop");
    }

    private static void TestMainWindowIsNotTopMost()
    {
        using var form = new MainForm();
        True(!form.TopMost, "main window must use normal z-order so other windows can cover it");
    }

    private static void TestManualDragIn()
    {
        WithTempDirectory(temp =>
        {
            var paths = CreateManualFiles(temp); var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items); var tray = new ReviewTray(loaded.Items);
            var result = tray.AddManualPaths(paths);
            Equal(2, result.Added, "two manual files added"); Equal(2, tray.Items.Count, "two files visible");
            True(tray.Items.All(item => item.Source == ReviewTraySource.MANUAL), "manual source shown");
        });
    }

    private static void TestCrossProjectFile()
    {
        WithTempDirectory(temp =>
        {
            var path = Path.Combine(temp, "outside-project.txt"); File.WriteAllText(path, "outside");
            var loaded = ManifestLoader.Load(SampleManifest()); var tray = new ReviewTray(loaded.Items); tray.AddManualPaths([path]);
            True(!path.StartsWith(loaded.Manifest.ProjectRoot!, StringComparison.OrdinalIgnoreCase), "fixture is outside project_root");
            True(tray.Paths.Contains(Path.GetFullPath(path), StringComparer.OrdinalIgnoreCase), "cross-project file accepted");
        });
    }

    private static void TestManifestManualDeduplication()
    {
        var loaded = ManifestLoader.Load(SampleManifest()); var tray = new ReviewTray(loaded.Items); var manifestPath = loaded.Items.First(item => item.Priority == ReviewPriority.MUST).FullPath;
        tray.AddManualPaths([manifestPath]);
        Equal(1, tray.Items.Count(item => StringComparer.OrdinalIgnoreCase.Equals(item.FullPath, manifestPath)), "one visible entry");
        Equal(ReviewTraySource.MUST, tray.Items.Single(item => StringComparer.OrdinalIgnoreCase.Equals(item.FullPath, manifestPath)).Source, "Manifest source wins");
        loaded.Items.First(item => StringComparer.OrdinalIgnoreCase.Equals(item.FullPath, manifestPath)).Selected = false;
        Equal(ReviewTraySource.MANUAL, tray.Items.Single(item => StringComparer.OrdinalIgnoreCase.Equals(item.FullPath, manifestPath)).Source, "manual source remains independent");
    }

    private static void TestRepeatedManualDrop()
    {
        WithTempDirectory(temp =>
        {
            var path = CreateManualFiles(temp)[0]; var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items); var tray = new ReviewTray(loaded.Items);
            tray.AddManualPaths([path]); var second = tray.AddManualPaths([path.ToUpperInvariant()]);
            Equal(1, tray.Items.Count, "one visible manual entry"); Equal(1, second.AlreadyPresent, "case-insensitive duplicate detected");
        });
    }

    private static void TestManifestToggleSync()
    {
        var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items); var tray = new ReviewTray(loaded.Items); var item = loaded.Items.First(entry => entry.Exists);
        Equal(0, tray.Items.Count, "empty after clear"); item.Selected = true; Equal(1, tray.Items.Count, "added after check"); item.Selected = false; Equal(0, tray.Items.Count, "removed after uncheck");
    }

    private static void TestMixedClipboard()
    {
        WithTempDirectory(temp =>
        {
            var (_, tray) = CreateMixedTray(temp); FileDropService.CopyPathsToClipboard(tray.Paths); var paths = Clipboard.GetFileDropList().Cast<string>().ToArray();
            Equal(5, paths.Length, "mixed clipboard count"); True(paths.All(File.Exists), "mixed clipboard real files");
            Equal(2, paths.Count(path => path.StartsWith(temp, StringComparison.OrdinalIgnoreCase)), "two manual clipboard files");
        });
    }

    private static void TestMixedDragOut()
    {
        WithTempDirectory(temp =>
        {
            var (_, tray) = CreateMixedTray(temp); var data = FileDropService.CreateDataObject(tray.Paths); var paths = (string[])data.GetData(DataFormats.FileDrop)!;
            Equal(5, paths.Length, "mixed drag payload count"); True(paths.All(File.Exists), "mixed drag payload real files");
        });
    }

    private static void TestMixedBundle()
    {
        WithTempDirectory(temp =>
        {
            var (_, tray) = CreateMixedTray(temp); var manifestPath = Path.Combine(temp, "manifest.json"); File.WriteAllText(manifestPath, "{}");
            var bundle = BundleService.CreateReviewBundle(manifestPath, tray.Paths);
            Equal(5, Directory.GetFiles(bundle).Length, "mixed bundle count"); True(tray.Paths.All(File.Exists), "sources remain unchanged");
        });
    }

    private static void TestRemoveManual()
    {
        WithTempDirectory(temp =>
        {
            var path = CreateManualFiles(temp)[0]; var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items); loaded.Items.First(item => item.Exists).Selected = true;
            var tray = new ReviewTray(loaded.Items); tray.AddManualPaths([path]); True(tray.RemoveManual(path), "manual removed");
            Equal(1, tray.Items.Count, "Manifest remains"); True(loaded.Items.Count(item => item.Selected) == 1, "Manifest selection unchanged");
        });
    }

    private static void TestClearManual()
    {
        WithTempDirectory(temp =>
        {
            var (loaded, tray) = CreateMixedTray(temp); tray.ClearManual();
            Equal(3, tray.Items.Count, "three Manifest files remain"); True(tray.Items.All(item => !item.IsManual), "all Manual entries cleared");
            Equal(3, loaded.Items.Count(item => item.Selected), "Manifest selection retained");
        });
    }

    private static void TestDirectoryRejection()
    {
        WithTempDirectory(temp =>
        {
            var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items); var tray = new ReviewTray(loaded.Items); var result = tray.AddManualPaths([temp]);
            Equal(1, result.DirectoriesIgnored, "directory reported"); Equal(0, tray.Items.Count, "directory not expanded");
            True(result.ToStatusMessage(0).Contains("folders are not supported", StringComparison.OrdinalIgnoreCase), "directory rejection has user-facing prompt");
        });
    }

    private static void TestEmptyStandaloneSession()
    {
        var session = new ReviewSession();
        True(!session.HasManifest, "empty session has no Manifest");
        Equal(0, session.Tray.Items.Count, "empty session has an empty usable tray");
    }

    private static void TestStandaloneManualTray()
    {
        WithTempDirectory(temp =>
        {
            var paths = CreateManualFiles(temp); var session = new ReviewSession(); var result = session.Tray.AddManualPaths(paths);
            Equal(2, result.Added, "standalone files added"); Equal(2, session.Tray.Items.Count, "standalone tray count");
            True(session.Tray.Items.All(item => item.Source == ReviewTraySource.MANUAL), "standalone files are Manual");
        });
    }

    private static void TestManifestLoadPreservesManual()
    {
        WithTempDirectory(temp =>
        {
            var manual = CreateManualFiles(temp); var session = new ReviewSession(); session.Tray.AddManualPaths(manual); session.LoadManifest(SampleManifest());
            True(session.HasManifest, "Manifest loaded");
            True(manual.All(path => session.Tray.Paths.Contains(Path.GetFullPath(path), StringComparer.OrdinalIgnoreCase)), "Manual files retained");
            Equal(4, session.Tray.Items.Count, "two Manifest defaults plus two Manual files");
        });
    }

    private static void TestUnloadManifestPreservesManual()
    {
        WithTempDirectory(temp =>
        {
            var manual = CreateManualFiles(temp); var session = new ReviewSession(); session.Tray.AddManualPaths(manual); session.LoadManifest(SampleManifest()); session.UnloadManifest();
            True(!session.HasManifest, "Manifest unloaded"); Equal(2, session.Tray.Items.Count, "only Manual files remain");
            True(session.Tray.Items.All(item => item.Source == ReviewTraySource.MANUAL), "remaining rows are Manual");
        });
    }

    private static void TestInvalidManifestPreservesSession()
    {
        WithTempDirectory(temp =>
        {
            var manual = CreateManualFiles(temp); var session = new ReviewSession(); session.Tray.AddManualPaths(manual); session.LoadManifest(SampleManifest());
            var loadedBefore = session.LoadedReview; var pathsBefore = session.Tray.Paths.ToArray();
            var invalid = Path.Combine(temp, "invalid.json"); File.WriteAllText(invalid, "{ not valid json");
            Throws<InvalidDataException>(() => session.LoadManifest(invalid));
            True(ReferenceEquals(loadedBefore, session.LoadedReview), "current Manifest retained after failed load");
            True(pathsBefore.SequenceEqual(session.Tray.Paths, StringComparer.OrdinalIgnoreCase), "current tray retained after failed load");
        });
    }

    private static void TestSingleRowDragPayload()
    {
        WithTempDirectory(temp =>
        {
            var session = new ReviewSession(); session.Tray.AddManualPaths(CreateManualFiles(temp)); var selected = session.Tray.Items[1];
            var data = FileDropService.CreateDataObject([selected.FullPath]); var paths = (string[])data.GetData(DataFormats.FileDrop)!;
            Equal(1, paths.Length, "single-row payload has one file");
            True(StringComparer.OrdinalIgnoreCase.Equals(selected.FullPath, paths[0]), "single-row payload contains selected row only");
        });
    }

    private static void TestStandaloneBundle()
    {
        WithTempDirectory(temp =>
        {
            var files = CreateManualFiles(temp); var bundleRoot = Path.Combine(temp, "bundle-root");
            var bundle = BundleService.CreateReviewBundleInDirectory(bundleRoot, files);
            Equal(2, Directory.GetFiles(bundle).Length, "standalone bundle contains all Manual files");
            True(files.All(File.Exists), "standalone bundle leaves sources unchanged");
        });
    }

    private static void TestWorkspace()
    {
        var workspace = CreateThreeHandoffWorkspace();
        Equal(3, workspace.HandoffCount, "three handoffs"); Equal(4, workspace.Sessions.Count, "Quick Tray plus handoffs");
        True(workspace.Sessions[0].IsQuickTray, "Quick Tray is first and fixed");
    }

    private static void TestHandoffIdentity11()
    {
        var translation = ManifestLoader.Load(HandoffManifest("translation01"));
        var test = ManifestLoader.Load(HandoffManifest("test01"));
        True(translation.IsSchema11 && test.IsSchema11, "schema 1.1 accepted");
        True(!StringComparer.OrdinalIgnoreCase.Equals(translation.SessionIdentity, test.SessionIdentity), "handoff_id defines distinct identities");
        WithManifest(new { schema_version = "1.1", handoff_id = "", project_root = Path.GetTempPath(), items = Array.Empty<object>() }, path => Throws<InvalidDataException>(() => ManifestLoader.Load(path)));
    }

    private static void TestSameProjectDifferentConversations()
    {
        var workspace = new ReviewWorkspace();
        var first = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true).Session;
        var second = workspace.AddOrUpdateManifest(HandoffManifest("test01"), false, true).Session;
        Equal(first.LoadedReview!.Manifest.ProjectRoot!, second.LoadedReview!.Manifest.ProjectRoot!, "fixture project roots match");
        Equal(2, workspace.HandoffCount, "different handoff_id creates two sessions");
    }

    private static void TestManifest12ConversationIdentity()
    {
        WithTempDirectory(temp =>
        {
            var loaded = ManifestLoader.Load(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4"));
            True(loaded.IsSchema12, "schema 1.2 accepted");
            Equal("conversation:conversation-01", loaded.SessionIdentity, "conversation_id defines identity");
            Equal("task-04", loaded.Manifest.HandoffId!, "handoff_id remains task audit identity");
        });
    }

    private static void TestSameConversationLatestWins()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var first = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4"), false, false);
            var second = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5"), false, true);
            True(first.Created && !second.Created, "later task refreshes existing conversation Session");
            Equal(1, workspace.HandoffCount, "same conversation occupies one Tab");
            True(ReferenceEquals(first.Session, second.Session), "same Session instance retained");
            Equal("task-05", second.Session.LoadedReview!.Manifest.HandoffId!, "latest handoff replaces visible task");
            Equal("Task 5", second.Session.TaskName, "latest task metadata replaces prior task");
        });
    }

    private static void TestConversationReplacementPreservesTitle()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4", "Competition 10"), false, false);
            var updated = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5", "COMP-04B-TM05"), false, true);
            Equal("Competition 10", updated.Session.DisplayName, "later task cannot overwrite conversation title");
            Equal("Task 5", updated.Session.TaskName, "later task still replaces task metadata");
        });
    }

    private static void TestConversationExplicitRename()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4", "Old title"), false, false);
            var renamed = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5", "Competition 10", true), false, true);
            Equal("Competition 10", renamed.Session.DisplayName, "explicit rename updates conversation title");
        });
    }

    private static void TestDifferentConversationIdsStaySeparate()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-a", "task-a", "Task A"), false, true);
            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-b", "task-b", "Task B"), false, true);
            Equal(2, workspace.HandoffCount, "different conversations retain separate Tabs");
        });
    }

    private static void TestConversationReplacementResetsManual()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var session = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4"), false, false).Session;
            var manual = CreateManualFiles(temp);
            session.Tray.AddManualPaths(manual);

            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5"), false, true);

            Equal("task-05", session.LoadedReview!.Manifest.HandoffId!, "new handoff is active");
            Equal(0, session.Tray.ManualCount, "Manual files reset for the new review round");
            True(session.Tray.Items.All(item => item.Source != ReviewTraySource.MANUAL), "new tray has no prior Manual sources");
            True(session.Tray.Items.All(item => !manual.Contains(item.FullPath, StringComparer.OrdinalIgnoreCase)), "prior Manual paths are absent");
            Equal("Task 5", session.Tray.Items.Single().ManifestItem!.Reason, "new Manifest source is present");

            var payload = FileDropService.CreateDataObject(session.Tray.Items);
            var payloadPaths = (string[])payload.GetData(DataFormats.FileDrop)!;
            True(payloadPaths.All(path => !manual.Contains(path, StringComparer.OrdinalIgnoreCase)), "drag and clipboard payload excludes prior Manual files");
            var bundle = BundleService.CreateHandoffBundle(session.LoadedReview, session.Tray.Items);
            var bundleFiles = Directory.GetFiles(bundle).Select(Path.GetFileName).ToArray();
            True(bundleFiles.All(name => !manual.Select(Path.GetFileName).Contains(name, StringComparer.OrdinalIgnoreCase)), "selected Bundle excludes prior Manual files");
        });
    }

    private static void TestFailedConversationReplacementRetainsManual()
    {
        WithTempDirectory(temp =>
        {
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", conversationId: "conversation-01"));
            var workspace = new ReviewWorkspace();
            var session = workspace.AddOrUpdateManifest(first.Result.ManifestPath!, false, false).Session;
            var manual = CreateManualFiles(temp);
            session.Tray.AddManualPaths(manual);

            var failed = HandoffProducer.Generate(WriteProducerRequest(temp, "task-05", [ProducerItem("missing.txt", "MUST", true)], conversationId: "conversation-01"));

            True(!failed.CanDeliver, "failed replacement is not deliverable");
            Equal("task-04", session.LoadedReview!.Manifest.HandoffId!, "last-known-good handoff remains active");
            Equal(manual.Length, session.Tray.ManualCount, "failed replacement retains Manual sources");
            True(manual.All(path => session.Tray.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)), "all prior Manual paths remain available");
        });
    }

    private static void TestConversationReplayRetainsManual()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var path = WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4");
            var session = workspace.AddOrUpdateManifest(path, false, false).Session;
            var manual = CreateManualFiles(temp);
            session.Tray.AddManualPaths(manual);

            workspace.AddOrUpdateManifest(path, false, true);

            Equal(manual.Length, session.Tray.ManualCount, "identical handoff replay retains Manual sources");
            True(manual.All(path => session.Tray.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)), "replay preserves every Manual path");
        });
    }

    private static void TestConversationReplacementIsolatesManual()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var sessionA = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-a", "task-a1", "Task A1"), false, false).Session;
            var sessionB = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-b", "task-b1", "Task B1"), false, false).Session;
            var manualA = CreateManualFiles(Path.Combine(temp, "manual-a"));
            var manualB = CreateManualFiles(Path.Combine(temp, "manual-b"));
            sessionA.Tray.AddManualPaths(manualA);
            sessionB.Tray.AddManualPaths(manualB);

            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-a", "task-a2", "Task A2"), false, true);

            Equal(0, sessionA.Tray.ManualCount, "replacement clears only conversation A Manual sources");
            Equal(manualB.Length, sessionB.Tray.ManualCount, "conversation B Manual sources remain intact");
            True(manualB.All(path => sessionB.Tray.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)), "conversation B retains every Manual path");
        });
    }

    private static void TestConversationReplacementUpdatesUnread()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var session = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4"), false, false).Session;
            True(!session.Unread, "initial task starts read");
            workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5"), false, true);
            True(session.Unread, "background replacement becomes unread");
            Equal(1, workspace.UnreadCount, "replacement contributes one unread conversation");
        });
    }

    private static void TestTabLabelFallbackAndTime()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteConversationManifest(temp, "conversation-example-01", "handoff-a", "Task A", "Example Competition Tool [conversa]");
            var session = ReviewSession.CreateHandoff(ManifestLoader.Load(path), false);
            Equal("Example Competition Tool [21:30]", ReviewTabLabel.Format(session), "fallback suffix is replaced by time");
        });
    }

    private static void TestTabLabelRawConversationIdUsesProject()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "00000000-0000-4000-8000-000000000001";
            var path = WriteConversationManifest(temp, conversationId, "handoff-a", "Task A", conversationId);
            var session = ReviewSession.CreateHandoff(ManifestLoader.Load(path), false);
            Equal("Conversation Tests [21:30]", ReviewTabLabel.Format(session), "raw conversation ID falls back to project name and time");
        });
    }

    private static void TestTabLabelHumanName()
    {
        WithTempDirectory(temp =>
        {
            var session = ReviewSession.CreateHandoff(ManifestLoader.Load(WriteConversationManifest(temp, "human-conversation", "handoff-a", "Task A", "Competition 10")), false);
            Equal("Competition 10 [21:30]", ReviewTabLabel.Format(session), "human display name retained");
        });
    }

    private static void TestTabLabelReplacementTime()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var first = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "time-conversation", "handoff-a", "Task A", "Project"), true, false).Session;
            Equal("Project [21:30]", ReviewTabLabel.Format(first), "initial handoff time");
            var secondPath = WriteConversationManifest(temp, "time-conversation", "handoff-b", "Task B", "Project");
            var json = JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(secondPath))!;
            json.GeneratedAt = "2030-01-03T21:10:00+08:00";
            File.WriteAllText(secondPath, JsonSerializer.Serialize(json));
            workspace.AddOrUpdateManifest(secondPath, true, false);
            Equal("Project [21:10]", ReviewTabLabel.Format(first), "replacement updates time");
        });
    }

    private static void TestTabLabelFailedReplacementTime()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var first = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "failed-time", "handoff-a", "Task A", "Project"), true, false).Session;
            var invalid = WriteConversationManifest(temp, "failed-time", "handoff-b", "Task B", "Project");
            var manifest = JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(invalid))!;
            manifest.GeneratedAt = "not-a-time";
            manifest.Items = null;
            File.WriteAllText(invalid, JsonSerializer.Serialize(manifest));
            Throws<InvalidDataException>(() => workspace.AddOrUpdateManifest(invalid, true, false));
            Equal("Project [21:30]", ReviewTabLabel.Format(first), "failed replacement preserves old time");
        });
    }

    private static void TestTabLabelInvalidTime()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteConversationManifest(temp, "invalid-time", "handoff-a", "Task A", "Project");
            var manifest = JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(path))!;
            manifest.GeneratedAt = "not-a-time";
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
            var session = ReviewSession.CreateHandoff(ManifestLoader.Load(path), false);
            Equal("Project", ReviewTabLabel.Format(session), "invalid time omits suffix");
        });
    }

    private static void TestTabLabelUserBrackets()
    {
        WithTempDirectory(temp =>
        {
            var session = ReviewSession.CreateHandoff(ManifestLoader.Load(WriteConversationManifest(temp, "bracket-conversation", "handoff-a", "Task A", "Research [Pilot]")), false);
            Equal("Research [Pilot] [21:30]", ReviewTabLabel.Format(session), "user brackets retained");
        });
    }

    private static void TestClosedConversationCanReappear()
    {
        WithTempDirectory(temp =>
        {
            var workspace = new ReviewWorkspace();
            var first = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-04", "Task 4"), false, true).Session;
            True(workspace.CloseHandoff(first), "conversation closes");
            var next = workspace.AddOrUpdateManifest(WriteConversationManifest(temp, "conversation-01", "task-05", "Task 5"), false, true);
            True(next.Created, "later task recreates closed conversation Tab");
            Equal(1, workspace.HandoffCount, "recreated conversation remains unique");
        });
    }

    private static void TestManifest10WorkspaceCompatibility()
    {
        var workspace = new ReviewWorkspace(); var result = workspace.AddOrUpdateManifest(HandoffManifest("example-stage-03"), true, false);
        True(!result.Session.LoadedReview!.IsSchema11, "schema 1.0 retained");
        True(result.Session.Identity.StartsWith("manifest:", StringComparison.Ordinal), "1.0 identity uses canonical Manifest path");
        Equal("ExampleStage03", result.Session.DisplayName, "1.0 stage display fallback");
    }

    private static void TestDuplicate11()
    {
        var workspace = new ReviewWorkspace(); var first = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true);
        first.Session.Tray.AddManualPaths([ManualHandoffFile()]);
        var duplicate = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true);
        Equal(1, workspace.HandoffCount, "one 1.1 session"); True(!duplicate.Created, "duplicate detected");
        Equal(1, duplicate.Session.Tray.ManualCount, "duplicate refresh preserves Manual files");
    }

    private static void TestDuplicate10()
    {
        var workspace = new ReviewWorkspace(); var path = HandoffManifest("legacy03");
        workspace.AddOrUpdateManifest(path, false, true); var duplicate = workspace.AddOrUpdateManifest(Path.GetFullPath(path), false, true);
        Equal(1, workspace.HandoffCount, "one 1.0 session"); True(!duplicate.Created, "canonical path duplicate detected");
    }

    private static void TestWorkspaceSessionIsolation()
    {
        var workspace = new ReviewWorkspace();
        var translation = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true).Session;
        var test = workspace.AddOrUpdateManifest(HandoffManifest("test01"), false, true).Session;
        var testPaths = test.Tray.Paths.ToArray(); var testSelection = test.LoadedReview!.Items.Select(item => item.Selected).ToArray();
        ReviewSelection.Clear(translation.LoadedReview!.Items); translation.Tray.AddManualPaths([ManualHandoffFile()]); translation.Tray.ClearManual();
        True(testPaths.SequenceEqual(test.Tray.Paths, StringComparer.OrdinalIgnoreCase), "other tray unchanged");
        True(testSelection.SequenceEqual(test.LoadedReview.Items.Select(item => item.Selected)), "other selection unchanged");
    }

    private static void TestQuickTrayIsolation()
    {
        var workspace = new ReviewWorkspace(); workspace.QuickTray.Tray.AddManualPaths([ManualHandoffFile()]);
        workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true);
        Equal(1, workspace.QuickTray.Tray.Items.Count, "Quick Tray file retained");
        True(workspace.QuickTray.Tray.Items.All(item => item.IsManual), "Quick Tray remains standalone");
    }

    private static void TestUnreadLifecycle()
    {
        var workspace = new ReviewWorkspace(); var session = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true).Session;
        True(session.Unread, "new background Handoff unread"); Equal(1, workspace.UnreadCount, "unread count");
        workspace.Activate(session); True(!session.Unread, "activation marks read"); Equal(0, workspace.UnreadCount, "unread cleared");
    }

    private static void TestCloseHandoff()
    {
        var workspace = CreateThreeHandoffWorkspace(); var closing = workspace.Handoffs[1]; workspace.Activate(closing);
        True(workspace.CloseHandoff(closing), "handoff closes"); Equal(2, workspace.HandoffCount, "only one removed");
        True(ReferenceEquals(workspace.ActiveSession, workspace.QuickTray), "closing active returns to Quick Tray");
        True(workspace.Sessions.Contains(workspace.QuickTray), "Quick Tray retained");
    }

    private static void TestTabCloseProtectsQuickTray()
    {
        var workspace = new ReviewWorkspace();
        var bounds = new Rectangle(0, 0, 120, 28);
        True(!HandoffTabClose.CanClose(workspace.QuickTray), "Quick Tray has no close target");
        Equal(Rectangle.Empty, HandoffTabClose.GetCloseBounds(workspace.QuickTray, bounds, 96), "Quick Tray close bounds empty");
        True(!workspace.CloseHandoff(workspace.QuickTray), "Quick Tray close rejected");
        True(ReferenceEquals(workspace.ActiveSession, workspace.QuickTray), "Quick Tray remains active");
    }

    private static void TestTabCloseRemovesExactHandoff()
    {
        var workspace = CreateThreeHandoffWorkspace();
        var first = workspace.Handoffs[0]; var closing = workspace.Handoffs[1]; var last = workspace.Handoffs[2];
        var bounds = new Rectangle(140, 4, 160, 30); var closeBounds = HandoffTabClose.GetCloseBounds(closing, bounds, 144);
        True(HandoffTabClose.IsCloseHit(closing, bounds, new Point(closeBounds.Left + 1, closeBounds.Top + 1), 144), "DPI close target hit");
        True(workspace.CloseHandoff(closing), "target Handoff closes");
        Equal(2, workspace.HandoffCount, "exactly one Handoff removed");
        True(workspace.Handoffs.Contains(first) && workspace.Handoffs.Contains(last), "adjacent Handoffs retained");
        True(!workspace.Handoffs.Contains(closing), "target Handoff removed");
    }

    private static void TestTabClosePreservesActiveHandoff()
    {
        var workspace = new ReviewWorkspace();
        var active = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), true, false).Session;
        var closing = workspace.AddOrUpdateManifest(HandoffManifest("test01"), false, true).Session;
        True(workspace.CloseHandoff(closing), "non-active Handoff closes");
        True(ReferenceEquals(active, workspace.ActiveSession), "active Handoff preserved");
        True(workspace.Handoffs.Contains(active), "other Handoff retained");
    }

    private static void TestTabCloseDecrementsUnread()
    {
        var workspace = CreateThreeHandoffWorkspace(); var closing = workspace.Handoffs[1];
        Equal(3, workspace.UnreadCount, "initial unread count");
        True(workspace.CloseHandoff(closing), "unread Handoff closes");
        Equal(2, workspace.UnreadCount, "unread count decremented");
    }

    private static void TestTabCloseActiveLeavesValidSession()
    {
        var workspace = CreateThreeHandoffWorkspace(); var closing = workspace.Handoffs[1]; workspace.Activate(closing);
        True(workspace.CloseHandoff(closing), "active Handoff closes");
        True(workspace.Sessions.Contains(workspace.ActiveSession), "active session remains valid");
        True(ReferenceEquals(workspace.QuickTray, workspace.ActiveSession), "Quick Tray selected after active close");
    }

    private static void TestTabCloseAllowsNewProjectHandoff()
    {
        var workspace = new ReviewWorkspace(); var first = workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true).Session;
        True(workspace.CloseHandoff(first), "first project Handoff closes");
        var next = workspace.AddOrUpdateManifest(HandoffManifest("test01"), false, true);
        True(next.Created, "same project with new handoff_id reappears");
        Equal(1, workspace.HandoffCount, "new Handoff is present");
    }

    private static void TestTabCloseAllowsSameIdentityAgain()
    {
        var workspace = new ReviewWorkspace(); var path = HandoffManifest("translation01");
        var first = workspace.AddOrUpdateManifest(path, false, true).Session;
        True(workspace.CloseHandoff(first), "original identity closes");
        var replay = workspace.AddOrUpdateManifest(path, false, true);
        True(replay.Created, "same identity can reappear after close");
        Equal(1, workspace.HandoffCount, "reappeared identity is unique");
    }

    private static void TestInvalidIncomingManifest()
    {
        var workspace = new ReviewWorkspace(); workspace.QuickTray.Tray.AddManualPaths([ManualHandoffFile()]);
        workspace.AddOrUpdateManifest(HandoffManifest("translation01"), false, true);
        WithManifest(new { schema_version = "9.0", project_root = Path.GetTempPath(), items = Array.Empty<object>() }, path => Throws<InvalidDataException>(() => workspace.AddOrUpdateManifest(path, false, true)));
        Equal(1, workspace.HandoffCount, "valid handoff retained"); Equal(1, workspace.QuickTray.Tray.Items.Count, "Quick Tray retained");
    }

    private static void TestIpcMessageParsing()
    {
        var activate = PickerIpcMessage.Parse("{\"type\":\"activate\"}"); Equal(PickerIpcMessage.ActivateType, activate.Type, "activate parsed");
        var path = HandoffManifest("translation01"); var open = PickerIpcMessage.Parse(PickerIpcMessage.OpenManifest(path).ToJson());
        Equal(PickerIpcMessage.OpenManifestType, open.Type, "open parsed"); Equal(Path.GetFullPath(path), open.Path!, "open path canonicalized");
        var result = PickerIpcMessage.Parse(PickerIpcMessage.OpenResult(path).ToJson());
        Equal(PickerIpcMessage.OpenResultType, result.Type, "open result parsed"); Equal(Path.GetFullPath(path), result.Path!, "result path canonicalized");
        True(PickerIpcResponse.Parse(PickerIpcResponse.Accept().ToJson()).Accepted, "accepted ACK parsed");
        True(!PickerIpcResponse.Parse(PickerIpcResponse.Reject("invalid").ToJson()).Accepted, "rejected ACK parsed");
        Throws<InvalidDataException>(() => PickerIpcMessage.Parse("{\"type\":\"unknown\"}"));
        Throws<InvalidDataException>(() => PickerIpcMessage.Parse("{\"type\":\"open_manifest\"}"));
    }

    private static void TestHandoffBundleIsolation()
    {
        var translation = ManifestLoader.Load(IsolationHandoffManifest("translation01")); var test = ManifestLoader.Load(IsolationHandoffManifest("test01"));
        var first = BundleService.CreateHandoffBundle(translation, ReviewSelection.ExistingSelected(translation.Items).Select(item => item.FullPath));
        var second = BundleService.CreateHandoffBundle(test, ReviewSelection.ExistingSelected(test.Items).Select(item => item.FullPath));
        True(!StringComparer.OrdinalIgnoreCase.Equals(first, second), "bundle directories differ");
        Equal(2, Directory.GetFiles(first).Length, "first bundle retained"); Equal(2, Directory.GetFiles(second).Length, "second bundle complete");
    }

    private static void TestActiveSessionOutput()
    {
        var workspace = new ReviewWorkspace(); var translation = workspace.AddOrUpdateManifest(IsolationHandoffManifest("translation01"), true, false).Session;
        var test = workspace.AddOrUpdateManifest(IsolationHandoffManifest("test01"), true, false).Session;
        True(ReferenceEquals(workspace.ActiveSession, test), "test session active");
        var data = FileDropService.CreateDataObject(workspace.ActiveSession.Tray.Paths); var paths = (string[])data.GetData(DataFormats.FileDrop)!;
        True(paths.All(path => test.Tray.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)), "payload only contains active session");
        True(paths.All(path => !translation.Tray.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)), "hidden tab files excluded");
    }

    private static void TestRequestSchemaValidation()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "invalid-schema", schemaVersion: "9.0");
            var generation = HandoffProducer.Generate(request);
            Equal(HandoffProducerStatuses.Blocked, generation.Result.Status, "invalid request blocked");
            True(generation.Result.Errors.Any(error => error.Contains("schema_version", StringComparison.Ordinal)), "schema error reported");
        });
    }

    private static void TestCanonicalWriterSchema()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteProducerRequest(temp, "canonical-writer");
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Equal(HandoffRequestContract.SchemaVersion, json.RootElement.GetProperty("schema_version").GetString()!, "canonical schema authority");
            True(HandoffProducer.Generate(path).CanDeliver, "canonical request accepted");
        });
    }

    private static void TestCanonicalV1EndToEnd()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteProducerRequest(temp, "canonical-end-to-end", conversationId: "canonical-end-to-end", finalResponse: "Canonical result");
            var workspace = new ReviewWorkspace();
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(path, manifest => {
                workspace.AddOrUpdateManifest(manifest, false, true);
                return PickerDeliveryOutcome.ExistingInstance();
            }), "canonical pipeline delivered");
            var session = workspace.Handoffs.Single();
            Equal("canonical-end-to-end", session.LoadedReview!.Manifest.HandoffId!, "Picker received canonical handoff");
            Equal("Canonical result", session.LoadedReview.Manifest.FinalResponse!, "canonical response reached Picker");
            Equal(HandoffProducerStatuses.Delivered, ReadProducerResult(Path.Combine(temp, ".gpt-review", "conversations", "canonical-end-to-end", "result.json")).Status, "delivery receipt persisted");
        });
    }

    private static void TestLegacyEvidenceRequestNormalization()
    {
        WithTempDirectory(temp =>
        {
            File.WriteAllText(Path.Combine(temp, "core.txt"), "core");
            var path = Path.Combine(temp, "legacy-request.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new {
                handoff_id = "legacy-evidence", conversation_id = "legacy-conversation", project_name = "Legacy Project",
                task_name = "Legacy Task", project_root = temp, final_response = "Exact legacy response",
                evidence = new[] { new { label = "Core", path = "core.txt", priority = "SHOULD", reason = "Legacy evidence", default_selected = true } }
            }));
            string? deliveredPath = null;
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(path, artifact => { deliveredPath = artifact; return PickerDeliveryOutcome.ExistingInstance(); }), "legacy request delivered");
            var loaded = ManifestLoader.Load(deliveredPath!);
            Equal("legacy-evidence", loaded.Manifest.HandoffId!, "legacy identity retained");
            Equal(ReviewPriority.RECOMMENDED, loaded.Items.Single(item => !item.IsVirtual).Priority, "SHOULD maps to RECOMMENDED");
            True(ReadProducerResult(Path.Combine(temp, ".gpt-review", "conversations", "legacy-conversation", "result.json")).Warnings.Any(w => w.Contains("LEGACY_REQUEST_NORMALIZED", StringComparison.Ordinal)), "normalization warning recorded");
        });
    }

    private static void TestMissingSchemaBlockedVisible()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteProducerRequest(temp, "missing-schema", requestPath: Path.Combine(temp, "missing-schema.json"));
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path))!;
            json.Remove("schema_version");
            File.WriteAllText(path, JsonSerializer.Serialize(json));
            var workspace = new ReviewWorkspace();
            Equal(HandoffProducerExitCodes.ValidationBlocked, HandoffProducerCommand.Run(path, result => { workspace.AddOrUpdateFailure(result, false, true); return PickerDeliveryOutcome.ExistingInstance(); }), "missing schema blocked");
            Equal(1, workspace.HandoffCount, "blocked Handoff visible");
            True(workspace.Handoffs.Single().IsFailure, "visible entry is failure-only");
            Contains(workspace.Handoffs.Single().FailedHandoff!.Result.Errors, "schema_version", "missing schema reason visible");
        });
    }

    private static void TestUnsupportedSchemaBlockedVisible()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteProducerRequest(temp, "unsupported-visible", schemaVersion: "999");
            var workspace = new ReviewWorkspace();
            Equal(HandoffProducerExitCodes.ValidationBlocked, HandoffProducerCommand.Run(path, result => { workspace.AddOrUpdateFailure(result, false, true); return PickerDeliveryOutcome.ExistingInstance(); }), "unsupported schema blocked");
            Contains(workspace.Handoffs.Single().FailedHandoff!.Result.Errors, "999", "unsupported schema reason visible");
        });
    }

    private static void TestFailedHandoffRefreshDeduplicates()
    {
        WithTempDirectory(temp =>
        {
            var path = WriteProducerRequest(temp, "failure-refresh", schemaVersion: "999", conversationId: "failure-refresh-conversation");
            string? resultPath = null;
            HandoffProducerCommand.Run(path, result => { resultPath = result; return PickerDeliveryOutcome.ExistingInstance(); });
            var workspace = new ReviewWorkspace();
            var first = workspace.AddOrUpdateFailure(resultPath!, false, true);
            var second = workspace.AddOrUpdateFailure(resultPath!, false, true);
            True(first.Created && !second.Created, "failure refresh updates existing entry");
            Equal(1, workspace.HandoffCount, "one failure entry retained");
        });
    }

    private static void TestValidProducerRequest()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "valid-request");
            var generation = HandoffProducer.Generate(request);
            True(generation.CanDeliver, "valid request can deliver");
            Equal(HandoffProducerStatuses.ManifestCreated, generation.Result.Status, "Manifest created status");
            True(File.Exists(generation.Result.ManifestPath), "Manifest exists");
            True(File.Exists(generation.Result.RequestPath), "request snapshot exists");
            True(File.Exists(generation.Result.ResultPath), "result exists");
        });
    }

    private static void TestUniqueHandoffPath()
    {
        WithTempDirectory(temp =>
        {
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "handoff-a"));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "handoff-b"));
            True(!StringComparer.OrdinalIgnoreCase.Equals(first.Result.ManifestPath, second.Result.ManifestPath), "unique Manifest paths");
            True(first.Result.ManifestPath!.Contains(Path.Combine("handoffs", "handoff-a"), StringComparison.OrdinalIgnoreCase), "first identity directory");
            True(second.Result.ManifestPath!.Contains(Path.Combine("handoffs", "handoff-b"), StringComparison.OrdinalIgnoreCase), "second identity directory");
        });
    }

    private static void TestProducerManifest11()
    {
        WithTempDirectory(temp =>
        {
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "manifest-11"));
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            True(loaded.IsSchema11, "generated Manifest is 1.1");
            Equal("manifest-11", loaded.Manifest.HandoffId!, "handoff_id retained");
            Equal("Producer manifest-11", loaded.DisplayName, "display name retained");
        });
    }

    private static void TestProducerConversationSlot()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "conversation-01";
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", taskName: "Task 4", conversationId: conversationId));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "task-05", taskName: "Task 5", conversationId: conversationId));
            Equal(first.Result.ManifestPath!, second.Result.ManifestPath!, "conversation Manifest path stays fixed");
            True(second.Result.ManifestPath!.Contains(Path.Combine("conversations", conversationId), StringComparison.OrdinalIgnoreCase), "conversation slot path used");
            Equal(3, Directory.GetFiles(Path.GetDirectoryName(second.Result.ManifestPath!)!).Length, "conversation slot contains only three JSON audit files");
            var loaded = ManifestLoader.Load(second.Result.ManifestPath!);
            True(loaded.IsSchema12, "conversation Producer emits schema 1.2");
            Equal("task-05", loaded.Manifest.HandoffId!, "latest task handoff stored");
            Equal("Task 5", loaded.TaskName, "latest task metadata stored");
            Equal(conversationId, second.Result.ConversationId!, "Result reports conversation identity");
        });
    }

    private static void TestProducerConversationTitleStaysStable()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "conversation-01";
            HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", taskName: "Task 4", conversationId: conversationId, displayName: "Competition 10"));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "task-05", taskName: "Task 5", conversationId: conversationId, displayName: "COMP-04B-TM05"));
            var loaded = ManifestLoader.Load(second.Result.ManifestPath!);
            Equal("Competition 10", loaded.DisplayName, "stored conversation title remains stable");
            Equal("Task 5", loaded.TaskName, "stored task metadata advances");
            True(second.Result.Warnings.Any(warning => warning.Contains("display_name was ignored", StringComparison.Ordinal)), "ignored title replacement is reported");
        });
    }

    private static void TestProducerExplicitConversationRename()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "conversation-01";
            HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", conversationId: conversationId, displayName: "Old title"));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "task-05", conversationId: conversationId, displayName: "Competition 10", renameConversation: true));
            var loaded = ManifestLoader.Load(second.Result.ManifestPath!);
            Equal("Competition 10", loaded.DisplayName, "explicit Producer rename is stored");
            True(loaded.Manifest.RenameConversation is true, "Manifest carries explicit rename intent");
        });
    }

    private static void TestProducerConversationFallbackAvoidsTaskTitle()
    {
        WithTempDirectory(temp =>
        {
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", taskName: "COMP-04B-TM04", conversationId: "conversation-01", omitDisplayName: true));
            Equal("Producer Tests [conversa]", ManifestLoader.Load(generation.Result.ManifestPath!).DisplayName, "stable fallback uses project and conversation identity");
        });
    }

    private static void TestConversationRenameRequestValidation()
    {
        WithTempDirectory(temp =>
        {
            var noConversation = HandoffProducer.Generate(WriteProducerRequest(temp, "rename-no-conversation", renameConversation: true));
            True(!noConversation.CanDeliver, "rename without conversation is blocked");
            True(noConversation.Result.Errors.Any(error => error.Contains("requires conversation_id", StringComparison.Ordinal)), "missing conversation identity is reported");

            var noTitle = HandoffProducer.Generate(WriteProducerRequest(temp, "rename-no-title", conversationId: "conversation-01", renameConversation: true, omitDisplayName: true));
            True(!noTitle.CanDeliver, "rename without title is blocked");
            True(noTitle.Result.Errors.Any(error => error.Contains("requires display_name", StringComparison.Ordinal)), "missing replacement title is reported");
        });
    }

    private static void TestProducerConversationReplay()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "task-04", conversationId: "conversation-01");
            var first = HandoffProducer.Generate(request); var replay = HandoffProducer.Generate(request);
            True(!first.Result.Replayed && replay.Result.Replayed, "same conversation task replays safely");
            Equal(first.Result.ManifestPath!, replay.Result.ManifestPath!, "replay uses same fixed slot");
        });
    }

    private static void TestBlockedConversationTaskPreservesCurrentSlot()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "conversation-01";
            var current = HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", conversationId: conversationId));
            var currentRequest = File.ReadAllText(current.Result.RequestPath!); var currentManifest = File.ReadAllText(current.Result.ManifestPath!);
            var currentResult = File.ReadAllText(current.Result.ResultPath!);
            var missingItems = new[] { ProducerItem("missing.txt", "MUST", true) };
            var input = WriteProducerRequest(temp, "task-05", missingItems, conversationId: conversationId);
            var blocked = HandoffProducer.Generate(input);
            True(!blocked.CanDeliver, "invalid later task is blocked");
            Equal(Path.GetFullPath(input), blocked.Result.RequestPath!, "blocked task reports its input Request");
            Equal(currentRequest, File.ReadAllText(current.Result.RequestPath!), "current Request remains intact");
            Equal(currentManifest, File.ReadAllText(current.Result.ManifestPath!), "current Manifest remains intact");
            Equal(currentResult, File.ReadAllText(current.Result.ResultPath!), "current Result remains intact");
        });
    }

    private static void TestInvalidConversationIdBlocks()
    {
        WithTempDirectory(temp =>
        {
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "task-04", conversationId: "invalid conversation/id"));
            True(!generation.CanDeliver, "invalid conversation ID blocks delivery");
            True(generation.Result.Errors.Any(error => error.Contains("conversation_id", StringComparison.Ordinal)), "conversation validation reported");
        });
    }

    private static void TestProducerRelativePath()
    {
        WithTempDirectory(temp =>
        {
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "relative-path"));
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            Equal("core.txt", loaded.Items.Single().RelativePath, "relative path retained");
            Equal(Path.Combine(temp, "core.txt"), loaded.Items.Single().FullPath, "relative path resolved from project root");
        });
    }

    private static void TestProducerOutsidePath()
    {
        WithTempDirectory(temp =>
        {
            var project = Directory.CreateDirectory(Path.Combine(temp, "project")).FullName;
            var outside = Path.Combine(temp, "outside.txt"); File.WriteAllText(outside, "outside");
            var items = new[] { ProducerItem(outside, "MUST", true) };
            var generation = HandoffProducer.Generate(WriteProducerRequest(project, "outside-path", items));
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            Equal(Path.GetFullPath(outside), loaded.Items.Single().FullPath, "outside absolute path retained");
            True(Path.IsPathFullyQualified(loaded.Items.Single().RelativePath), "outside path stays absolute in Manifest");
        });
    }

    private static void TestMissingMustBlocks()
    {
        WithTempDirectory(temp =>
        {
            var items = new[] { ProducerItem("missing.txt", "MUST", true) };
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "missing-must", items));
            True(!generation.CanDeliver, "Missing MUST cannot deliver");
            Equal(HandoffProducerStatuses.Blocked, generation.Result.Status, "Missing MUST blocked status");
            True(generation.Result.Errors.Any(error => error.Contains("Missing MUST", StringComparison.Ordinal)), "Missing MUST reported");
            True(!File.Exists(generation.Result.ManifestPath), "blocked Manifest not exposed");
        });
    }

    private static void TestMissingOptionalWarns()
    {
        WithTempDirectory(temp =>
        {
            var core = Path.Combine(temp, "core.txt"); File.WriteAllText(core, "core");
            var items = new[] { ProducerItem("core.txt", "MUST", true), ProducerItem("missing.log", "OPTIONAL", false) };
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "missing-optional", items));
            True(generation.CanDeliver, "Missing optional allows delivery");
            Equal(1, generation.Result.Warnings.Count, "one Missing optional warning");
            Equal(2, ManifestLoader.Load(generation.Result.ManifestPath!).Items.Count, "Missing optional retained in Manifest");
        });
    }

    private static void TestProducerPathDeduplication()
    {
        WithTempDirectory(temp =>
        {
            var core = Path.Combine(temp, "core.txt"); File.WriteAllText(core, "core");
            var items = new[] { ProducerItem("core.txt", "MUST", true), ProducerItem(core.ToUpperInvariant(), "MUST", true) };
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "dedup", items));
            Equal(1, ManifestLoader.Load(generation.Result.ManifestPath!).Items.Count, "canonical duplicate collapsed");
        });
    }

    private static void TestProducerPriorityConflict()
    {
        WithTempDirectory(temp =>
        {
            var core = Path.Combine(temp, "core.txt"); File.WriteAllText(core, "core");
            var items = new[] { ProducerItem("core.txt", "OPTIONAL", false), ProducerItem(core, "MUST", true) };
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "priority", items));
            var item = ManifestLoader.Load(generation.Result.ManifestPath!).Items.Single();
            Equal(ReviewPriority.MUST, item.Priority, "higher priority wins");
            True(item.Selected, "winning default selection retained");
        });
    }

    private static void TestAtomicManifestWrite()
    {
        WithTempDirectory(temp =>
        {
            var generation = HandoffProducer.Generate(WriteProducerRequest(temp, "atomic"));
            var handoffDirectory = Path.GetDirectoryName(generation.Result.ManifestPath!)!;
            True(File.Exists(Path.Combine(handoffDirectory, "manifest.json")), "final Manifest exists");
            Equal(0, Directory.GetFiles(handoffDirectory, "*.tmp.*").Length, "no temporary files exposed");
        });
    }

    private static void TestProducerResultSuccess()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "result-success");
            var exitCode = HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.ExistingInstance());
            Equal(HandoffProducerExitCodes.Delivered, exitCode, "success exit code");
            var result = ReadProducerResult(ProducerResultPath(temp, "result-success"));
            Equal(HandoffProducerStatuses.Delivered, result.Status, "delivered result status");
        });
    }

    private static void TestProducerResultBlocked()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "result-blocked", [ProducerItem("missing.txt", "MUST", true)]);
            string? deliveredResult = null;
            var exitCode = HandoffProducerCommand.Run(request, result => { deliveredResult = result; return PickerDeliveryOutcome.ExistingInstance(); });
            Equal(HandoffProducerExitCodes.ValidationBlocked, exitCode, "blocked exit code");
            var result = ReadProducerResult(ProducerResultPath(temp, "result-blocked"));
            Equal(HandoffProducerStatuses.Blocked, result.Status, "blocked result status");
            Equal(result.ResultPath!, deliveredResult!, "blocked Result delivered for visibility");
        });
    }

    private static void TestProducerExitSemantics()
    {
        WithTempDirectory(temp =>
        {
            var invalid = WriteProducerRequest(temp, "exit-invalid", schemaVersion: "2.0");
            Equal(2, HandoffProducerCommand.Run(invalid, _ => PickerDeliveryOutcome.ExistingInstance()), "validation exit");
            var failed = WriteProducerRequest(temp, "exit-failed");
            Equal(3, HandoffProducerCommand.Run(failed, _ => PickerDeliveryOutcome.Failed("offline")), "delivery exit");
            var delivered = WriteProducerRequest(temp, "exit-success");
            Equal(0, HandoffProducerCommand.Run(delivered, _ => PickerDeliveryOutcome.ExistingInstance()), "delivered exit");
        });
    }

    private static void TestSameHandoffRetry()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "retry");
            Equal(0, HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.ExistingInstance()), "initial delivery");
            Equal(0, HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.ExistingInstance()), "replay delivery");
            var result = ReadProducerResult(ProducerResultPath(temp, "retry"));
            True(result.Replayed, "replay explicitly reported");
            Equal(1, Directory.GetDirectories(Path.Combine(temp, ".gpt-review", "handoffs")).Length, "one Handoff directory");
        });
    }

    private static void TestSameConversationCommandReplay()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "conversation-replay-command", conversationId: "conversation-replay-command", finalResponse: "A");
            var callbackCount = 0;
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(request, _ => { callbackCount++; return PickerDeliveryOutcome.ExistingInstance(); }), "initial conversation delivery");
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(request, _ => { callbackCount++; return PickerDeliveryOutcome.ExistingInstance(); }), "conversation replay delivery");
            Equal(2, callbackCount, "replay invokes delivery once per command");
            var result = ReadProducerResult(Path.Combine(temp, ".gpt-review", "conversations", "conversation-replay-command", "result.json"));
            True(result.Replayed, "conversation replay explicitly reported");
            Equal("conversation-replay-command", result.HandoffId!, "replay keeps handoff identity");
        });
    }

    private static void TestImmutableIntakeSurvivesFixedRequestReplacement()
    {
        WithTempDirectory(temp =>
        {
            var fixedRequest = Path.Combine(temp, "producer-request.json");
            var requestA = WriteProducerRequest(temp, "intake-a", conversationId: "intake-conversation-a", finalResponse: "A", requestPath: fixedRequest);
            var requestB = WriteProducerRequest(temp, "intake-b", conversationId: "intake-conversation-b", finalResponse: "B", requestPath: Path.Combine(temp, "request-b.json"));
            using var captured = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            SetIntakeCapturedHook(() => { captured.Set(); release.Wait(); });
            try
            {
                var taskA = Task.Run(() => HandoffProducerCommand.Run(requestA, manifest =>
                {
                    var loaded = ManifestLoader.Load(manifest);
                    Equal("intake-a", loaded.Manifest.HandoffId!, "captured A handoff retained");
                    Equal("A", loaded.Manifest.FinalResponse!, "captured A response retained");
                    return PickerDeliveryOutcome.ExistingInstance();
                }));
                True(captured.Wait(2000), "intake capture reached barrier");
                File.Copy(requestB, fixedRequest, true);
                release.Set();
                Equal(HandoffProducerExitCodes.Delivered, taskA.Result, "captured A delivered");
            }
            finally { SetIntakeCapturedHook(null); }
            var slotA = Path.Combine(temp, ".gpt-review", "conversations", "intake-conversation-a");
            Equal("intake-a", ReadProducerResult(Path.Combine(slotA, "result.json")).HandoffId!, "A slot remains immutable intake");
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(fixedRequest, _ => PickerDeliveryOutcome.ExistingInstance()), "B can reuse fixed input after capture");
            var slotB = Path.Combine(temp, ".gpt-review", "conversations", "intake-conversation-b");
            Equal("intake-b", ReadProducerResult(Path.Combine(slotB, "result.json")).HandoffId!, "B slot delivered");
        });
    }

    private static void TestConflictingHandoffRetry()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "conflict");
            var first = HandoffProducer.Generate(request);
            var manifestHash = GetSha256(first.Result.ManifestPath!);
            var conflictingItems = new[] { ProducerItem("other.txt", "MUST", true) };
            File.WriteAllText(Path.Combine(temp, "other.txt"), "other");
            WriteProducerRequest(temp, "conflict", conflictingItems, requestPath: request, taskName: "Different task");
            var second = HandoffProducer.Generate(request);
            True(!second.CanDeliver, "conflicting replay blocked");
            Contains(second.Result.Errors, "HANDOFF_REUSE_WITH_CHANGED_CONTENT", "conflict code reported");
            Equal(manifestHash, GetSha256(first.Result.ManifestPath!), "existing Manifest preserved");
        });
    }

    private static void TestConcurrentIndependentHandoffs()
    {
        WithTempDirectory(temp =>
        {
            var firstRequest = WriteProducerRequest(temp, "parallel-a");
            var secondRequest = WriteProducerRequest(temp, "parallel-b");
            var tasks = new[] {
                Task.Run(() => HandoffProducer.Generate(firstRequest)),
                Task.Run(() => HandoffProducer.Generate(secondRequest))
            };
            Task.WaitAll(tasks);
            True(tasks.All(task => task.Result.CanDeliver), "both parallel Handoffs generated");
            True(tasks.All(task => File.Exists(task.Result.Result.ManifestPath)), "both parallel Manifests complete");
        });
    }

    private static void TestSameConversationDeliverySerialization()
    {
        WithTempDirectory(temp =>
        {
            const string conversation = "serialized-conversation";
            var requestA = WriteProducerRequest(temp, "serialized-a", conversationId: conversation, finalResponse: "A");
            var requestB = WriteProducerRequest(temp, "serialized-b", conversationId: conversation, finalResponse: "B");
            using var aEntered = new ManualResetEventSlim();
            using var releaseA = new ManualResetEventSlim();
            using var bEntered = new ManualResetEventSlim();
            var taskA = Task.Run(() => HandoffProducerCommand.Run(requestA, manifest =>
            {
                var loaded = ManifestLoader.Load(manifest);
                Equal("serialized-a", loaded.Manifest.HandoffId!, "A callback reads A handoff");
                Equal("A", loaded.Manifest.FinalResponse!, "A callback reads A response");
                aEntered.Set();
                releaseA.Wait();
                return PickerDeliveryOutcome.ExistingInstance();
            }));
            True(aEntered.Wait(2000), "A delivery entered");
            var taskB = Task.Run(() => HandoffProducerCommand.Run(requestB, manifest =>
            {
                bEntered.Set();
                var loaded = ManifestLoader.Load(manifest);
                Equal("serialized-b", loaded.Manifest.HandoffId!, "B callback reads B handoff");
                Equal("B", loaded.Manifest.FinalResponse!, "B callback reads B response");
                return PickerDeliveryOutcome.ExistingInstance();
            }));
            True(!bEntered.Wait(200), "B delivery remains blocked while A callback is active");
            var slot = Path.Combine(temp, ".gpt-review", "conversations", conversation);
            Equal("serialized-a", JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(Path.Combine(slot, "manifest.json")))!.HandoffId!, "slot remains A during delivery");
            Equal(HandoffProducerStatuses.ManifestCreated, ReadProducerResult(Path.Combine(slot, "result.json")).Status, "receipt remains A transaction during delivery");
            releaseA.Set();
            Task.WaitAll(taskA, taskB);
            Equal(0, taskA.Result, "A delivered");
            Equal(0, taskB.Result, "B delivered after A");
            Equal("serialized-b", ReadProducerResult(Path.Combine(slot, "result.json")).HandoffId!, "final receipt is B");
        });
    }

    private static void TestDifferentConversationDeliveryConcurrency()
    {
        WithTempDirectory(temp =>
        {
            var requestA = WriteProducerRequest(temp, "parallel-conversation-a", conversationId: "parallel-conversation-a", finalResponse: "A");
            var requestB = WriteProducerRequest(temp, "parallel-conversation-b", conversationId: "parallel-conversation-b", finalResponse: "B");
            using var aEntered = new ManualResetEventSlim();
            using var bEntered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var taskA = Task.Run(() => HandoffProducerCommand.Run(requestA, _ => { aEntered.Set(); release.Wait(); return PickerDeliveryOutcome.ExistingInstance(); }));
            True(aEntered.Wait(2000), "conversation A delivery entered");
            var taskB = Task.Run(() => HandoffProducerCommand.Run(requestB, _ => { bEntered.Set(); return PickerDeliveryOutcome.ExistingInstance(); }));
            True(bEntered.Wait(2000), "conversation B delivery entered concurrently");
            release.Set();
            Task.WaitAll(taskA, taskB);
            Equal(0, taskA.Result, "conversation A delivered");
            Equal(0, taskB.Result, "conversation B delivered");
        });
    }

    private static void TestFailedDeliveryReleasesConversationLock()
    {
        WithTempDirectory(temp =>
        {
            const string conversation = "failed-release-conversation";
            var requestA = WriteProducerRequest(temp, "failed-release-a", conversationId: conversation, finalResponse: "A");
            var requestB = WriteProducerRequest(temp, "failed-release-b", conversationId: conversation, finalResponse: "B");
            Equal(HandoffProducerExitCodes.DeliveryFailed, HandoffProducerCommand.Run(requestA, _ => PickerDeliveryOutcome.Failed("offline")), "failed delivery exit");
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(requestB, _ => PickerDeliveryOutcome.ExistingInstance()), "subsequent delivery succeeds");
            var slot = Path.Combine(temp, ".gpt-review", "conversations", conversation);
            Equal("failed-release-b", ReadProducerResult(Path.Combine(slot, "result.json")).HandoffId!, "subsequent handoff owns slot");
            Equal(HandoffProducerStatuses.Delivered, ReadProducerResult(Path.Combine(slot, "result.json")).Status, "subsequent receipt delivered");
        });
    }

    private static void TestPickerExistingDeliveryResult()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "existing-picker");
            Equal(0, HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.ExistingInstance()), "existing Picker exit");
            Equal(PickerDeliveryModes.IpcExistingInstance, ReadProducerResult(ProducerResultPath(temp, "existing-picker")).PickerDelivery!, "existing Picker mode");
        });
    }

    private static void TestPickerNotRunningDeliveryResult()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "started-picker");
            Equal(0, HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.StartedPrimary()), "started Picker exit");
            Equal(PickerDeliveryModes.StartedPrimary, ReadProducerResult(ProducerResultPath(temp, "started-picker")).PickerDelivery!, "started Picker mode");
        });
    }

    private static void TestCanonicalHandoffHashPersistence()
    {
        WithTempDirectory(temp =>
        {
            const string finalResponse = "Exact\r\nCanonical response\n";
            const string conversationId = "hash-conversation";
            var request = WriteProducerRequest(temp, "hash-handoff", [], conversationId: conversationId, finalResponse: finalResponse);
            Equal(HandoffProducerExitCodes.Delivered, HandoffProducerCommand.Run(request, _ => PickerDeliveryOutcome.ExistingInstance()), "delivery succeeds");

            var result = ReadProducerResult(Path.Combine(temp, ".gpt-review", "conversations", conversationId, "result.json"));
            var snapshot = JsonSerializer.Deserialize<HandoffRequest>(File.ReadAllText(result.RequestPath!))!;
            var manifest = ManifestLoader.Load(result.ManifestPath!).Manifest;
            var expectedHash = CanonicalResponseHash.Compute(finalResponse);
            Equal(expectedHash, snapshot.CanonicalResponseSha256!, "Request snapshot hash");
            Equal(expectedHash, manifest.CanonicalResponseSha256!, "Manifest hash");
            Equal(expectedHash, result.CanonicalResponseSha256!, "Result hash");
            Equal(finalResponse, snapshot.FinalResponse!, "Request exact response");
            Equal(finalResponse, manifest.FinalResponse!, "Manifest exact response");
            Equal("hash-handoff", result.HandoffId!, "Result handoff identity");
            Equal(conversationId, result.ConversationId!, "Result conversation identity");
        });
    }

    private static void TestCandidateConsistencyFence()
    {
        var request = new HandoffRequest {
            SchemaVersion = "1.0", HandoffId = "handoff-a", ConversationId = "conversation-a", FinalResponse = "A",
            CanonicalResponseSha256 = CanonicalResponseHash.Compute("A")
        };
        var manifest = new ReviewManifest {
            SchemaVersion = "1.2", HandoffId = "handoff-a", ConversationId = "conversation-a", FinalResponse = "A",
            CanonicalResponseSha256 = CanonicalResponseHash.Compute("A"), ProjectRoot = Path.GetTempPath(), Items = []
        };
        Equal(0, HandoffProducer.ValidateCandidateSnapshot(request, manifest).Count, "matching candidate accepted");

        manifest.FinalResponse = "B";
        Contains(HandoffProducer.ValidateCandidateSnapshot(request, manifest), "HANDOFF_CANONICAL_MISMATCH", "final response mismatch");
        manifest.FinalResponse = "A";
        manifest.CanonicalResponseSha256 = "TAMPERED";
        Contains(HandoffProducer.ValidateCandidateSnapshot(request, manifest), "CANONICAL_RESPONSE_HASH_MISMATCH", "hash mismatch");
        manifest.CanonicalResponseSha256 = request.CanonicalResponseSha256;
        manifest.HandoffId = "handoff-b";
        Contains(HandoffProducer.ValidateCandidateSnapshot(request, manifest), "HANDOFF_ID_MISMATCH", "handoff ID mismatch");
        manifest.HandoffId = request.HandoffId;
        manifest.ConversationId = "conversation-b";
        Contains(HandoffProducer.ValidateCandidateSnapshot(request, manifest), "CONVERSATION_ID_MISMATCH", "conversation ID mismatch");
    }

    private static void TestManifestHashMismatchPreservesWorkspaceRound()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "hash-preserve";
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "handoff-a", [], conversationId: conversationId, finalResponse: "A"));
            var workspace = new ReviewWorkspace();
            var session = workspace.AddOrUpdateManifest(first.Result.ManifestPath!, false, true).Session;
            var manual = CreateManualFiles(temp); session.Tray.AddManualPaths(manual);
            var candidate = ManifestLoader.Load(first.Result.ManifestPath!).Manifest;
            candidate.HandoffId = "handoff-b";
            candidate.FinalResponse = "B";
            // Deliberately retain A's declared hash to emulate an inconsistent incoming candidate.
            var invalidManifestPath = Path.Combine(temp, "tampered-manifest.json");
            File.WriteAllText(invalidManifestPath, HandoffProducer.Serialize(candidate));

            ThrowsWithMessage<InvalidDataException>(() => workspace.AddOrUpdateManifest(invalidManifestPath, false, true), "CANONICAL_RESPONSE_HASH_MISMATCH");
            Equal("handoff-a", session.LoadedReview!.Manifest.HandoffId!, "old handoff remains active");
            Equal("A", session.LoadedReview.Manifest.FinalResponse!, "old manifest remains active");
            Equal(manual.Length, session.Tray.ManualCount, "Manual sources remain after rejection");
        });
    }

    private static void TestReplayValidatesPersistedPair()
    {
        WithTempDirectory(temp =>
        {
            var source = WriteProducerRequest(temp, "replay-fence", [], conversationId: "replay-fence-conversation", finalResponse: "A");
            var first = HandoffProducer.Generate(source);
            var originalManifest = File.ReadAllText(first.Result.ManifestPath!);
            var tampered = JsonSerializer.Deserialize<ReviewManifest>(originalManifest)!;
            tampered.FinalResponse = "B";
            File.WriteAllText(first.Result.ManifestPath!, HandoffProducer.Serialize(tampered));

            var replay = HandoffProducer.Generate(source);
            True(!replay.CanDeliver, "tampered replay is blocked");
            Contains(replay.Result.Errors, "HANDOFF_CANONICAL_MISMATCH", "replay canonical mismatch reported");
            Contains(replay.Result.Errors, "CANONICAL_RESPONSE_HASH_MISMATCH", "replay hash mismatch reported");
            Equal("B", JsonSerializer.Deserialize<ReviewManifest>(File.ReadAllText(first.Result.ManifestPath!))!.FinalResponse!, "tampered persisted manifest is not overwritten by replay");
        });
    }

    private static void TestNewHandoffIdReplacesRoundAndResetsManual()
    {
        WithTempDirectory(temp =>
        {
            const string conversationId = "round-replacement";
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "handoff-a", [], conversationId: conversationId, finalResponse: "A"));
            var workspace = new ReviewWorkspace();
            var session = workspace.AddOrUpdateManifest(first.Result.ManifestPath!, false, true).Session;
            session.Tray.AddManualPaths(CreateManualFiles(temp));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "handoff-b", [], conversationId: conversationId, finalResponse: "B"));
            workspace.AddOrUpdateManifest(second.Result.ManifestPath!, false, true);

            Equal("handoff-b", session.LoadedReview!.Manifest.HandoffId!, "new handoff becomes active");
            Equal("B", session.LoadedReview.Manifest.FinalResponse!, "new canonical response becomes active");
            Equal(0, session.Tray.ManualCount, "new review round clears Manual sources");
        });
    }

    private static void TestFinalResponseOnly()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "final-only", [], conversationId: "conversation-final", finalResponse: "# Final\n\nExact answer.");
            var generation = HandoffProducer.Generate(request);
            True(generation.CanDeliver, "final-only request delivers");
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            Equal(1, loaded.Items.Count, "final-only exposes one logical item");
            True(loaded.Items.Single().IsVirtual, "final response is virtual");
            Equal("# Final\n\nExact answer.", loaded.Manifest.FinalResponse!, "final response survives manifest");
            True(loaded.Items.Single().Selected, "final response selected by default");
        });
    }

    private static void TestFinalResponsePlusEvidence()
    {
        WithTempDirectory(temp =>
        {
            File.WriteAllText(Path.Combine(temp, "core.txt"), "core");
            var request = WriteProducerRequest(temp, "final-evidence", [ProducerItem("core.txt", "MUST", true), ProducerItem("extra.txt", "RECOMMENDED", true)], conversationId: "conversation-final", finalResponse: "canonical response");
            File.WriteAllText(Path.Combine(temp, "extra.txt"), "evidence");
            var generation = HandoffProducer.Generate(request);
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            Equal(3, loaded.Items.Count, "final plus two evidence items");
            True(loaded.Items.Any(item => item.IsVirtual), "logical final response present");
        });
    }

    private static void TestFinalResponseConversationReplacement()
    {
        WithTempDirectory(temp =>
        {
            const string conversation = "conversation-final-replace";
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "task-a", conversationId: conversation, finalResponse: "A"));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "task-b", conversationId: conversation, finalResponse: "B"));
            Equal(first.Result.ManifestPath!, second.Result.ManifestPath!, "fixed manifest slot retained");
            var loaded = ManifestLoader.Load(second.Result.ManifestPath!);
            Equal("B", loaded.Manifest.FinalResponse!, "latest final response replaces prior");
            Equal(3, Directory.GetFiles(Path.GetDirectoryName(second.Result.ManifestPath!)!).Length, "slot remains bounded to JSON files");
        });
    }

    private static void TestFailedFinalResponseReplacementPreservesCurrent()
    {
        WithTempDirectory(temp =>
        {
            const string conversation = "conversation-final-fail";
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "task-a", conversationId: conversation, finalResponse: "A"));
            var secondRequest = WriteProducerRequest(temp, "task-b", [ProducerItem("missing.txt", "MUST", true)], conversationId: conversation, finalResponse: "B");
            var blocked = HandoffProducer.Generate(secondRequest);
            True(!blocked.CanDeliver, "missing MUST blocks replacement");
            var loaded = ManifestLoader.Load(first.Result.ManifestPath!);
            Equal("A", loaded.Manifest.FinalResponse!, "previous final response preserved");
            Equal("task-a", loaded.Manifest.HandoffId!, "previous evidence metadata preserved");
        });
    }

    private static void TestFinalResponseConversationIsolation()
    {
        WithTempDirectory(temp =>
        {
            var first = HandoffProducer.Generate(WriteProducerRequest(temp, "task-a", conversationId: "conversation-final-a", finalResponse: "A"));
            var second = HandoffProducer.Generate(WriteProducerRequest(temp, "task-b", conversationId: "conversation-final-b", finalResponse: "B"));
            True(!string.Equals(first.Result.ManifestPath, second.Result.ManifestPath, StringComparison.OrdinalIgnoreCase), "different conversations use separate slots");
            Equal("A", ManifestLoader.Load(first.Result.ManifestPath!).Manifest.FinalResponse!, "first conversation remains isolated");
            Equal("B", ManifestLoader.Load(second.Result.ManifestPath!).Manifest.FinalResponse!, "second conversation remains isolated");
        });
    }

    private static void TestFinalResponseMaterialization()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "final-materialize", [], conversationId: "conversation-final-materialize", finalResponse: "materialized text");
            var generation = HandoffProducer.Generate(request);
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            var item = loaded.Items.Single();
            var materialized = FileDropService.Materialize(item);
            Equal("CODEX_FINAL_RESPONSE.md", Path.GetFileName(materialized), "logical filename materialized");
            Equal("materialized text", File.ReadAllText(materialized), "materialized content exact");
            True(!Directory.GetFiles(temp, "CODEX_FINAL_RESPONSE.md", SearchOption.AllDirectories).Any(), "no permanent project markdown created");
        });
    }

    private static void TestFinalResponseClipboardDragAndBundle()
    {
        WithTempDirectory(temp =>
        {
            var request = WriteProducerRequest(temp, "final-outputs", [], conversationId: "conversation-final-outputs", finalResponse: "output exact");
            var generation = HandoffProducer.Generate(request);
            var loaded = ManifestLoader.Load(generation.Result.ManifestPath!);
            FileDropService.CopyToClipboard(loaded.Items);
            var clipboardPath = Clipboard.GetFileDropList().Cast<string>().Single();
            Equal("CODEX_FINAL_RESPONSE.md", Path.GetFileName(clipboardPath), "Clipboard logical filename");
            Equal("output exact", File.ReadAllText(clipboardPath), "Clipboard content exact");
            var dragPath = ((string[])FileDropService.CreateDataObject(loaded.Items).GetData(DataFormats.FileDrop)!).Single();
            Equal("output exact", File.ReadAllText(dragPath), "drag content exact");
            var bundle = BundleService.CreateSelectedBundle(generation.Result.ManifestPath!, loaded.Items);
            var bundled = Directory.GetFiles(bundle).Single(path => Path.GetFileName(path).Equals("CODEX_FINAL_RESPONSE.md", StringComparison.OrdinalIgnoreCase));
            Equal("output exact", File.ReadAllText(bundled), "bundle content exact");
        });
    }

    private static ReviewWorkspace CreateThreeHandoffWorkspace()
    {
        var workspace = new ReviewWorkspace();
        foreach (var name in new[] { "translation01", "test01", "legacy03" }) workspace.AddOrUpdateManifest(HandoffManifest(name), false, true);
        return workspace;
    }

    private static (LoadedReview Loaded, ReviewTray Tray) CreateMixedTray(string temp)
    {
        var loaded = ManifestLoader.Load(SampleManifest()); ReviewSelection.Clear(loaded.Items);
        foreach (var item in loaded.Items.Where(item => item.Exists).Take(3)) item.Selected = true;
        var tray = new ReviewTray(loaded.Items); tray.AddManualPaths(CreateManualFiles(temp)); return (loaded, tray);
    }

    private static string[] CreateManualFiles(string directory)
    {
        Directory.CreateDirectory(directory);
        var first = Path.Combine(directory, "manual-one.txt"); var second = Path.Combine(directory, "manual-two.log");
        File.WriteAllText(first, "manual one"); File.WriteAllText(second, "manual two"); return [first, second];
    }

    private static void WithTempDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "GPTReviewPickerTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { action(directory); } finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static string SampleManifest() => PublicFixtures.SampleManifest();
    private static string HandoffManifest(string name) => PublicFixtures.HandoffManifest(name);
    private static string IsolationHandoffManifest(string name) => PublicFixtures.IsolationHandoffManifest(name);
    private static string ManualHandoffFile() => PublicFixtures.ManualHandoffFile();

    private static class PublicFixtures
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), $"GPTReviewPickerPublicTests-{Environment.ProcessId}-{Guid.NewGuid():N}");
        private static readonly string ProjectRoot = Path.Combine(Root, "project");
        private static bool _initialized;

        internal static string SampleManifest()
        {
            Ensure();
            return Path.Combine(Root, "sample-manifest.json");
        }

        internal static string HandoffManifest(string name)
        {
            Ensure();
            var path = Path.Combine(Root, "handoffs", name, "manifest.json");
            if (!File.Exists(path)) throw new ArgumentException($"Unknown public fixture: {name}", nameof(name));
            return path;
        }

        internal static string IsolationHandoffManifest(string name)
        {
            Ensure();
            var path = Path.Combine(Root, "isolation-handoffs", name, "manifest.json");
            if (!File.Exists(path)) throw new ArgumentException($"Unknown isolation fixture: {name}", nameof(name));
            return path;
        }

        internal static string ManualHandoffFile()
        {
            Ensure();
            return Path.Combine(Root, "manual", "session-note.txt");
        }

        internal static string ProjectRootPath()
        {
            Ensure();
            return ProjectRoot;
        }

        private static void Ensure()
        {
            if (_initialized) return;
            Directory.CreateDirectory(ProjectRoot);
            File.WriteAllText(Path.Combine(ProjectRoot, "report.md"), "Synthetic report fixture for public tests.\n");
            File.WriteAllText(Path.Combine(ProjectRoot, "source.cs"), "// Synthetic source fixture for public tests.\n");
            File.WriteAllText(Path.Combine(ProjectRoot, "metadata.json"), "{\"fixture\":true}\n");
            File.WriteAllText(Path.Combine(ProjectRoot, "data.csv"), "name,value\nsynthetic,1\n");
            File.WriteAllText(Path.Combine(ProjectRoot, "notes.txt"), "Synthetic optional fixture.\n");

            var sample = new {
                schema_version = "1.0", stage = "PUBLIC_SAMPLE", project_root = ProjectRoot,
                items = new[] {
                    new { label = "Synthetic report", path = "report.md", priority = "MUST", reason = "Primary synthetic evidence", default_selected = true },
                    new { label = "Synthetic source", path = "source.cs", priority = "MUST", reason = "Secondary synthetic evidence", default_selected = true },
                    new { label = "Missing synthetic file", path = "missing.md", priority = "MUST", reason = "Missing-file handling", default_selected = true },
                    new { label = "Synthetic metadata", path = "metadata.json", priority = "RECOMMENDED", reason = "Structured fixture", default_selected = false },
                    new { label = "Synthetic data", path = "data.csv", priority = "OPTIONAL", reason = "Tabular fixture", default_selected = false },
                    new { label = "Synthetic notes", path = "notes.txt", priority = "OPTIONAL", reason = "Plain-text fixture", default_selected = false }
                }
            };
            File.WriteAllText(Path.Combine(Root, "sample-manifest.json"), JsonSerializer.Serialize(sample));

            foreach (var name in new[] { "translation01", "test01" })
            {
                var directory = Path.Combine(Root, "handoffs", name);
                Directory.CreateDirectory(directory);
                var handoff = new {
                    schema_version = "1.1", handoff_id = name, project_name = "Example Review Project",
                    task_name = $"Synthetic {name} task", stage = "PUBLIC_SYNTHETIC", project_root = ProjectRoot,
                    generated_at = "2030-01-02T03:04:05Z",
                    items = new[] { new { label = "Synthetic report", path = "report.md", priority = "MUST", reason = "Synthetic handoff evidence", default_selected = true } }
                };
                File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(handoff));
            }

            foreach (var name in new[] { "translation01", "test01" })
            {
                var isolationProjectRoot = Path.Combine(Root, "isolation-projects", name);
                Directory.CreateDirectory(isolationProjectRoot);
                File.WriteAllText(Path.Combine(isolationProjectRoot, "report.md"), $"Synthetic report for {name}.\n");
                File.WriteAllText(Path.Combine(isolationProjectRoot, "verification.txt"), $"Synthetic verification for {name}.\n");

                var isolationDirectory = Path.Combine(Root, "isolation-handoffs", name);
                Directory.CreateDirectory(isolationDirectory);
                var isolationHandoff = new {
                    schema_version = "1.1", handoff_id = $"isolation-{name}", project_name = "Example Isolation Project",
                    task_name = $"Synthetic isolation {name} task", stage = "PUBLIC_SYNTHETIC_ISOLATION", project_root = isolationProjectRoot,
                    generated_at = "2030-01-02T03:04:05Z",
                    items = new[] {
                        new { label = "Synthetic report", path = "report.md", priority = "MUST", reason = "Primary synthetic isolation evidence", default_selected = true },
                        new { label = "Synthetic verification", path = "verification.txt", priority = "RECOMMENDED", reason = "Secondary synthetic isolation evidence", default_selected = true }
                    }
                };
                File.WriteAllText(Path.Combine(isolationDirectory, "manifest.json"), JsonSerializer.Serialize(isolationHandoff));
            }

            foreach (var (name, stage) in new[] { ("example-stage-03", "ExampleStage03"), ("legacy03", "SyntheticLegacy03") })
            {
                var legacyDirectory = Path.Combine(Root, "handoffs", name);
                Directory.CreateDirectory(legacyDirectory);
                var legacy = new {
                    schema_version = "1.0", stage, project_root = ProjectRoot,
                    items = new[] { new { label = "Synthetic report", path = "report.md", priority = "MUST", reason = "Synthetic legacy evidence", default_selected = true } }
                };
                File.WriteAllText(Path.Combine(legacyDirectory, "manifest.json"), JsonSerializer.Serialize(legacy));
            }

            var manualDirectory = Path.Combine(Root, "manual");
            Directory.CreateDirectory(manualDirectory);
            File.WriteAllText(Path.Combine(manualDirectory, "session-note.txt"), "Synthetic manual handoff note.\n");
            _initialized = true;
        }
    }
    private static string WriteConversationManifest(string directory, string conversationId, string handoffId, string taskName, string? displayName = null, bool renameConversation = false)
    {
        var projectRoot = PublicFixtures.ProjectRootPath();
        var path = Path.Combine(directory, $"{handoffId}.manifest.json");
        var manifest = new ReviewManifest {
            SchemaVersion = "1.2", HandoffId = handoffId, ConversationId = conversationId,
            DisplayName = displayName ?? "Conversation 01", RenameConversation = renameConversation ? true : null,
            ProjectName = "Conversation Tests", TaskName = taskName,
            Stage = "V0_5_2_TEST", ProjectRoot = projectRoot, GeneratedAt = "2030-01-02T21:30:00+08:00",
            Items = [new ReviewManifestItem { Label = "Report", Path = "report.md", Priority = "MUST", Reason = taskName, DefaultSelected = true }]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
        return path;
    }
    private static HandoffRequestItem ProducerItem(string path, string priority, bool selected) => new() {
        Label = Path.GetFileName(path), Path = path, Priority = priority, Reason = $"Review {Path.GetFileName(path)}", DefaultSelected = selected
    };
    private static string WriteProducerRequest(
        string projectRoot,
        string handoffId,
        IEnumerable<HandoffRequestItem>? items = null,
        string schemaVersion = HandoffRequestContract.SchemaVersion,
        string? requestPath = null,
        string? taskName = null,
        string? conversationId = null,
        string? finalResponse = null,
        string? displayName = null,
        bool renameConversation = false,
        bool omitDisplayName = false)
    {
        Directory.CreateDirectory(projectRoot);
        var core = Path.Combine(projectRoot, "core.txt");
        if (items is null && !File.Exists(core)) File.WriteAllText(core, "core");
        var request = new HandoffRequest {
            SchemaVersion = schemaVersion,
            HandoffId = handoffId,
            ConversationId = conversationId,
            DisplayName = omitDisplayName ? null : displayName ?? $"Producer {handoffId}",
            RenameConversation = renameConversation,
            ProjectName = "Producer Tests",
            TaskName = taskName ?? "Producer test",
            Stage = "V0_5_TEST",
            ProjectRoot = projectRoot,
            GeneratedAt = "2030-01-02T20:00:00+08:00",
            FinalResponse = finalResponse,
            Items = (items ?? [ProducerItem("core.txt", "MUST", true)]).ToList()
        };
        requestPath ??= Path.Combine(projectRoot, $"{handoffId}.request.json");
        File.WriteAllText(requestPath, HandoffProducer.Serialize(request));
        return requestPath;
    }
    private static string ProducerResultPath(string root, string handoffId)
        => Path.Combine(root, ".gpt-review", "handoffs", handoffId, "result.json");
    private static HandoffProducerResult ReadProducerResult(string path)
        => JsonSerializer.Deserialize<HandoffProducerResult>(File.ReadAllText(path)) ?? throw new Exception("Producer result could not be read.");
    private static string GetSha256(string path) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
    private static void WithManifest(object value, Action<string> action) { var path = Path.Combine(Path.GetTempPath(), $"manifest-{Guid.NewGuid():N}.json"); try { File.WriteAllText(path, JsonSerializer.Serialize(value)); action(path); } finally { File.Delete(path); } }
    private static void Equal<T>(T expected, T actual, string label) where T : notnull { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{label}: expected {expected}, got {actual}"); }
    private static void SetIntakeCapturedHook(Action? hook)
        => typeof(HandoffProducerCommand).GetProperty("IntakeCapturedHook", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null, hook);
    private static void True(bool value, string label) { if (!value) throw new Exception(label); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    private static void ThrowsWithMessage<T>(Action action, string expectedFragment) where T : Exception
    {
        try { action(); }
        catch (T exception)
        {
            if (exception.Message.Contains(expectedFragment, StringComparison.Ordinal)) return;
            throw new Exception($"Expected {typeof(T).Name} message containing {expectedFragment}, got {exception.Message}");
        }
        throw new Exception($"Expected {typeof(T).Name}");
    }
    private static void Contains(IEnumerable<string> values, string expectedFragment, string label)
    {
        if (!values.Any(value => value.Contains(expectedFragment, StringComparison.Ordinal)))
            throw new Exception($"{label}: expected {expectedFragment}");
    }
}
