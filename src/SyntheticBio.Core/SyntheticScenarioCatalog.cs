namespace SyntheticBio.Core;

public static class SyntheticScenarioCatalog
{
    private static readonly IReadOnlyDictionary<string, SyntheticScenarioDefinition> Scenarios =
        CreateScenarios();

    public static IReadOnlyCollection<SyntheticScenarioDefinition> All => Scenarios.Values.OrderBy(s => s.ScenarioId).ToArray();

    public static SyntheticScenarioDefinition Get(string scenarioId)
    {
        if (!Scenarios.TryGetValue(scenarioId, out SyntheticScenarioDefinition? scenario))
            throw new InvalidOperationException($"Unknown scenario '{scenarioId}'.");

        return scenario;
    }

    public static SyntheticLiveProfileSet CreateStandardProfileSet()
    {
        return SyntheticShowcasePreset.CreateProfileSet();
    }

    public static SyntheticScenarioDefinition ForLiveDevice(SyntheticLiveDeviceDefinition device, double durationSeconds)
    {
        SyntheticScenarioDefinition baseScenario = Get(device.ScenarioId);
        return baseScenario with
        {
            Seed = baseScenario.Seed + device.SeedOffset,
            DurationSeconds = durationSeconds,
            DisplayName = device.Name,
        };
    }

    private static IReadOnlyDictionary<string, SyntheticScenarioDefinition> CreateScenarios()
    {
        return new Dictionary<string, SyntheticScenarioDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["regular"] = new()
            {
                ScenarioId = "regular",
                DisplayName = "Regular breathing / stable sinus rhythm",
                ExpectedBehavior = "Lower entropy and moderate coherence.",
                Seed = 1001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 24d,
                    SlowModulationMs = 9d,
                    IrregularNoiseMs = 3d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.27d,
                    HarmonicAmplitude01 = 0.024d,
                    JitterAmount01 = 0.015d,
                },
            },
            ["coherence_high"] = new()
            {
                ScenarioId = "coherence_high",
                DisplayName = "Coherence demo / high resonance",
                ExpectedBehavior = "Coherence should settle high near 0.10 Hz once the rolling window fills.",
                Seed = 1101,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 900d,
                    RsaAmplitudeMs = 110d,
                    SlowModulationMs = 12d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 6d,
                    Amplitude01 = 0.31d,
                    HarmonicAmplitude01 = 0.030d,
                    JitterAmount01 = 0.010d,
                    WobbleRateHz = 0.08d,
                },
            },
            ["coherence_low"] = new()
            {
                ScenarioId = "coherence_low",
                DisplayName = "Coherence demo / low and unstable",
                ExpectedBehavior = "Coherence should stay low because respiration remains off-resonance and RR stays noisy.",
                Seed = 1102,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 840d,
                    RsaAmplitudeMs = 14d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 62d,
                    EctopyEveryNBeats = 9,
                    EctopyDeltaMs = 180d,
                    CompensatoryDeltaMs = 115d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 17.5d,
                    Amplitude01 = 0.22d,
                    HarmonicAmplitude01 = 0.018d,
                    JitterAmount01 = 0.11d,
                    WobbleRateHz = 0.19d,
                },
            },
            ["hrv_high"] = new()
            {
                ScenarioId = "hrv_high",
                DisplayName = "HRV demo / high beat-to-beat variability",
                ExpectedBehavior = "RMSSD, SDNN, and pNN50 should stay materially above hrv_low because adjacent RR differences are larger.",
                Seed = 1201,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 890d,
                    RsaAmplitudeMs = 92d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 18d,
                    Amplitude01 = 0.30d,
                    HarmonicAmplitude01 = 0.026d,
                    JitterAmount01 = 0.012d,
                    WobbleRateHz = 0.11d,
                },
            },
            ["hrv_low"] = new()
            {
                ScenarioId = "hrv_low",
                DisplayName = "HRV demo / low beat-to-beat variability",
                ExpectedBehavior = "RMSSD, SDNN, and pNN50 should stay low because the accepted RR stream changes only slightly from beat to beat.",
                Seed = 1202,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 8d,
                    SlowModulationMs = 4d,
                    IrregularNoiseMs = 1.5d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.24d,
                    HarmonicAmplitude01 = 0.014d,
                    JitterAmount01 = 0.005d,
                    WobbleRateHz = 0.07d,
                },
            },
            ["resonance_010hz"] = new()
            {
                ScenarioId = "resonance_010hz",
                DisplayName = "0.10 Hz resonance breathing",
                ExpectedBehavior = "High coherence near 0.10 Hz and stable breathing metrics.",
                Seed = 2001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 900d,
                    RsaAmplitudeMs = 110d,
                    SlowModulationMs = 12d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 6d,
                    Amplitude01 = 0.31d,
                    HarmonicAmplitude01 = 0.030d,
                    JitterAmount01 = 0.010d,
                },
            },
            ["off_12bpm"] = new()
            {
                ScenarioId = "off_12bpm",
                DisplayName = "Off-resonance 12 BPM",
                ExpectedBehavior = "Coherence lower than resonance.",
                Seed = 3001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 45d,
                    SlowModulationMs = 9d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.26d,
                    HarmonicAmplitude01 = 0.026d,
                    JitterAmount01 = 0.020d,
                },
            },
            ["off_10bpm"] = new()
            {
                ScenarioId = "off_10bpm",
                DisplayName = "Off-resonance 10 BPM",
                ExpectedBehavior = "Coherence lower than resonance with slower but off-target breathing.",
                Seed = 3000,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 880d,
                    RsaAmplitudeMs = 58d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 10d,
                    Amplitude01 = 0.28d,
                    HarmonicAmplitude01 = 0.028d,
                    JitterAmount01 = 0.018d,
                },
            },
            ["off_18bpm"] = new()
            {
                ScenarioId = "off_18bpm",
                DisplayName = "Off-resonance 18 BPM",
                ExpectedBehavior = "Coherence lower than resonance.",
                Seed = 3002,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 840d,
                    RsaAmplitudeMs = 24d,
                    SlowModulationMs = 8d,
                    IrregularNoiseMs = 5d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 18d,
                    Amplitude01 = 0.23d,
                    HarmonicAmplitude01 = 0.020d,
                    JitterAmount01 = 0.025d,
                },
            },
            ["off_24bpm"] = new()
            {
                ScenarioId = "off_24bpm",
                DisplayName = "Off-resonance 24 BPM",
                ExpectedBehavior = "Coherence lower than resonance and smaller breathing excursion.",
                Seed = 3003,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 820d,
                    RsaAmplitudeMs = 12d,
                    SlowModulationMs = 7d,
                    IrregularNoiseMs = 5d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 24d,
                    Amplitude01 = 0.18d,
                    HarmonicAmplitude01 = 0.015d,
                    JitterAmount01 = 0.020d,
                },
            },
            ["entropy_low"] = new()
            {
                ScenarioId = "entropy_low",
                DisplayName = "Entropy demo / calm paced breathing",
                ExpectedBehavior = "Interval and amplitude entropy should remain low while breathing stays regular.",
                Seed = 4100,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 24d,
                    SlowModulationMs = 9d,
                    IrregularNoiseMs = 1.5d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.24d,
                    HarmonicAmplitude01 = 0.014d,
                    JitterAmount01 = 0.005d,
                    WobbleRateHz = 0.07d,
                },
            },
            ["entropy_high"] = new()
            {
                ScenarioId = "entropy_high",
                DisplayName = "Entropy demo / high irregularity",
                ExpectedBehavior = "Interval and amplitude entropy should stay above entropy_low because breath timing and excursion depth keep varying.",
                Seed = 4102,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 28d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 9d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.28d,
                    HarmonicAmplitude01 = 0.035d,
                    JitterAmount01 = 0.22d,
                    WobbleRateHz = 0.17d,
                },
            },
            ["jittered_breathing"] = new()
            {
                ScenarioId = "jittered_breathing",
                DisplayName = "Jittered breathing",
                ExpectedBehavior = "Higher interval and amplitude entropy than regular breathing.",
                Seed = 4001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 28d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 9d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.28d,
                    HarmonicAmplitude01 = 0.035d,
                    JitterAmount01 = 0.22d,
                    WobbleRateHz = 0.17d,
                },
            },
            ["entropy_rising"] = new()
            {
                ScenarioId = "entropy_rising",
                DisplayName = "Entropy demo / increasing variability",
                ExpectedBehavior = "Interval and amplitude entropy should rise gradually as breathing becomes less regular.",
                Seed = 4101,
                TransitionStartFraction = 0.10d,
                TransitionEndFraction = 0.65d,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 860d,
                    RsaAmplitudeMs = 24d,
                    SlowModulationMs = 9d,
                    IrregularNoiseMs = 2d,
                },
                HeartEnd = new SyntheticHeartProfile
                {
                    CenterIbiMs = 858d,
                    RsaAmplitudeMs = 28d,
                    SlowModulationMs = 10d,
                    IrregularNoiseMs = 9d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.26d,
                    HarmonicAmplitude01 = 0.022d,
                    JitterAmount01 = 0.018d,
                    WobbleRateHz = 0.09d,
                },
                BreathingEnd = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.28d,
                    HarmonicAmplitude01 = 0.035d,
                    JitterAmount01 = 0.22d,
                    WobbleRateHz = 0.17d,
                },
            },
            ["flat_breathing"] = new()
            {
                ScenarioId = "flat_breathing",
                DisplayName = "Flat low-excursion breathing",
                ExpectedBehavior = "Breathing dynamics should remain not-ready.",
                Seed = 5001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 850d,
                    RsaAmplitudeMs = 8d,
                    SlowModulationMs = 5d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.015d,
                    HarmonicAmplitude01 = 0.004d,
                    JitterAmount01 = 0.01d,
                },
            },
            ["breathing_pause"] = new()
            {
                ScenarioId = "breathing_pause",
                DisplayName = "Breathing pause / stale input",
                ExpectedBehavior = "Breathing dynamics should turn stale when the waveform pauses near the end of the scenario.",
                Seed = 6001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 870d,
                    RsaAmplitudeMs = 24d,
                    SlowModulationMs = 9d,
                    IrregularNoiseMs = 4d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.26d,
                    HarmonicAmplitude01 = 0.020d,
                    JitterAmount01 = 0.03d,
                    PauseTailSeconds = 40d,
                },
            },
            ["irregular_rr"] = new()
            {
                ScenarioId = "irregular_rr",
                DisplayName = "Irregular RR / AF-like",
                ExpectedBehavior = "Coherence should be materially lower than resonance.",
                Seed = 7001,
                Heart = new SyntheticHeartProfile
                {
                    CenterIbiMs = 890d,
                    RsaAmplitudeMs = 16d,
                    SlowModulationMs = 14d,
                    IrregularNoiseMs = 95d,
                    EctopyEveryNBeats = 7,
                    EctopyDeltaMs = 220d,
                    CompensatoryDeltaMs = 140d,
                },
                Breathing = new SyntheticBreathingProfile
                {
                    BaseRateBpm = 12d,
                    Amplitude01 = 0.24d,
                    HarmonicAmplitude01 = 0.030d,
                    JitterAmount01 = 0.18d,
                },
            },
        };
    }
}
