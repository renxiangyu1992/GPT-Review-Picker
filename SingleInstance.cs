using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GPTReviewPicker;

public sealed class PickerIpcMessage
{
    public const string OpenManifestType = "open_manifest";
    public const string OpenResultType = "open_result";
    public const string ActivateType = "activate";

    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("path")] public string? Path { get; init; }

    public static PickerIpcMessage OpenManifest(string path) => new() { Type = OpenManifestType, Path = System.IO.Path.GetFullPath(path) };
    public static PickerIpcMessage OpenResult(string path) => new() { Type = OpenResultType, Path = System.IO.Path.GetFullPath(path) };
    public static PickerIpcMessage Activate() => new() { Type = ActivateType };

    public static PickerIpcMessage Parse(string json)
    {
        PickerIpcMessage? message;
        try { message = JsonSerializer.Deserialize<PickerIpcMessage>(json); }
        catch (JsonException ex) { throw new InvalidDataException($"Invalid IPC JSON: {ex.Message}", ex); }
        if (message is null) throw new InvalidDataException("IPC message is empty.");
        if (string.Equals(message.Type, ActivateType, StringComparison.Ordinal)) return Activate();
        if (string.Equals(message.Type, OpenManifestType, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(message.Path)) throw new InvalidDataException("open_manifest requires path.");
            return OpenManifest(message.Path);
        }
        if (string.Equals(message.Type, OpenResultType, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(message.Path)) throw new InvalidDataException("open_result requires path.");
            return OpenResult(message.Path);
        }
        throw new InvalidDataException($"Unsupported IPC command: {message.Type}");
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}

public sealed class PickerIpcResponse
{
    public const string AcceptedStatus = "accepted";
    public const string RejectedStatus = "rejected";

    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("error")] public string? Error { get; init; }

    public bool Accepted => string.Equals(Status, AcceptedStatus, StringComparison.Ordinal);
    public static PickerIpcResponse Accept() => new() { Status = AcceptedStatus };
    public static PickerIpcResponse Reject(string error) => new() { Status = RejectedStatus, Error = error };
    public string ToJson() => JsonSerializer.Serialize(this);

    public static PickerIpcResponse Parse(string json)
    {
        PickerIpcResponse? response;
        try { response = JsonSerializer.Deserialize<PickerIpcResponse>(json); }
        catch (JsonException ex) { throw new InvalidDataException($"Invalid IPC response JSON: {ex.Message}", ex); }
        if (response is null) throw new InvalidDataException("IPC response is empty.");
        if (response.Status is not AcceptedStatus and not RejectedStatus)
            throw new InvalidDataException($"Unsupported IPC response: {response.Status}");
        return response;
    }
}

public static class SingleInstanceNames
{
    public const string MutexName = @"Local\GPTReviewPicker.SingleInstance";
    public const string PipeName = "GPTReviewPicker.CurrentUser";
}

public sealed class PickerIpcServer : IDisposable
{
    private readonly Func<PickerIpcMessage, PickerIpcResponse> _onMessage;
    private readonly Action<string> _onError;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _serverTask;

    public PickerIpcServer(Func<PickerIpcMessage, PickerIpcResponse> onMessage, Action<string> onError)
    {
        _onMessage = onMessage;
        _onError = onError;
    }

    public void Start() => _serverTask ??= Task.Run(RunAsync);

    private async Task RunAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    SingleInstanceNames.PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(_cancellation.Token);
                using var reader = new StreamReader(pipe, leaveOpen: true);
                var json = await reader.ReadLineAsync(_cancellation.Token);
                if (json is null) throw new InvalidDataException("IPC client sent no message.");
                var response = _onMessage(PickerIpcMessage.Parse(json));
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
                await writer.WriteLineAsync(response.ToJson());
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
            catch (Exception ex) when (!_cancellation.IsCancellationRequested) { _onError(ex.Message); }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        try { _serverTask?.Wait(1000); } catch (AggregateException) { }
        _cancellation.Dispose();
    }
}

public static class PickerIpcClient
{
    public static bool Send(PickerIpcMessage message, int attempts = 40, int retryDelayMilliseconds = 100)
    {
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", SingleInstanceNames.PipeName, PipeDirection.InOut, PipeOptions.None);
                pipe.Connect(200);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
                writer.WriteLine(message.ToJson());
                using var reader = new StreamReader(pipe, leaveOpen: true);
                var responseJson = reader.ReadLine();
                if (responseJson is null) return true; // V0.4 server compatibility: delivery completed without ACK.
                return PickerIpcResponse.Parse(responseJson).Accepted;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException)
            {
                if (attempt == attempts) return false;
                Thread.Sleep(retryDelayMilliseconds);
            }
        }
        return false;
    }
}
