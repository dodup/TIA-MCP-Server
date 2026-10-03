# Siemens TIA Portal V21 Openness MCP Server
## Complete Skills & Tools Reference Manual

**Version:** 2.0.0  
**Target Platform:** Siemens TIA Portal V21 (PublicAPI .NET Framework 4.8 x64)  
**Supported Transports:** HTTP JSON-RPC 2.0, Server-Sent Events (SSE), stdio MCP  
**Default Endpoints:** `http://localhost:5001/mcp`, `http://localhost:5001/sse`, `http://localhost:5001/status`

---

## 1. Architecture & Connection Persistence

The TIA Openness MCP Server functions as a high-performance, persistent connector between AI agents and Siemens TIA Portal. 

```
 +--------------------------------------------------------------------------------+
 |                           AI Agent / Client Applications                       |
 +--------------------+----------------------------+-------------------------------+
                      |                            |
       HTTP JSON-RPC  |  Server-Sent Events (SSE)  |  Standard I/O (MCP stdio)
       POST /mcp      |  GET /sse                  |  stdin / stdout
                      v                            v
 +--------------------------------------------------------------------------------+
 |                     TiaOpennessMcp Daemon Process (Port 5001)                  |
 |                                                                                |
 |  [ServerConfig]              [HttpMcpServer]              [McpServer]          |
 |  - config.json               - Multi-client SSE Sessions  - stdio framing      |
 |  - Host:Port / Mode          - POST /rpc, POST /mcp       - stderr logging     |
 |  - AutoConnect & PID         - GET /status health check                        |
 |                                                                                |
 |  [TiaManager Singleton]                                                        |
 |  - Holds live Openness COM object references (TiaPortal, Project)              |
 |  - Guarantees persistent connection across turns ("connexion must stay")       |
 |  - Dynamic assembly resolution to Siemens PublicAPI                            |
 +-------------------------------------+------------------------------------------+
                                       | Siemens Openness COM / .NET API
                                       v
 +--------------------------------------------------------------------------------+
 |                    Siemens TIA Portal V21 Process (Active Project)             |
 |  - Hardware Configuration & Topology Engine                                    |
 |  - PLC Software Container (OB, FB, FC, DB, UDT, TagTables, TestSuite)         |
 |  - WinCC Unified PC Runtime (Screens, ScreenItems, Dynamizations, JS Engine)   |
 |  - ICompilable Native Diagnostic Compiler                                      |
 +--------------------------------------------------------------------------------+
```

### Key Architectural Tenets:
1. **Always-On Connection (`connexion must stay`)**: TIA Portal and project instances are heavy COM entities. The server maintains the `TiaPortal` and `Project` instances alive inside the `TiaManager` singleton so every agent turn executes instantly without re-attaching or reopening.
2. **Dual Transport**: Supports standard stdio MCP transport for direct terminal/editor agents, and concurrently exposes an HTTP/SSE server on a configurable TCP port for browser tools, external sidecars, or multi-agent orchestrators.
3. **Clean-Room Assembly Loading**: Siemens assemblies (`Siemens.Engineering.*`) are resolved dynamically at runtime from the local installation directory, avoiding unauthorized binary redistribution.

---

## 2. Server Configuration (`config.json`)

The server reads configuration from `config.json` located in its working or executable directory:

```json
{
  "server": {
    "host": "http://localhost:5001/",
    "port": 5001,
    "mode": "HttpAndStdio",
    "enableCors": true
  },
  "tia": {
    "autoConnect": true,
    "preferredProcessId": 16028,
    "keepAliveConnection": true,
    "apiPath": "C:\\Program Files\\Siemens\\Automation\\Portal V21\\PublicAPI\\V21\\net48"
  }
}
```

### Configuration Parameters:
- **`server.port`** *(int)*: TCP port for the HTTP/SSE listener (default: `5001`).
- **`server.mode`** *(string)*: Transport mode:
  - `"HttpAndStdio"`: Runs HTTP/SSE server in background while handling stdio messages.
  - `"Http"`: Standalone daemon serving HTTP/SSE only.
  - `"Stdio"`: Classic MCP stdio server only.
- **`server.enableCors`** *(bool)*: Emits CORS headers (`*`) on all HTTP responses for web/agent browser access.
- **`tia.autoConnect`** *(bool)*: Automatically attaches to a running TIA Portal instance on startup.
- **`tia.preferredProcessId`** *(int?)*: Target PID to attach to when multiple instances are running.
- **`tia.keepAliveConnection`** *(bool)*: Retains project instance across all client sessions.

---

## 3. The AI Agent Skill (`tia-portal-openness`)

The `tia-portal-openness` skill instructs AI agents on standard operating procedures for engineering TIA Portal projects.

### 3.1 Workflow Sequence
```
  [1. Check Status]
         │
         ▼
  `tia_get_status` ──► Connected? ──No──► `tia_list_processes` ──► `tia_connect`
         │ Yes
         ├────────────────────────────────────────┬─────────────────────────────────────┐
         ▼                                        ▼                                     ▼
[2. Hardware & PLCs]                     [3. WinCC Unified HMI]               [4. TestSuite Tests]
  - `tia_list_devices`                     - `hmi_list_targets`                 - `tia_testsuite_list_tests`
  - `tia_list_plcs`                        - `hmi_list_screens`                 - `tia_testsuite_load_test`
  - `tia_list_blocks`                      - `hmi_get_screen_items`
  - `tia_get_block_code`                   - `hmi_add_label` / `button`
  - `tia_import_block`                     - `hmi_set_tag_dynamization`
         │                                 - `hmi_update_button_scripts`
         ▼                                        │
  `tia_compile` (PLC)                             ▼
                                           `hmi_compile` (HMI)
```

### 3.2 WinCC Unified JavaScript Rules & Best Practices
1. **Bit Setting & Toggling**:
   - Set Bit: `Tags.SysFct.SetBitInTag("TagName", bitIndex);`
   - Invert (Toggle) Bit: `Tags.SysFct.InvertBitInTag("TagName", bitIndex);`
   - Reset Bit: `Tags.SysFct.ResetBitInTag("TagName", bitIndex);`
2. **Screen Navigation**:
   - `HMIRuntime.UI.SysFct.ChangeScreen("TargetScreen", "~");`
   - *Crucial:* Second argument must be `"~"` to signify the current screen window.
3. **HTML Label & Button Wrapping**:
   - WinCC Unified labels and buttons render text via mini-HTML. Plain text strings must be wrapped in `<body><p>Text</p></body>`. The MCP server's `hmi_add_label` and `hmi_add_button` handle this automatically.
4. **Single-Object Range Dynamization**:
   - Instead of creating two overlapping shapes and toggling their visibility, configure a single shape with a `ConditionType.Range` dynamization on `BackColor`.
   - Range 0..0 -> Inactive color (e.g. gray `#94A3B8`).
   - Range 1..1 -> Active color (e.g. green `#22C55E` or red `#EF4444`).
5. **In-Place Script Updates**:
   - When modifying button actions, use `hmi_update_button_scripts` rather than deleting and recreating items. This maintains coordinate positioning, z-ordering, and screen element identifiers intact.

---

## 4. Complete Tool Reference (50 Tools)

---

### Category 1: Connection & Session Management (8 Tools)

#### 1. `tia_get_status`
- **Description:** Retrieve current connection status, attached process ID, open project name, file path, and instance ownership.
- **Input Parameters:** None (`{}`)
- **Returns:**
  ```json
  {
    "IsConnected": true,
    "AttachedProcessId": 16028,
    "ProjectName": "TestMCP-DDup",
    "ProjectPath": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
    "OwnsPortalInstance": false
  }
  ```

#### 2. `tia_list_processes`
- **Description:** Enumerate all running Siemens TIA Portal instances on the machine with PID, open project path, and mode.
- **Input Parameters:** None (`{}`)
- **Returns:** Array of process objects (`Id`, `ProjectPath`, `ProjectName`, `Mode`).

#### 3. `tia_connect`
- **Description:** Attach the MCP server to a running TIA Portal instance. If `process_id` is omitted, attaches to the first available instance.
- **Input Parameters:**
  - `process_id` *(integer, optional)*: PID of the TIA Portal instance.
- **Sample Invocation:**
  ```json
  { "process_id": 16028 }
  ```

#### 4. `tia_disconnect`
- **Description:** Detach from the active TIA Portal instance and release COM references.
- **Input Parameters:** None (`{}`)

#### 5. `tia_get_project_info`
- **Description:** Retrieve metadata of the currently open project (name, path, author, comments, timestamps, modified flag).
- **Input Parameters:** None (`{}`)

#### 6. `tia_open_project`
- **Description:** Launch a new TIA Portal instance and open a project file (`.ap21`, `.ap20`) from disk.
- **Input Parameters:**
  - `project_path` *(string, required)*: Full path to `.ap21` file.
  - `with_gui` *(boolean, optional, default: true)*: Launch with graphical UI or headless.

#### 7. `tia_save_project`
- **Description:** Save all pending changes to the open project file.
- **Input Parameters:** None (`{}`)

#### 8. `tia_close_project`
- **Description:** Close the currently open project without closing TIA Portal.
- **Input Parameters:** None (`{}`)

---

### Category 2: Hardware & Device Topology (2 Tools)

#### 9. `tia_list_devices`
- **Description:** Enumerate all devices (PLCs, HMIs, Drives, distributed I/O racks) in the active project.
- **Input Parameters:** None (`{}`)
- **Returns:** Array of devices with `Name`, `TypeIdentifier`, and `DeviceGroup`.

#### 10. `tia_get_device_tree`
- **Description:** Inspect hardware topology, racks, slots, sub-modules, and communication interfaces for a specified device.
- **Input Parameters:**
  - `device_name` *(string, required)*: Name of the device (e.g. `"PLC_1"`).

---

### Category 3: PLC Software, Blocks & Compilation (9 Tools)

#### 11. `tia_list_plcs`
- **Description:** List all CPU devices and summarize their software metrics (block counts, UDT counts, tag table counts).
- **Input Parameters:** None (`{}`)

#### 12. `tia_list_blocks`
- **Description:** Enumerate all program blocks (OB, FB, FC, DB) with language (SCL, LAD, FBD, STL) and compilation consistency.
- **Input Parameters:**
  - `plc_name` *(string, optional)*: Target PLC name (defaults to first PLC).
  - `block_type` *(string, optional)*: Filter by type (`"OB"`, `"FB"`, `"FC"`, `"DB"`).
  - `folder` *(string, optional)*: Subfolder path inside Program Blocks.

#### 13. `tia_get_block_code`
- **Description:** Export a block as SimaticML XML and extract clean Structured Control Language (SCL) text.
- **Input Parameters:**
  - `block_name` *(string, required)*: Name of the block (e.g. `"FB_MixingLine"`).
  - `plc_name` *(string, optional)*: Target PLC name.

#### 14. `tia_import_block`
- **Description:** Import or update a block from SimaticML XML.
- **Input Parameters:**
  - `xml_content` *(string, required)*: Complete SimaticML XML string.
  - `overwrite` *(boolean, optional, default: true)*: Overwrite existing block.
  - `plc_name` *(string, optional)*: Target PLC.
  - `folder` *(string, optional)*: Subfolder inside Program Blocks.

#### 15. `tia_delete_block`
- **Description:** Delete a block from the PLC software container.
- **Input Parameters:**
  - `block_name` *(string, required)*: Name of block to delete.
  - `plc_name` *(string, optional)*: Target PLC.

#### 16. `tia_list_plc_types`
- **Description:** List all PLC User Data Types (UDTs) in the project.
- **Input Parameters:**
  - `plc_name` *(string, optional)*: Target PLC name.

#### 17. `tia_get_plc_type`
- **Description:** Export a User Data Type (UDT) as SimaticML XML.
- **Input Parameters:**
  - `type_name` *(string, required)*: Name of the UDT (e.g. `"typeMotorControl"`).
  - `plc_name` *(string, optional)*: Target PLC.

#### 18. `tia_import_plc_type`
- **Description:** Import or update a User Data Type (UDT) from SimaticML XML.
- **Input Parameters:**
  - `xml_content` *(string, required)*: SimaticML XML string.
  - `overwrite` *(boolean, optional, default: true)*: Overwrite existing UDT.
  - `plc_name` *(string, optional)*: Target PLC.

#### 19. `tia_compile`
- **Description:** Compile PLC software to check syntax, consistency, and generate machine code.
- **Input Parameters:**
  - `plc_name` *(string, optional)*: Target PLC name.
- **Returns:**
  ```json
  {
    "State": "Success",
    "ErrorCount": 0,
    "WarningCount": 0,
    "Messages": [
      {
        "DateTime": "2026-09-30 18:30:00",
        "Description": "Block compilation successful",
        "Path": "PLC_1/Program Blocks/FB_MixingLine",
        "State": "Success"
      }
    ]
  }
  ```

---

### Category 4: WinCC Unified HMI Screen & Widget Engineering (18 Tools)

#### 20. `hmi_list_targets`
- **Description:** Discover all HMI runtime devices and software containers in the project.
- **Input Parameters:** None (`{}`)
- **Returns:** Array of HMI targets (`DeviceName`, `SoftwareName`, `ScreenCount`, `TagTableCount`).

#### 21. `hmi_list_screens`
- **Description:** List all screens in the target HMI with dimensions and screen item counts.
- **Input Parameters:**
  - `target_name` *(string, optional)*: HMI software name.
- **Returns:**
  ```json
  [
    { "Name": "Screen_1", "Width": 1920, "Height": 1080, "ItemCount": 108 },
    { "Name": "Screen_2", "Width": 1920, "Height": 1080, "ItemCount": 150 }
  ]
  ```

#### 22. `hmi_create_screen`
- **Description:** Create or ensure existence of an HMI screen with specific dimensions.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Name of screen (e.g. `"Screen_2"`).
  - `width` *(integer, optional)*: Width in pixels (e.g. `1920`).
  - `height` *(integer, optional)*: Height in pixels (e.g. `1080`).
  - `target_name` *(string, optional)*: HMI software name.

#### 23. `hmi_delete_screen`
- **Description:** Delete an HMI screen from the project.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Name of screen.
  - `target_name` *(string, optional)*: HMI software name.

#### 24. `hmi_get_screen_items`
- **Description:** Detailed readout of all objects (buttons, labels, shapes, IO fields) on a screen with positions, colors, scripts, and dynamizations.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Name of screen.
  - `target_name` *(string, optional)*: HMI software name.
- **Returns:** Array of objects with `Left`, `Top`, `Width`, `Height`, `BackColor`, `BorderColor`, `Text`, `TappedScript`, and `Dynamizations`.

#### 25. `hmi_delete_screen_items`
- **Description:** Delete specific screen items by name list or prefix (e.g. `"M101_"`).
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `item_names` *(array of string, optional)*: Specific item names.
  - `prefix` *(string, optional)*: Prefix filter.
  - `target_name` *(string, optional)*: HMI software name.

#### 26. `hmi_add_label`
- **Description:** Add or update a label / text box with typography, alignment, and colors.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Target screen.
  - `name` *(string, required)*: Item name (e.g. `"lbl_Header"`).
  - `text` *(string, required)*: Display text.
  - `left`, `top`, `width`, `height` *(integers, required)*: Geometry.
  - `font_size` *(number, optional, default: 11)*: Font size in pt.
  - `is_bold` *(boolean, optional, default: false)*: Bold text.
  - `align` *(string, optional, default: "Center")*: `"Left"`, `"Center"`, `"Right"`.
  - `fore_color` *(string, optional)*: Hex or named color (`"#FFFFFF"`).
  - `back_color` *(string, optional)*: Background color (`"transparent"`).
  - `target_name` *(string, optional)*: Target HMI software.

#### 27. `hmi_add_button`
- **Description:** Add or update a button with caption, colors, pressed state tag, and OnClick/Tapped JavaScript script.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `name` *(string, required)*: Button item name.
  - `text` *(string, required)*: Button caption.
  - `left`, `top`, `width`, `height` *(integers, required)*: Geometry.
  - `back_color` *(string, optional)*: Button color (e.g. `"#16A34A"`).
  - `fore_color` *(string, optional)*: Text color.
  - `pressed_tag` *(string, optional)*: HMI tag for pressed state.
  - `tapped_script` *(string, optional)*: JavaScript script for Tapped event.
  - `target_name` *(string, optional)*: Target HMI software.
- **Sample Invocation:**
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

#### 28. `hmi_add_shape`
- **Description:** Add or update a shape (`"Rectangle"` or `"Circle"`) with center/radius or coordinates, fill and border.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `name` *(string, required)*: Shape item name.
  - `shape_type` *(string, required)*: `"Rectangle"` or `"Circle"`.
  - `left`, `top`, `width`, `height` *(integers, optional)*: Used for Rectangle.
  - `center_x`, `center_y`, `radius` *(integers, optional)*: Used for Circle.
  - `back_color` *(string, optional)*: Fill color.
  - `border_color` *(string, optional)*: Border color.
  - `border_width` *(integer, optional, default: 1)*: Border width in px.
  - `target_name` *(string, optional)*: Target HMI software.

#### 29. `hmi_add_io_field`
- **Description:** Add or update an IO Field bound to an HMI tag.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `name` *(string, required)*: Item name.
  - `left`, `top`, `width`, `height` *(integers, required)*: Geometry.
  - `mode` *(string, optional, default: "Output")*: `"Output"` or `"InputOutput"`.
  - `process_tag` *(string, optional)*: HMI tag bound to ProcessValue.
  - `target_name` *(string, optional)*: Target HMI software.

#### 30. `hmi_set_tag_dynamization`
- **Description:** Configure tag dynamization on an item property (such as `BackColor` range dynamization).
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `item_name` *(string, required)*: Item name.
  - `property_name` *(string, required)*: Property to dynamize (`"BackColor"`, `"Visible"`).
  - `tag_name` *(string, required)*: HMI tag name.
  - `condition_type` *(string, optional, default: "None")*: `"None"` or `"Range"`.
  - `ranges` *(array of objects, optional)*: List of `{ "from": 0, "to": 0, "value": "gray" }`.
  - `target_name` *(string, optional)*: Target HMI software.
- **Sample Invocation:**
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

#### 31. `hmi_update_button_scripts`
- **Description:** Modify button event scripts **in-place** without deleting items. Supports substring search/replace or unconditional script setting, running Openness JavaScript syntax checking.
- **Input Parameters:**
  - `screen_name` *(string, required)*: Screen name.
  - `find_text` *(string, optional)*: Text to find (e.g. `"SetBitInTag"`).
  - `replace_text` *(string, optional)*: Replacement text (e.g. `"InvertBitInTag"`).
  - `new_script` *(string, optional)*: Complete script code override.
  - `button_names` *(array of string, optional)*: Filter specific buttons.
  - `target_name` *(string, optional)*: Target HMI software.
- **Sample Invocation:**
  ```json
  {
    "screen_name": "Screen_2",
    "find_text": "SetBitInTag",
    "replace_text": "InvertBitInTag"
  }
  ```

#### 32. `hmi_list_tag_tables`
- **Description:** Enumerate all HMI tag tables in the HMI software target.
- **Input Parameters:**
  - `target_name` *(string, optional)*: Target HMI software.

#### 33. `hmi_list_tags`
- **Description:** List HMI tags with their table, connection, PLC tag binding, and data type.
- **Input Parameters:**
  - `table_name` *(string, optional)*: Tag table filter.
  - `target_name` *(string, optional)*: Target HMI software.

#### 34. `hmi_create_tag`
- **Description:** Create or update an HMI tag linked to a PLC tag path.
- **Input Parameters:**
  - `tag_name` *(string, required)*: HMI tag name.
  - `plc_tag` *(string, required)*: PLC tag path (e.g. `"DB_MixingLine.Line.MotorInfeed.RunFb"`).
  - `connection` *(string, optional, default: "HMI_Connection_1")*: HMI connection name.
  - `table_name` *(string, optional, default: "Default tag table")*: Target table.
  - `target_name` *(string, optional)*: Target HMI software.

#### 35. `hmi_update_tag_acquisition_cycles`
- **Description:** Batch update acquisition cycles (e.g. `T100ms`, `T1s`, `T500ms`) for HMI tags in WinCC Unified.
- **Input Parameters:**
  - `cycle` *(string, required)*: Target acquisition cycle (e.g. `"T100ms"`, `"T1s"`).
  - `plc_only` *(boolean, optional, default: true)*: Only update PLC-connected tags.
  - `table_name` *(string, optional)*: Specific tag table (all tables if omitted).
  - `target_name` *(string, optional)*: Target HMI software.

#### 36. `hmi_set_tag_range`
- **Description:** Configure minimum and maximum constant limits (`InitialMinValue` / `InitialMaxValue`) on an HMI tag.
- **Input Parameters:**
  - `tag_name` *(string, required)*: Target HMI tag name.
  - `min_value` *(number, optional)*: Lower limit value.
  - `max_value` *(number, optional)*: Upper limit value.
  - `table_name` *(string, optional)*: Specific tag table.
  - `target_name` *(string, optional)*: Target HMI software.

#### 37. `hmi_create_data_log`
- **Description:** Create a new DataLog in WinCC Unified HMI software.
- **Input Parameters:**
  - `log_name` *(string, required)*: Name of the DataLog (e.g. `"Process_DataLog"`).
  - `target_name` *(string, optional)*: Target HMI software.

#### 38. `hmi_assign_logging_tags`
- **Description:** Assign multiple HMI tags to a DataLog with logging mode and cycle.
- **Input Parameters:**
  - `log_name` *(string, required)*: Target DataLog name.
  - `tag_names` *(array, required)*: List of HMI tag names to log.
  - `cycle` *(string, optional, default: "T1s")*: Logging cycle interval.
  - `mode` *(string, optional, default: "Cyclic")*: Logging mode (`"Cyclic"`, `"OnChange"`, `"OnDemand"`).
  - `target_name` *(string, optional)*: Target HMI software.

#### 39. `hmi_compile`
- **Description:** Compile HMI software and runtime device via Openness `ICompilable`. Returns compilation state, message logs, and error counts.
- **Input Parameters:**
  - `target_device` *(string, optional)*: Target device (e.g. `"PC-System_1"`).
- **Sample Invocation & Output:**
  ```json
  { "target_device": "PC-System_1" }
  ```
  ```json
  {
    "State": "Success",
    "ErrorCount": 0,
    "WarningCount": 0,
    "Messages": [
      {
        "DateTime": "2026-09-30 23:01:25",
        "Description": "Compiling finished (errors: 0; warnings: 0)",
        "State": "Success"
      }
    ]
  }
  ```

#### 40. `hmi_list_connections`
- **Description:** List all configured HMI communication links (name, driver, partner station, partner node, and disabled status).
- **Input Parameters:**
  - `device_name` *(string, optional)*: Target HMI device or software name.

#### 41. `hmi_create_connection`
- **Description:** Create or configure an integrated HMI connection between an HMI target and partner PLC device/CPU via Openness `CommunicationManagement.Connections.Create<HmiConnection>`.
- **Input Parameters:**
  - `connection_name` *(string, required)*: Connection name (e.g. `"HMI_PLC_1_Connection_1"`).
  - `partner_plc_name` *(string, required)*: Partner PLC name (e.g. `"PLC_1"`).
  - `device_name` *(string, optional)*: Target HMI device name.
  - `communication_driver` *(string, optional, default: "SIMATIC S7 1200/1500")*: Driver name.
  - `disabled_at_startup` *(boolean, optional, default: false)*: Disabled at startup flag.
  - `comment` *(string, optional)*: Optional comment.

#### 42. `hmi_delete_connection`
- **Description:** Delete an HMI connection from WinCC Unified software and/or hardware communication management.
- **Input Parameters:**
  - `connection_name` *(string, required)*: Connection name to delete (e.g. `"HMI_Connection_2"`).
  - `device_name` *(string, optional)*: Target HMI device name.

#### 43. `hmi_list_text_lists`
- **Description:** List all user-defined text lists on a WinCC Unified device with their integer values/ranges, default texts, and multilingual entries.
- **Input Parameters:**
  - `device_name` *(string, optional)*: Target HMI device or software name.
- **Returns:** Array of text lists with `Name`, `Entries` (values, ranges, default text, multilingual dictionary).

#### 44. `hmi_create_text_list`
- **Description:** Create a new text list on a WinCC Unified device with specified entries and values/ranges. Automatically discovers configured device runtime languages (`en-US`, `fr-CA`, etc.) and populates entries across all languages via Version 2.0 YAML import.
- **Input Parameters:**
  - `list_name` *(string, required)*: Name of the text list to create.
  - `entries` *(array, required)*: List of entries: `[{ "value": 0, "from_value": 0, "to_value": 0, "text": "zero", "multilingual_texts": { "en-US": "zero" } }]`.
  - `device_name` *(string, optional)*: Target HMI device or software name.

#### 45. `hmi_delete_text_list`
- **Description:** Delete an existing HMI text list by name from a WinCC Unified device.
- **Input Parameters:**
  - `list_name` *(string, required)*: Name of the text list to delete.
  - `device_name` *(string, optional)*: Target HMI device or software name.

#### 46. `hmi_export_text_lists`
- **Description:** Export all HMI text lists on a WinCC Unified device to YAML files (`.hmi.yml` and `.TextLibrary.hmi.yml`) in a target directory.
- **Input Parameters:**
  - `destination_folder` *(string, required)*: Target directory on disk.
  - `base_filename` *(string, required)*: Base filename prefix for export (e.g. `"AllTextLists"`).
  - `device_name` *(string, optional)*: Target HMI device or software name.

#### 47. `tia_list_tag_tables`
- **Description:** Enumerate PLC tag tables in the CPU software container.
- **Input Parameters:**
  - `plc_name` *(string, optional)*: Target PLC.

#### 48. `tia_get_tags`
- **Description:** Read PLC tags (%I, %Q, %M addresses, data types, comments) from a PLC tag table.
- **Input Parameters:**
  - `table_name` *(string, optional)*: PLC tag table name.
  - `plc_name` *(string, optional)*: Target PLC.

---

### Category 5: Siemens TestSuite Automation (2 Tools)

#### 49. `tia_testsuite_list_tests`
- **Description:** Enumerate configured application test cases in the project's TestSuite container.
- **Input Parameters:** None (`{}`)
- **Returns:**
  ```json
  [
    {
      "Name": "TC_MixingLine_Sequence",
      "Scope": "PLC_1"
    }
  ]
  ```

#### 50. `tia_testsuite_load_test`
- **Description:** Load, import, or reload a `.tat` test case file into the project with `ImportOptions.Override`.
- **Input Parameters:**
  - `file_path` *(string, required)*: Path to the `.tat` file on disk.
- **Sample Invocation:**
  ```json
  {
    "file_path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\UserFiles\\AI\\TC_MixingLine_Sequence.tat"
  }
  ```

---

### Category 6: S7-PLCSIM Advanced Live Simulation & Testing (12 Tools)

#### 51. `plcsim_list_instances`
- **Description:** Enumerates all registered virtual S7-1500 controller instances managed by S7-PLCSIM Advanced runtime with ID, name, operating state, CPU type, and IP address.
- **Input Parameters:** *(none)*

#### 52. `plcsim_connect`
- **Description:** Attaches directly to a virtual S7-PLCSIM Advanced instance by name, caching symbolic tag tables for zero-overhead live memory I/O.
- **Input Parameters:**
  - `instance_name` *(string, optional, default: `"PLC_1"`)*: Target virtual controller instance name.

#### 53. `plcsim_disconnect`
- **Description:** Disconnects from the current virtual PLC instance and clears cached tag metadata.
- **Input Parameters:** *(none)*

#### 54. `plcsim_get_status`
- **Description:** Queries real-time status: name, operating state (`Run`/`Stop`), scale factor, CPU model, IP, and total tags in memory.
- **Input Parameters:** *(none)*

#### 55. `plcsim_set_operating_state`
- **Description:** Commands the virtual CPU operating mode (`Run`, `Stop`, or `MemoryReset`).
- **Input Parameters:**
  - `state` *(string, required)*: Desired operating state (`"Run"`, `"Stop"`, `"MemoryReset"`).

#### 56. `plcsim_set_scale_factor`
- **Description:** Sets the simulation clock scale factor (e.g. `2.0` = 2x speed, `5.0` = 5x speed) to accelerate automated testing deterministically.
- **Input Parameters:**
  - `scale_factor` *(number, required)*: Positive speed scale multiplier (> 0.0).

#### 57. `plcsim_read_tag`
- **Description:** Reads the live value and data type of any variable from DBs, inputs, outputs, or markers by symbolic name.
- **Input Parameters:**
  - `tag_name` *(string, required)*: Symbolic tag name (e.g. `'DB_Sequence.currentStep'`).

#### 58. `plcsim_write_tag`
- **Description:** Writes a live value to a symbolic variable in virtual PLC memory.
- **Input Parameters:**
  - `tag_name` *(string, required)*: Symbolic tag name (e.g. `'DB_Sequence.cmdStart'`).
  - `value` *(any, required)*: Value to write (boolean, integer, float, string).

#### 59. `plcsim_read_tags`
- **Description:** Efficient bulk read of multiple variables in a single call.
- **Input Parameters:**
  - `tag_names` *(array of strings, required)*: List of symbolic tag names to read.

#### 60. `plcsim_write_tags`
- **Description:** Bulk write to multiple symbolic variables in a single call.
- **Input Parameters:**
  - `tags` *(object, required)*: Key-value dictionary mapping tag names to values.

#### 61. `plcsim_run_sequence`
- **Description:** Executes a generic automated online sequence test on any PLC sequence or state machine. Sets initial/parameter tags, pulses start, tracks step transitions, samples monitored tags at ~80ms, and generates an execution report.
- **Input Parameters:**
  - `start_command_tag` *(string, required)*: Tag name pulsed TRUE to initiate sequence.
  - `step_number_tag` *(string, required)*: Tag name holding current step number.
  - `done_tag` *(string, required)*: Tag name indicating sequence completion.
  - `sequence_name` *(string, optional, default: `"Sequence_01"`)*: Descriptive sequence identifier.
  - `reset_command_tag` *(string, optional)*: Tag name pulsed TRUE to reset alarms before start.
  - `step_name_tag` *(string, optional)*: String tag holding step description.
  - `fault_tag` *(string, optional)*: Tag name indicating fault/alarm.
  - `idle_step` *(integer, optional, default: `0`)*: Step number indicating idle standby state.
  - `initial_tags` *(object, optional)*: Initial values written before test start.
  - `parameter_tags` *(object, optional)*: Recipe or setpoint parameters applied before start.
  - `monitored_tags` *(array of strings, optional)*: Tag names to sample in real-time telemetry.
  - `scale_factor` *(number, optional, default: `1.0`)*: Virtual controller speed multiplier.
  - `timeout_seconds` *(integer, optional, default: `60`)*: Max timeout in seconds.

#### 62. `plcsim_run_mixing_sequence`
- **Description:** Executes an automated multi-parameter batch sequence test. Sets recipe parameters, triggers start, tracks step transitions, records 80ms telemetry, and generates an execution report.
- **Input Parameters:**
  - `recipe_id` *(string, optional, default: `"RECIPE_01"`)*: Recipe identifier.
  - `water_liters` *(number, optional, default: `100.0`)*: Target liquid volume.
  - `additive_type` *(integer, optional, default: `1`)*: Ingredient type index.
  - `additive_amount` *(number, optional, default: `20.0`)*: Target ingredient amount.
  - `sugar_kg` *(number, optional, default: `10.0`)*: Target dry ingredient mass.
  - `mix_time_seconds` *(number, optional, default: `3.0`)*: Active duration in seconds.
  - `mix_speed_rpm` *(number, optional, default: `1200.0`)*: Drive speed setpoint in RPM.
  - `scale_factor` *(number, optional, default: `1.0`)*: Simulation speed multiplier.
  - `timeout_seconds` *(integer, optional, default: `60`)*: Max timeout before declaring test failure.

---

## 5. Skills Overview

### 1. `figma-to-wincc-unified`
- **Path:** `.agents/skills/figma-to-wincc-unified/SKILL.md`
- **Purpose:** Converts visual Figma HMI designs and design tokens into pixel-accurate Siemens WinCC Unified V21 screens.

### 2. `tia-portal-openness`
- **Path:** `.agents/skills/tia-portal-openness/SKILL.md`
- **Purpose:** Comprehensive programmatic engineering of Siemens TIA Portal V21 hardware, PLC logic, and WinCC Unified HMI screens.

### 3. `plcsim-sequence-tester`
- **Path:** `.agents/skills/plcsim-sequence-tester/SKILL.md`
- **Purpose:** Hardware-in-the-loop automated sequence verification tests on Siemens S7-PLCSIM Advanced virtual controllers. Runs multi-parameter sequence tests, records high-resolution telemetry, and compiles verification reports.

---

## 6. Diagnostic Verification Checklist

| Test Item | Verification Procedure | Expected Result | Status |
| :--- | :--- | :--- | :---: |
| **Server Startup** | `.\TiaOpennessMcp.exe` | Loads `config.json`, starts HTTP on port 5001 | **PASSED** |
| **Health Check** | `GET http://localhost:5001/status` | Returns `connected: true`, PID, toolCount: 62 | **PASSED** |
| **Tool Registry** | `POST /mcp` with `tools/list` | Returns all 62 tool definitions | **PASSED** |
| **PLCSIM Live Connect** | `plcsim_connect` on `PLC_1` | Attaches to virtual S7-1500 controller, caches tags | **PASSED** |
| **Live Tag I/O** | `plcsim_read_tag` & `write_tag` | Reads/writes DB variables with zero COM overhead | **PASSED** |
| **Online Sequence Testing** | `.\TiaOpennessMcp.exe --run-sequence-test` | Executes parameterized sequence validation | **PASSED** |
| **Validation Report Artifact** | Automated workflow output | Generates `SAMPLE_VERIFICATION_REPORT.md` | **PASSED** |
| **Screen Discovery** | `hmi_list_screens` | Returns active screens with widget counts | **PASSED** |
| **Text Lists** | `hmi_list_text_lists` | Returns configured text lists | **PASSED** |
| **TestSuite Lookup**| `tia_testsuite_list_tests` | Returns configured test cases | **PASSED** |
| **HMI Compilation** | `hmi_compile` on Unified target | Returns `State: Success`, `Errors: 0` | **PASSED** |

