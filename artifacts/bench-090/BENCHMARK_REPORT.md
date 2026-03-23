# SyntheticBio Benchmark Report

Generated at 2026-03-22T12:02:33.3057685+00:00
Short-term HRV benchmark settings: 59 s window / 24 RR minimum

## Checks
- Resonance coherence dominates the 10/12/18/24 BPM off-resonance sweep and irregular RR: PASS
- RR-derived HRV windows solve for regular, resonance, and irregular scenarios: PASS
- Jittered breathing entropy exceeds regular breathing: PASS
- Flat breathing stays not-ready: PASS
- Breathing pause scenario ends stale: FAIL

## Observations
### breathing_pause
- Label: Breathing pause / stale input
- Coherence: 0.535
- Peak frequency: 0.202 Hz
- HRV state: WarmingUp
- HRV RMSSD: -- ms
- HRV SDNN: -- ms
- Tracking state: Tracking
- Interval entropy: 0.672
- Amplitude entropy: 1.609

### coherence_high
- Label: Coherence demo / high resonance
- Coherence: 0.846
- Peak frequency: 0.099 Hz
- HRV state: Tracking
- HRV RMSSD: 27.1 ms
- HRV SDNN: 50.3 ms
- Tracking state: Tracking
- Interval entropy: --
- Amplitude entropy: --

### coherence_low
- Label: Coherence demo / low and unstable
- Coherence: 0.376
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 130.8 ms
- HRV SDNN: 87.1 ms
- Tracking state: Tracking
- Interval entropy: 1.078
- Amplitude entropy: 1.061

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.542
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 8.5 ms
- HRV SDNN: 10.4 ms
- Tracking state: Tracking
- Interval entropy: 0.134
- Amplitude entropy: 0.405

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.511
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 13.1 ms
- HRV SDNN: 14.5 ms
- Tracking state: Tracking
- Interval entropy: 1.735
- Amplitude entropy: 0.629

### flat_breathing
- Label: Flat low-excursion breathing
- Coherence: 0.313
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 3.3 ms
- HRV SDNN: 4.9 ms
- Tracking state: WaitingForBreathingTracking
- Interval entropy: --
- Amplitude entropy: --

### irregular_rr
- Label: Irregular RR / AF-like
- Coherence: 0.329
- Peak frequency: 0.173 Hz
- HRV state: Tracking
- HRV RMSSD: 175.3 ms
- HRV SDNN: 110.7 ms
- Tracking state: Tracking
- Interval entropy: 0.916
- Amplitude entropy: 0.762

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 13.9 ms
- HRV SDNN: 15.5 ms
- Tracking state: Tracking
- Interval entropy: 1.792
- Amplitude entropy: 1.030

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.169 Hz
- HRV state: Tracking
- HRV RMSSD: 21.5 ms
- HRV SDNN: 25.3 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 2.079

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 17.6 ms
- HRV SDNN: 18.2 ms
- Tracking state: Tracking
- Interval entropy: 0.709
- Amplitude entropy: --

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 11.6 ms
- HRV SDNN: 10.2 ms
- Tracking state: Tracking
- Interval entropy: 0.565
- Amplitude entropy: 1.705

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.7 ms
- HRV SDNN: 7.1 ms
- Tracking state: Tracking
- Interval entropy: 0.436
- Amplitude entropy: --

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 9.9 ms
- HRV SDNN: 11.6 ms
- Tracking state: Tracking
- Interval entropy: 0.069
- Amplitude entropy: 0.847

### resonance_010hz
- Label: 0.10 Hz resonance breathing
- Coherence: 0.844
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.1 ms
- HRV SDNN: 50.4 ms
- Tracking state: Tracking
- Interval entropy: --
- Amplitude entropy: --

