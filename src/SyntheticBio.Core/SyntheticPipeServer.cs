using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyntheticBio.Core;

public sealed class SyntheticPipeServerOptions
{
    public string PipeBaseName { get; init; } = "polarh10-synth";
    public double DurationSeconds { get; init; } = 180d;
    public bool LoopScenarios { get; init; } = true;
    public SyntheticLiveProfileSet ProfileSet { get; init; } = SyntheticScenarioCatalog.CreateStandardProfileSet();
}

public sealed class SyntheticPipeServer : IAsyncDisposable
{
    private readonly SyntheticPipeServerOptions _options;
    private readonly List<Task> _serverTasks = [];
    private CancellationTokenSource? _cts;

    public SyntheticPipeServer(SyntheticPipeServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsRunning => _cts is { IsCancellationRequested: false };

    public Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _serverTasks.Clear();
        _serverTasks.Add(RunDiscoveryLoopAsync(_cts.Token));
        foreach (SyntheticLiveDeviceDefinition device in _options.ProfileSet.Devices)
            _serverTasks.Add(RunDeviceLoopAsync(device, _cts.Token));

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();
        try
        {
            await Task.WhenAll(_serverTasks);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _serverTasks.Clear();
        }
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task RunDiscoveryLoopAsync(CancellationToken ct)
    {
        string pipeName = GetDiscoveryPipeName(_options.PipeBaseName);
        while (!ct.IsCancellationRequested)
        {
            await using var stream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await stream.WaitForConnectionAsync(ct);
            ClientEnvelope? request = await ReadMessageAsync<ClientEnvelope>(stream, ct);
            if (request?.Type == "scan")
            {
                await WriteMessageAsync(stream, new ServerEnvelope
                {
                    Type = "scanResult",
                    Devices = _options.ProfileSet.Devices.Select(d => new DiscoveredDevice
                    {
                        Address = d.Address,
                        Name = d.Name,
                        Rssi = -38,
                    }).ToList(),
                }, ct);
            }
        }
    }

    private async Task RunDeviceLoopAsync(SyntheticLiveDeviceDefinition device, CancellationToken ct)
    {
        string pipeName = GetDevicePipeName(_options.PipeBaseName, device.Address);
        while (!ct.IsCancellationRequested)
        {
            await using var stream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await stream.WaitForConnectionAsync(ct);
            await WriteMessageAsync(stream, new ServerEnvelope { Type = "connected", Connected = true }, ct);

            try
            {
                do
                {
                    SyntheticScenarioDefinition scenario =
                        SyntheticScenarioCatalog.ForLiveDevice(device, _options.DurationSeconds);
                    SyntheticScenarioBundle bundle =
                        SyntheticSignalGenerator.GenerateScenario(scenario, DateTimeOffset.UtcNow);

                    await StreamScenarioAsync(stream, bundle, ct);
                }
                while (_options.LoopScenarios && stream.IsConnected && !ct.IsCancellationRequested);

                if (stream.IsConnected)
                {
                    await WriteMessageAsync(
                        stream,
                        new ServerEnvelope
                        {
                            Type = "disconnect",
                            Reason = _options.LoopScenarios ? "Server stopped" : "Scenario completed",
                        },
                        ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
        }
    }

    private static async Task StreamScenarioAsync(Stream stream, SyntheticScenarioBundle bundle, CancellationToken ct)
    {
        List<TimedEnvelope> envelopes = new(bundle.HrSamples.Count + bundle.BreathingSamples.Count + bundle.EcgFrames.Count);
        int sequence = 0;

        foreach (SyntheticHrSample sample in bundle.HrSamples)
        {
            envelopes.Add(new TimedEnvelope(
                sample.TimestampUtc,
                sequence++,
                new ServerEnvelope
                {
                    Type = "hrNotification",
                    Payload = SyntheticSignalGenerator.EncodeHeartRateMeasurement(sample),
                }));
        }

        foreach (SyntheticEcgFrame frame in bundle.EcgFrames)
        {
            envelopes.Add(new TimedEnvelope(
                frame.TimestampUtc,
                sequence++,
                new ServerEnvelope
                {
                    Type = "pmdData",
                    Payload = SyntheticSignalGenerator.EncodeEcgPmdFrame(frame),
                }));
        }

        foreach (SyntheticBreathingSample sample in bundle.BreathingSamples)
        {
            envelopes.Add(new TimedEnvelope(
                sample.TimestampUtc,
                sequence++,
                new ServerEnvelope
                {
                    Type = "breathing",
                    Breathing = new BreathingTelemetryEnvelope
                    {
                        SampleTimeUtc = sample.TimestampUtc,
                        Volume01 = sample.Volume01,
                        State = sample.State,
                        HasTracking = sample.HasTracking,
                        IsStale = sample.IsStale,
                        Tag = "synthetic",
                    },
                }));
        }

        DateTimeOffset? last = null;
        foreach (TimedEnvelope envelope in envelopes
                     .OrderBy(item => item.TimestampUtc)
                     .ThenBy(item => item.Sequence))
        {
            if (last.HasValue)
            {
                TimeSpan delay = envelope.TimestampUtc - last.Value;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, ct);
            }

            await WriteMessageAsync(stream, envelope.Envelope, ct);
            last = envelope.TimestampUtc;
        }
    }

    private static string GetDiscoveryPipeName(string pipeBaseName) => $"{NormalizePipeBaseName(pipeBaseName)}.discovery";

    private static string GetDevicePipeName(string pipeBaseName, string deviceAddress)
        => $"{NormalizePipeBaseName(pipeBaseName)}.device.{SanitizeDeviceTag(deviceAddress)}";

    private static string NormalizePipeBaseName(string? pipeBaseName)
        => string.IsNullOrWhiteSpace(pipeBaseName) ? "polarh10-synth" : pipeBaseName.Trim();

    private static string SanitizeDeviceTag(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');

        return builder.ToString();
    }

    private static async Task WriteMessageAsync<T>(Stream stream, T message, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(message, JsonOptions);
        byte[] payload = Encoding.UTF8.GetBytes(json + "\n");
        await stream.WriteAsync(payload, ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<T?> ReadMessageAsync<T>(Stream stream, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[1];
        bool sawAnyData = false;

        while (true)
        {
            int bytesRead = await stream.ReadAsync(chunk, ct);
            if (bytesRead == 0)
                break;

            sawAnyData = true;
            if (chunk[0] == (byte)'\n')
                break;

            buffer.WriteByte(chunk[0]);
        }

        if (!sawAnyData)
            return default;

        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(buffer.ToArray()), JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class ClientEnvelope
    {
        public string? Type { get; set; }
        public int? DurationMs { get; set; }
    }

    private sealed class ServerEnvelope
    {
        public string? Type { get; set; }
        public List<DiscoveredDevice>? Devices { get; set; }
        public byte[]? Payload { get; set; }
        public BreathingTelemetryEnvelope? Breathing { get; set; }
        public bool? Connected { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class DiscoveredDevice
    {
        public string Address { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Rssi { get; set; }
    }

    private sealed class BreathingTelemetryEnvelope
    {
        public DateTimeOffset SampleTimeUtc { get; set; }
        public float Volume01 { get; set; }
        public string State { get; set; } = "Pausing";
        public bool HasTracking { get; set; }
        public bool IsStale { get; set; }
        public string? Tag { get; set; }
    }

    private readonly record struct TimedEnvelope(
        DateTimeOffset TimestampUtc,
        int Sequence,
        ServerEnvelope Envelope);
}
