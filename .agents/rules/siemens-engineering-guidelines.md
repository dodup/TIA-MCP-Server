---
description: Universal engineering rules and behavioral invariants for Siemens TIA Portal V21 Openness, WinCC Unified, and S7-PLCSIM Advanced.
globs: ["**/*.scl", "**/*.xml", "**/*.tat", "**/*.cs", "**/*.json", "**/*.md"]
---

# Siemens Automation & Engineering Guidelines

These rules define the required behavioral guardrails, API constraints, and engineering practices when interacting with Siemens TIA Portal V21, WinCC Unified, and S7-PLCSIM Advanced.

---

## 1. Connection Persistence ("connexion must stay")
- **Never Tear Down Connections**: TIA Portal Openness (`TiaPortal`, `Project`) and S7-PLCSIM Advanced (`IInstance`) COM objects are heavy resources. Keep connections alive across all turns and tool calls inside the `TiaManager` and `PlcSimManager` singletons.
- **Check Status First**: Always call `tia_get_status` and/or `plcsim_get_status` before initiating project operations or test runs to verify active connection and CPU operating state.
- **Auto-Attach**: If disconnected, auto-attach to the active process PID or `PLC_1` rather than launching redundant instances.

---

## 2. S7-PLCSIM Advanced API Constraints
- **Operating State Read-Only**: `IInstance.OperatingState` cannot be assigned directly (`CS0200`). To change state, always invoke the runtime methods:
  - `_instance.Run()`
  - `_instance.Stop()`
  - `_instance.MemoryReset()`
- **Data Value Type System**: `SDataValue` uses `EPrimitiveDataType` (not `EDataType`). Extract values via typed properties (`sv.Bool`, `sv.Int16`, `sv.Float`, `sv.Double`, `sv.WChar`, etc.) or custom extractors.
- **Simulation Time Scaling**: Use `IInstance.ScaleFactor` (e.g. `2.0` or `5.0`) to accelerate online sequence tests deterministically without modifying PLC scan cycle logic.
- **Symbolic Tag Resolution**: Always query tags via symbolic names (e.g. `DB_MixingLine.Status_StepNumber`).

---

## 3. WinCC Unified HMI Engineering
- **In-Place Modification Over Deletion**: Never delete and recreate screen items to update logic. Use `hmi_update_button_scripts` to preserve layout coordinates, item names, object IDs, and z-ordering.
- **HTML Text Wrapping**: All WinCC Unified label and button captions must be wrapped in valid HTML paragraphs:
  ```html
  <body><p style="margin: 0; text-align: center;">Label Text</p></body>
  ```
- **Unified System Functions (`SysFct`)**:
  - Tags: `Tags.SysFct.InvertBitInTag()`, `Tags.SysFct.SetTagValue()`, `Tags.SysFct.LinearScaling()`
  - Navigation: `HMIRuntime.UI.SysFct.ChangeScreen("ScreenName", "~")` (parameter 2 must always be `"~"`).
  - Popups: `HMIRuntime.UI.SysFct.OpenScreenInPopup()` / `ClosePopup()`.
- **Text Lists**: Create text lists at device level (`hmi_create_text_list`) with explicit integer or range mappings.
- **Connections**: Link HMI to PLC via `hmi_create_connection` with station references.

---

## 4. Sequence Testing & Verification
- **Mandatory Report Artifact**: Every automated sequence verification run must generate a comprehensive Markdown report artifact (`mixing_sequence_online_test_report.md` or `*_report.md`) detailing:
  - Test environment metadata (CPU type, IP, scale factor, tag counts).
  - Multi-parameter recipe matrix.
  - Step transition duration breakdown.
  - High-resolution telemetry samples (level, motor speed, current draw, valve states).
  - Explicit pass/fail checklist against engineering tolerances.
- **Preconditioning**: Always ensure safety interlocks (`EmergencyStopOk = TRUE`), reset faults (`Cmd_Reset`), and verify readiness before commanding batch starts.
