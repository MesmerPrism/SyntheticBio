using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using SyntheticBio.Core;

namespace SyntheticBio.App;

internal sealed record PolarAppRuntimeStatusSnapshot(
    int ProcessId,
    string? ProcessPath,
    string TransportName,
    string SyntheticPipeBaseName,
    DateTimeOffset StartedAtUtc);

public partial class MainWindow : Window
{
    private const int SwRestore = 9;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private SyntheticPipeServer? _server;
    private readonly SyntheticLiveProfileSet _profileSet = SyntheticScenarioCatalog.CreateStandardProfileSet();
    private string? _activePipeBaseName;
    private double? _activeDurationSeconds;

    public MainWindow()
    {
        InitializeComponent();
        OutputFolderTextBox.Text = GetDefaultOutputFolder();
        PolarAppPathTextBox.Text = DetectPolarAppPath() ?? string.Empty;
        PopulateDevices();
        SetStatus("Idle", "GraphiteBrush");
        AddLog("SyntheticBio operator app ready.");
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        _ = StartServerAsync();
    }

    private async Task StartServerAsync(string? preferredPipeBaseName = null)
    {
        if (!TryGetDurationSeconds(out double durationSeconds))
            return;

        string pipeBaseName = string.IsNullOrWhiteSpace(preferredPipeBaseName)
            ? ResolvePipeBaseName()
            : preferredPipeBaseName.Trim();

        if (_server is not null)
        {
            bool samePipe = string.Equals(_activePipeBaseName, pipeBaseName, StringComparison.OrdinalIgnoreCase);
            bool sameDuration = _activeDurationSeconds is not null &&
                                Math.Abs(_activeDurationSeconds.Value - durationSeconds) < 0.001d;
            if (samePipe && sameDuration)
                return;

            await StopServerAsync();
        }

        _server = new SyntheticPipeServer(new SyntheticPipeServerOptions
        {
            PipeBaseName = pipeBaseName,
            DurationSeconds = durationSeconds,
            LoopScenarios = true,
            ProfileSet = _profileSet,
        });

        await _server.StartAsync();
        _activePipeBaseName = pipeBaseName;
        _activeDurationSeconds = durationSeconds;
        PipeBaseNameTextBox.Text = pipeBaseName;
        SetStatus("Running", "TelemetryGreenBrush");
        SessionHintTextBlock.Text = $"Live server ready on '{pipeBaseName}'. Each virtual device loops its {durationSeconds:0.#} s scenario until you stop the server.";
        AddLog($"Started live server on pipe base '{pipeBaseName}' with looping {durationSeconds:0.#} s scenarios.");
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        _ = StopServerAsync();
    }

    private async Task StopServerAsync()
    {
        if (_server is null)
            return;

        await _server.DisposeAsync();
        _server = null;
        _activePipeBaseName = null;
        _activeDurationSeconds = null;
        SetStatus("Stopped", "HazardYellowBrush");
        SessionHintTextBlock.Text = "Server stopped. Start it again before launching or reconnecting PolarH10.";
        AddLog("Stopped live server.");
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        _ = ExportAsync();
    }

    private async Task ExportAsync()
    {
        if (!TryGetDurationSeconds(out double durationSeconds))
            return;

        string outputFolder = ResolveOutputFolder();
        await SyntheticSessionExporter.ExportScenarioCatalogAsync(outputFolder, durationSeconds);
        SetStatus("Fixtures exported", "FocusBlueBrush");
        SessionHintTextBlock.Text = $"Fixtures exported to '{outputFolder}'.";
        AddLog($"Exported fixtures to {outputFolder}");
    }

    private async void OnEstablishConnectionClick(object sender, RoutedEventArgs e)
    {
        await EstablishPolarConnectionAsync();
    }

    private void OnBrowsePolarAppClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select fallback PolarH10.App.exe",
            Filter = "Executable|*.exe",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) != true)
            return;

        PolarAppPathTextBox.Text = dialog.FileName;
    }

    private void OnOpenOutputFolderClick(object sender, RoutedEventArgs e)
    {
        string outputFolder = ResolveOutputFolder();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{outputFolder}\"",
            UseShellExecute = true,
        });
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
            _server = null;
        }

        base.OnClosed(e);
    }

    private void PopulateDevices()
    {
        DevicesListBox.Items.Clear();
        foreach (SyntheticLiveDeviceDefinition device in _profileSet.Devices)
            DevicesListBox.Items.Add($"{device.Name}\n{device.Address}  /  scenario={device.ScenarioId}");
    }

    private void SetStatus(string text, string brushResourceKey)
    {
        StatusTextBlock.Text = text;
        StatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource(brushResourceKey);
    }

    private void AddLog(string message)
    {
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private async Task EstablishPolarConnectionAsync()
    {
        PolarAppRuntimeStatusSnapshot? runningStatus = TryReadRunningPolarStatus();
        if (runningStatus is not null)
        {
            if (string.Equals(runningStatus.TransportName, "synthetic", StringComparison.OrdinalIgnoreCase))
            {
                string runningPipeBaseName = string.IsNullOrWhiteSpace(runningStatus.SyntheticPipeBaseName)
                    ? "polarh10-synth"
                    : runningStatus.SyntheticPipeBaseName.Trim();

                if (!string.Equals(ResolvePipeBaseName(), runningPipeBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    PipeBaseNameTextBox.Text = runningPipeBaseName;
                    AddLog($"Aligned pipe base to the running PolarH10 session: '{runningPipeBaseName}'.");
                }

                await StartServerAsync(runningPipeBaseName);
                BringProcessToFront(runningStatus.ProcessId);
                SetStatus("PolarH10 connected", "FocusBlueBrush");
                SessionHintTextBlock.Text = $"Using the running PolarH10 session on synthetic pipe '{runningPipeBaseName}'. Scan and connect to a virtual device in PolarH10.";
                AddLog($"Reused the running PolarH10 session (PID {runningStatus.ProcessId}) on synthetic pipe '{runningPipeBaseName}'.");
                return;
            }

            BringProcessToFront(runningStatus.ProcessId);
            SetStatus("Restart required", "HazardYellowBrush");
            SessionHintTextBlock.Text = "PolarH10 is already running with Windows BLE transport. Close it, then establish connection again to start or reuse a synthetic session.";
            AddLog($"Connection blocked: running PolarH10 session (PID {runningStatus.ProcessId}) uses '{runningStatus.TransportName}' transport.");
            return;
        }

        int recycledHeadlessCount = RecycleHeadlessPolarProcesses(TimeSpan.FromSeconds(5));
        if (recycledHeadlessCount > 0)
        {
            AddLog($"Removed {recycledHeadlessCount} headless PolarH10 process(es) before launch.");
            TryDeleteStalePolarStatus(GetPolarRuntimeStatusPath());
        }

        Process? runningProcess = FindRunningPolarProcess();
        if (runningProcess is not null)
        {
            BringProcessToFront(runningProcess.Id);
            SetStatus("PolarH10 already open", "HazardYellowBrush");
            SessionHintTextBlock.Text = "A PolarH10 window is already open, but this build could not verify its transport mode. Close it, then establish connection again if you want a guaranteed synthetic session.";
            AddLog($"Connection blocked: found an existing PolarH10 window (PID {runningProcess.Id}) without a readable runtime status.");
            return;
        }

        string? polarAppPath = await ResolvePolarAppPathAsync();
        if (string.IsNullOrWhiteSpace(polarAppPath) || !File.Exists(polarAppPath))
        {
            SetStatus("PolarH10 missing", "SignalRedBrush");
            SessionHintTextBlock.Text = "Keep the sibling PolarH10 repo available or select a valid fallback PolarH10.App.exe before launching the session.";
            AddLog("Launch blocked: no launchable PolarH10 workspace build or fallback PolarH10.App.exe was found.");
            return;
        }

        await StartServerAsync();
        if (_server is null)
            return;

        string pipeBaseName = ResolvePipeBaseName();
        bool launchSucceeded = await LaunchPolarProcessAndWaitAsync(polarAppPath, pipeBaseName);
        if (!launchSucceeded)
            return;

        SetStatus("PolarH10 launched", "FocusBlueBrush");
        SessionHintTextBlock.Text = $"Launched PolarH10 with synthetic transport on pipe '{pipeBaseName}'. Scan and connect to one of the virtual devices.";
        AddLog($"Launched PolarH10 with synthetic pipe '{pipeBaseName}'.");
    }

    private async Task<bool> LaunchPolarProcessAndWaitAsync(string polarAppPath, string pipeBaseName)
    {
        StartPolarProcess(polarAppPath, pipeBaseName);
        PolarAppRuntimeStatusSnapshot? launchedStatus = await WaitForRunningPolarStatusAsync(
            pipeBaseName,
            TimeSpan.FromSeconds(6));

        if (launchedStatus is not null)
            return true;

        int recycledHeadlessCount = RecycleHeadlessPolarProcesses(TimeSpan.FromSeconds(2));
        if (recycledHeadlessCount > 0)
        {
            AddLog($"Launch did not produce a usable PolarH10 window. Removed {recycledHeadlessCount} headless process(es) and retrying once.");
            TryDeleteStalePolarStatus(GetPolarRuntimeStatusPath());
            StartPolarProcess(polarAppPath, pipeBaseName);
            launchedStatus = await WaitForRunningPolarStatusAsync(
                pipeBaseName,
                TimeSpan.FromSeconds(6));
            if (launchedStatus is not null)
                return true;
        }

        SetStatus("PolarH10 launch failed", "SignalRedBrush");
        SessionHintTextBlock.Text = $"PolarH10 did not open a usable synthetic session on pipe '{pipeBaseName}'. If an older build is stuck in the background, close it and try again.";
        AddLog($"Launch failed: no usable PolarH10 synthetic session appeared on pipe '{pipeBaseName}'.");
        return false;
    }

    private static void StartPolarProcess(string polarAppPath, string pipeBaseName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = polarAppPath,
            WorkingDirectory = Path.GetDirectoryName(polarAppPath) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--transport");
        startInfo.ArgumentList.Add("synthetic");
        startInfo.ArgumentList.Add("--synthetic-pipe");
        startInfo.ArgumentList.Add(pipeBaseName);
        startInfo.Environment["POLARH10_TRANSPORT"] = "synthetic";
        startInfo.Environment["POLARH10_SYNTHETIC_PIPE"] = pipeBaseName;

        Process.Start(startInfo);
    }

    private string ResolvePipeBaseName()
        => string.IsNullOrWhiteSpace(PipeBaseNameTextBox.Text)
            ? "polarh10-synth"
            : PipeBaseNameTextBox.Text.Trim();

    private string ResolveOutputFolder()
    {
        string path = string.IsNullOrWhiteSpace(OutputFolderTextBox.Text)
            ? GetDefaultOutputFolder()
            : OutputFolderTextBox.Text.Trim();
        Directory.CreateDirectory(path);
        OutputFolderTextBox.Text = path;
        return path;
    }

    private static string GetDefaultOutputFolder()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SyntheticBio",
            "Fixtures");

    private async Task<string?> ResolvePolarAppPathAsync()
    {
        string? polarRepoRoot = DetectPolarRepoRoot();
        if (!string.IsNullOrWhiteSpace(polarRepoRoot))
        {
            string workspaceAppPath = GetCanonicalPolarWorkspaceAppPath(polarRepoRoot);
            PolarAppPathTextBox.Text = workspaceAppPath;

            if (await EnsurePolarWorkspaceBuildAsync(polarRepoRoot, workspaceAppPath))
                return workspaceAppPath;

            return null;
        }

        string candidate = PolarAppPathTextBox.Text.Trim();
        if (File.Exists(candidate))
            return candidate;

        string? detected = DetectPolarAppPath();
        if (detected is not null)
            PolarAppPathTextBox.Text = detected;
        return detected is not null && File.Exists(detected) ? detected : null;
    }

    private bool TryGetDurationSeconds(out double durationSeconds)
    {
        if (!double.TryParse(DurationTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out durationSeconds) ||
            durationSeconds < 30d)
        {
            SetStatus("Invalid duration", "SignalRedBrush");
            SessionHintTextBlock.Text = "Use a duration of at least 30 seconds.";
            AddLog($"Invalid duration value '{DurationTextBox.Text}'.");
            durationSeconds = 0d;
            return false;
        }

        return true;
    }

    private static string? DetectPolarAppPath()
    {
        string? repoRoot = DetectPolarRepoRoot();
        if (!string.IsNullOrWhiteSpace(repoRoot))
            return GetCanonicalPolarWorkspaceAppPath(repoRoot);

        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PolarH10",
                "app-single",
                "PolarH10.App.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "source",
                "repos",
                "PolarH10",
                "artifacts",
                "publish",
                "PolarH10.App-win-x64",
                "PolarH10.App.exe"),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private async Task<bool> EnsurePolarWorkspaceBuildAsync(string polarRepoRoot, string workspaceAppPath)
    {
        string buildScriptPath = Path.Combine(polarRepoRoot, "tools", "app", "Build-Workspace-App.ps1");
        if (!File.Exists(buildScriptPath))
        {
            SetStatus("PolarH10 build script missing", "SignalRedBrush");
            SessionHintTextBlock.Text = "The sibling PolarH10 repo is present but its workspace build script is missing.";
            AddLog($"Launch blocked: workspace build script not found at {buildScriptPath}.");
            return false;
        }

        SetStatus("Building PolarH10", "FocusBlueBrush");
        SessionHintTextBlock.Text = "Building the current PolarH10 workspace app before launch.";
        AddLog($"Building PolarH10 workspace app at {workspaceAppPath}.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = polarRepoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(buildScriptPath);
        startInfo.ArgumentList.Add("-Configuration");
        startInfo.ArgumentList.Add("Release");

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            SetStatus("PolarH10 build failed", "SignalRedBrush");
            SessionHintTextBlock.Text = "Unable to start the PolarH10 workspace build process.";
            AddLog("Launch blocked: PowerShell did not start the PolarH10 workspace build.");
            return false;
        }

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || !File.Exists(workspaceAppPath))
        {
            SetStatus("PolarH10 build failed", "SignalRedBrush");
            SessionHintTextBlock.Text = "The sibling PolarH10 workspace build failed. Fix the build before launching the session.";
            AddLog($"Launch blocked: PolarH10 workspace build failed with exit code {process.ExitCode}.");

            string? detail = GetLastNonEmptyLine(stderr) ?? GetLastNonEmptyLine(stdout);
            if (!string.IsNullOrWhiteSpace(detail))
                AddLog(detail);

            return false;
        }

        string? summary = GetLastNonEmptyLine(stdout);
        if (!string.IsNullOrWhiteSpace(summary))
            AddLog(summary);

        return true;
    }

    private static string? DetectPolarRepoRoot()
    {
        string? syntheticRepoRoot = FindRepoRoot(AppContext.BaseDirectory, "SyntheticBio.sln");
        if (!string.IsNullOrWhiteSpace(syntheticRepoRoot))
        {
            string? siblingParent = Directory.GetParent(syntheticRepoRoot)?.FullName;
            if (!string.IsNullOrWhiteSpace(siblingParent))
            {
                string siblingPolarRoot = Path.Combine(siblingParent, "PolarH10");
                if (File.Exists(Path.Combine(siblingPolarRoot, "PolarH10.sln")))
                    return siblingPolarRoot;
            }
        }

        string defaultPolarRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "source",
            "repos",
            "PolarH10");

        return File.Exists(Path.Combine(defaultPolarRoot, "PolarH10.sln"))
            ? defaultPolarRoot
            : null;
    }

    private static string? FindRepoRoot(string? startingDirectory, string markerFileName)
    {
        string? current = startingDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, markerFileName)))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }

    private static string GetCanonicalPolarWorkspaceAppPath(string polarRepoRoot)
        => Path.Combine(polarRepoRoot, "out", "workspace-app", "PolarH10.App.exe");

    private static string? GetLastNonEmptyLine(string text)
    {
        string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length > 0 ? lines[^1] : null;
    }

    private static PolarAppRuntimeStatusSnapshot? TryReadRunningPolarStatus()
    {
        string path = GetPolarRuntimeStatusPath();
        if (!File.Exists(path))
            return null;

        try
        {
            PolarAppRuntimeStatusSnapshot? status = JsonSerializer.Deserialize<PolarAppRuntimeStatusSnapshot>(
                File.ReadAllText(path),
                JsonOptions);
            if (status is null)
                return null;

            Process process = Process.GetProcessById(status.ProcessId);
            if (process.HasExited || !IsLikelyPolarProcess(process, status.ProcessPath))
            {
                TryDeleteStalePolarStatus(path);
                return null;
            }

            return status;
        }
        catch
        {
            TryDeleteStalePolarStatus(path);
            return null;
        }
    }

    private static string GetPolarRuntimeStatusPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PolarH10",
            "runtime-status.json");

    private static void TryDeleteStalePolarStatus(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static Process? FindRunningPolarProcess()
        => Process
            .GetProcesses()
            .Where(process => IsLikelyPolarProcess(process, null) && HasInteractiveWindow(process))
            .OrderBy(SafeGetStartTime)
            .FirstOrDefault();

    private static int RecycleHeadlessPolarProcesses(TimeSpan minimumAge)
    {
        int recycledCount = 0;
        foreach (int processId in FindHeadlessPolarProcessIds())
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                if (process.HasExited || HasInteractiveWindow(process))
                    continue;

                if (DateTime.UtcNow - process.StartTime.ToUniversalTime() < minimumAge)
                    continue;

                process.Kill(entireProcessTree: false);
                process.WaitForExit(5000);
                recycledCount++;
            }
            catch
            {
                // Best effort cleanup only.
            }
        }

        return recycledCount;
    }

    private static int[] FindHeadlessPolarProcessIds()
        => Process
            .GetProcesses()
            .Where(process => IsLikelyPolarProcess(process, null) && !HasInteractiveWindow(process))
            .Select(process => process.Id)
            .ToArray();

    private static bool IsLikelyPolarProcess(Process process, string? expectedPath)
    {
        try
        {
            string? executablePath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(expectedPath) &&
                string.Equals(executablePath, expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(process.ProcessName, "PolarH10.App", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Path.GetFileName(executablePath), "PolarH10.App.exe", StringComparison.OrdinalIgnoreCase) ||
                   process.MainWindowTitle.Contains("Polar H10", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static DateTime SafeGetStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    private static bool HasInteractiveWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle != IntPtr.Zero ||
                   !string.IsNullOrWhiteSpace(process.MainWindowTitle);
        }
        catch
        {
            return false;
        }
    }

    private static void BringProcessToFront(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                process.Refresh();
                IntPtr handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindowAsync(handle, SwRestore);
                    SetForegroundWindow(handle);
                    return;
                }

                Thread.Sleep(100);
            }
        }
        catch
        {
            // Best effort focus only.
        }
    }

    private static async Task<PolarAppRuntimeStatusSnapshot?> WaitForRunningPolarStatusAsync(
        string pipeBaseName,
        TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            PolarAppRuntimeStatusSnapshot? status = TryReadRunningPolarStatus();
            if (status is not null &&
                string.Equals(status.TransportName, "synthetic", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(status.SyntheticPipeBaseName, pipeBaseName, StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }

            await Task.Delay(250);
        }

        return null;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
