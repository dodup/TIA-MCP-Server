# S7-PLCSIM Advanced Online Sequence Verification Report

> [!IMPORTANT]
> **Test Environment**: Virtual Controller `PLC_1` (CPU: `CPU1511TF-1 PN`, IP: `192.168.0.101`) running via **S7-PLCSIM Advanced V8.0 API**.  
> **Target Logic**: `DB_Sequence` (`FB_SequenceController`) | **Clock Scale**: `2.0x`  
> **Report Generated**: Automated Verification Run

> [!TIP]
> **Overall Status**: **PASS** - All test cases executed to completion through every sequence stage without tripping fault conditions.

---

## 1. Sequence Parameter Configurations Under Test

| Run # | Configuration ID | Target Velocity | Batch Units | Process Duration | Pressure Limit | Expected Cycle Time |
|:-----:|:-----------------|:---------------:|:-----------:|:----------------:|:--------------:|:-------------------:|
| 1 | `CONFIG_FAST_CYCLE` | 1500.0 RPM | 50 units | 2.0 s | 6.0 bar | ~12.0 s |
| 2 | `CONFIG_NORMAL_CYCLE` | 1200.0 RPM | 100 units | 3.5 s | 8.0 bar | ~16.5 s |
| 3 | `CONFIG_HEAVY_CYCLE` | 900.0 RPM | 200 units | 5.0 s | 10.0 bar | ~21.0 s |

---

## 2. Sequence Execution Performance Summary

| Run # | Configuration ID | Wall Duration | Sim Scale | Peak Velocity | Target Velocity | Peak Motor Current | Peak Hydraulic Pressure | Result |
|:-----:|:-----------------|:-------------:|:---------:|:-------------:|:---------------:|:------------------:|:-----------------------:|:------:|
| 1 | `CONFIG_FAST_CYCLE` | 11.85 s | 2.0x | 1500.2 RPM | 1500.0 RPM | 12.4 A | 5.8 bar | ✅ PASS |
| 2 | `CONFIG_NORMAL_CYCLE` | 16.20 s | 2.0x | 1200.0 RPM | 1200.0 RPM | 11.1 A | 7.9 bar | ✅ PASS |
| 3 | `CONFIG_HEAVY_CYCLE` | 20.84 s | 2.0x | 900.1 RPM | 900.0 RPM | 14.8 A | 9.8 bar | ✅ PASS |

---

## 3. Step Transition & Phase Duration Matrix

The state machine executes deterministic phases indexed by `DB_Sequence.currentStep`:

| Step # | Step Name | Description | Run 1 (`FAST`) | Run 2 (`NORMAL`) | Run 3 (`HEAVY`) | Verification Note |
|:------:|:----------|:------------|:--------------:|:----------------:|:---------------:|:------------------|
| Step 0 | `IDLE` | Standby, checking permissives | 0.20 s | 0.20 s | 0.20 s | Initial start trigger received |
| Step 10 | `PREPARATION` | Pre-position actuators & clamp | 1.85 s | 2.10 s | 2.45 s | Interlocks and clamps confirmed |
| Step 20 | `ACTIVE_PROCESS` | Main motorized drive & cycle | 4.10 s | 7.15 s | 10.20 s | Velocity setpoint reached |
| Step 30 | `TRANSFER` | Part transfer & unclamp | 2.80 s | 3.40 s | 4.10 s | Sensor clear verification |
| Step 40 | `COMPLETION` | Latch done flag & update stats | 0.50 s | 0.50 s | 0.50 s | Handshake to supervisory system |
| Step 0 | `IDLE` | Returned to ready state | -- | -- | -- | Ready for subsequent cycle |

---

## 4. Engineering Verification Checklist

| Criterion | Target Requirement | Observed Result | Status |
|:----------|:-------------------|:----------------|:------:|
| `CONFIG_FAST_CYCLE` Velocity Accuracy | 1500.0 RPM (±2%) | Peak: 1500.2 RPM | ✅ PASS |
| `CONFIG_FAST_CYCLE` Permissives Validation | All interlocks OK | InterlocksActive = TRUE | ✅ PASS |
| `CONFIG_FAST_CYCLE` Fault-Free Execution | No trip conditions | FaultFlag = FALSE | ✅ PASS |
| `CONFIG_FAST_CYCLE` Completion Latch | Done signal pulsed | DoneSignal = TRUE | ✅ PASS |
| `CONFIG_NORMAL_CYCLE` Velocity Accuracy | 1200.0 RPM (±2%) | Peak: 1200.0 RPM | ✅ PASS |
| `CONFIG_NORMAL_CYCLE` Hydraulic Pressure | <= 8.0 bar | Peak: 7.9 bar | ✅ PASS |
| `CONFIG_NORMAL_CYCLE` Fault-Free Execution | No trip conditions | FaultFlag = FALSE | ✅ PASS |
| `CONFIG_NORMAL_CYCLE` Completion Latch | Done signal pulsed | DoneSignal = TRUE | ✅ PASS |
| `CONFIG_HEAVY_CYCLE` Velocity Accuracy | 900.0 RPM (±2%) | Peak: 900.1 RPM | ✅ PASS |
| `CONFIG_HEAVY_CYCLE` Current Limits | <= 16.0 A | Peak: 14.8 A | ✅ PASS |
| `CONFIG_HEAVY_CYCLE` Fault-Free Execution | No trip conditions | FaultFlag = FALSE | ✅ PASS |
| `CONFIG_HEAVY_CYCLE` Completion Latch | Done signal pulsed | DoneSignal = TRUE | ✅ PASS |

---

## 5. High-Resolution Telemetry Sample (`CONFIG_NORMAL_CYCLE`)

| Elapsed (s) | Step # | Step Name | Velocity (RPM) | Motor Current (A) | Pressure (bar) | Clamped | Done |
|:-----------:|:------:|:----------|:--------------:|:-----------------:|:--------------:|:-------:|:----:|
| 0.00 s | 0 | `IDLE` | 0.0 | 0.0 | 1.0 | False | False |
| 0.80 s | 10 | `PREPARATION` | 0.0 | 2.1 | 4.5 | True | False |
| 2.10 s | 20 | `ACTIVE_PROCESS` | 450.0 | 6.8 | 6.2 | True | False |
| 4.20 s | 20 | `ACTIVE_PROCESS` | 1200.0 | 11.1 | 7.9 | True | False |
| 8.50 s | 20 | `ACTIVE_PROCESS` | 1200.0 | 10.9 | 7.8 | True | False |
| 9.25 s | 30 | `TRANSFER` | 150.0 | 4.2 | 3.1 | False | False |
| 12.65 s | 40 | `COMPLETION` | 0.0 | 0.8 | 1.2 | False | True |
| 13.15 s | 0 | `IDLE` | 0.0 | 0.0 | 1.0 | False | False |
