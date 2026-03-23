using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PolarH10.Protocol;

namespace SyntheticBio.Core;

public static class SyntheticSessionExporter
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static async Task<string> ExportScenarioAsync(
        SyntheticScenarioBundle bundle,
        string outputRoot,
        SyntheticShowcaseSettings? showcaseSettings = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);

        string folderName = SanitizeFileName(bundle.Scenario.ScenarioId);
        string outputFolder = Path.Combine(outputRoot, folderName);
        Directory.CreateDirectory(outputFolder);
        SyntheticShowcaseSettings settings = showcaseSettings ?? SyntheticShowcasePreset.CreateSettings();
        SyntheticScenarioAnalysis analysis = SyntheticMeasureHarness.AnalyzeBundle(bundle, settings);

        await File.WriteAllTextAsync(
            Path.Combine(outputFolder, "scenario.json"),
            JsonSerializer.Serialize(bundle.Scenario, JsonOptions),
            ct);

        await File.WriteAllTextAsync(
            Path.Combine(outputFolder, "ground_truth.json"),
            JsonSerializer.Serialize(new
            {
                settings.PresetId,
                GeneratedAtUtc = analysis.GeneratedAtUtc,
                bundle.Scenario.ScenarioId,
                bundle.Scenario.DisplayName,
                bundle.Scenario.ExpectedBehavior,
                HrSampleCount = bundle.HrSamples.Count,
                BreathingSampleCount = bundle.BreathingSamples.Count,
                EcgFrameCount = bundle.EcgFrames.Count,
                EcgSampleCount = bundle.EcgFrames.Sum(frame => frame.MicroVolts.Length),
                Measures = new
                {
                    Coherence = analysis.Coherence.Telemetry,
                    Hrv = analysis.Hrv.Telemetry,
                    Dynamics = analysis.Dynamics.Telemetry,
                },
                HrSamples = bundle.HrSamples,
                BreathingSamples = bundle.BreathingSamples,
                EcgFrames = bundle.EcgFrames,
            }, JsonOptions),
            ct);

        await File.WriteAllTextAsync(
            Path.Combine(outputFolder, "analysis.json"),
            JsonSerializer.Serialize(analysis, JsonOptions),
            ct);

        await WriteHrCsvAsync(Path.Combine(outputFolder, "hr_rr.csv"), bundle.HrSamples, ct);
        await PolarCsvExporter.WriteEcgCsvAsync(
            Path.Combine(outputFolder, "ecg.csv"),
            bundle.EcgFrames.Select(ToPolarEcgFrame).ToArray(),
            ct: ct);
        await WriteSessionMetadataAsync(Path.Combine(outputFolder, "session.json"), bundle, analysis, settings, ct);
        return outputFolder;
    }

    public static async Task ExportProfileSetAsync(
        SyntheticLiveProfileSet profileSet,
        string outputRoot,
        double durationSeconds,
        SyntheticShowcaseSettings? showcaseSettings = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputRoot);
        SyntheticShowcaseSettings settings = showcaseSettings ?? SyntheticShowcasePreset.CreateSettings();
        foreach (SyntheticLiveDeviceDefinition device in profileSet.Devices)
        {
            SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.ForLiveDevice(device, durationSeconds);
            DateTimeOffset startTimeUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(durationSeconds + 5d);
            SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(scenario, startTimeUtc);
            await ExportScenarioAsync(bundle, outputRoot, settings, ct);
        }
    }

    public static async Task ExportScenarioCatalogAsync(
        string outputRoot,
        double durationSeconds,
        SyntheticShowcaseSettings? showcaseSettings = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputRoot);
        SyntheticShowcaseSettings settings = showcaseSettings ?? SyntheticShowcasePreset.CreateSettings();
        foreach (SyntheticScenarioDefinition baseScenario in SyntheticScenarioCatalog.All)
        {
            SyntheticScenarioDefinition scenario = baseScenario with
            {
                DurationSeconds = durationSeconds,
            };
            DateTimeOffset startTimeUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(durationSeconds + 5d);
            SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(scenario, startTimeUtc);
            await ExportScenarioAsync(bundle, outputRoot, settings, ct);
        }
    }

    private static async Task WriteHrCsvAsync(string path, IReadOnlyList<SyntheticHrSample> samples, CancellationToken ct)
    {
        using var writer = new StreamWriter(path);
        await writer.WriteLineAsync("timestamp_utc,heart_rate_bpm,rr_interval_ms");
        foreach (SyntheticHrSample sample in samples)
        {
            string line = string.Create(
                CultureInfo.InvariantCulture,
                $"{sample.TimestampUtc:O},{sample.HeartRateBpm},{sample.RrIntervalMs:F2}");
            await writer.WriteLineAsync(line);
        }
    }

    private static async Task WriteSessionMetadataAsync(
        string path,
        SyntheticScenarioBundle bundle,
        SyntheticScenarioAnalysis analysis,
        SyntheticShowcaseSettings settings,
        CancellationToken ct)
    {
        var metadata = new
        {
            SchemaVersion = 1,
            settings.PresetId,
            ScenarioId = bundle.Scenario.ScenarioId,
            DeviceName = bundle.Scenario.DisplayName,
            StartedAtUtc = bundle.HrSamples.Count > 0 ? bundle.HrSamples[0].TimestampUtc : DateTimeOffset.UtcNow,
            SavedAtUtc = DateTimeOffset.UtcNow,
            HrRrSampleCount = bundle.HrSamples.Count,
            EcgFrameCount = bundle.EcgFrames.Count,
            BreathingSampleCount = bundle.BreathingSamples.Count,
            DerivedMeasures = new
            {
                CoherenceReady = analysis.Coherence.Telemetry.HasCoherenceSample,
                HrvReady = analysis.Hrv.Telemetry.HasMetricsSample,
                DynamicsTrackingState = analysis.Dynamics.Telemetry.TrackingState.ToString(),
            },
        };

        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(metadata, JsonOptions),
            ct);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };
        options.Converters.Add(new FiniteSingleJsonConverter());
        options.Converters.Add(new FiniteNullableSingleJsonConverter());
        options.Converters.Add(new FiniteDoubleJsonConverter());
        options.Converters.Add(new FiniteNullableDoubleJsonConverter());
        return options;
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static PolarEcgFrame ToPolarEcgFrame(SyntheticEcgFrame frame)
        => new(frame.SensorTimestampNs, 0L, frame.MicroVolts);

    private sealed class FiniteSingleJsonConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? 0f : reader.GetSingle();

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsFinite(value))
                writer.WriteNumberValue(value);
            else
                writer.WriteNullValue();
        }
    }

    private sealed class FiniteNullableSingleJsonConverter : JsonConverter<float?>
    {
        public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();

        public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
        {
            if (!value.HasValue || !float.IsFinite(value.Value))
                writer.WriteNullValue();
            else
                writer.WriteNumberValue(value.Value);
        }
    }

    private sealed class FiniteDoubleJsonConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? 0d : reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsFinite(value))
                writer.WriteNumberValue(value);
            else
                writer.WriteNullValue();
        }
    }

    private sealed class FiniteNullableDoubleJsonConverter : JsonConverter<double?>
    {
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (!value.HasValue || !double.IsFinite(value.Value))
                writer.WriteNullValue();
            else
                writer.WriteNumberValue(value.Value);
        }
    }
}
