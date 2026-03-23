using PolarH10.Protocol;

namespace SyntheticBio.Core;

public sealed record SyntheticCoherenceAnalysis(
    PolarCoherenceTelemetry Telemetry,
    PolarCoherenceDiagnostics Diagnostics);

public sealed record SyntheticHrvAnalysis(
    PolarHrvTelemetry Telemetry,
    PolarHrvDiagnostics Diagnostics);

public sealed record SyntheticBreathingDynamicsAnalysis(
    PolarBreathingDynamicsTelemetry Telemetry,
    PolarBreathingDynamicsDiagnostics Diagnostics);

public sealed record SyntheticScenarioAnalysis(
    string PresetId,
    DateTimeOffset GeneratedAtUtc,
    SyntheticCoherenceAnalysis Coherence,
    SyntheticHrvAnalysis Hrv,
    SyntheticBreathingDynamicsAnalysis Dynamics);
