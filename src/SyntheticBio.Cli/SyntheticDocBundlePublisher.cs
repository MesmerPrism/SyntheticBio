using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using PolarH10.Protocol;
using SyntheticBio.Core;

namespace SyntheticBio.Cli;

internal static class SyntheticDocBundlePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static readonly string[] RequiredScenarioFiles =
    [
        "analysis.json",
        "ecg.csv",
        "ground_truth.json",
        "hr_rr.csv",
        "scenario.json",
        "session.json",
    ];

    public static async Task<string> PublishAsync(
        string polarDocsRoot,
        double durationSeconds,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(polarDocsRoot);

        string docsRoot = Path.GetFullPath(polarDocsRoot);
        if (!Directory.Exists(docsRoot))
            throw new DirectoryNotFoundException($"PolarH10 docs root was not found: {docsRoot}");

        string dataRoot = Path.Combine(docsRoot, "data", "synthetic-showcase");
        string scenarioRoot = Path.Combine(dataRoot, "scenarios");
        string assetsRoot = Path.Combine(docsRoot, "assets", "synthetic-showcase");

        RecreateDirectory(dataRoot);
        RecreateDirectory(assetsRoot);
        Directory.CreateDirectory(scenarioRoot);

        SyntheticShowcaseSettings showcaseSettings = SyntheticShowcasePreset.CreateSettings();
        await SyntheticSessionExporter.ExportScenarioCatalogAsync(scenarioRoot, durationSeconds, showcaseSettings, ct);

        string syntheticRepoRoot = FindRepoRoot(AppContext.BaseDirectory, "SyntheticBio.sln") ??
                                   throw new DirectoryNotFoundException("Unable to locate the SyntheticBio repo root.");
        string rendererPath = Path.Combine(syntheticRepoRoot, "scripts", "render_showcase_figures.py");
        if (!File.Exists(rendererPath))
            throw new FileNotFoundException("Showcase figure renderer script was not found.", rendererPath);

        string inventoryPath = Path.Combine(dataRoot, "figure-assets.generated.json");
        await RunFigureRendererAsync(rendererPath, dataRoot, assetsRoot, inventoryPath, ct);

        FigureInventory inventory = await LoadFigureInventoryAsync(inventoryPath, ct);
        string manifestPath = Path.Combine(dataRoot, "showcase-manifest.json");
        ShowcaseManifest manifest = BuildManifest(docsRoot, dataRoot, scenarioRoot, assetsRoot, durationSeconds, showcaseSettings, inventory);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), ct);

        File.Delete(inventoryPath);
        return manifestPath;
    }

    private static async Task RunFigureRendererAsync(
        string rendererPath,
        string dataRoot,
        string assetsRoot,
        string inventoryPath,
        CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "python",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        process.StartInfo.ArgumentList.Add(rendererPath);
        process.StartInfo.ArgumentList.Add("--data-root");
        process.StartInfo.ArgumentList.Add(dataRoot);
        process.StartInfo.ArgumentList.Add("--assets-root");
        process.StartInfo.ArgumentList.Add(assetsRoot);
        process.StartInfo.ArgumentList.Add("--inventory-out");
        process.StartInfo.ArgumentList.Add(inventoryPath);

        if (!process.Start())
            throw new InvalidOperationException("Unable to start the showcase figure renderer.");

        string stdout = await process.StandardOutput.ReadToEndAsync(ct);
        string stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Showcase figure renderer failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    private static async Task<FigureInventory> LoadFigureInventoryAsync(string path, CancellationToken ct)
    {
        await using FileStream stream = File.OpenRead(path);
        FigureInventory? inventory = await JsonSerializer.DeserializeAsync<FigureInventory>(stream, JsonOptions, ct);
        if (inventory is null || inventory.Assets.Count == 0)
            throw new InvalidOperationException("The figure renderer did not produce a usable asset inventory.");

        return inventory;
    }

    private static ShowcaseManifest BuildManifest(
        string docsRoot,
        string dataRoot,
        string scenarioRoot,
        string assetsRoot,
        double durationSeconds,
        SyntheticShowcaseSettings showcaseSettings,
        FigureInventory inventory)
    {
        List<ShowcaseScenarioEntry> scenarios = Directory.GetDirectories(scenarioRoot)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => BuildScenarioEntry(docsRoot, path))
            .ToList();

        List<ShowcaseAssetEntry> assets = inventory.Assets
            .OrderBy(static asset => asset.Id, StringComparer.OrdinalIgnoreCase)
            .Select(asset => BuildAssetEntry(docsRoot, assetsRoot, asset))
            .ToList();

        return new ShowcaseManifest(
            PresetId: showcaseSettings.PresetId,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            DurationSeconds: durationSeconds,
            GeneratorVersion: typeof(SyntheticDocBundlePublisher).Assembly.GetName().Version?.ToString() ?? "1.0.0.0",
            TrackerSettings: new ShowcaseTrackerSettings(showcaseSettings.Coherence, showcaseSettings.Hrv, showcaseSettings.Dynamics),
            ScenarioIds: scenarios.Select(static scenario => scenario.ScenarioId).ToArray(),
            Scenarios: scenarios,
            Assets: assets,
            DataRootPath: ToDocsRelativePath(docsRoot, dataRoot));
    }

    private static ShowcaseScenarioEntry BuildScenarioEntry(string docsRoot, string scenarioDirectory)
    {
        string scenarioId = Path.GetFileName(scenarioDirectory);
        List<ShowcaseFileEntry> files = RequiredScenarioFiles
            .Select(fileName => Path.Combine(scenarioDirectory, fileName))
            .Select(path => BuildFileEntry(docsRoot, path))
            .ToList();

        return new ShowcaseScenarioEntry(
            ScenarioId: scenarioId,
            Path: ToDocsRelativePath(docsRoot, scenarioDirectory),
            Files: files);
    }

    private static ShowcaseAssetEntry BuildAssetEntry(
        string docsRoot,
        string assetsRoot,
        FigureInventoryAsset asset)
    {
        string fullPath = Path.Combine(assetsRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
        ShowcaseFileEntry file = BuildFileEntry(docsRoot, fullPath);
        return new ShowcaseAssetEntry(
            Id: asset.Id,
            Role: asset.Role,
            Format: asset.Format,
            Title: asset.Title,
            Path: file.Path,
            SizeBytes: file.SizeBytes,
            LastWriteTimeUtc: file.LastWriteTimeUtc,
            Sha256: file.Sha256);
    }

    private static ShowcaseFileEntry BuildFileEntry(string docsRoot, string fullPath)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Expected showcase file was not found.", fullPath);

        FileInfo info = new(fullPath);
        return new ShowcaseFileEntry(
            Name: info.Name,
            Path: ToDocsRelativePath(docsRoot, fullPath),
            SizeBytes: info.Length,
            LastWriteTimeUtc: info.LastWriteTimeUtc.ToString("O", CultureInfo.InvariantCulture),
            Sha256: ComputeSha256(fullPath));
    }

    private static string ComputeSha256(string path)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        Directory.CreateDirectory(path);
    }

    private static string ToDocsRelativePath(string docsRoot, string fullPath)
        => Path.GetRelativePath(docsRoot, fullPath).Replace('\\', '/');

    private static string? FindRepoRoot(string startDirectory, string markerFileName)
    {
        DirectoryInfo? current = new(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, markerFileName)))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }

    private sealed record FigureInventory(IReadOnlyList<FigureInventoryAsset> Assets);

    private sealed record FigureInventoryAsset(
        string Id,
        string Role,
        string Format,
        string Path,
        string Title);

    private sealed record ShowcaseManifest(
        string PresetId,
        DateTimeOffset GeneratedAtUtc,
        double DurationSeconds,
        string GeneratorVersion,
        ShowcaseTrackerSettings TrackerSettings,
        IReadOnlyList<string> ScenarioIds,
        IReadOnlyList<ShowcaseScenarioEntry> Scenarios,
        IReadOnlyList<ShowcaseAssetEntry> Assets,
        string DataRootPath);

    private sealed record ShowcaseTrackerSettings(
        PolarCoherenceSettings Coherence,
        PolarHrvSettings Hrv,
        PolarBreathingDynamicsSettings Dynamics);

    private sealed record ShowcaseScenarioEntry(
        string ScenarioId,
        string Path,
        IReadOnlyList<ShowcaseFileEntry> Files);

    private sealed record ShowcaseFileEntry(
        string Name,
        string Path,
        long SizeBytes,
        string LastWriteTimeUtc,
        string Sha256);

    private sealed record ShowcaseAssetEntry(
        string Id,
        string Role,
        string Format,
        string Title,
        string Path,
        long SizeBytes,
        string LastWriteTimeUtc,
        string Sha256);
}
