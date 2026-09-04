namespace GPTReviewPicker;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--handoff-request", StringComparison.Ordinal))
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: GPTReviewPicker.exe --handoff-request <request.json>");
                return HandoffProducerExitCodes.ValidationBlocked;
            }
            return HandoffProducerCommand.Run(args[1]);
        }

        var startupResult = args.Length == 2 && string.Equals(args[0], "--handoff-result", StringComparison.Ordinal);

        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, SingleInstanceNames.MutexName, out var isPrimary);
        if (!isPrimary)
        {
            var message = startupResult ? PickerIpcMessage.OpenResult(args[1])
                : args.Length > 0 ? PickerIpcMessage.OpenManifest(args[0]) : PickerIpcMessage.Activate();
            if (PickerIpcClient.Send(message)) return 0;
            MessageBox.Show("The running GPT Review Picker could not be reached.", "GPT Review Picker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 2;
        }

        try
        {
            var workspace = new ReviewWorkspace();
            if (startupResult)
            {
                try { workspace.AddOrUpdateFailure(args[1], activate: true, markUnread: false); }
                catch (Exception ex) { workspace.QuickTray.SetStatus($"Invalid startup Result: {ex.Message}", true); }
            }
            else if (args.Length > 0)
            {
                try { workspace.AddOrUpdateManifest(args[0], activate: true, markUnread: false); }
                catch (Exception ex) { workspace.QuickTray.SetStatus($"Invalid startup Manifest: {ex.Message}", true); }
            }

            using var form = new MainForm(workspace);
            using var server = new PickerIpcServer(form.HandleIpcMessage, form.HandleIpcError);
            form.Shown += (_, _) => server.Start();
            Application.Run(form);
            return 0;
        }
        finally { mutex.ReleaseMutex(); }
    }
}
