using PolarH10.Protocol;
using PolarH10.Transport.Abstractions;
using PolarH10.Transport.Synthetic;
using SyntheticBio.Core;

namespace SyntheticBio.Core.Tests;

public sealed class SyntheticHarnessTests
{
    [Fact]
    public void GenerateScenario_IsDeterministic_ForFixedSeedAndStartTime()
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("regular") with
        {
            DurationSeconds = 120d,
        };
        DateTimeOffset startTimeUtc = new(2026, 03, 20, 10, 00, 00, TimeSpan.Zero);

        SyntheticScenarioBundle first = SyntheticSignalGenerator.GenerateScenario(scenario, startTimeUtc);
        SyntheticScenarioBundle second = SyntheticSignalGenerator.GenerateScenario(scenario, startTimeUtc);

        Assert.Equal(first.HrSamples, second.HrSamples);
        Assert.Equal(first.BreathingSamples, second.BreathingSamples);
        Assert.Equal(first.EcgFrames.Count, second.EcgFrames.Count);
        for (int i = 0; i < first.EcgFrames.Count; i++)
        {
            Assert.Equal(first.EcgFrames[i].TimestampUtc, second.EcgFrames[i].TimestampUtc);
            Assert.Equal(first.EcgFrames[i].SensorTimestampNs, second.EcgFrames[i].SensorTimestampNs);
            Assert.Equal(first.EcgFrames[i].MicroVolts, second.EcgFrames[i].MicroVolts);
        }
    }

    [Fact]
    public void EncodeHeartRateMeasurement_RoundTripsThroughPolarDecoder()
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("regular") with
        {
            DurationSeconds = 60d,
        };
        SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(
            scenario,
            new DateTimeOffset(2026, 03, 20, 10, 00, 00, TimeSpan.Zero));
        SyntheticHrSample sample = bundle.HrSamples[0];

        byte[] payload = SyntheticSignalGenerator.EncodeHeartRateMeasurement(sample);
        HrRrSample decoded = PolarHrRrDecoder.Decode(payload);

        Assert.Equal(sample.HeartRateBpm, decoded.HeartRateBpm);
        Assert.Single(decoded.RrIntervalsMs);
        Assert.InRange(Math.Abs(decoded.RrIntervalsMs[0] - sample.RrIntervalMs), 0f, 0.6f);
    }

    [Fact]
    public void EncodeEcgFrame_RoundTripsThroughPolarDecoder()
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("regular") with
        {
            DurationSeconds = 20d,
        };
        SyntheticScenarioBundle bundle = SyntheticSignalGenerator.GenerateScenario(
            scenario,
            new DateTimeOffset(2026, 03, 20, 10, 00, 00, TimeSpan.Zero));
        SyntheticEcgFrame frame = bundle.EcgFrames[0];

        byte[] payload = SyntheticSignalGenerator.EncodeEcgPmdFrame(frame);
        PolarEcgFrame decoded = PolarEcgDecoder.DecodeFrame(payload, receivedUtcTicks: 0L);

        Assert.Equal(frame.SensorTimestampNs, decoded.SensorTimestampNs);
        Assert.Equal(frame.MicroVolts, decoded.MicroVolts);
    }

    [Fact]
    public void ResonanceScenario_Dominates_OffResonanceAndIrregular()
    {
        SyntheticScenarioObservation resonance = Observe("resonance_010hz");
        SyntheticScenarioObservation off18 = Observe("off_18bpm");
        SyntheticScenarioObservation irregular = Observe("irregular_rr");

        Assert.InRange(resonance.Coherence.PeakFrequencyHz, 0.09f, 0.11f);
        Assert.True(resonance.Coherence.CurrentCoherence01 > off18.Coherence.CurrentCoherence01);
        Assert.True(resonance.Coherence.CurrentCoherence01 > irregular.Coherence.CurrentCoherence01);
    }

    [Fact]
    public void FeatureShowcaseProfileSet_UsesDedicatedCoherenceHrvAndEntropyDevices()
    {
        SyntheticLiveProfileSet profileSet = SyntheticScenarioCatalog.CreateStandardProfileSet();

        Assert.Collection(
            profileSet.Devices,
            device => Assert.Equal("coherence_high", device.ScenarioId),
            device => Assert.Equal("coherence_low", device.ScenarioId),
            device => Assert.Equal("hrv_high", device.ScenarioId),
            device => Assert.Equal("hrv_low", device.ScenarioId),
            device => Assert.Equal("entropy_high", device.ScenarioId),
            device => Assert.Equal("entropy_low", device.ScenarioId));
    }

    [Fact]
    public void CoherenceShowcase_HasClearHighVsLowSeparation()
    {
        SyntheticScenarioObservation coherenceHigh = Observe("coherence_high", 120d);
        SyntheticScenarioObservation coherenceLow = Observe("coherence_low", 120d);

        Assert.InRange(coherenceHigh.Coherence.PeakFrequencyHz, 0.09f, 0.11f);
        Assert.True(coherenceHigh.Coherence.CurrentCoherence01 > 0.75f);
        Assert.True(coherenceHigh.Coherence.CurrentCoherence01 > coherenceLow.Coherence.CurrentCoherence01 + 0.30f);
    }

    [Fact]
    public void HrvShowcase_HasClearHighVsLowSeparation()
    {
        SyntheticScenarioObservation hrvHigh = Observe("hrv_high", 360d);
        SyntheticScenarioObservation hrvLow = Observe("hrv_low", 360d);

        Assert.True(hrvHigh.Hrv.HasMetricsSample);
        Assert.True(hrvLow.Hrv.HasMetricsSample);
        Assert.True(hrvHigh.Hrv.CurrentRmssdMs > hrvLow.Hrv.CurrentRmssdMs + 10f);
        Assert.True(hrvHigh.Hrv.SdnnMs > hrvLow.Hrv.SdnnMs + 15f);
        Assert.True(hrvHigh.Hrv.Pnn50Percent >= hrvLow.Hrv.Pnn50Percent);
    }

    [Fact]
    public void EntropyShowcase_HasClearHighVsLowSeparation()
    {
        SyntheticScenarioObservation entropyLow = Observe("entropy_low", 150d);
        SyntheticScenarioObservation entropyHigh = Observe("entropy_high", 150d);

        Assert.True(entropyLow.Dynamics.IntervalHasEntropyMetrics);
        Assert.True(entropyLow.Dynamics.AmplitudeHasEntropyMetrics);
        Assert.True(entropyHigh.Dynamics.IntervalHasEntropyMetrics);
        Assert.True(entropyHigh.Dynamics.AmplitudeHasEntropyMetrics);
        Assert.True(entropyHigh.Dynamics.Interval.SampleEntropy > entropyLow.Dynamics.Interval.SampleEntropy + 0.50f);
        Assert.True(entropyHigh.Dynamics.Amplitude.SampleEntropy > entropyLow.Dynamics.Amplitude.SampleEntropy + 0.10f);
    }

    [Fact]
    public void JitteredBreathing_HasHigherEntropyThanRegular()
    {
        SyntheticScenarioObservation regular = Observe("regular");
        SyntheticScenarioObservation jittered = Observe("jittered_breathing");

        Assert.True(regular.Dynamics.IntervalHasEntropyMetrics);
        Assert.True(regular.Dynamics.AmplitudeHasEntropyMetrics);
        Assert.True(jittered.Dynamics.IntervalHasEntropyMetrics);
        Assert.True(jittered.Dynamics.AmplitudeHasEntropyMetrics);
        Assert.True(jittered.Dynamics.Interval.SampleEntropy > regular.Dynamics.Interval.SampleEntropy);
        Assert.True(jittered.Dynamics.Amplitude.SampleEntropy > regular.Dynamics.Amplitude.SampleEntropy);
    }

    [Fact]
    public void RegularScenario_ProducesPreviewHrvMetrics()
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get("regular") with
        {
            DurationSeconds = 180d,
        };

        SyntheticScenarioObservation observation = SyntheticMeasureHarness.ObserveScenario(
            scenario,
            hrvSettings: SyntheticMeasureHarness.CreatePreviewHrvSettings());

        Assert.True(observation.Hrv.HasMetricsSample);
        Assert.Equal(PolarHrvTrackingState.Tracking, observation.Hrv.TrackingState);
        Assert.True(observation.Hrv.CurrentRmssdMs > 0f);
        Assert.True(observation.Hrv.SdnnMs > 0f);
    }

    [Fact]
    public void FlatBreathing_RemainsNotReady_ForEntropy()
    {
        SyntheticScenarioObservation flat = Observe("flat_breathing");

        Assert.False(flat.Dynamics.IntervalHasEntropyMetrics);
        Assert.False(flat.Dynamics.AmplitudeHasEntropyMetrics);
        Assert.False(flat.Dynamics.HasAcceptedAnyBreath);
    }

    [Theory]
    [InlineData(180d)]
    [InlineData(360d)]
    public void BreathingPauseScenario_EndsStale(double durationSeconds)
    {
        SyntheticScenarioObservation paused = Observe("breathing_pause", durationSeconds);

        Assert.Equal(PolarBreathingDynamicsTrackingState.Stale, paused.Dynamics.TrackingState);
        Assert.True(paused.Dynamics.LastWaveformSampleAgeSeconds >= paused.Dynamics.Settings.StaleTimeoutSeconds);
    }

    [Fact]
    public async Task PipeServer_StreamsDiscoverableDevices_HrNotifications_PmdEcg_AndBreathingTelemetry()
    {
        string pipeBaseName = $"polarh10-synth-test-{Guid.NewGuid():N}";
        var profileSet = SyntheticScenarioCatalog.CreateStandardProfileSet();
        await using var server = new SyntheticPipeServer(new SyntheticPipeServerOptions
        {
            PipeBaseName = pipeBaseName,
            DurationSeconds = 12d,
            ProfileSet = profileSet,
        });

        await server.StartAsync();

        var factory = new SyntheticBleAdapterFactory(new SyntheticTransportOptions
        {
            PipeBaseName = pipeBaseName,
        });
        var scanner = factory.CreateScanner();
        var devices = new List<BleDeviceFound>();
        scanner.DeviceFound += devices.Add;

        await scanner.StartScanAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(profileSet.Devices.Count, devices.Count);
        BleDeviceFound firstDevice = devices[0];

        await using IBleConnection connection = factory.CreateConnection(firstDevice.Address);
        var breathingSource = Assert.IsAssignableFrom<ISyntheticBreathingTelemetrySource>(connection);
        var hrNotificationTcs = new TaskCompletionSource<BleNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var breathingTelemetryTcs = new TaskCompletionSource<PolarBreathingTelemetry>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ctrlSettingsTcs = new TaskCompletionSource<BleNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ctrlStartTcs = new TaskCompletionSource<BleNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pmdDataTcs = new TaskCompletionSource<BleNotification>(TaskCreationOptions.RunContinuationsAsynchronously);

        breathingSource.BreathingTelemetryReceived += telemetry => breathingTelemetryTcs.TrySetResult(telemetry);

        await connection.ConnectAsync();
        IGattServiceHandle? hrService = await connection.GetServiceAsync(PolarGattIds.HeartRateService);
        Assert.NotNull(hrService);
        IGattServiceHandle? pmdService = await connection.GetServiceAsync(PolarGattIds.PmdService);
        Assert.NotNull(pmdService);
        IGattCharacteristicHandle? hrCharacteristic = await hrService!.GetCharacteristicAsync(PolarGattIds.HeartRateMeasurement);
        Assert.NotNull(hrCharacteristic);
        IGattCharacteristicHandle? pmdCtrl = await pmdService!.GetCharacteristicAsync(PolarGattIds.PmdControlPoint);
        IGattCharacteristicHandle? pmdData = await pmdService.GetCharacteristicAsync(PolarGattIds.PmdData);
        Assert.NotNull(pmdCtrl);
        Assert.NotNull(pmdData);

        hrCharacteristic!.NotificationReceived += notification => hrNotificationTcs.TrySetResult(notification);
        pmdCtrl!.NotificationReceived += notification =>
        {
            if (!ctrlSettingsTcs.Task.IsCompleted)
                ctrlSettingsTcs.TrySetResult(notification);
            else
                ctrlStartTcs.TrySetResult(notification);
        };
        pmdData!.NotificationReceived += notification => pmdDataTcs.TrySetResult(notification);
        await hrCharacteristic.EnableNotificationsAsync();
        await pmdCtrl.EnableNotificationsAsync();
        await pmdData.EnableNotificationsAsync();

        BleWriteResult settingsWrite = await pmdCtrl.WriteAsync(PolarPmdCommandBuilder.BuildGetSettingsRequest(PolarGattIds.MeasurementTypeEcg));
        Assert.True(settingsWrite.Success);
        BleNotification settingsNotification = await ctrlSettingsTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(PolarPmdControlPointParser.TryParseSettings(settingsNotification.Data, out byte measurementType, out PmdSettings settings));
        Assert.Equal(PolarGattIds.MeasurementTypeEcg, measurementType);
        Assert.Contains((ushort)130, settings.SampleRates);
        Assert.Contains((ushort)14, settings.Resolutions);

        BleWriteResult startWrite = await pmdCtrl.WriteAsync(PolarPmdCommandBuilder.BuildStartEcgRequest());
        Assert.True(startWrite.Success);

        BleNotification notification = await hrNotificationTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        BleNotification startNotification = await ctrlStartTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        BleNotification pmdNotification = await pmdDataTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        PolarBreathingTelemetry breathingTelemetry = await breathingTelemetryTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        HrRrSample decoded = PolarHrRrDecoder.Decode(notification.Data);
        Assert.True(PolarPmdControlPointParser.TryParse(startNotification.Data, out PmdControlPointResponse response));
        Assert.True(response.IsSuccess);
        PolarEcgFrame ecgFrame = PolarEcgDecoder.DecodeFrame(pmdNotification.Data, receivedUtcTicks: 0L);

        Assert.True(decoded.HeartRateBpm > 0);
        Assert.NotEmpty(decoded.RrIntervalsMs);
        Assert.NotEmpty(ecgFrame.MicroVolts);
        Assert.InRange(breathingTelemetry.CurrentVolume01, 0f, 1f);
        Assert.True(breathingTelemetry.HasTracking);
    }

    private static SyntheticScenarioObservation Observe(string scenarioId, double durationSeconds = 180d)
    {
        SyntheticScenarioDefinition scenario = SyntheticScenarioCatalog.Get(scenarioId) with
        {
            DurationSeconds = durationSeconds,
        };

        return SyntheticMeasureHarness.ObserveScenario(scenario);
    }
}
