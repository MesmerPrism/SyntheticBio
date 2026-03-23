# SyntheticBio Benchmark Report

Generated at 2026-03-22T12:23:50.8618324+00:00
Short-term HRV benchmark settings: 300 s window / 90 RR minimum

## Checks
- Resonance coherence dominates the 10/12/18/24 BPM off-resonance sweep and irregular RR: PASS
- Feature-showcase coherence high stays above coherence low: PASS
- RR-derived HRV windows solve for regular, resonance, and irregular scenarios: PASS
- Feature-showcase HRV high stays above HRV low: PASS
- Feature-showcase entropy high stays above entropy low: PASS
- Feature-showcase entropy rising stays above entropy low: PASS
- Jittered breathing entropy exceeds regular breathing: PASS
- Flat breathing stays not-ready: PASS
- Breathing pause scenario ends stale: PASS

## Observations
### breathing_pause
- Label: Breathing pause / stale input
- Coherence: 0.535
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 9.9 ms
- HRV SDNN: 11.4 ms
- Tracking state: Stale
- Interval entropy: 0.569
- Amplitude entropy: 0.349

### coherence_high
- Label: Coherence demo / high resonance
- Coherence: 0.846
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.5 ms
- HRV SDNN: 49.4 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: --

### coherence_low
- Label: Coherence demo / low and unstable
- Coherence: 0.376
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 130.9 ms
- HRV SDNN: 86.8 ms
- Tracking state: Tracking
- Interval entropy: 0.991
- Amplitude entropy: 1.099

### entropy_high
- Label: Entropy demo / high irregularity
- Coherence: 0.526
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 14.1 ms
- HRV SDNN: 15.1 ms
- Tracking state: Tracking
- Interval entropy: 1.230
- Amplitude entropy: 0.426

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.542
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 8.5 ms
- HRV SDNN: 10.4 ms
- Tracking state: Tracking
- Interval entropy: 0.044
- Amplitude entropy: 0.148

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.561
- Peak frequency: 0.200 Hz
- HRV state: Tracking
- HRV RMSSD: 12.6 ms
- HRV SDNN: 14.0 ms
- Tracking state: Tracking
- Interval entropy: 0.890
- Amplitude entropy: 0.445

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

### hrv_high
- Label: HRV demo / high beat-to-beat variability
- Coherence: 0.024
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 58.5 ms
- HRV SDNN: 40.1 ms
- Tracking state: Tracking
- Interval entropy: 0.514
- Amplitude entropy: 0.938

### hrv_low
- Label: HRV demo / low beat-to-beat variability
- Coherence: 0.401
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 3.1 ms
- HRV SDNN: 4.1 ms
- Tracking state: Tracking
- Interval entropy: 0.015
- Amplitude entropy: 0.189

### irregular_rr
- Label: Irregular RR / AF-like
- Coherence: 0.329
- Peak frequency: 0.170 Hz
- HRV state: Tracking
- HRV RMSSD: 186.4 ms
- HRV SDNN: 126.0 ms
- Tracking state: Tracking
- Interval entropy: 1.429
- Amplitude entropy: 0.301

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.200 Hz
- HRV state: Tracking
- HRV RMSSD: 13.9 ms
- HRV SDNN: 15.1 ms
- Tracking state: Tracking
- Interval entropy: 1.174
- Amplitude entropy: 0.426

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.169 Hz
- HRV state: Tracking
- HRV RMSSD: 21.0 ms
- HRV SDNN: 24.4 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 1.303

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 17.6 ms
- HRV SDNN: 18.2 ms
- Tracking state: Tracking
- Interval entropy: 0.625
- Amplitude entropy: 0.393

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 11.9 ms
- HRV SDNN: 10.5 ms
- Tracking state: Tracking
- Interval entropy: 0.537
- Amplitude entropy: 1.217

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.6 ms
- HRV SDNN: 7.0 ms
- Tracking state: Tracking
- Interval entropy: 0.379
- Amplitude entropy: 0.823

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.201 Hz
- HRV state: Tracking
- HRV RMSSD: 9.9 ms
- HRV SDNN: 11.5 ms
- Tracking state: Tracking
- Interval entropy: 0.190
- Amplitude entropy: 0.422

### resonance_010hz
- Label: 0.10 Hz resonance breathing
- Coherence: 0.844
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.5 ms
- HRV SDNN: 49.5 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 3.135

