# SyntheticBio Benchmark Report

Generated at 2026-03-22T12:02:05.2186418+00:00
Short-term HRV benchmark settings: 79 s window / 24 RR minimum

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
- HRV state: Tracking
- HRV RMSSD: 9.7 ms
- HRV SDNN: 11.2 ms
- Tracking state: Tracking
- Interval entropy: 0.651
- Amplitude entropy: 0.773

### coherence_high
- Label: Coherence demo / high resonance
- Coherence: 0.846
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.6 ms
- HRV SDNN: 49.4 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: --

### coherence_low
- Label: Coherence demo / low and unstable
- Coherence: 0.376
- Peak frequency: 0.172 Hz
- HRV state: Tracking
- HRV RMSSD: 132.1 ms
- HRV SDNN: 87.5 ms
- Tracking state: Tracking
- Interval entropy: 1.084
- Amplitude entropy: 1.179

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.542
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 8.5 ms
- HRV SDNN: 10.5 ms
- Tracking state: Tracking
- Interval entropy: 0.144
- Amplitude entropy: 0.375

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.530
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 13.3 ms
- HRV SDNN: 14.3 ms
- Tracking state: Tracking
- Interval entropy: 1.447
- Amplitude entropy: 0.452

### flat_breathing
- Label: Flat low-excursion breathing
- Coherence: 0.313
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 3.3 ms
- HRV SDNN: 4.8 ms
- Tracking state: WaitingForBreathingTracking
- Interval entropy: --
- Amplitude entropy: --

### irregular_rr
- Label: Irregular RR / AF-like
- Coherence: 0.329
- Peak frequency: 0.170 Hz
- HRV state: Tracking
- HRV RMSSD: 183.0 ms
- HRV SDNN: 119.6 ms
- Tracking state: Tracking
- Interval entropy: 1.153
- Amplitude entropy: 0.731

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 13.8 ms
- HRV SDNN: 14.9 ms
- Tracking state: Tracking
- Interval entropy: 2.001
- Amplitude entropy: 0.802

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.169 Hz
- HRV state: Tracking
- HRV RMSSD: 20.8 ms
- HRV SDNN: 24.3 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 1.466

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 17.5 ms
- HRV SDNN: 17.9 ms
- Tracking state: Tracking
- Interval entropy: 0.685
- Amplitude entropy: 0.847

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 12.4 ms
- HRV SDNN: 10.7 ms
- Tracking state: Tracking
- Interval entropy: 0.551
- Amplitude entropy: 1.482

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.7 ms
- HRV SDNN: 7.0 ms
- Tracking state: Tracking
- Interval entropy: 0.451
- Amplitude entropy: 1.455

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 9.8 ms
- HRV SDNN: 11.3 ms
- Tracking state: Tracking
- Interval entropy: 0.049
- Amplitude entropy: 0.693

### resonance_010hz
- Label: 0.10 Hz resonance breathing
- Coherence: 0.844
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.6 ms
- HRV SDNN: 49.4 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: --

