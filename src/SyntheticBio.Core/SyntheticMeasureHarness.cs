using System.Numerics;
using PolarH10.Protocol;

namespace SyntheticBio.Core;

public sealed record SyntheticScenarioObservation(
    SyntheticScenarioBundle Bundle,
    PolarCoherenceTelemetry Coherence,
    PolarHrvTelemetry Hrv,
    PolarBreathingDynamicsTelemetry Dynamics);

public static class SyntheticMeasureHarness
{
    public static SyntheticScenarioObservation ObserveScenario(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset? startTimeUtc = null,
        PolarCoherenceSettings? coherenceSettings = null,
        PolarHrvSettings? hrvSettings = null,
        PolarBreathingDynamicsSettings? breathingDynamicsSettings = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        DateTimeOffset resolvedStartTime = startTimeUtc ?? DateTimeOffset.UtcNow - TimeSpan.FromSeconds(scenario.DurationSeconds + 0.5d);
        SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(scenario, resolvedStartTime);
        return ObserveBundle(bundle, coherenceSettings, hrvSettings, breathingDynamicsSettings);
    }

    public static SyntheticScenarioObservation ObserveBundle(
        SyntheticScenarioBundle bundle,
        PolarCoherenceSettings? coherenceSettings = null,
        PolarHrvSettings? hrvSettings = null,
        PolarBreathingDynamicsSettings? breathingDynamicsSettings = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        (PolarCoherenceTracker coherenceTracker, PolarHrvTracker hrvTracker, PolarBreathingDynamicsTracker dynamicsTracker) = RunTrackers(
            bundle,
            coherenceSettings ?? CreateDefaultCoherenceSettings(),
            hrvSettings ?? CreateDefaultHrvSettings(),
            breathingDynamicsSettings ?? CreateDefaultBreathingDynamicsSettings());

        return new SyntheticScenarioObservation(
            bundle,
            coherenceTracker.GetTelemetry(),
            hrvTracker.GetTelemetry(),
            dynamicsTracker.GetTelemetry());
    }

    public static SyntheticScenarioAnalysis AnalyzeScenario(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset? startTimeUtc = null,
        SyntheticShowcaseSettings? showcaseSettings = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        DateTimeOffset resolvedStartTime = startTimeUtc ?? DateTimeOffset.UtcNow - TimeSpan.FromSeconds(scenario.DurationSeconds + 0.5d);
        SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(scenario, resolvedStartTime);
        return AnalyzeBundle(bundle, showcaseSettings);
    }

    public static SyntheticScenarioAnalysis AnalyzeBundle(
        SyntheticScenarioBundle bundle,
        SyntheticShowcaseSettings? showcaseSettings = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        SyntheticShowcaseSettings settings = showcaseSettings ?? SyntheticShowcasePreset.CreateSettings();
        (PolarCoherenceTracker coherenceTracker, PolarHrvTracker hrvTracker, PolarBreathingDynamicsTracker dynamicsTracker) = RunTrackers(
            bundle,
            settings.Coherence,
            settings.Hrv,
            settings.Dynamics);

        return new SyntheticScenarioAnalysis(
            settings.PresetId,
            DateTimeOffset.UtcNow,
            new SyntheticCoherenceAnalysis(
                coherenceTracker.GetTelemetry(),
                coherenceTracker.GetDiagnostics()),
            new SyntheticHrvAnalysis(
                hrvTracker.GetTelemetry(),
                hrvTracker.GetDiagnostics()),
            new SyntheticBreathingDynamicsAnalysis(
                dynamicsTracker.GetTelemetry(),
                dynamicsTracker.GetDiagnostics()));
    }

    public static PolarCoherenceSettings CreateDefaultCoherenceSettings()
    {
        return new PolarCoherenceSettings
        {
            CoherenceSmoothingSpeed = 0f,
        };
    }

    public static PolarHrvSettings CreateDefaultHrvSettings()
        => PolarHrvSettings.CreateDefault();

    public static PolarHrvSettings CreatePreviewHrvSettings(float windowSeconds = 120f, int minimumRrSamples = 32)
    {
        return (PolarHrvSettings.CreateDefault() with
        {
            WindowSeconds = windowSeconds,
            MinimumRrSamples = minimumRrSamples,
            StaleTimeoutSeconds = 6f,
        }).Clamp();
    }

    public static PolarBreathingDynamicsSettings CreateDefaultBreathingDynamicsSettings()
    {
        return new PolarBreathingDynamicsSettings
        {
            MinimumBreathsForBasicStats = 6,
            MinimumBreathsForEntropy = 18,
            FullConfidenceBreathCount = 72,
            RetainedBreathCount = 180,
        };
    }

    public static PolarBreathingTelemetry ToBreathingTelemetry(SyntheticBreathingSample sample)
    {
        bool hasTracking = sample.HasTracking && !sample.IsStale;
        PolarBreathingState state = Enum.TryParse(sample.State, true, out PolarBreathingState parsed)
            ? parsed
            : PolarBreathingState.Pausing;

        return new PolarBreathingTelemetry(
            IsTransportConnected: true,
            HasReceivedAnySample: true,
            IsCalibrating: false,
            IsCalibrated: true,
            HasTracking: hasTracking,
            HasUsefulSignal: hasTracking,
            HasXzModel: true,
            CalibrationProgress01: 1f,
            CurrentVolume01: sample.Volume01,
            CurrentState: state,
            EstimatedSampleRateHz: 12.5f,
            UsefulAxisRangeG: 0.024f,
            LastProjectionG: 0f,
            Volume3d01: sample.Volume01,
            VolumeBase01: sample.Volume01,
            VolumeXz01: sample.Volume01,
            Axis: Vector3.UnitZ,
            Center: Vector3.Zero,
            BoundMin: 0f,
            BoundMax: 1f,
            XzAxis: new Vector2(1f, 0f),
            XzBoundMin: 0f,
            XzBoundMax: 1f,
            AccFrameCount: 0,
            AccSampleCount: 0,
            LastSampleAgeSeconds: Math.Max(0f, (float)(DateTimeOffset.UtcNow - sample.TimestampUtc).TotalSeconds),
            LastCalibrationFailureReason: string.Empty,
            Settings: PolarBreathingSettings.CreateDefault(),
            LastSampleReceivedAtUtc: sample.TimestampUtc);
    }

    private static (PolarCoherenceTracker Coherence, PolarHrvTracker Hrv, PolarBreathingDynamicsTracker Dynamics) RunTrackers(
        SyntheticScenarioBundle bundle,
        PolarCoherenceSettings coherenceSettings,
        PolarHrvSettings hrvSettings,
        PolarBreathingDynamicsSettings breathingDynamicsSettings)
    {
        var coherenceTracker = new PolarCoherenceTracker(coherenceSettings);
        coherenceTracker.SetTransportConnected(true);
        foreach (SyntheticHrSample sample in bundle.HrSamples)
            coherenceTracker.SubmitRrInterval(sample.RrIntervalMs);

        var hrvTracker = new PolarHrvTracker(hrvSettings);
        hrvTracker.SetTransportConnected(true);
        foreach (SyntheticHrSample sample in bundle.HrSamples)
            hrvTracker.SubmitRrInterval(sample.RrIntervalMs);

        var dynamicsTracker = new PolarBreathingDynamicsTracker(breathingDynamicsSettings);
        dynamicsTracker.SetTransportConnected(true);
        foreach (SyntheticBreathingSample sample in bundle.BreathingSamples)
            dynamicsTracker.SubmitBreathingTelemetry(ToBreathingTelemetry(sample));

        return (coherenceTracker, hrvTracker, dynamicsTracker);
    }
}
