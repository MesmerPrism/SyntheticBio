using System.Text.Json;
using SyntheticBio.Core;

namespace SyntheticBio.Core.Tests;

public sealed class SyntheticShowcaseExportTests
{
    [Fact]
    public void AnalyzeBundle_WithShowcasePreset_EmitsIntermediateDiagnostics()
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("entropy_high") with
        {
            DurationSeconds = 180d,
        };
        SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(
            scenario,
            new DateTimeOffset(2026, 03, 20, 10, 00, 00, TimeSpan.Zero));

        SyntheticScenarioAnalysis analysis = SyntheticMeasureHarness.AnalyzeBundle(
            bundle,
            SyntheticShowcasePreset.CreateSettings());

        Assert.Equal(SyntheticShowcasePreset.PresetId, analysis.PresetId);
        Assert.NotEmpty(analysis.Coherence.Diagnostics.AcceptedRrSamples);
        Assert.NotEmpty(analysis.Coherence.Diagnostics.ResampledTachogram);
        Assert.NotEmpty(analysis.Coherence.Diagnostics.PowerSpectrum);
        Assert.NotEmpty(analysis.Hrv.Diagnostics.AcceptedRrSamples);
        Assert.NotEmpty(analysis.Hrv.Diagnostics.AdjacentRrDeltas);
        Assert.NotEmpty(analysis.Dynamics.Diagnostics.WaveformSamples);
        Assert.NotEmpty(analysis.Dynamics.Diagnostics.AcceptedExtrema);
        Assert.NotEmpty(analysis.Dynamics.Diagnostics.IntervalSeries);
        Assert.NotEmpty(analysis.Dynamics.Diagnostics.AmplitudeSeries);
    }

    [Fact]
    public async Task ExportScenarioAsync_WritesCanonicalShowcaseFiles()
    {
        string outputRoot = Path.Combine(Path.GetTempPath(), $"syntheticbio-showcase-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("coherence_high") with
            {
                DurationSeconds = 180d,
            };
            SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(
                scenario,
                new DateTimeOffset(2026, 03, 20, 10, 00, 00, TimeSpan.Zero));

            string exportedFolder = await SyntheticSessionExporter.ExportScenarioAsync(
                bundle,
                outputRoot,
                SyntheticShowcasePreset.CreateSettings());

            Assert.True(File.Exists(Path.Combine(exportedFolder, "analysis.json")));
            Assert.True(File.Exists(Path.Combine(exportedFolder, "ecg.csv")));
            Assert.True(File.Exists(Path.Combine(exportedFolder, "ground_truth.json")));
            Assert.True(File.Exists(Path.Combine(exportedFolder, "hr_rr.csv")));
            Assert.True(File.Exists(Path.Combine(exportedFolder, "scenario.json")));
            Assert.True(File.Exists(Path.Combine(exportedFolder, "session.json")));

            using JsonDocument analysis = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(exportedFolder, "analysis.json")));
            Assert.Equal(SyntheticShowcasePreset.PresetId, analysis.RootElement.GetProperty("PresetId").GetString());
            Assert.True(analysis.RootElement.GetProperty("Coherence").GetProperty("Diagnostics").GetProperty("AcceptedRrSamples").GetArrayLength() > 0);
            Assert.True(analysis.RootElement.GetProperty("Hrv").GetProperty("Diagnostics").GetProperty("AdjacentRrDeltas").GetArrayLength() > 0);
            Assert.True(analysis.RootElement.GetProperty("Dynamics").GetProperty("Diagnostics").GetProperty("WaveformSamples").GetArrayLength() > 0);
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ExportScenarioCatalogAsync_WritesCurrentShowcaseScenarioSet()
    {
        string outputRoot = Path.Combine(Path.GetTempPath(), $"syntheticbio-showcase-catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            await SyntheticSessionExporter.ExportScenarioCatalogAsync(
                outputRoot,
                180d,
                SyntheticShowcasePreset.CreateSettings());

            string[] requiredScenarioIds =
            [
                "coherence_high",
                "coherence_low",
                "hrv_high",
                "hrv_low",
                "entropy_high",
                "entropy_low",
                "entropy_rising",
                "flat_breathing",
                "breathing_pause",
                "irregular_rr",
                "resonance_010hz",
                "off_10bpm",
                "off_12bpm",
                "off_18bpm",
                "off_24bpm",
                "jittered_breathing",
            ];

            foreach (string scenarioId in requiredScenarioIds)
            {
                string scenarioFolder = Path.Combine(outputRoot, scenarioId);
                Assert.True(Directory.Exists(scenarioFolder), $"Expected scenario folder '{scenarioId}' was not exported.");
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "scenario.json")));
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "ground_truth.json")));
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "analysis.json")));
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "hr_rr.csv")));
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "ecg.csv")));
                Assert.True(File.Exists(Path.Combine(scenarioFolder, "session.json")));
            }
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ExportScenarioAsync_SerializesNonFiniteTelemetryAsValidJson()
    {
        string outputRoot = Path.Combine(Path.GetTempPath(), $"syntheticbio-showcase-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputRoot);

        try
        {
            SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("breathing_pause") with
            {
                DurationSeconds = 360d,
            };
            SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(
                scenario,
                new DateTimeOffset(2026, 03, 20, 10, 00, 00, TimeSpan.Zero));

            string exportedFolder = await SyntheticSessionExporter.ExportScenarioAsync(
                bundle,
                outputRoot,
                SyntheticShowcasePreset.CreateSettings());

            string analysisJson = await File.ReadAllTextAsync(Path.Combine(exportedFolder, "analysis.json"));
            Assert.DoesNotContain("NaN", analysisJson, StringComparison.Ordinal);
            Assert.DoesNotContain("Infinity", analysisJson, StringComparison.Ordinal);

            using JsonDocument analysis = JsonDocument.Parse(analysisJson);
            Assert.Equal(SyntheticShowcasePreset.PresetId, analysis.RootElement.GetProperty("PresetId").GetString());
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }
}
