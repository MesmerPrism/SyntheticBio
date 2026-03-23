using System.Globalization;
using System.Text;
using PolarH10.Protocol;
using SyntheticBio.Core;

namespace SyntheticBio.Cli;

internal static class BenchmarkRunner
{
    public static async Task<string> RunAsync(string outputFolder, double durationSeconds, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);
        SyntheticShowcaseSettings showcaseSettings = SyntheticShowcasePreset.CreateSettings();

        IReadOnlyDictionary<string, ScenarioObservation> observations = SyntheticScenarioCatalog.All
            .Select(scenario => Observe(scenario.ScenarioId, durationSeconds, showcaseSettings))
            .ToDictionary(observation => observation.ScenarioId, StringComparer.OrdinalIgnoreCase);

        ScenarioObservation regular = observations["regular"];
        ScenarioObservation resonance = observations["resonance_010hz"];
        ScenarioObservation coherenceHigh = observations["coherence_high"];
        ScenarioObservation coherenceLow = observations["coherence_low"];
        ScenarioObservation hrvHigh = observations["hrv_high"];
        ScenarioObservation hrvLow = observations["hrv_low"];
        ScenarioObservation entropyHigh = observations["entropy_high"];
        ScenarioObservation entropyLow = observations["entropy_low"];
        ScenarioObservation entropyRising = observations["entropy_rising"];
        ScenarioObservation off10 = observations["off_10bpm"];
        ScenarioObservation off12 = observations["off_12bpm"];
        ScenarioObservation off18 = observations["off_18bpm"];
        ScenarioObservation off24 = observations["off_24bpm"];
        ScenarioObservation jittered = observations["jittered_breathing"];
        ScenarioObservation flat = observations["flat_breathing"];
        ScenarioObservation paused = observations["breathing_pause"];
        ScenarioObservation irregular = observations["irregular_rr"];

        bool resonancePass =
            resonance.Coherence.PeakFrequencyHz >= 0.09f &&
            resonance.Coherence.PeakFrequencyHz <= 0.11f &&
            resonance.Coherence.CurrentCoherence01 > off10.Coherence.CurrentCoherence01 &&
            resonance.Coherence.CurrentCoherence01 > off12.Coherence.CurrentCoherence01 &&
            resonance.Coherence.CurrentCoherence01 > off18.Coherence.CurrentCoherence01 &&
            resonance.Coherence.CurrentCoherence01 > off24.Coherence.CurrentCoherence01 &&
            resonance.Coherence.CurrentCoherence01 > irregular.Coherence.CurrentCoherence01;

        bool coherenceShowcasePass =
            coherenceHigh.Coherence.PeakFrequencyHz >= 0.09f &&
            coherenceHigh.Coherence.PeakFrequencyHz <= 0.11f &&
            coherenceHigh.Coherence.CurrentCoherence01 > coherenceLow.Coherence.CurrentCoherence01;

        bool hrvShowcasePass =
            hrvHigh.Hrv.HasMetricsSample &&
            hrvLow.Hrv.HasMetricsSample &&
            hrvHigh.Hrv.CurrentRmssdMs > hrvLow.Hrv.CurrentRmssdMs &&
            hrvHigh.Hrv.SdnnMs > hrvLow.Hrv.SdnnMs &&
            hrvHigh.Hrv.Pnn50Percent > hrvLow.Hrv.Pnn50Percent;

        bool entropyStableShowcasePass =
            entropyHigh.Dynamics.Interval.SampleEntropy > entropyLow.Dynamics.Interval.SampleEntropy &&
            entropyHigh.Dynamics.Amplitude.SampleEntropy > entropyLow.Dynamics.Amplitude.SampleEntropy;

        bool entropyShowcasePass =
            entropyRising.Dynamics.Interval.SampleEntropy > entropyLow.Dynamics.Interval.SampleEntropy &&
            entropyRising.Dynamics.Amplitude.SampleEntropy > entropyLow.Dynamics.Amplitude.SampleEntropy;

        bool jitterPass =
            jittered.Dynamics.Interval.SampleEntropy > regular.Dynamics.Interval.SampleEntropy &&
            jittered.Dynamics.Amplitude.SampleEntropy > regular.Dynamics.Amplitude.SampleEntropy;

        bool flatPass =
            !flat.Dynamics.IntervalHasEntropyMetrics &&
            !flat.Dynamics.AmplitudeHasEntropyMetrics;

        bool stalePass = paused.Dynamics.TrackingState == PolarH10.Protocol.PolarBreathingDynamicsTrackingState.Stale;
        bool hrvSolvePass =
            regular.Hrv.HasMetricsSample &&
            resonance.Hrv.HasMetricsSample &&
            irregular.Hrv.HasMetricsSample;

        string reportPath = Path.Combine(outputFolder, "BENCHMARK_REPORT.md");
        var sb = new StringBuilder();
        sb.AppendLine("# SyntheticBio Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"Generated at {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Showcase preset: {showcaseSettings.PresetId}");
        sb.AppendLine($"Short-term HRV benchmark settings: {showcaseSettings.Hrv.WindowSeconds:0.#} s window / {showcaseSettings.Hrv.MinimumRrSamples} RR minimum");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine($"- Resonance coherence dominates the 10/12/18/24 BPM off-resonance sweep and irregular RR: {(resonancePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Feature-showcase coherence high stays above coherence low: {(coherenceShowcasePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- RR-derived HRV windows solve for regular, resonance, and irregular scenarios: {(hrvSolvePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Feature-showcase HRV high stays above HRV low: {(hrvShowcasePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Feature-showcase entropy high stays above entropy low: {(entropyStableShowcasePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Feature-showcase entropy rising stays above entropy low: {(entropyShowcasePass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Jittered breathing entropy exceeds regular breathing: {(jitterPass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Flat breathing stays not-ready: {(flatPass ? "PASS" : "FAIL")}");
        sb.AppendLine($"- Breathing pause scenario ends stale: {(stalePass ? "PASS" : "FAIL")}");
        sb.AppendLine();
        sb.AppendLine("## Observations");
        foreach (ScenarioObservation observation in observations.Values.OrderBy(value => value.ScenarioId))
            AppendObservation(sb, observation);

        await File.WriteAllTextAsync(reportPath, sb.ToString(), ct);
        return reportPath;
    }

    private static ScenarioObservation Observe(string scenarioId, double durationSeconds, SyntheticShowcaseSettings showcaseSettings)
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get(scenarioId) with
        {
            DurationSeconds = durationSeconds,
        };
        SyntheticScenarioObservation observation = SyntheticMeasureHarness.ObserveScenario(
            scenario,
            coherenceSettings: showcaseSettings.Coherence,
            hrvSettings: showcaseSettings.Hrv,
            breathingDynamicsSettings: showcaseSettings.Dynamics);
        return new ScenarioObservation(
            scenario.ScenarioId,
            scenario.DisplayName,
            observation.Coherence,
            observation.Hrv,
            observation.Dynamics);
    }

    private static void AppendObservation(StringBuilder sb, ScenarioObservation observation)
    {
        sb.AppendLine($"### {observation.ScenarioId}");
        sb.AppendLine($"- Label: {observation.DisplayName}");
        sb.AppendLine($"- Coherence: {observation.Coherence.CurrentCoherence01.ToString("0.000", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"- Peak frequency: {observation.Coherence.PeakFrequencyHz.ToString("0.000", CultureInfo.InvariantCulture)} Hz");
        sb.AppendLine($"- HRV state: {observation.Hrv.TrackingState}");
        sb.AppendLine($"- HRV RMSSD: {(observation.Hrv.HasMetricsSample ? observation.Hrv.CurrentRmssdMs.ToString("0.0", CultureInfo.InvariantCulture) : "--")} ms");
        sb.AppendLine($"- HRV SDNN: {(observation.Hrv.HasMetricsSample ? observation.Hrv.SdnnMs.ToString("0.0", CultureInfo.InvariantCulture) : "--")} ms");
        sb.AppendLine($"- Tracking state: {observation.Dynamics.TrackingState}");
        sb.AppendLine($"- Interval entropy: {(observation.Dynamics.IntervalHasEntropyMetrics ? observation.Dynamics.Interval.SampleEntropy.ToString("0.000", CultureInfo.InvariantCulture) : "--")}");
        sb.AppendLine($"- Amplitude entropy: {(observation.Dynamics.AmplitudeHasEntropyMetrics ? observation.Dynamics.Amplitude.SampleEntropy.ToString("0.000", CultureInfo.InvariantCulture) : "--")}");
        sb.AppendLine();
    }
    private sealed record ScenarioObservation(
        string ScenarioId,
        string DisplayName,
        PolarH10.Protocol.PolarCoherenceTelemetry Coherence,
        PolarH10.Protocol.PolarHrvTelemetry Hrv,
        PolarH10.Protocol.PolarBreathingDynamicsTelemetry Dynamics);
}
