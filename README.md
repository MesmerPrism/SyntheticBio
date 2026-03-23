# SyntheticBio

Windows-first `.NET 8` harness for generating live synthetic HR/RR data, beat-synchronous PMD ECG, and synthetic breathing-volume telemetry for same-machine testing against `PolarH10`.

Public entry points:

- site: `https://mesmerprism.github.io/SyntheticBio/`
- download: `https://mesmerprism.github.io/SyntheticBio/download.html`
- releases: `https://github.com/MesmerPrism/SyntheticBio/releases`

V1 scope:

- exercises `PolarCoherenceTracker` through the real Heart Rate Measurement decode path
- emits synthetic PMD ECG frames that stay synchronized with the exported RR timeline
- exercises `PolarBreathingDynamicsTracker` with synthetic breathing telemetry injected behind the ACC layer
- does not emulate PMD ACC
- does not route synthetic breathing through `PolarBreathingTracker`

## Projects

- `src/PolarH10.Protocol`: vendored protocol layer needed so SyntheticBio builds as a standalone repo
- `src/PolarH10.Transport.Abstractions`: vendored transport abstraction interfaces used by the test suite
- `src/PolarH10.Transport.Synthetic`: vendored named-pipe transport used by the test suite
- `src/SyntheticBio.Core`: scenario catalog, deterministic generators, measure harness, exporter, named-pipe live server
- `src/SyntheticBio.Cli`: `list`, `serve`, `export`, `benchmark`, and `publish-doc-bundle` commands
- `src/SyntheticBio.App`: WPF operator shell for running the live server and exporting fixtures
- `src/SyntheticBio.App.Package`: MSIX packaging project for the Windows research preview
- `src/SyntheticBio.PreviewInstaller`: guided setup bootstrapper for trusting the preview certificate and opening App Installer
- `tests/SyntheticBio.Core.Tests`: deterministic, tracker-behavior, and live transport tests

## Quick Start

List built-in scenarios and the default live virtual devices:

```powershell
dotnet run --project src\SyntheticBio.Cli -- list
```

Run the live synthetic server on the default pipe base:

```powershell
dotnet run --project src\SyntheticBio.Cli -- serve --pipe polarh10-synth --duration 360
```

Run the WPF operator app:

```powershell
dotnet run --project src\SyntheticBio.App
```

Published desktop app:

- `C:\Users\tillh\source\repos\SyntheticBio\artifacts\publish\SyntheticBio.App-win-x64\SyntheticBio.App.exe`
- desktop shortcut: `C:\Users\tillh\Desktop\SyntheticBio App.lnk`

Export offline fixtures:

```powershell
dotnet run --project src\SyntheticBio.Cli -- export --out .\artifacts\fixtures --duration 360
```

Run the systematic benchmark matrix:

```powershell
dotnet run --project src\SyntheticBio.Cli -- benchmark --out .\artifacts\benchmark --duration 360
```

Publish the GitHub Pages showcase bundle into the sibling `PolarH10` docs tree:

```powershell
dotnet run --project src\SyntheticBio.Cli -- publish-doc-bundle --polar-docs-root ..\PolarH10\docs --duration 360
```

The benchmark writes `BENCHMARK_REPORT.md` and validates:

- resonance coherence beats the off-resonance sweep and irregular RR
- feature-showcase coherence high beats coherence low
- feature-showcase HRV high beats HRV low
- feature-showcase entropy high beats entropy low
- feature-showcase entropy rising beats entropy low
- jittered breathing entropy exceeds regular breathing
- flat breathing stays not-ready
- breathing pause ends stale

## Built-In Scenario Packs

- `regular`
- `coherence_high`
- `coherence_low`
- `hrv_high`
- `hrv_low`
- `entropy_high`
- `entropy_low`
- `entropy_rising`
- `resonance_010hz`
- `off_10bpm`
- `off_12bpm`
- `off_18bpm`
- `off_24bpm`
- `jittered_breathing`
- `flat_breathing`
- `breathing_pause`
- `irregular_rr`

The default live profile set now exposes six documentation-friendly virtual devices:

- `Polar H10 Demo Coherence High`
- `Polar H10 Demo Coherence Low`
- `Polar H10 Demo HRV High`
- `Polar H10 Demo HRV Low`
- `Polar H10 Demo Entropy High`
- `Polar H10 Demo Entropy Low`

For HRV-oriented documentation captures or exported fixtures, prefer `360 s` or
longer so the Polar app's default `300 s` rolling HRV window can fill without a
scenario loop boundary.

## Documentation Showcase Matrix

- `coherence_high`: narrow, strong RR oscillation near `0.10 Hz`. Use it with `PolarH10/docs/coherence-formulas.md` to show how concentrated `P_peak` relative to `P_total` drives the normalized coherence score upward.
- `coherence_low`: off-resonance breathing plus noisy RR timing. Use it to show how broader spectral spread lowers the same `P_peak / P_total` style coherence readout.
- `hrv_high`: large adjacent RR changes across the rolling window. Use it with `PolarH10/docs/hrv-formulas.md` to show why `RMSSD`, `SDNN`, and `pNN50` all rise when beat-to-beat differences are larger.
- `hrv_low`: small adjacent RR changes and a narrow RR spread. Use it to show the opposite side of the same `RMSSD`, `SDNN`, and `pNN50` formulas.
- `entropy_high`: jittered breath timing and excursion depth. Use it with `PolarH10/docs/breathing-dynamics-formulas.md` to show higher interval/amplitude sample entropy from less repetitive derived breath series.
- `entropy_low`: repeatable breath timing and excursion depth. Use it to show how the same derived interval/amplitude series stay more regular and produce lower entropy.
- `flat_breathing`: excursion stays below the dynamics acceptance thresholds, so entropy never becomes ready.
- `breathing_pause`: the breathing waveform pauses during the final `40 s`, so the dynamics tracker ends stale even in longer documentation exports.

## PolarH10 Integration

Start `SyntheticBio` first, then launch `PolarH10` against the synthetic transport.

CLI example:

```powershell
dotnet run --project C:\Users\tillh\source\repos\PolarH10\src\PolarH10.Cli -- scan --transport synthetic --synthetic-pipe polarh10-synth
```

App example from PowerShell:

```powershell
$env:POLARH10_TRANSPORT = 'synthetic'
$env:POLARH10_SYNTHETIC_PIPE = 'polarh10-synth'
powershell -ExecutionPolicy Bypass -File C:\Users\tillh\source\repos\PolarH10\tools\app\Build-Workspace-App.ps1
C:\Users\tillh\source\repos\PolarH10\out\workspace-app\PolarH10.App.exe
```

Machine-local desktop launcher installer:

- script: `C:\Users\tillh\source\repos\SyntheticBio\scripts\Install-Desktop-Launchers.ps1`

Desktop launches should use shortcuts only. Do not keep copied `.exe` files on the desktop, because they drift away from the current publish output and create duplicate-looking launchers.

Use the `Establish connection` button inside `SyntheticBio` to start the live server, reuse an already-running synthetic `PolarH10` session when possible, or launch the canonical sibling `PolarH10` workspace app with synthetic transport enabled on the selected pipe.

## Exported Artifacts

Each exported scenario folder contains:

- `scenario.json`
- `ground_truth.json`
- `analysis.json`
- `hr_rr.csv`
- `ecg.csv`
- `session.json`

`ground_truth.json` remains the summary export. `analysis.json` carries the
intermediate traces needed for documentation figures, including the accepted RR
window, resampled tachogram, PSD bins, adjacent RR deltas, accepted breathing
extrema, and derived interval and amplitude series.

The `publish-doc-bundle` command also writes:

- `PolarH10/docs/data/synthetic-showcase/showcase-manifest.json`
- `PolarH10/docs/data/synthetic-showcase/scenarios/...`
- `PolarH10/docs/assets/synthetic-showcase/*.svg`
- `PolarH10/docs/assets/synthetic-showcase/*.png`
- `PolarH10/docs/assets/synthetic-showcase/synthetic-showcase-figure-pack.pdf`

## Validation

Run the local test suite:

```powershell
dotnet test SyntheticBio.sln
```

The test suite covers:

- deterministic generation for a fixed seed and start time
- HRS payload round-tripping through `PolarHrRrDecoder`
- PMD ECG frame round-tripping through `PolarEcgDecoder`
- coherence ranking across resonance, off-resonance, and irregular RR
- breathing entropy ranking for regular vs jittered breathing
- flat-breathing and stale-input behavior
- live named-pipe discovery plus HR notification, PMD ECG delivery, and breathing telemetry delivery
