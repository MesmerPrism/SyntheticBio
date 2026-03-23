# SyntheticBio Benchmark Report

Generated at 2026-03-22T12:15:00.0682226+00:00
Short-term HRV benchmark settings: 118 s window / 32 RR minimum

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
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 10.0 ms
- HRV SDNN: 11.5 ms
- Tracking state: Stale
- Interval entropy: 0.624
- Amplitude entropy: 0.457

### coherence_high
- Label: Coherence demo / high resonance
- Coherence: 0.846
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.5 ms
- HRV SDNN: 49.1 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: --

### coherence_low
- Label: Coherence demo / low and unstable
- Coherence: 0.376
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 132.3 ms
- HRV SDNN: 87.3 ms
- Tracking state: Tracking
- Interval entropy: 0.987
- Amplitude entropy: 1.196

### entropy_high
- Label: Entropy demo / high irregularity
- Coherence: 0.526
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 14.5 ms
- HRV SDNN: 15.6 ms
- Tracking state: Tracking
- Interval entropy: 1.409
- Amplitude entropy: 0.500

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.542
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 8.6 ms
- HRV SDNN: 10.5 ms
- Tracking state: Tracking
- Interval entropy: 0.091
- Amplitude entropy: 0.209

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.555
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 13.3 ms
- HRV SDNN: 14.8 ms
- Tracking state: Tracking
- Interval entropy: 1.113
- Amplitude entropy: 0.388

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
- HRV RMSSD: 58.8 ms
- HRV SDNN: 40.3 ms
- Tracking state: Tracking
- Interval entropy: 0.527
- Amplitude entropy: 2.526

### hrv_low
- Label: HRV demo / low beat-to-beat variability
- Coherence: 0.401
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 3.2 ms
- HRV SDNN: 4.2 ms
- Tracking state: Tracking
- Interval entropy: 0.031
- Amplitude entropy: 0.262

### irregular_rr
- Label: Irregular RR / AF-like
- Coherence: 0.329
- Peak frequency: 0.170 Hz
- HRV state: Tracking
- HRV RMSSD: 187.3 ms
- HRV SDNN: 130.5 ms
- Tracking state: Tracking
- Interval entropy: 1.022
- Amplitude entropy: 0.384

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 14.1 ms
- HRV SDNN: 15.4 ms
- Tracking state: Tracking
- Interval entropy: 1.204
- Amplitude entropy: 0.482

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.168 Hz
- HRV state: Tracking
- HRV RMSSD: 19.3 ms
- HRV SDNN: 22.7 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 1.135

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 17.7 ms
- HRV SDNN: 18.3 ms
- Tracking state: Tracking
- Interval entropy: 0.676
- Amplitude entropy: 0.405

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 12.2 ms
- HRV SDNN: 10.6 ms
- Tracking state: Tracking
- Interval entropy: 0.553
- Amplitude entropy: 1.684

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.6 ms
- HRV SDNN: 7.0 ms
- Tracking state: Tracking
- Interval entropy: 0.415
- Amplitude entropy: 0.967

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 10.0 ms
- HRV SDNN: 11.6 ms
- Tracking state: Tracking
- Interval entropy: 0.031
- Amplitude entropy: 0.414

### resonance_010hz
- Label: 0.10 Hz resonance breathing
- Coherence: 0.844
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.5 ms
- HRV SDNN: 49.1 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: --

