using System.CommandLine;
using SyntheticBio.Core;
using SyntheticBio.Cli;

var pipeOption = new Option<string>(
    "--pipe",
    () => "polarh10-synth",
    "Named-pipe base name used for discovery and device streams");

var durationOption = new Option<double>(
    "--duration",
    () => 360d,
    "Scenario duration in seconds");

var outputOption = new Option<string>(
    "--out",
    () => Path.Combine(Environment.CurrentDirectory, "output"),
    "Output folder for exported fixtures or reports");

var serveCmd = new Command("serve", "Run the same-machine synthetic live server")
{
    pipeOption,
    durationOption,
};

serveCmd.SetHandler(async (string pipeBaseName, double durationSeconds) =>
{
    var server = new SyntheticPipeServer(new SyntheticPipeServerOptions
    {
        PipeBaseName = pipeBaseName,
        DurationSeconds = durationSeconds,
        LoopScenarios = true,
        ProfileSet = SyntheticScenarioCatalog.CreateStandardProfileSet(),
    });

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    await server.StartAsync(cts.Token);
    Console.WriteLine($"SyntheticBio live server running on pipe base '{pipeBaseName}' with looping {durationSeconds:0.#} s scenarios.");
    Console.WriteLine("Devices:");
    foreach (SyntheticLiveDeviceDefinition device in SyntheticScenarioCatalog.CreateStandardProfileSet().Devices)
        Console.WriteLine($"  {device.Name,-34} {device.Address}  scenario={device.ScenarioId}");
    Console.WriteLine("Press Ctrl+C to stop.");

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        await server.DisposeAsync();
    }
}, pipeOption, durationOption);

var exportCmd = new Command("export", "Export built-in scenarios as offline fixtures")
{
    outputOption,
    durationOption,
};

exportCmd.SetHandler(async (string outputFolder, double durationSeconds) =>
{
    await SyntheticSessionExporter.ExportScenarioCatalogAsync(outputFolder, durationSeconds);
    Console.WriteLine($"Exported fixtures to {Path.GetFullPath(outputFolder)}");
}, outputOption, durationOption);

var benchmarkCmd = new Command("benchmark", "Run coherence and breathing-dynamics benchmarks with the real PolarH10 trackers")
{
    outputOption,
    durationOption,
};

benchmarkCmd.SetHandler(async (string outputFolder, double durationSeconds) =>
{
    string reportPath = await BenchmarkRunner.RunAsync(outputFolder, durationSeconds);
    Console.WriteLine($"Benchmark report written to {reportPath}");
}, outputOption, durationOption);

var polarDocsRootOption = new Option<string>(
    "--polar-docs-root",
    "Path to the sibling PolarH10 docs folder")
{
    IsRequired = true,
};

var publishDocBundleCmd = new Command("publish-doc-bundle", "Export the showcase data bundle and figure assets into PolarH10/docs")
{
    polarDocsRootOption,
    durationOption,
};

publishDocBundleCmd.SetHandler(async (string polarDocsRoot, double durationSeconds) =>
{
    string manifestPath = await SyntheticDocBundlePublisher.PublishAsync(polarDocsRoot, durationSeconds);
    Console.WriteLine($"Synthetic showcase bundle published to {manifestPath}");
}, polarDocsRootOption, durationOption);

var listCmd = new Command("list", "List built-in scenarios and the default live virtual devices");
listCmd.SetHandler(() =>
{
    Console.WriteLine("Built-in scenarios:");
    foreach (SyntheticScenarioDefinition scenario in SyntheticScenarioCatalog.All)
    {
        Console.WriteLine($"  {scenario.ScenarioId,-18}  {scenario.DisplayName}");
        Console.WriteLine($"      {scenario.ExpectedBehavior}");
    }

    Console.WriteLine();
    Console.WriteLine("Default live virtual devices:");
    foreach (SyntheticLiveDeviceDefinition device in SyntheticScenarioCatalog.CreateStandardProfileSet().Devices)
        Console.WriteLine($"  {device.Name,-34} {device.Address}  scenario={device.ScenarioId}");
});

var root = new RootCommand("SyntheticBio live synthetic HR/RR + PMD ECG + breathing-volume harness")
{
    listCmd,
    serveCmd,
    exportCmd,
    benchmarkCmd,
    publishDocBundleCmd,
};

return await root.InvokeAsync(args);
