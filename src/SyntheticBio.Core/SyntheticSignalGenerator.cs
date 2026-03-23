namespace SyntheticBio.Core;

public static class SyntheticSignalGenerator
{
    private const double SyntheticEcgSampleRateHz = 130d;
    private const int SyntheticEcgSamplesPerFrame = 13;
    private const int Ecg24BitMin = -8_388_608;
    private const int Ecg24BitMax = 8_388_607;

    public static SyntheticScenarioBundle GenerateScenario(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset? startTimeUtc = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        DateTimeOffset scenarioStart = startTimeUtc ?? DateTimeOffset.UtcNow;
        IReadOnlyList<SyntheticBreathingSample> breathingSamples = GenerateBreathingSamples(scenario, scenarioStart);
        IReadOnlyList<SyntheticHrSample> hrSamples = GenerateHrSamples(scenario, scenarioStart);
        IReadOnlyList<SyntheticEcgFrame> ecgFrames = GenerateEcgFrames(scenario, scenarioStart, hrSamples);
        return new SyntheticScenarioBundle(scenario, hrSamples, breathingSamples, ecgFrames);
    }

    public static IReadOnlyList<SyntheticBreathingSample> GenerateBreathingSamples(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset startTimeUtc)
    {
        var samples = new List<SyntheticBreathingSample>();
        SyntheticBreathingProfile profile = ResolveBreathingProfile(scenario, 0d);
        double sampleRateHz = Math.Max(1d, profile.SampleRateHz);
        double stepSeconds = 1d / sampleRateHz;

        for (double t = 0d; t < scenario.DurationSeconds; t += stepSeconds)
        {
            profile = ResolveBreathingProfile(scenario, t);
            if (IsBreathingPaused(scenario, profile, t))
                continue;

            float volume = (float)ComputeBreathingVolume(scenario, t);
            float previousVolume = (float)ComputeBreathingVolume(scenario, Math.Max(0d, t - stepSeconds));
            float nextVolume = (float)ComputeBreathingVolume(scenario, Math.Min(scenario.DurationSeconds, t + stepSeconds));
            float derivative = nextVolume - previousVolume;
            string state = Math.Abs(derivative) < 0.0015f
                ? "Pausing"
                : derivative > 0f ? "Inhaling" : "Exhaling";
            bool hasTracking = profile.Amplitude01 >= 0.05d;

            samples.Add(new SyntheticBreathingSample(
                startTimeUtc.AddSeconds(t),
                t,
                volume,
                state,
                hasTracking,
                IsStale: false));
        }

        return samples;
    }

    public static IReadOnlyList<SyntheticHrSample> GenerateHrSamples(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset startTimeUtc)
    {
        var samples = new List<SyntheticHrSample>();
        int beatIndex = 0;
        double t = 0d;

        while (t < scenario.DurationSeconds)
        {
            double rrMs = ComputeRrIntervalMs(scenario, t, beatIndex);
            ushort heartRateBpm = (ushort)Math.Clamp(
                Math.Round(60_000d / Math.Max(400d, rrMs)),
                30d,
                220d);

            samples.Add(new SyntheticHrSample(
                startTimeUtc.AddSeconds(t),
                t,
                heartRateBpm,
                (float)rrMs));

            t += rrMs / 1000d;
            beatIndex++;
        }

        return samples;
    }

    public static byte[] EncodeHeartRateMeasurement(SyntheticHrSample sample)
    {
        ushort rrUnits = (ushort)Math.Clamp(
            (int)Math.Round(sample.RrIntervalMs / 0.9765625f),
            1,
            ushort.MaxValue);

        return
        [
            0x10,
            (byte)Math.Clamp(sample.HeartRateBpm, (ushort)0, (ushort)255),
            (byte)(rrUnits & 0xFF),
            (byte)((rrUnits >> 8) & 0xFF),
        ];
    }

    public static IReadOnlyList<SyntheticEcgFrame> GenerateEcgFrames(
        SyntheticScenarioDefinition scenario,
        DateTimeOffset startTimeUtc,
        IReadOnlyList<SyntheticHrSample> hrSamples)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(hrSamples);

        int totalSampleCount = Math.Max(
            1,
            (int)Math.Ceiling(Math.Max(1d, scenario.DurationSeconds) * SyntheticEcgSampleRateHz));
        double samplePeriodSeconds = 1d / SyntheticEcgSampleRateHz;
        var frames = new List<SyntheticEcgFrame>((totalSampleCount + SyntheticEcgSamplesPerFrame - 1) / SyntheticEcgSamplesPerFrame);
        var frameSamples = new int[SyntheticEcgSamplesPerFrame];
        int frameSampleIndex = 0;

        for (int sampleIndex = 0; sampleIndex < totalSampleCount; sampleIndex++)
        {
            double elapsedSeconds = sampleIndex * samplePeriodSeconds;
            frameSamples[frameSampleIndex++] = ComputeEcgMicroVolts(scenario, elapsedSeconds, hrSamples);

            bool flushFrame = frameSampleIndex == SyntheticEcgSamplesPerFrame || sampleIndex == totalSampleCount - 1;
            if (!flushFrame)
                continue;

            double frameElapsedSeconds = elapsedSeconds;
            long sensorTimestampNs = (long)Math.Round(frameElapsedSeconds * 1_000_000_000d);
            int[] samples = new int[frameSampleIndex];
            Array.Copy(frameSamples, samples, frameSampleIndex);
            frames.Add(new SyntheticEcgFrame(
                startTimeUtc.AddSeconds(frameElapsedSeconds),
                frameElapsedSeconds,
                sensorTimestampNs,
                samples));

            frameSampleIndex = 0;
        }

        return frames;
    }

    public static byte[] EncodeEcgPmdFrame(SyntheticEcgFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        byte[] payload = new byte[PolarH10.Protocol.PolarEcgDecoder.PmdHeaderSize + (frame.MicroVolts.Length * PolarH10.Protocol.PolarEcgDecoder.BytesPerSample)];
        payload[0] = PolarH10.Protocol.PolarGattIds.MeasurementTypeEcg;
        byte[] timestamp = BitConverter.GetBytes((ulong)Math.Max(0L, frame.SensorTimestampNs));
        Array.Copy(timestamp, 0, payload, 1, timestamp.Length);
        payload[9] = 0x00;

        int offset = PolarH10.Protocol.PolarEcgDecoder.PmdHeaderSize;
        for (int i = 0; i < frame.MicroVolts.Length; i++)
        {
            int sample = Math.Clamp(frame.MicroVolts[i], Ecg24BitMin, Ecg24BitMax);
            payload[offset++] = (byte)(sample & 0xFF);
            payload[offset++] = (byte)((sample >> 8) & 0xFF);
            payload[offset++] = (byte)((sample >> 16) & 0xFF);
        }

        return payload;
    }

    private static bool IsBreathingPaused(
        SyntheticScenarioDefinition scenario,
        SyntheticBreathingProfile profile,
        double elapsedSeconds)
    {
        bool inScheduledPause =
            profile.PauseStartSeconds >= 0d &&
            elapsedSeconds >= profile.PauseStartSeconds &&
            elapsedSeconds < profile.PauseStartSeconds + profile.PauseDurationSeconds;
        bool inTailPause =
            profile.PauseTailSeconds > 0d &&
            elapsedSeconds >= Math.Max(0d, scenario.DurationSeconds - profile.PauseTailSeconds);

        return inScheduledPause || inTailPause;
    }

    private static double ComputeBreathingVolume(SyntheticScenarioDefinition scenario, double elapsedSeconds)
    {
        SyntheticBreathingProfile profile = ResolveBreathingProfile(scenario, elapsedSeconds);
        double breathingHz = profile.BaseRateBpm / 60d;
        double phase = (elapsedSeconds * breathingHz * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 0.17d);
        double warp =
            profile.JitterAmount01 * Math.Sin((elapsedSeconds * (profile.WobbleRateHz + 0.05d) * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 0.33d)) +
            (profile.JitterAmount01 * 0.5d * Math.Cos((elapsedSeconds * (profile.WobbleRateHz * 1.9d + 0.07d) * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 0.71d)));

        double amplitude = profile.Amplitude01 * (1d + (profile.JitterAmount01 * 0.35d * Math.Sin((elapsedSeconds * 0.11d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 1.13d))));
        double volume =
            0.5d +
            (Math.Sin(phase + warp) * amplitude) +
            (Math.Sin(((phase + warp) * 2d) + 0.35d) * profile.HarmonicAmplitude01) +
            (Math.Cos((phase * 0.35d) + PhaseFromSeed(scenario.Seed, 1.91d)) * profile.JitterAmount01 * 0.03d);

        return Math.Clamp(volume, 0.02d, 0.98d);
    }

    private static double ComputeRrIntervalMs(SyntheticScenarioDefinition scenario, double elapsedSeconds, int beatIndex)
    {
        SyntheticHeartProfile profile = ResolveHeartProfile(scenario, elapsedSeconds);
        double breathingCarrier = (ComputeBreathingVolume(scenario, elapsedSeconds) - 0.5d) * 2d;
        double rrMs =
            profile.CenterIbiMs +
            (breathingCarrier * profile.RsaAmplitudeMs) +
            (Math.Sin((elapsedSeconds * 0.03d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 2.71d)) * profile.SlowModulationMs) +
            (Math.Sin((elapsedSeconds * 0.17d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 3.17d)) * profile.IrregularNoiseMs) +
            (Math.Cos((elapsedSeconds * 0.31d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 4.11d)) * profile.IrregularNoiseMs * 0.5d);

        if (profile.EctopyEveryNBeats > 0 && beatIndex > 0)
        {
            int beatInCycle = beatIndex % profile.EctopyEveryNBeats;
            if (beatInCycle == profile.EctopyEveryNBeats - 1)
                rrMs -= profile.EctopyDeltaMs;
            else if (beatInCycle == 0)
                rrMs += profile.CompensatoryDeltaMs;
        }

        return Math.Clamp(rrMs, 420d, 1_800d);
    }

    private static int ComputeEcgMicroVolts(
        SyntheticScenarioDefinition scenario,
        double elapsedSeconds,
        IReadOnlyList<SyntheticHrSample> hrSamples)
    {
        double breathingCarrier = (ComputeBreathingVolume(scenario, elapsedSeconds) - 0.5d) * 2d;
        double baselineMicroVolts =
            (breathingCarrier * 65d) +
            (Math.Sin((elapsedSeconds * 0.05d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 5.37d)) * 16d);
        double deterministicNoise =
            (Math.Sin((elapsedSeconds * 19.1d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 6.13d)) * 5d) +
            (Math.Cos((elapsedSeconds * 27.7d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 7.07d)) * 3d);

        double waveform = baselineMicroVolts + deterministicNoise;

        for (int i = 0; i < hrSamples.Count; i++)
        {
            double deltaSeconds = elapsedSeconds - hrSamples[i].ElapsedSeconds;
            if (deltaSeconds < -0.24d)
                break;

            if (deltaSeconds > 0.45d)
                continue;

            double beatScale =
                1d +
                (ComputeBreathingVolume(scenario, hrSamples[i].ElapsedSeconds) - 0.5d) * 0.08d +
                (Math.Sin((hrSamples[i].ElapsedSeconds * 0.21d * Math.PI * 2d) + PhaseFromSeed(scenario.Seed, 8.17d)) * 0.03d);

            waveform += ComputeGaussianWave(deltaSeconds, -0.18d, 0.030d, 85d * beatScale);
            waveform += ComputeGaussianWave(deltaSeconds, -0.045d, 0.010d, -140d * beatScale);
            waveform += ComputeGaussianWave(deltaSeconds, 0.000d, 0.012d, 980d * beatScale);
            waveform += ComputeGaussianWave(deltaSeconds, 0.040d, 0.014d, -220d * beatScale);
            waveform += ComputeGaussianWave(deltaSeconds, 0.240d, 0.060d, 280d * beatScale);
        }

        return (int)Math.Round(Math.Clamp(waveform, Ecg24BitMin, Ecg24BitMax));
    }

    private static SyntheticHeartProfile ResolveHeartProfile(SyntheticScenarioDefinition scenario, double elapsedSeconds)
    {
        if (scenario.HeartEnd is null)
            return scenario.Heart;

        float t = ComputeScenarioTransition(scenario, elapsedSeconds);
        return new SyntheticHeartProfile
        {
            CenterIbiMs = Lerp(scenario.Heart.CenterIbiMs, scenario.HeartEnd.CenterIbiMs, t),
            RsaAmplitudeMs = Lerp(scenario.Heart.RsaAmplitudeMs, scenario.HeartEnd.RsaAmplitudeMs, t),
            SlowModulationMs = Lerp(scenario.Heart.SlowModulationMs, scenario.HeartEnd.SlowModulationMs, t),
            IrregularNoiseMs = Lerp(scenario.Heart.IrregularNoiseMs, scenario.HeartEnd.IrregularNoiseMs, t),
            EctopyEveryNBeats = (int)Math.Round(Lerp(scenario.Heart.EctopyEveryNBeats, scenario.HeartEnd.EctopyEveryNBeats, t)),
            EctopyDeltaMs = Lerp(scenario.Heart.EctopyDeltaMs, scenario.HeartEnd.EctopyDeltaMs, t),
            CompensatoryDeltaMs = Lerp(scenario.Heart.CompensatoryDeltaMs, scenario.HeartEnd.CompensatoryDeltaMs, t),
        };
    }

    private static SyntheticBreathingProfile ResolveBreathingProfile(SyntheticScenarioDefinition scenario, double elapsedSeconds)
    {
        if (scenario.BreathingEnd is null)
            return scenario.Breathing;

        float t = ComputeScenarioTransition(scenario, elapsedSeconds);
        SyntheticBreathingProfile end = scenario.BreathingEnd;
        return new SyntheticBreathingProfile
        {
            SampleRateHz = Lerp(scenario.Breathing.SampleRateHz, end.SampleRateHz, t),
            BaseRateBpm = Lerp(scenario.Breathing.BaseRateBpm, end.BaseRateBpm, t),
            Amplitude01 = Lerp(scenario.Breathing.Amplitude01, end.Amplitude01, t),
            HarmonicAmplitude01 = Lerp(scenario.Breathing.HarmonicAmplitude01, end.HarmonicAmplitude01, t),
            JitterAmount01 = Lerp(scenario.Breathing.JitterAmount01, end.JitterAmount01, t),
            WobbleRateHz = Lerp(scenario.Breathing.WobbleRateHz, end.WobbleRateHz, t),
            PauseStartSeconds = Lerp(scenario.Breathing.PauseStartSeconds, end.PauseStartSeconds, t),
            PauseDurationSeconds = Lerp(scenario.Breathing.PauseDurationSeconds, end.PauseDurationSeconds, t),
            PauseTailSeconds = Lerp(scenario.Breathing.PauseTailSeconds, end.PauseTailSeconds, t),
        };
    }

    private static float ComputeScenarioTransition(SyntheticScenarioDefinition scenario, double elapsedSeconds)
    {
        if (scenario.DurationSeconds <= 0d)
            return 1f;

        double start = Math.Clamp(scenario.TransitionStartFraction, 0d, 1d);
        double end = Math.Clamp(scenario.TransitionEndFraction, start, 1d);
        if (end <= start)
            return 1f;

        double progress = Math.Clamp(elapsedSeconds / scenario.DurationSeconds, 0d, 1d);
        double normalized = Math.Clamp((progress - start) / Math.Max(1e-6d, end - start), 0d, 1d);
        return (float)(normalized * normalized * (3d - (2d * normalized)));
    }

    private static double ComputeGaussianWave(double x, double center, double width, double amplitude)
    {
        if (width <= 0d)
            return 0d;

        double normalized = (x - center) / width;
        return amplitude * Math.Exp(-0.5d * normalized * normalized);
    }

    private static double PhaseFromSeed(int seed, double multiplier)
    {
        double normalized = ((seed * multiplier) % 1d + 1d) % 1d;
        return normalized * Math.PI * 2d;
    }

    private static double Lerp(double from, double to, float t)
        => from + ((to - from) * Math.Clamp(t, 0f, 1f));
}
