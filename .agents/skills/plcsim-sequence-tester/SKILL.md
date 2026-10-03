---
name: plcsim-sequence-tester
description: Execute automated online sequence verification tests on Siemens S7-PLCSIM Advanced virtual controllers, test state machines with parameterized inputs, record real-time telemetry, and generate comprehensive validation reports.
---

# S7-PLCSIM Advanced Online Sequence Tester & Verification Workflow

This skill guides you through executing automated online sequence testing against virtual Siemens S7-1500 controllers running in **Siemens S7-PLCSIM Advanced** (via API V8.0). It enables hardware-in-the-loop (HIL) automated regression testing, parameterized recipe and setpoint validation, real-time telemetry capture, and automatic verification report generation for any industrial state machine.

---

## 1. Prerequisites & Environment

1. **S7-PLCSIM Advanced Runtime**:
   - Runtime API library: `C:\Program Files (x86)\Common Files\Siemens\PLCSIMADV\API\8.0\Siemens.Simatic.Simulation.Runtime.Api.x64.dll`.
   - Running Virtual Controller instance (e.g. `PLC_1` with IP `192.168.0.101`, CPU `CPU1511TF`).
2. **TIA Openness MCP Connector**:
   - `C:\Users\dominicae\Desktop\tia-mcp-agy\repo\src\bin\Release\net48\TiaOpennessMcp.exe`.
   - Native integration with 12 PLCSIM tools and CLI flag `--run-sequence-test`.

---

## 2. Available S7-PLCSIM Advanced Tools

| Tool Name | Parameters | Description |
|:----------|:-----------|:------------|
| `plcsim_list_instances` | *(none)* | Enumerates all virtual controller instances registered in the PLCSIM runtime manager. |
| `plcsim_connect` | `instance_name` (string, default `"PLC_1"`) | Connects to an instance and registers all symbolic tags in live memory. |
| `plcsim_disconnect` | *(none)* | Disconnects from the current virtual PLC instance. |
| `plcsim_get_status` | *(none)* | Queries live status: name, operating state (`Run`/`Stop`), CPU type, IP address, scale factor, and tag count. |
| `plcsim_set_operating_state` | `state` (`"Run"`, `"Stop"`, `"MemoryReset"`) | Commands the controller operating mode. |
| `plcsim_set_scale_factor` | `scale_factor` (number, e.g. `1.0`, `2.0`, `5.0`) | Scales simulation clock speed to accelerate tests deterministically without modifying PLC code. |
| `plcsim_read_tag` | `tag_name` (string) | Reads a live variable from DBs, inputs, outputs, or markers by symbolic name. |
| `plcsim_write_tag` | `tag_name` (string), `value` (bool/num/str) | Writes a live value to a symbolic variable in PLC memory. |
| `plcsim_read_tags` | `tag_names` (array of strings) | Bulk reads multiple variables in a single call. |
| `plcsim_write_tags` | `tags` (object: key-value dictionary) | Bulk writes multiple variables in a single call. |
| `plcsim_run_sequence` | Sequence configuration (see below) | Universal sequence runner: configures tags, pulses start, monitors steps, captures telemetry, and returns a structured report. |
| `plcsim_run_mixing_sequence` | Legacy recipe parameters | Multi-recipe automated sequence runner alias. |

---

## 3. Universal Sequence Testing Configuration (`plcsim_run_sequence`)

When testing any PLC sequence or state machine, provide the following parameters:

```json
{
  "sequence_name": "AssemblyLine_CycleA",
  "start_command_tag": "DB_Sequence.cmdStart",
  "reset_command_tag": "DB_Sequence.cmdReset",
  "step_number_tag": "DB_Sequence.currentStep",
  "step_name_tag": "DB_Sequence.currentStepName",
  "done_tag": "DB_Sequence.statusDone",
  "fault_tag": "DB_Sequence.statusFault",
  "idle_step": 0,
  "parameter_tags": {
    "DB_Sequence.config.targetVelocity": 1200.0,
    "DB_Sequence.config.batchQuantity": 100,
    "DB_Sequence.config.dwellTime": 2.5
  },
  "monitored_tags": [
    "DB_Sequence.telemetry.actualVelocity",
    "DB_Sequence.telemetry.motorCurrent",
    "DB_Sequence.telemetry.completedUnits",
    "DB_Sequence.telemetry.pressureBar"
  ],
  "scale_factor": 2.0,
  "timeout_seconds": 60
}
```

### Parameter Reference:
- `sequence_name`: Descriptive identifier for the test run.
- `start_command_tag`: Boolean tag pulsed `TRUE` (then `FALSE` once step changes) to initiate sequence.
- `reset_command_tag`: Optional boolean tag pulsed before start to clear alarms or reset states.
- `step_number_tag`: Integer tag holding current step/state index (e.g. `0`, `10`, `20`, `30`).
- `step_name_tag`: Optional string tag indicating current step description.
- `done_tag`: Boolean tag that transitions to `TRUE` upon successful sequence completion.
- `fault_tag`: Optional boolean tag monitored for abnormal termination.
- `idle_step`: The step integer value indicating sequence is in idle standby (default `0`).
- `parameter_tags`: Key-value dictionary of recipe values, setpoints, or configuration parameters applied before start.
- `monitored_tags`: Array of tag names polled at high frequency (~80ms) throughout execution.
- `scale_factor`: PLCSIM clock scaling multiplier (e.g. `2.0` = 2x real-time speed).
- `timeout_seconds`: Wall-clock timeout limit before marking test failed.

---

## 4. Universal Sequence State Machine Architecture

A robust industrial sequence typically implements the standard state pattern:

```
  +------------------+
  |    0: IDLE       | <------------------------------------+
  +------------------+                                      |
           |                                                |
           | Cmd_Start == TRUE                              |
           v                                                |
  +------------------+                                      |
  |  10: PREPARATION | (Validate permissives, initialize)   |
  +------------------+                                      |
           |                                                |
           | Permissives OK                                 |
           v                                                |
  +------------------+                                      |
  |  20: EXECUTION   | (Main process action, actuators on)  |
  +------------------+                                      |
           |                                                |
           | Process conditions met / timer elapsed         |
           v                                                |
  +------------------+                                      |
  |  30: TRANSFER    | (Eject / transfer / cool down)       |
  +------------------+                                      |
           |                                                |
           | Transfer confirmed                             |
           v                                                |
  +------------------+                                      |
  |  40: COMPLETION  | (Latch Done flag, finalize stats)    |
  +------------------+                                      |
           |                                                |
           +------------------------------------------------+
```

---

## 5. Automated Verification Workflow & Report Generation

### Method A: Programmatic MCP Tool Calls
1. **Connect**: Call `plcsim_connect` with `instance_name: "PLC_1"`.
2. **Verify Controller State**: Call `plcsim_get_status` and verify `OperatingState == "Run"`.
3. **Execute Sequence**: Call `plcsim_run_sequence` with parameter configuration.
4. **Compile Report**: Format the output telemetry and step transitions into a structured verification report.

### Method B: Direct CLI Execution
```powershell
& "C:\Users\dominicae\Desktop\tia-mcp-agy\repo\src\bin\Release\net48\TiaOpennessMcp.exe" --run-sequence-test "verification_report.md"
```

---

## 6. Verification Criteria for Pass/Fail Assessment

A sequence run is considered **PASS** only if all of the following conditions are met:
1. **Sequence Completion**: `done_tag` transitions to `TRUE` and controller returns to `idle_step`.
2. **Zero Faults**: `fault_tag` remains `FALSE` throughout all active steps.
3. **Setpoints Achieved**: All controlled process variables reach within acceptable engineering tolerance (e.g. ±5%) of configured setpoints.
4. **Step Progression Order**: Step numbers traverse strictly according to state machine specifications without missing intermediate phases.
5. **Phase Timing**: Step durations adhere to expected timer setpoints within clock-scaled tolerances.
