# SyntheticBio Benchmark Report

Generated at 2026-03-22T11:57:26.9016302+00:00
Short-term HRV benchmark settings: 120 s window / 32 RR minimum

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
- HRV RMSSD: 10.1 ms
- HRV SDNN: 11.6 ms
- Tracking state: Tracking
- Interval entropy: 0.066
- Amplitude entropy: 0.503

### coherence_low
- Label: Coherence demo / low and unstable
- Coherence: 0.376
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 131.5 ms
- HRV SDNN: 87.3 ms
- Tracking state: Tracking
- Interval entropy: 1.042
- Amplitude entropy: 1.060

### coherence_rising
- Label: Coherence demo / settling into resonance
- Coherence: 0.520
- Peak frequency: 0.076 Hz
- HRV state: Tracking
- HRV RMSSD: 20.1 ms
- HRV SDNN: 46.8 ms
- Tracking state: Tracking
- Interval entropy: 0.158
- Amplitude entropy: 0.167

### entropy_low
- Label: Entropy demo / calm paced breathing
- Coherence: 0.592
- Peak frequency: 0.206 Hz
- HRV state: Tracking
- HRV RMSSD: 12.0 ms
- HRV SDNN: 13.2 ms
- Tracking state: Tracking
- Interval entropy: 0.816
- Amplitude entropy: 0.294

### entropy_rising
- Label: Entropy demo / increasing variability
- Coherence: 0.601
- Peak frequency: 0.220 Hz
- HRV state: Tracking
- HRV RMSSD: 19.2 ms
- HRV SDNN: 19.3 ms
- Tracking state: Tracking
- Interval entropy: 1.086
- Amplitude entropy: 0.336

### flat_breathing
- Label: Flat low-excursion breathing
- Coherence: 0.313
- Peak frequency: 0.171 Hz
- HRV state: Tracking
- HRV RMSSD: 3.3 ms
- HRV SDNN: 4.7 ms
- Tracking state: WaitingForBreathingTracking
- Interval entropy: --
- Amplitude entropy: --

### irregular_rr
- Label: Irregular RR / AF-like
- Coherence: 0.329
- Peak frequency: 0.170 Hz
- HRV state: Tracking
- HRV RMSSD: 189.1 ms
- HRV SDNN: 128.7 ms
- Tracking state: Tracking
- Interval entropy: 1.116
- Amplitude entropy: 0.351

### jittered_breathing
- Label: Jittered breathing
- Coherence: 0.453
- Peak frequency: 0.203 Hz
- HRV state: Tracking
- HRV RMSSD: 14.2 ms
- HRV SDNN: 15.3 ms
- Tracking state: Tracking
- Interval entropy: 1.200
- Amplitude entropy: 0.523

### off_10bpm
- Label: Off-resonance 10 BPM
- Coherence: 0.811
- Peak frequency: 0.168 Hz
- HRV state: Tracking
- HRV RMSSD: 19.0 ms
- HRV SDNN: 22.2 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 1.213

### off_12bpm
- Label: Off-resonance 12 BPM
- Coherence: 0.732
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 17.8 ms
- HRV SDNN: 18.4 ms
- Tracking state: Tracking
- Interval entropy: 0.672
- Amplitude entropy: 0.501

### off_18bpm
- Label: Off-resonance 18 BPM
- Coherence: 0.269
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 12.2 ms
- HRV SDNN: 10.6 ms
- Tracking state: Tracking
- Interval entropy: 0.547
- Amplitude entropy: 1.631

### off_24bpm
- Label: Off-resonance 24 BPM
- Coherence: 0.387
- Peak frequency: 0.040 Hz
- HRV state: Tracking
- HRV RMSSD: 6.6 ms
- HRV SDNN: 7.1 ms
- Tracking state: Tracking
- Interval entropy: 0.421
- Amplitude entropy: 0.894

### regular
- Label: Regular breathing / stable sinus rhythm
- Coherence: 0.561
- Peak frequency: 0.202 Hz
- HRV state: Tracking
- HRV RMSSD: 10.1 ms
- HRV SDNN: 11.6 ms
- Tracking state: Tracking
- Interval entropy: 0.026
- Amplitude entropy: 0.375

### resonance_010hz
- Label: 0.10 Hz resonance breathing
- Coherence: 0.844
- Peak frequency: 0.098 Hz
- HRV state: Tracking
- HRV RMSSD: 27.5 ms
- HRV SDNN: 49.5 ms
- Tracking state: Tracking
- Interval entropy: 0.000
- Amplitude entropy: 2.303

