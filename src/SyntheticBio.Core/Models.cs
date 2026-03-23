namespace SyntheticBio.Core;

public sealed record SyntheticHeartProfile
{
    public double CenterIbiMs { get; init; } = 860d;
    public double RsaAmplitudeMs { get; init; } = 30d;
    public double SlowModulationMs { get; init; } = 10d;
    public double IrregularNoiseMs { get; init; } = 4d;
    public int EctopyEveryNBeats { get; init; }
    public double EctopyDeltaMs { get; init; } = 180d;
    public double CompensatoryDeltaMs { get; init; } = 120d;
}

public sealed record SyntheticBreathingProfile
{
    public double SampleRateHz { get; init; } = 12.5d;
    public double BaseRateBpm { get; init; } = 12d;
    public double Amplitude01 { get; init; } = 0.28d;
    public double HarmonicAmplitude01 { get; init; } = 0.03d;
    public double JitterAmount01 { get; init; } = 0.02d;
    public double WobbleRateHz { get; init; } = 0.09d;
    public double PauseStartSeconds { get; init; } = -1d;
    public double PauseDurationSeconds { get; init; }
    public double PauseTailSeconds { get; init; }
}

public sealed record SyntheticScenarioDefinition
{
    public string ScenarioId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ExpectedBehavior { get; init; } = string.Empty;
    public int Seed { get; init; } = 1337;
    public double DurationSeconds { get; init; } = 180d;
    public SyntheticHeartProfile Heart { get; init; } = new();
    public SyntheticHeartProfile? HeartEnd { get; init; }
    public SyntheticBreathingProfile Breathing { get; init; } = new();
    public SyntheticBreathingProfile? BreathingEnd { get; init; }
    public double TransitionStartFraction { get; init; }
    public double TransitionEndFraction { get; init; } = 1d;
}

public sealed record SyntheticLiveDeviceDefinition(string Address, string Name, string ScenarioId, int SeedOffset = 0);

public sealed record SyntheticLiveProfileSet(string ProfileSetId, IReadOnlyList<SyntheticLiveDeviceDefinition> Devices);

public sealed record SyntheticHrSample(DateTimeOffset TimestampUtc, double ElapsedSeconds, ushort HeartRateBpm, float RrIntervalMs);

public sealed record SyntheticBreathingSample(
    DateTimeOffset TimestampUtc,
    double ElapsedSeconds,
    float Volume01,
    string State,
    bool HasTracking,
    bool IsStale);

public sealed record SyntheticEcgFrame(
    DateTimeOffset TimestampUtc,
    double ElapsedSeconds,
    long SensorTimestampNs,
    int[] MicroVolts);

public sealed record SyntheticScenarioBundle(
    SyntheticScenarioDefinition Scenario,
    IReadOnlyList<SyntheticHrSample> HrSamples,
    IReadOnlyList<SyntheticBreathingSample> BreathingSamples,
    IReadOnlyList<SyntheticEcgFrame> EcgFrames);
