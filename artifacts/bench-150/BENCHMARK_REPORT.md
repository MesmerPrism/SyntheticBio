# SyntheticBio Benchmark Report

Generated at 2026-03-22T12:02:05.9653017+00:00
Short-term HRV benchmark settings: 99 s window / 32 RR minimum

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
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 9.9 ms
- HRV SDNN: 11.4 ms
- Tracking state: Tracking
- Interval entropy: 0.624
- Amplitude entropy: 0.457

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
- HRV RMSSD: 133.4 ms
- HRV SDNN: 86.8 ms
- Tracking state: Tracking
- Interval entropy: 0.993
- Amplitude entropy: 1.262

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.542
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 8.6 ms
- HRV SDNN: 10.5 ms
- Tracking state: Tracking
- Interval entropy: 0.112
- Amplitude entropy: 0.272

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.555
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 13.3 ms
- HRV SDNN: 14.7 ms
- Tracking state: Tracking
- Interval entropy: 1.213
- Amplitude entropy: 0.632

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
- HRV RMSSD: 187.3 ms
- HRV SDNN: 129.0 ms
- Tracking state: Tracking
- Interval entropy: 1.046
- Amplitude entropy: 0.440

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 14.1 ms
- HRV SDNN: 15.3 ms
- Tracking state: Tracking
- Interval entropy: 1.609
- Amplitude entropy: 0.505

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.168 Hz
- HRV state: Tracking
- HRV RMSSD: 19.9 ms
- HRV SDNN: 23.3 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 1.099

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 17.6 ms
- HRV SDNN: 18.2 ms
- Tracking state: Tracking
- Interval entropy: 0.644
- Amplitude entropy: 0.463

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 12.0 ms
- HRV SDNN: 10.5 ms
- Tracking state: Tracking
- Interval entropy: 0.542
- Amplitude entropy: 1.435

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.7 ms
- HRV SDNN: 7.1 ms
- Tracking state: Tracking
- Interval entropy: 0.431
- Amplitude entropy: 1.081

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 9.9 ms
- HRV SDNN: 11.5 ms
- Tracking state: Tracking
- Interval entropy: 0.038
- Amplitude entropy: 0.288

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

