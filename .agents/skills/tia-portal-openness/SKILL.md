---
name: tia-portal-openness
description: >-
  Interact with Siemens TIA Portal V21 and WinCC Unified HMI via the persistent Openness MCP server connector.
  Provides workflows for checking status, inspecting devices & PLCs, managing SCL/SimaticML blocks,
  creating and modifying WinCC Unified screens and widgets, configuring range dynamizations, updating button scripts in-place,
  compiling PLCs and HMIs with error diagnostics, and executing Siemens TestSuite tests.
---

# Siemens TIA Portal V21 Openness Skill

This skill guides AI agents on controlling and automating Siemens TIA Portal V21 through the persistent TIA Openness MCP connector.

## 1. Architecture & Connection Management

The TIA Openness MCP server operates as a persistent daemon process maintaining an open connection to Siemens TIA Portal (`connexion must stay`):
- **Configuration**: Loaded from `config.json` (host, port default `5001`, mode `HttpAndStdio`, `autoConnect: true`).
- **Persistence**: Because TIA Openness instances and projects take time to initialize, the connection stays alive continuously across agent turns.
- **Transports**:
  - Direct HTTP JSON-RPC endpoint: `POST http://localhost:5001/mcp`
  - Server-Sent Events (SSE): `GET http://localhost:5001/sse`
  - Health/Status endpoint: `GET http://localhost:5001/status`
  - Standard stdio MCP transport for agent runners.

### Checking Status & Connecting
1. **`tia_get_status`**: Always check server and TIA Openness status first:
   ```json
   {}
   ```
   Returns `IsConnected`, `AttachedProcessId`, `ProjectName`, `ProjectPath`.
2. **`tia_list_processes`**: List all running TIA Portal instances if not connected.
3. **`tia_connect`**: Attach to a running instance:
   ```json
   { "process_id": 1512 }
   ```
4. **`tia_open_project`**: Open a project from disk if no instance is active:
   ```json
   { "project_path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21", "with_gui": true }
   ```

---

## 2. PLC Software & Program Blocks

### Inspecting Software & Blocks
- **`tia_list_plcs`**: Discover CPU device items and software containers.
- **`tia_list_blocks`**: List all blocks (OB, FB, FC, DB) with programming language and compile consistency status.
- **`tia_get_block_code`**: Retrieve block definition as SimaticML XML and extracted SCL code.

### Authoring & Updating Blocks
- **`tia_import_block`**: Import or replace block logic from SimaticML XML:
  ```json
  { "xml_content": "<Document>...", "overwrite": true }
  ```
- **`tia_compile`**: Always compile after modifying PLC blocks or UDTs:
  ```json
  { "plc_name": "PLC_1" }
  ```
  Returns `State`, `ErrorCount`, `WarningCount`, and detailed compiler messages with file paths and line numbers.

---

## 3. WinCC Unified HMI Screen & Widget Engineering

### Discovery & Screen Lifecycle
- **`hmi_list_targets`**: Discover all HMI runtime devices and software containers.
- **`hmi_list_screens`**: Enumerate screens with dimensions (`Width`, `Height`) and item counts.
- **`hmi_create_screen`**: Create a screen with target resolution:
  ```json
  { "screen_name": "Screen_2", "width": 1920, "height": 1080 }
  ```
- **`hmi_get_screen_items`**: Inspect all widgets, shapes, text, scripts, and dynamizations on a screen.

### Creating Widgets & Controls
- **`hmi_add_label`**: Add a text label or banner (text is HTML-escaped and wrapped automatically):
  ```json
  {
    "screen_name": "Screen_2",
    "name": "lbl_Title",
    "text": "MOTOR OVERVIEW",
    "left": 40, "top": 30, "width": 400, "height": 40,
    "font_size": 18, "is_bold": true, "align": "Center",
    "fore_color": "white", "back_color": "#1E293B"
  }
  ```
- **`hmi_add_button`**: Add a control button with an OnClick/Tapped JavaScript handler:
  ```json
  {
    "screen_name": "Screen_2",
    "name": "btn_M101_Start",
    "text": "Start",
    "left": 40, "top": 100, "width": 90, "height": 34,
    "back_color": "#16A34A", "fore_color": "white",
    "tapped_script": "Tags.SysFct.InvertBitInTag(\"HMI_MotorInfeed_Start\", 0);"
  }
  ```
- **`hmi_add_shape`**: Add indicator shapes:
  - Circle (e.g. pilot light, motor status):
    ```json
    {
      "screen_name": "Screen_2",
      "name": "circ_M101",
      "shape_type": "Circle",
      "center_x": 160, "center_y": 140, "radius": 18,
      "back_color": "gray", "border_color": "white", "border_width": 2
    }
    ```
  - Rectangle (e.g. background cards, valve indicators):
    ```json
    {
      "screen_name": "Screen_2",
      "name": "rect_Card1",
      "shape_type": "Rectangle",
      "left": 30, "top": 80, "width": 320, "height": 220,
      "back_color": "#1E293B", "border_color": "#334155", "border_width": 1
    }
    ```
- **`hmi_add_io_field`**: Add numeric/text IO display bound to process tag:
  ```json
  {
    "screen_name": "Screen_2",
    "name": "io_ActualSpeed",
    "left": 180, "top": 150, "width": 100, "height": 30,
    "mode": "Output", "process_tag": "HMI_MotorInfeed_Speed"
  }
  ```

### Dynamization & Color Mapping
Use **single-object dynamization** with `ConditionType.Range` instead of dual overlaid objects with visibility toggles:
- **`hmi_set_tag_dynamization`**:
  ```json
  {
    "screen_name": "Screen_2",
    "item_name": "circ_M101",
    "property_name": "BackColor",
    "tag_name": "HMI_MotorInfeed_RunFb",
    "condition_type": "Range",
    "ranges": [
      { "from": 0, "to": 0, "value": "gray" },
      { "from": 1, "to": 1, "value": "green" }
    ]
  }
  ```

### In-Place Button Script Updating
When modifying existing button functions (e.g. changing `SetBitInTag` to `InvertBitInTag` or changing target tags), **do NOT delete and recreate screen items**. Modifying items in-place preserves layouts, object order, and styles:
- **`hmi_update_button_scripts`**:
  ```json
  {
    "screen_name": "Screen_2",
    "find_text": "SetBitInTag",
    "replace_text": "InvertBitInTag"
  }
  ```
  The MCP server applies changes directly to the Openness DOM and runs `handler.Script.SyntaxCheck()` to guarantee syntax validity.

### HMI Tag Management & Acquisition Cycles
- **`hmi_list_tags`**: List all tags, tables, PLC bindings, data types, and acquisition cycles.
- **`hmi_create_tag`**: Create or update an HMI tag linked to a PLC tag path:
  ```json
  {
    "tag_name": "HMI_MotorInfeed_RunFb",
    "plc_tag": "DB_MixingLine.Line.MotorInfeed.RunFb",
    "connection": "HMI_Connection_1",
    "table_name": "Default tag table"
  }
  ```
- **`hmi_update_tag_acquisition_cycles`**: Batch update acquisition cycles for PLC-connected or internal tags (e.g. `T100ms`, `T1s`):
  ```json
  {
    "cycle": "T100ms",
    "plc_only": true
  }
  ```
- **`hmi_set_tag_range`**: Configure minimum and maximum limit constants (`InitialMinValue` / `InitialMaxValue`) on an HMI tag (e.g. speeds between 0 and 1800):
  ```json
  {
    "tag_name": "HMI_MotorInfeed_SpeedSetpoint",
    "min_value": 0.0,
    "max_value": 1800.0
  }
  ```
- **`hmi_create_data_log`**: Create a persistent DataLog in the WinCC Unified runtime:
  ```json
  {
    "log_name": "Process_DataLog"
  }
  ```
- **`hmi_assign_logging_tags`**: Assign analog/digital tags to a DataLog with mode (`Cyclic`, `OnChange`, `OnDemand`) and cycle (`T1s`, `T500ms`, `T100ms`):
  ```json
  {
    "log_name": "Process_DataLog",
    "tag_names": [
      "HMI_MotorInfeed_SpeedAct",
      "HMI_MotorInfeed_CurrentAct",
      "HMI_TankCityWater_Level"
    ],
    "cycle": "T1s",
    "mode": "Cyclic"
  }
  ```
### Integrated HMI Connections & Communication Management
- **`hmi_list_connections`**: Enumerate all configured HMI communication links on an HMI target:
  ```json
  {
    "device_name": "PC-System_1"
  }
  ```
  Returns connection name, communication driver (`SIMATIC S7 1200/1500`), partner station name (`PLC_1`), partner node description, and startup disabled status.
- **`hmi_create_connection`**: Establish or configure an integrated hardware HMI connection to a partner PLC:
  ```json
  {
    "connection_name": "HMI_PLC_1_Connection_1",
    "partner_plc_name": "PLC_1",
    "device_name": "PC-System_1",
    "communication_driver": "SIMATIC S7 1200/1500",
    "disabled_at_startup": false,
    "comment": "Integrated S7-1500 Ethernet communication link"
  }
  ```
  *Openness Integration Note*: An integrated HMI connection binds hardware nodes via `CommunicationManagement.Connections.Create<HmiConnection>(localNode, partnerCpuItem, partnerNode)`. TIA Openness automatically links the software connection in WinCC Unified runtime to the partner CPU, subnet, and interface.
- **`hmi_delete_connection`**: Delete an HMI connection from WinCC Unified software and/or hardware communication management:
  ```json
  {
    "connection_name": "HMI_Connection_2",
    "device_name": "PC-System_1"
  }
  ```
  Safely removes dangling or unneeded HMI connections from both hardware topology (`CommunicationManagement.Connections`) and runtime software connections (`HmiSoftware.Connections`), eliminating compiler errors.

### HMI Text Lists Management
WinCC Unified V21 manages multilingual text lists through its Version 2.0 YAML specification (`.hmi.yml` and `.TextLibrary.hmi.yml`):
- **`hmi_list_text_lists`**: List all user-defined text lists on the target Unified device along with their integer values/ranges, default texts, and multilingual entries:
  ```json
  {
    "device_name": "PC-System_1"
  }
  ```
- **`hmi_create_text_list`**: Create a new text list with decimal value or range entries and automatic multilingual propagation:
  ```json
  {
    "list_name": "testList",
    "entries": [
      { "value": 0, "text": "zero" },
      { "value": 1, "text": "one" },
      { "value": 2, "text": "two" }
    ],
    "device_name": "PC-System_1"
  }
  ```
  *Note*: Accepts optional `from_value`, `to_value`, and `multilingual_texts` dictionary per entry.
- **`hmi_delete_text_list`**: Delete an existing HMI text list by name:
  ```json
  {
    "list_name": "testList",
    "device_name": "PC-System_1"
  }
  ```
- **`hmi_export_text_lists`**: Export all HMI text lists to a directory in `.hmi.yml` and `.TextLibrary.hmi.yml` format:
  ```json
  {
    "destination_folder": "C:\\path\\to\\export",
    "base_filename": "AllTextLists",
    "device_name": "PC-System_1"
  }
  ```

### HMI Compilation
Always compile the HMI target after modifying screens or tags to ensure runtime consistency:
- **`hmi_compile`**:
  ```json
  { "target_device": "PC-System_1" }
  ```
  Verifies that JavaScript event scripts, tag bindings, and screen geometries compile with 0 errors.

---

## 4. Siemens TestSuite Automation

- **`tia_testsuite_list_tests`**: List test cases configured in the project.
- **`tia_testsuite_load_test`**: Import or update a `.tat` test case file from disk:
  ```json
  { "file_path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\UserFiles\\AI\\TC_MixingLine_Sequence.tat" }
  ```
  Uses `ImportOptions.Override` to cleanly refresh test logic.

---

## 5. WinCC Unified Runtime System Functions Catalog (`SysFct`)

All system functions can be called directly in JavaScript scripts configured on buttons (Tapped event), screen events (Loaded, Unloaded), or cyclic tasks.

### 5.1 Tag System Functions (`Tags.SysFct.*`)
- **`Tags.SysFct.InvertBitInTag(tagName, bitNumber)`**: Invert (toggle) specific bit and send to PLC. Ideal for Start/Stop toggle buttons.
- **`Tags.SysFct.SetBitInTag(tagName, bitNumber)`**: Set specific bit to 1 (true).
- **`Tags.SysFct.ResetBitInTag(tagName, bitNumber)`**: Reset specific bit to 0 (false).
- **`Tags.SysFct.SetTagValue(tagName, value)`**: Write numeric, boolean, or string value directly.
- **`Tags.SysFct.IncreaseTag(tagName, stepValue)`**: Increment numeric tag value by step.
- **`Tags.SysFct.DecreaseTag(tagName, stepValue)`**: Decrement numeric tag value by step.
- **`Tags.SysFct.LinearScaling(sourceTag, sMin, sMax, targetTag, tMin, tMax)`**: Scale analog value between ranges.
- **`Tags.SysFct.ShiftAndMask(tag, shift, mask, targetTag)`**: Bitwise shift and mask operation.
- **`Tags.SysFct.UpdateTag(updateId)`**: Force one-shot update for cyclic tag group.

### 5.2 Screen Navigation & Window Functions (`HMIRuntime.UI.SysFct.*`)
- **`HMIRuntime.UI.SysFct.ChangeScreen(screenName, screenWindowPath)`**: Switch screen in window. **Parameter `screenWindowPath` must be `"~"`** for current window.
- **`HMIRuntime.UI.SysFct.ChangeScreenAsync(screenName, screenWindowPath)`**: Asynchronous screen switch.
- **`HMIRuntime.UI.SysFct.ChangeScreenAsyncByNumber(screenNumber, screenWindowPath)`**: Screen switch by index.
- **`HMIRuntime.UI.SysFct.OpenScreenInPopup(popupName, screenName, title, left, top, showHeader, flags)`**: Open floating modal/modeless popup window.
- **`HMIRuntime.UI.SysFct.ClosePopup(popupName)`**: Close specified popup window.
- **`HMIRuntime.UI.SysFct.ZoomIn(screenWindowPath, factor)`**: Zoom into screen window.
- **`HMIRuntime.UI.SysFct.ZoomOut(screenWindowPath, factor)`**: Zoom out of screen window.
- **`HMIRuntime.UI.SysFct.ResetZoom(screenWindowPath)`**: Reset zoom level to 100% (1.0).

### 5.3 Screen Item Properties & Focus (`HMIRuntime.UI.SysFct.*`)
- **`HMIRuntime.UI.SysFct.SetPropertyValue(itemPath, propertyName, value)`**: Programmatically write item property (e.g. BackColor).
- **`HMIRuntime.UI.SysFct.GetPropertyValue(itemPath, propertyName)`**: Read item property at runtime.
- **`HMIRuntime.UI.SysFct.SetFocusOnElement(itemName, screenWindowPath)`**: Set input keyboard/mouse focus.

### 5.4 Alarms & Event Functions (`HMIRuntime.Alarming.SysFct.*`)
- **`HMIRuntime.Alarming.SysFct.AcknowledgeAlarm(alarmId)`**: Acknowledge active alarm.
- **`HMIRuntime.Alarming.SysFct.ResetAlarm(alarmId)`**: Reset cleared alarm.
- **`HMIRuntime.Alarming.SysFct.ExportAlarmLog(logName, filePath, timeRange, format)`**: Export archived alarm log to CSV.
- **`HMIRuntime.Alarming.SysFct.RestoreAlarmLog(logName, filePath)`**: Restore alarm log backup.

### 5.5 Historical Logging & Process Data (`HMIRuntime.Logging.SysFct.*`)
- **`HMIRuntime.Logging.SysFct.StartTagLogging(logName)`**: Resume historical data logging.
- **`HMIRuntime.Logging.SysFct.StopTagLogging(logName)`**: Pause historical data logging.
- **`HMIRuntime.Logging.SysFct.ExportTagLog(logName, filePath, timeRange, format)`**: Export tag log to CSV.
- **`HMIRuntime.Logging.SysFct.RestoreTagLog(logName, filePath)`**: Restore historical tag segment.
- **`HMIRuntime.Logging.SysFct.WriteManualValue(logName, tagName, timestamp, value)`**: Insert manual audit/lab value.

### 5.6 User Management & Security (`HMIRuntime.UserManagement.SysFct.*`)
- **`HMIRuntime.UserManagement.SysFct.LogOff()`**: Log off active user.
- **`HMIRuntime.UserManagement.SysFct.ShowLogonDialog()`**: Display login credential dialog.
- **`HMIRuntime.UserManagement.SysFct.ChangePassword(userName, oldPwd, newPwd)`**: Change password.

### 5.7 Diagnostics, Programs & Connections
- **`HMIRuntime.UI.SysFct.StartProgram(programPath, parameters)`**: Launch external process on PC runtime.
- **`HMIRuntime.UI.SysFct.ShowSoftwareVersion()`**: Display runtime build and version dialog.
- **`HMIRuntime.UI.SysFct.ShowControlPanel()`**: Open SIMATIC Control Panel.
- **`HMIRuntime.Connections.SysFct.ChangeConnection(connectionName, ipAddress, slot, rack)`**: Re-route PLC connection.

---

## 6. Prompt Engineering Rules for AI Agents

To get the highest quality results when prompting agents equipped with this skill:
1. **Specify Exact Target & Scope**: Include HMI software name (`HMI_RT_1`), Screen (`Screen_2`), and CPU (`PLC_1`).
2. **State & Action Clarity**: Differentiate between toggle (`InvertBitInTag`), momentary set (`SetBitInTag`), or value writing (`SetTagValue`).
3. **Preserve Over Recreate**: For modifications, explicitly instruct: *"Modify existing items in-place using `hmi_update_button_scripts`; do not delete screen items."*
4. **Single-Object Dynamization**: Always specify single-object range dynamization (`ConditionType.Range`) on `BackColor` rather than overlapping visibility objects.
5. **Always Enforce Verification**: Instruct the agent to run `hmi_compile` or `tia_compile` and verify that the operation finishes with 0 errors.

---

## 7. Virtual Controller Simulation & Testing (S7-PLCSIM Advanced)

The MCP server integrates directly with **Siemens S7-PLCSIM Advanced API V8.0** (`Siemens.Simatic.Simulation.Runtime.Api.x64.dll`), allowing agents to validate PLC logic in live memory without physical hardware:

### 7.1 Key Tools
- **Connection & State**: `plcsim_list_instances`, `plcsim_connect`, `plcsim_disconnect`, `plcsim_get_status`, `plcsim_set_operating_state`.
- **Clock Scaling**: `plcsim_set_scale_factor` (e.g. `2.0` = 2x speed) for rapid automated testing.
- **Live Memory I/O**: `plcsim_read_tag`, `plcsim_write_tag`, `plcsim_read_tags`, `plcsim_write_tags` with zero COM overhead.
- **Automated Sequence Testing**: `plcsim_run_mixing_sequence` runs end-to-end recipe tests, captures 80ms telemetry, and generates validation reports.

### 7.2 Dedicated Verification Skill
For executing automated multi-parameter batch testing, recipe matrix comparison, and verification report artifact generation, use the companion skill:
👉 [`.agents/skills/plcsim-sequence-tester/SKILL.md`](file:///d:/TIA_Project/_AEs/TestMCP-DDup/UserFiles/AI/.agents/skills/plcsim-sequence-tester/SKILL.md)

