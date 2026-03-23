using PolarH10.Protocol;

namespace SyntheticBio.Core;

public sealed record SyntheticShowcaseSettings(
    string PresetId,
    PolarCoherenceSettings Coherence,
    PolarHrvSettings Hrv,
    PolarBreathingDynamicsSettings Dynamics);

public static class SyntheticShowcasePreset
{
    public const string PresetId = "showcase-v1";

    public static SyntheticShowcaseSettings CreateSettings()
    {
        return new SyntheticShowcaseSettings(
            PresetId,
            CreateCoherenceSettings(),
            CreateHrvSettings(),
            CreateBreathingDynamicsSettings());
    }

    public static PolarCoherenceSettings CreateCoherenceSettings()
    {
        return (PolarCoherenceSettings.CreateDefault() with
        {
            MinimumIbiSamples = 12,
            CoherenceWindowSeconds = 32f,
            CoherenceSmoothingSpeed = 0f,
        }).Clamp();
    }

    public static PolarHrvSettings CreateHrvSettings()
    {
        return (PolarHrvSettings.CreateDefault() with
        {
            MinimumRrSamples = 32,
            WindowSeconds = 120f,
            StaleTimeoutSeconds = 6f,
        }).Clamp();
    }

    public static PolarBreathingDynamicsSettings CreateBreathingDynamicsSettings()
    {
        return (PolarBreathingDynamicsSettings.CreateDefault() with
        {
            RetainedBreathCount = 180,
            MinimumBreathsForBasicStats = 6,
            MinimumBreathsForEntropy = 18,
            FullConfidenceBreathCount = 72,
        }).Clamp();
    }

    public static SyntheticLiveProfileSet CreateProfileSet()
    {
        return new SyntheticLiveProfileSet(
            "feature-showcase",
            [
                new SyntheticLiveDeviceDefinition("SYNTH-COH-HI-01", "Polar H10 Demo Coherence High", "coherence_high", 0),
                new SyntheticLiveDeviceDefinition("SYNTH-COH-LO-01", "Polar H10 Demo Coherence Low", "coherence_low", 11),
                new SyntheticLiveDeviceDefinition("SYNTH-HRV-HI-01", "Polar H10 Demo HRV High", "hrv_high", 23),
                new SyntheticLiveDeviceDefinition("SYNTH-HRV-LO-01", "Polar H10 Demo HRV Low", "hrv_low", 37),
                new SyntheticLiveDeviceDefinition("SYNTH-ENT-HI-01", "Polar H10 Demo Entropy High", "entropy_high", 47),
                new SyntheticLiveDeviceDefinition("SYNTH-ENT-LO-01", "Polar H10 Demo Entropy Low", "entropy_low", 59),
            ]);
    }
}
