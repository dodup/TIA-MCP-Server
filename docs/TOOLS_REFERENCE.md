# Siemens TIA Portal V21 Openness MCP Server
## Complete Tools Reference Dictionary (62 Tools)

**Protocol Specification:** Model Context Protocol (MCP) JSON-RPC 2.0  
**Transport Binding:** HTTP (`POST /mcp`), SSE (`GET /sse`), stdio  
**Host & Port:** `http://localhost:5001/` (Configured via `config.json`)  
**Target API:** Siemens TIA Portal Openness V21 & S7-PLCSIM Advanced API V8.0 (.NET 4.8 x64)  

---

## Tool Category Index

| Category | Domain | Tool Count | Tools Included |
| :---: | :--- | :---: | :--- |
| **1** | [Connection & Session Management](#category-1-connection--session-management-8-tools) | 8 | `tia_get_status`, `tia_list_processes`, `tia_connect`, `tia_disconnect`, `tia_get_project_info`, `tia_open_project`, `tia_save_project`, `tia_close_project` |
| **2** | [Hardware & Device Topology](#category-2-hardware--device-topology-2-tools) | 2 | `tia_list_devices`, `tia_get_device_tree` |
| **3** | [PLC Software, Blocks & Compilation](#category-3-plc-software-blocks--compilation-9-tools) | 9 | `tia_list_plcs`, `tia_list_blocks`, `tia_get_block_code`, `tia_import_block`, `tia_delete_block`, `tia_list_plc_types`, `tia_get_plc_type`, `tia_import_plc_type`, `tia_compile` |
| **4** | [WinCC Unified HMI Engineering](#category-4-wincc-unified-hmi-screen--widget-engineering-29-tools) | 29 | `hmi_list_targets`, `hmi_list_screens`, `hmi_create_screen`, `hmi_delete_screen`, `hmi_get_screen_items`, `hmi_delete_screen_items`, `hmi_add_label`, `hmi_add_button`, `hmi_add_shape`, `hmi_add_io_field`, `hmi_set_tag_dynamization`, `hmi_update_button_scripts`, `hmi_list_tag_tables`, `hmi_list_tags`, `hmi_create_tag`, `hmi_update_tag_acquisition_cycles`, `hmi_set_tag_range`, `hmi_create_data_log`, `hmi_assign_logging_tags`, `hmi_list_connections`, `hmi_create_connection`, `hmi_delete_connection`, `hmi_list_text_lists`, `hmi_create_text_list`, `hmi_delete_text_list`, `hmi_export_text_lists`, `hmi_compile`, `tia_list_tag_tables`, `tia_get_tags` |
| **5** | [Siemens TestSuite Automation](#category-5-siemens-testsuite-automation-2-tools) | 2 | `tia_testsuite_list_tests`, `tia_testsuite_load_test` |
| **6** | [S7-PLCSIM Advanced Live Simulation & Testing](#category-6-s7-plcsim-advanced-live-simulation--testing-12-tools) | 12 | `plcsim_list_instances`, `plcsim_connect`, `plcsim_disconnect`, `plcsim_get_status`, `plcsim_set_operating_state`, `plcsim_set_scale_factor`, `plcsim_read_tag`, `plcsim_write_tag`, `plcsim_read_tags`, `plcsim_write_tags`, `plcsim_run_sequence`, `plcsim_run_mixing_sequence` |

---

## Category 1: Connection & Session Management (8 Tools)

### 1. `tia_get_status`
- **Description:** Returns the live connection state of the MCP server, active process ID (PID), open project name, file path, and whether the server owns the TIA instance.
- **Parameters:** None (`{}`)
- **Request Example:**
  ```json
  {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "tia_get_status",
      "arguments": {}
    }
  }
  ```
- **Response Example:**
  ```json
  {
    "IsConnected": true,
    "AttachedProcessId": 16028,
    "ProjectName": "TestMCP-DDup",
    "ProjectPath": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
    "OwnsPortalInstance": false
  }
  ```

---

### 2. `tia_list_processes`
- **Description:** Lists all running TIA Portal instances on the host with their PID, project name, path, and UI mode.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  [
    {
      "Id": 16028,
      "ProjectPath": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
      "ProjectName": "TestMCP-DDup",
      "Mode": "WithUserInterface"
    }
  ]
  ```

---

### 3. `tia_connect`
- **Description:** Attaches the server to a running TIA Portal process. If `process_id` is omitted, attaches to the first running instance found.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `process_id` | integer | No | null | Target TIA Portal process ID (PID). |
- **Request Example:**
  ```json
  {
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "tia_connect",
      "arguments": { "process_id": 16028 }
    }
  }
  ```
- **Response Example:**
  ```json
  {
    "Success": true,
    "ProcessId": 16028,
    "ProjectName": "TestMCP-DDup",
    "ProjectPath": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
    "Message": "Successfully attached to TIA Portal PID 16028 with project 'TestMCP-DDup'."
  }
  ```

---

### 4. `tia_disconnect`
- **Description:** Detaches from the active TIA Portal instance and releases COM references without closing TIA Portal.
- **Parameters:** None (`{}`)
- **Response Example:** `"Disconnected from TIA Portal."`

---

### 5. `tia_get_project_info`
- **Description:** Returns metadata for the currently active project.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  {
    "Name": "TestMCP-DDup",
    "Path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
    "Author": "Engineering User",
    "Comment": "Automated Mixing Line Project",
    "DateCreated": "2026-09-20 10:14:00",
    "DateModified": "2026-09-30 18:45:00",
    "IsModified": true,
    "AttachedProcessId": 16028
  }
  ```

---

### 6. `tia_open_project`
- **Description:** Launches a new TIA Portal instance and opens a specified project file (`.ap21`, `.ap20`).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `project_path` | string | **Yes** | — | Absolute path to the `.ap21` project file. |
  | `with_gui` | boolean | No | `true` | `true` for GUI, `false` for headless background mode. |
- **Request Example:**
  ```json
  {
    "name": "tia_open_project",
    "arguments": {
      "project_path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\TestMCP-DDup.ap21",
      "with_gui": true
    }
  }
  ```

---

### 7. `tia_save_project`
- **Description:** Persists all modifications to the active project file.
- **Parameters:** None (`{}`)
- **Response Example:** `"Project saved successfully."`

---

### 8. `tia_close_project`
- **Description:** Closes the current project without terminating the TIA Portal instance.
- **Parameters:** None (`{}`)
- **Response Example:** `"Project closed."`

---

## Category 2: Hardware & Device Topology (2 Tools)

### 9. `tia_list_devices`
- **Description:** Lists all top-level devices (PLCs, HMIs, PC systems, drives, distributed I/O racks).
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  [
    {
      "Name": "PLC_1",
      "TypeIdentifier": "OrderNumber:6ES7 516-3AN02-0AB0/V3.1",
      "DeviceGroup": "Line1"
    },
    {
      "Name": "PC-System_1",
      "TypeIdentifier": "OrderNumber:6AV2 107-...",
      "DeviceGroup": "HMI"
    }
  ]
  ```

---

### 10. `tia_get_device_tree`
- **Description:** Recursively inspects hardware slots, racks, interface modules, and subnets of a device.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `device_name` | string | **Yes** | — | Target device name (e.g. `"PLC_1"`). |

---

## Category 3: PLC Software, Blocks & Compilation (9 Tools)

### 11. `tia_list_plcs`
- **Description:** Lists all PLCs with block counts, UDT counts, and tag table counts.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  [
    {
      "DeviceName": "PLC_1",
      "ItemName": "PLC_1",
      "PlcName": "PLC_1",
      "BlockCount": 24,
      "TypeCount": 8,
      "TagTableCount": 5
    }
  ]
  ```

---

### 12. `tia_list_blocks`
- **Description:** Enumerate all program blocks (OB, FB, FC, DB) with language and consistency state.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `plc_name` | string | No | null | Target PLC (defaults to first PLC). |
  | `block_type` | string | No | null | Filter: `"OB"`, `"FB"`, `"FC"`, `"DB"`. |
  | `folder` | string | No | null | Subfolder inside Program Blocks. |
- **Response Example:**
  ```json
  [
    {
      "Name": "Main",
      "Number": 1,
      "BlockType": "OB",
      "ProgrammingLanguage": "SCL",
      "IsConsistent": true,
      "IsKnowHowProtected": false
    },
    {
      "Name": "FB_MixingLine",
      "Number": 10,
      "BlockType": "FB",
      "ProgrammingLanguage": "SCL",
      "IsConsistent": true,
      "IsKnowHowProtected": false
    }
  ]
  ```

---

### 13. `tia_get_block_code`
- **Description:** Exports block definition as SimaticML XML and extracts readable SCL logic.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `block_name` | string | **Yes** | — | Block name (e.g. `"FB_MixingLine"`). |
  | `plc_name` | string | No | null | Target PLC name. |
- **Response Example:**
  ```json
  {
    "Name": "FB_MixingLine",
    "Number": 10,
    "BlockType": "FB",
    "ProgrammingLanguage": "SCL",
    "XmlContent": "<?xml version=\"1.0\"...<Document>...</Document>",
    "ExtractedScl": "REGION Motor Control\n  IF #Cmd_Start THEN\n    #Motor_Run := TRUE;\n  END_IF;\nEND_REGION"
  }
  ```

---

### 14. `tia_import_block`
- **Description:** Imports or updates a block from SimaticML XML.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `xml_content` | string | **Yes** | — | SimaticML XML string. |
  | `overwrite` | boolean | No | `true` | Overwrite existing block. |
  | `plc_name` | string | No | null | Target PLC name. |
  | `folder` | string | No | null | Subfolder inside Program Blocks. |

---

### 15. `tia_delete_block`
- **Description:** Deletes a program block from the PLC.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `block_name` | string | **Yes** | — | Block name to delete. |
  | `plc_name` | string | No | null | Target PLC name. |

---

### 16. `tia_list_plc_types`
- **Description:** Lists all User Data Types (UDTs) configured in the PLC.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `plc_name` | string | No | null | Target PLC name. |

---

### 17. `tia_get_plc_type`
- **Description:** Exports a User Data Type (UDT) as SimaticML XML.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `type_name` | string | **Yes** | — | UDT name (e.g. `"typeMotorControl"`). |
  | `plc_name` | string | No | null | Target PLC name. |

---

### 18. `tia_import_plc_type`
- **Description:** Imports or replaces a User Data Type (UDT) from SimaticML XML.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `xml_content` | string | **Yes** | — | SimaticML XML definition. |
  | `overwrite` | boolean | No | `true` | Overwrite if type exists. |
  | `plc_name` | string | No | null | Target PLC name. |

---

### 19. `tia_compile`
- **Description:** Compiles PLC software via Openness `ICompilable`. Returns error counts, warnings, and compiler messages with line numbers.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `plc_name` | string | No | null | Target PLC name. |
- **Response Example:**
  ```json
  {
    "State": "Success",
    "ErrorCount": 0,
    "WarningCount": 0,
    "Messages": [
      {
        "DateTime": "2026-09-30 18:30:00",
        "Description": "Compilation completed successfully",
        "Path": "PLC_1",
        "State": "Success",
        "WarningCount": 0,
        "ErrorCount": 0
      }
    ]
  }
  ```

---

## Category 4: WinCC Unified HMI Screen & Widget Engineering (18 Tools)

### 20. `hmi_list_targets`
- **Description:** Enumerates all WinCC Unified runtime devices and software containers.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  [
    {
      "DeviceName": "PC-System_1",
      "DeviceItemName": "HMI_RT_1",
      "SoftwareName": "HMI_RT_1",
      "ScreenCount": 2,
      "TagTableCount": 3
    }
  ]
  ```

---

### 21. `hmi_list_screens`
- **Description:** Lists all screens in the target HMI with resolution and item count.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `target_name` | string | No | null | HMI software target name. |
- **Response Example:**
  ```json
  [
    { "Name": "Screen_1", "Width": 1920, "Height": 1080, "ItemCount": 108 },
    { "Name": "Screen_2", "Width": 1920, "Height": 1080, "ItemCount": 150 }
  ]
  ```

---

### 22. `hmi_create_screen`
- **Description:** Creates or ensures existence of an HMI screen with specific dimensions.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Screen name (e.g. `"Screen_2"`). |
  | `width` | integer | No | null | Screen width in pixels. |
  | `height` | integer | No | null | Screen height in pixels. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 23. `hmi_delete_screen`
- **Description:** Deletes a screen from the HMI software target.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Screen name to delete. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 24. `hmi_get_screen_items`
- **Description:** Reads every element (button, label, shape, IO field) on a screen with positions, colors, event scripts, and dynamizations.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Screen name to inspect. |
  | `target_name` | string | No | null | Target HMI software. |
- **Response Example:**
  ```json
  [
    {
      "Name": "btn_M101_Start",
      "Type": "HmiButton",
      "Left": 40,
      "Top": 100,
      "Width": 90,
      "Height": 34,
      "BackColor": "#16A34A",
      "Text": "Start",
      "TappedScript": "Tags.SysFct.InvertBitInTag(\"HMI_MotorInfeed_Start\", 0);"
    }
  ]
  ```

---

### 25. `hmi_delete_screen_items`
- **Description:** Deletes specific screen items by name list or name prefix.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `item_names` | array of str | No | null | List of exact item names to delete. |
  | `prefix` | string | No | null | Prefix filter (e.g. `"M101_"`). |
  | `target_name` | string | No | null | Target HMI software. |

---

### 26. `hmi_add_label`
- **Description:** Adds or updates a text label with typography, alignment, and colors (automatically HTML-wrapped).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `name` | string | **Yes** | — | Item name (e.g. `"lbl_Header"`). |
  | `text` | string | **Yes** | — | Display text string. |
  | `left` | integer | **Yes** | — | X coordinate in pixels. |
  | `top` | integer | **Yes** | — | Y coordinate in pixels. |
  | `width` | integer | **Yes** | — | Width in pixels. |
  | `height` | integer | **Yes** | — | Height in pixels. |
  | `font_size` | number | No | `11.0` | Font size in points. |
  | `is_bold` | boolean | No | `false` | Bold text weight. |
  | `align` | string | No | `"Center"` | `"Left"`, `"Center"`, or `"Right"`. |
  | `fore_color` | string | No | null | Text color (hex `#FFFFFF` or named). |
  | `back_color` | string | No | null | Background fill color. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 27. `hmi_add_button`
- **Description:** Adds or updates a button with caption, colors, pressed state tag, and OnClick/Tapped JavaScript event handler.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `name` | string | **Yes** | — | Button item name. |
  | `text` | string | **Yes** | — | Button text caption. |
  | `left`, `top`, `width`, `height` | integer | **Yes** | — | Geometry in pixels. |
  | `back_color` | string | No | null | Button background color. |
  | `fore_color` | string | No | null | Button text color. |
  | `pressed_tag` | string | No | null | Bound tag for pressed state. |
  | `tapped_script` | string | No | null | JavaScript code to execute on tap. |
  | `target_name` | string | No | null | Target HMI software. |
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

---

### 28. `hmi_add_shape`
- **Description:** Adds or updates a shape (`"Rectangle"` or `"Circle"`) with dimensions, center/radius, fill and border.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `name` | string | **Yes** | — | Shape item name. |
  | `shape_type` | string | **Yes** | — | `"Rectangle"` or `"Circle"`. |
  | `left`, `top`, `width`, `height` | integer | No | null | Geometry for Rectangle. |
  | `center_x`, `center_y`, `radius` | integer | No | null | Coordinates for Circle. |
  | `back_color` | string | No | null | Fill color. |
  | `border_color` | string | No | null | Border stroke color. |
  | `border_width` | integer | No | `1` | Border width in px. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 29. `hmi_add_io_field`
- **Description:** Adds or updates an IO Field bound to an HMI tag for numeric or string display.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `name` | string | **Yes** | — | IO Field item name. |
  | `left`, `top`, `width`, `height` | integer | **Yes** | — | Geometry in pixels. |
  | `mode` | string | No | `"Output"` | `"Output"` or `"InputOutput"`. |
  | `process_tag` | string | No | null | Bound HMI tag for ProcessValue. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 30. `hmi_set_tag_dynamization`
- **Description:** Binds a screen item property (e.g. `BackColor`, `Visible`) to an HMI tag using `ConditionType.Range`.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `item_name` | string | **Yes** | — | Target screen item name. |
  | `property_name` | string | **Yes** | — | Property to dynamize (`"BackColor"`). |
  | `tag_name` | string | **Yes** | — | HMI tag name. |
  | `condition_type` | string | No | `"None"` | `"None"` or `"Range"`. |
  | `ranges` | array | No | null | List of `{ "from": 0, "to": 0, "value": "gray" }`. |
  | `target_name` | string | No | null | Target HMI software. |
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

---

### 31. `hmi_update_button_scripts`
- **Description:** Modifies existing button scripts **in-place** without deleting screen items, running Openness JavaScript syntax checking.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `screen_name` | string | **Yes** | — | Target screen. |
  | `find_text` | string | No | null | Text to search for (e.g. `"SetBitInTag"`). |
  | `replace_text` | string | No | null | Replacement text (e.g. `"InvertBitInTag"`). |
  | `new_script` | string | No | null | Exact new script override. |
  | `button_names` | array of str | No | null | Filter specific buttons. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 32. `hmi_list_tag_tables`
- **Description:** Lists all HMI tag tables in the HMI software target.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `target_name` | string | No | null | Target HMI software. |

---

### 33. `hmi_list_tags`
- **Description:** Lists HMI tags with their table, connection, PLC tag binding, and data type.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `table_name` | string | No | null | Tag table name filter. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 34. `hmi_create_tag`
- **Description:** Creates or updates an HMI tag linked to a PLC tag path.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tag_name` | string | **Yes** | — | HMI tag name. |
  | `plc_tag` | string | **Yes** | — | PLC tag address (e.g. `"\"DB_MixingLine\".Line.MotorInfeed.RunFb"`). |
  | `connection` | string | No | `"HMI_Connection_1"` | HMI connection name. |
  | `table_name` | string | No | `"Default tag table"` | Target table name. |
  | `target_name` | string | No | null | Target HMI software. |

---

### 35. `hmi_update_tag_acquisition_cycles`
- **Description:** Batch updates the acquisition cycle (e.g. `T100ms`, `T1s`, `T500ms`) for HMI tags in WinCC Unified.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `cycle` | string | **Yes** | — | Target acquisition cycle name or interval (e.g. `"T100ms"`, `"T1s"`). |
  | `plc_only` | boolean | No | `true` | Only update PLC-connected tags (`Connection` or `PlcTag` populated). |
  | `table_name` | string | No | null | Specific tag table name (all tables if omitted). |
  | `target_name` | string | No | null | Target HMI software. |
- **Response Example:**
  ```json
  {
    "TotalTags": 75,
    "ModifiedCount": 75,
    "AcquisitionCycle": "T100ms",
    "UpdatedTags": [
      "Clock_Byte",
      "HMI_MotorInfeed_RunFb",
      "..."
    ]
  }
  ```

---

### 36. `hmi_set_tag_range`
- **Description:** Configures minimum and maximum range limits (constant values) on an HMI tag (`InitialMinValue` / `InitialMaxValue`).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tag_name` | string | **Yes** | — | HMI tag name. |
  | `min_value` | number | No | null | Minimum limit value (e.g. `0.0`). |
  | `max_value` | number | No | null | Maximum limit value (e.g. `1800.0`). |
  | `table_name` | string | No | null | Specific tag table name. |
  | `target_name` | string | No | null | Target HMI software. |
- **Response Example:**
  `"Tag 'HMI_MotorInfeed_SpeedSetpoint' range updated: Min=0, Max=1800."`

---

### 37. `hmi_create_data_log`
- **Description:** Creates a new DataLog in WinCC Unified HMI software.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `log_name` | string | **Yes** | — | Name of the DataLog to create (e.g. `"Process_DataLog"`). |
  | `target_name` | string | No | null | Target HMI software. |
- **Response Example:**
  `"DataLog 'Process_DataLog' created successfully."`

---

### 38. `hmi_assign_logging_tags`
- **Description:** Assigns multiple HMI tags to a DataLog with logging mode (`Cyclic`, `OnChange`, `OnDemand`) and cycle (`T1s`, `T500ms`, `T100ms`).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `log_name` | string | **Yes** | — | Target DataLog name. |
  | `tag_names` | array | **Yes** | — | List of HMI tag names to assign. |
  | `cycle` | string | No | `"T1s"` | Logging cycle (`"T1s"`, `"T500ms"`, `"T100ms"`). |
  | `mode` | string | No | `"Cyclic"` | Logging mode (`"Cyclic"`, `"OnChange"`, `"OnDemand"`). |
  | `target_name` | string | No | null | Target HMI software. |
- **Response Example:**
  ```json
  {
    "DataLog": "Process_DataLog",
    "AssignedCount": 14,
    "Mode": "Cyclic",
    "Cycle": "T1s",
    "AssignedTags": [
      "HMI_MotorInfeed_SpeedAct",
      "HMI_MotorInfeed_CurrentAct",
      "HMI_TankCityWater_Level",
      "..."
    ]
  }
  ```

---

### 39. `hmi_compile`
- **Description:** Compiles the WinCC Unified HMI device and runtime software using Openness `ICompilable`.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `target_device` | string | No | null | Target device name (e.g. `"PC-System_1"`). |
- **Response Example:**
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

---

### 40. `hmi_list_connections`
- **Description:** List all HMI communication connections in the WinCC Unified HMI software (name, communication driver, partner station, partner node, and disabled status).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `device_name` | string | No | null | Target HMI device or software name (defaults to first HMI target). |
- **Response Example:**
  ```json
  [
    {
      "Name": "HMI_PLC_1_Connection_1",
      "CommunicationDriver": "SIMATIC S7 1200/1500",
      "Partner": "PLC_1",
      "Station": "PLC_1",
      "Node": "CPU 1511TF-1 PN, PROFINET interface (R0/S1)",
      "DisabledAtStartup": false,
      "Comment": ""
    }
  ]
  ```

---

### 41. `hmi_create_connection`
- **Description:** Create or configure an integrated HMI connection between a WinCC Unified HMI target and a partner PLC device/CPU over industrial Ethernet. Automatically creates the integrated hardware connection using `CommunicationManagement.Connections.Create<HmiConnection>` between local and partner CPU interfaces.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `connection_name` | string | **Yes** | — | Name of the HMI connection to create (e.g. `"HMI_PLC_1_Connection_1"`). |
  | `partner_plc_name` | string | **Yes** | — | Name of the partner PLC device in the project (e.g. `"PLC_1"`). |
  | `device_name` | string | No | null | Target HMI device or software name. |
  | `communication_driver` | string | No | `"SIMATIC S7 1200/1500"` | Communication driver. |
  | `disabled_at_startup` | boolean | No | `false` | Whether the connection is disabled at startup. |
  | `comment` | string | No | null | Optional comment describing the connection. |
- **Response Example:**
  ```json
  {
    "Success": true,
    "Message": "Created integrated HMI connection 'HMI_PLC_1_Connection_1' between 'PC-System_1' (X1: 192.168.0.1) and 'PLC_1' (X1: 192.168.0.101).",
    "Connection": {
      "Name": "HMI_PLC_1_Connection_1",
      "CommunicationDriver": "SIMATIC S7 1200/1500",
      "Partner": "PLC_1",
      "Station": "PLC_1",
      "Node": "CPU 1511TF-1 PN, PROFINET interface (R0/S1)",
      "DisabledAtStartup": false,
      "Comment": ""
    }
  }
  ```

---

### 42. `hmi_delete_connection`
- **Description:** Delete an HMI connection from WinCC Unified software and/or hardware communication management. Removes the connection from both hardware topology (`CommunicationManagement.Connections`) and runtime software connections (`HmiSoftware.Connections`).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `connection_name` | string | **Yes** | — | Name of the HMI connection to delete (e.g. `"HMI_Connection_2"`). |
  | `device_name` | string | No | null | Target HMI device or software name. |
- **Response Example:**
  ```json
  {
    "Success": true,
    "ConnectionName": "HMI_Connection_2",
    "DeletedHardware": false,
    "DeletedSoftware": true,
    "Message": "Successfully deleted HMI connection 'HMI_Connection_2' (Hardware: False, Software: True)."
  }
  ```

---

### 43. `hmi_list_text_lists`
- **Description:** List all user-defined text lists on a WinCC Unified device with their integer values/ranges, default texts, and multilingual entries.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `device_name` | string | No | null | Target HMI device or software name (optional). |
- **Sample Invocation:**
  ```json
  {
    "name": "hmi_list_text_lists",
    "arguments": {
      "device_name": "PC-System_1"
    }
  }
  ```
- **Response Example:**
  ```json
  [
    {
      "Name": "testList",
      "Entries": [
        {
          "FromValue": 0,
          "ToValue": 0,
          "DefaultText": "zero",
          "MultilingualTexts": {
            "en-US": "zero",
            "fr-CA": "zero"
          }
        },
        {
          "FromValue": 1,
          "ToValue": 1,
          "DefaultText": "one",
          "MultilingualTexts": {
            "en-US": "one",
            "fr-CA": "one"
          }
        },
        {
          "FromValue": 2,
          "ToValue": 2,
          "DefaultText": "two",
          "MultilingualTexts": {
            "en-US": "two",
            "fr-CA": "two"
          }
        }
      ]
    }
  ]
  ```

---

### 44. `hmi_create_text_list`
- **Description:** Create a new text list on a WinCC Unified device with specified entries and values/ranges. Automatically discovers configured device runtime languages (`en-US`, `fr-CA`, etc.) and populates entries across all languages.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `list_name` | string | **Yes** | — | Name of the text list to create. |
  | `entries` | array | **Yes** | — | List of entries: `[{ "value": 0, "from_value": 0, "to_value": 0, "text": "zero", "multilingual_texts": { "en-US": "zero" } }]`. |
  | `device_name` | string | No | null | Target HMI device or software name (optional). |
- **Sample Invocation:**
  ```json
  {
    "name": "hmi_create_text_list",
    "arguments": {
      "list_name": "testList",
      "entries": [
        { "value": 0, "text": "zero" },
        { "value": 1, "text": "one" },
        { "value": 2, "text": "two" }
      ],
      "device_name": "PC-System_1"
    }
  }
  ```
- **Response Example:**
  ```json
  {
    "Success": true,
    "ListName": "testList",
    "EntryCount": 3,
    "Message": "Successfully created text list 'testList' with 3 entries."
  }
  ```

---

### 45. `hmi_delete_text_list`
- **Description:** Delete an existing HMI text list from a WinCC Unified device.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `list_name` | string | **Yes** | — | Name of the text list to delete. |
  | `device_name` | string | No | null | Target HMI device or software name (optional). |
- **Sample Invocation:**
  ```json
  {
    "name": "hmi_delete_text_list",
    "arguments": {
      "list_name": "statusList"
    }
  }
  ```
- **Response Example:**
  ```json
  {
    "Success": true,
    "ListName": "statusList",
    "EntryCount": 0,
    "Message": "Successfully deleted text list 'statusList'."
  }
  ```

---

### 46. `hmi_export_text_lists`
- **Description:** Export all HMI text lists on a WinCC Unified device to YAML files (`.hmi.yml` and `.TextLibrary.hmi.yml`) in a target directory.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `destination_folder` | string | **Yes** | — | Target directory on disk to receive exported files. |
  | `base_filename` | string | **Yes** | — | Base filename prefix for export (e.g. `"AllTextLists"`). |
  | `device_name` | string | No | null | Target HMI device or software name (optional). |
- **Response Example:**
  ```json
  [
    "C:\\Exports\\AllTextLists.hmi.yml",
    "C:\\Exports\\AllTextLists.TextLibrary.hmi.yml"
  ]
  ```

---

### 47. `tia_list_tag_tables`
- **Description:** Enumerate PLC tag tables in the CPU software container.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `plc_name` | string | No | null | Target PLC name. |

---

### 48. `tia_get_tags`
- **Description:** Read PLC tags (%I, %Q, %M addresses, data types, comments) from a PLC tag table.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `table_name` | string | No | null | PLC tag table name. |
  | `plc_name` | string | No | null | Target PLC name. |

---

## Category 5: Siemens TestSuite Automation (2 Tools)

### 49. `tia_testsuite_list_tests`
- **Description:** Lists all configured application test cases in the project's TestSuite container.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  [
    {
      "Name": "TC_MixingLine_Sequence",
      "Scope": "PLC_1"
    }
  ]
  ```

---

### 50. `tia_testsuite_load_test`
- **Description:** Loads, imports, or reloads a `.tat` test file into the project with `ImportOptions.Override`.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `file_path` | string | **Yes** | — | Absolute path to `.tat` test file. |
- **Sample Invocation:**
  ```json
  {
    "name": "tia_testsuite_load_test",
    "arguments": {
      "file_path": "D:\\TIA_Project\\_AEs\\TestMCP-DDup\\UserFiles\\AI\\TC_MixingLine_Sequence.tat"
    }
  }
  ```
- **Response Example:**
  ```json
  {
    "Success": true,
    "Message": "Successfully loaded 1 test case(s) from 'TC_MixingLine_Sequence.tat'.",
    "LoadedCases": [
      {
        "Name": "TC_MixingLine_Sequence",
        "Scope": "PLC_1"
      }
    ]
  }
  ```

---

## Category 6: S7-PLCSIM Advanced Live Simulation & Testing (11 Tools)

### 51. `plcsim_list_instances`
- **Description:** Lists all registered virtual S7-1500 controller instances managed by S7-PLCSIM Advanced runtime with ID, name, operating state, CPU type, and IP address.
- **Parameters:** None (`{}`)
- **Sample Response:**
  ```json
  [
    {
      "ID": 0,
      "Name": "PLC_1",
      "OperatingState": "Run",
      "CPUType": "CPU1511TF",
      "IP": "192.168.0.101"
    }
  ]
  ```

---

### 52. `plcsim_connect`
- **Description:** Attaches directly to a virtual S7-PLCSIM Advanced instance by name, caching symbolic tag tables for zero-overhead live I/O.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `instance_name` | string | No | `"PLC_1"` | Target virtual controller name. |
- **Sample Invocation:**
  ```json
  {
    "name": "plcsim_connect",
    "arguments": { "instance_name": "PLC_1" }
  }
  ```
- **Response Example:**
  ```json
  {
    "Success": true,
    "InstanceName": "PLC_1",
    "ID": 0,
    "OperatingState": "Run",
    "OperatingMode": "Default",
    "CPUType": "CPU1511TF",
    "IP": "192.168.0.101",
    "ScaleFactor": 1.0,
    "TagCount": 314,
    "Message": "Successfully connected to S7-PLCSIM Advanced instance 'PLC_1'."
  }
  ```

---

### 53. `plcsim_disconnect`
- **Description:** Disconnects from the current virtual PLC instance and clears cached tag metadata.
- **Parameters:** None (`{}`)

---

### 54. `plcsim_get_status`
- **Description:** Retrieves real-time status of the connected virtual controller: state (`Run`/`Stop`), scale factor, CPU model, IP, and total tags in memory.
- **Parameters:** None (`{}`)
- **Response Example:**
  ```json
  {
    "Name": "PLC_1",
    "ID": 0,
    "OperatingState": "Run",
    "OperatingMode": "Default",
    "CPUType": "CPU1511TF",
    "IP": "192.168.0.101",
    "ScaleFactor": 2.0,
    "TagCount": 314
  }
  ```

---

### 55. `plcsim_set_operating_state`
- **Description:** Commands the virtual CPU operating mode (`Run`, `Stop`, or `MemoryReset`).
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `state` | string | **Yes** | — | Desired operating state (`"Run"`, `"Stop"`, `"MemoryReset"`). |

---

### 56. `plcsim_set_scale_factor`
- **Description:** Sets the simulation clock scale factor (e.g. `2.0` = 2x speed, `5.0` = 5x speed) to accelerate automated testing deterministically.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `scale_factor` | number | **Yes** | — | Positive speed scale multiplier (> 0.0). |

---

### 57. `plcsim_read_tag`
- **Description:** Reads the live value and data type of any variable from DBs, inputs, outputs, or markers by symbolic name.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tag_name` | string | **Yes** | — | Symbolic tag name (e.g. `'DB_MixingLine.Status_StepNumber'`). |
- **Response Example:**
  ```json
  {
    "TagName": "DB_MixingLine.Status_StepNumber",
    "Value": 20,
    "DataType": "Int"
  }
  ```

---

### 58. `plcsim_write_tag`
- **Description:** Writes a live value to a symbolic variable in virtual PLC memory.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tag_name` | string | **Yes** | — | Symbolic tag name (e.g. `'DB_MixingLine.Cmd_StartBatch'`). |
  | `value` | any | **Yes** | — | Value to write (boolean, integer, float, string). |

---

### 59. `plcsim_read_tags`
- **Description:** Efficient bulk read of multiple variables in a single call.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tag_names` | array | **Yes** | — | List of symbolic tag names to read. |

---

### 60. `plcsim_write_tags`
- **Description:** Bulk write to multiple symbolic variables in a single call.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `tags` | object | **Yes** | — | Key-value dictionary mapping tag names to values. |

---

### 61. `plcsim_run_sequence`
- **Description:** Executes a generic automated online sequence test on any PLC sequence or state machine in S7-PLCSIM Advanced. Writes initial and parameter tags, pulses the start tag, tracks step number progression, samples specified monitored tags at ~80ms, and generates an execution telemetry report.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `sequence_name` | string | No | `"Sequence_01"` | Name/identifier for the sequence run. |
  | `start_command_tag` | string | **Yes** | — | Symbolic tag pulsed TRUE to trigger sequence start. |
  | `reset_command_tag` | string | No | null | Optional tag pulsed TRUE to reset alarms before start. |
  | `step_number_tag` | string | **Yes** | — | Integer tag holding current step/state index. |
  | `step_name_tag` | string | No | null | Optional string tag holding step description. |
  | `done_tag` | string | **Yes** | — | Boolean tag indicating sequence completion. |
  | `fault_tag` | string | No | null | Optional boolean tag indicating fault/alarm. |
  | `idle_step` | integer | No | `0` | Step number indicating idle standby state. |
  | `initial_tags` | object | No | null | Map of initial values written prior to test start. |
  | `parameter_tags` | object | No | null | Map of recipe or setpoint parameters written. |
  | `monitored_tags` | array | No | null | List of tag names to sample in real-time telemetry. |
  | `scale_factor` | number | No | `1.0` | Simulation time speed multiplier. |
  | `timeout_seconds` | integer | No | `60` | Max timeout before declaring test failure. |
- **Sample Response:**
  ```json
  {
    "Success": true,
    "SequenceName": "AssemblyLine_CycleA",
    "ScaleFactor": 2.0,
    "TotalDurationSeconds": 16.20,
    "StepTransitions": [
      { "StepNumber": 10, "StepName": "PREPARATION", "DurationSeconds": 2.10 },
      { "StepNumber": 20, "StepName": "ACTIVE_PROCESS", "DurationSeconds": 7.15 },
      { "StepNumber": 30, "StepName": "TRANSFER", "DurationSeconds": 3.40 },
      { "StepNumber": 40, "StepName": "COMPLETION", "DurationSeconds": 0.50 },
      { "StepNumber": 0, "StepName": "IDLE", "DurationSeconds": 0.0 }
    ],
    "Telemetry": [
      { "ElapsedSeconds": 0.08, "StepNumber": 10, "Values": { "DB_Sequence.actualSpeed": 0.0, "DB_Sequence.pressureBar": 4.5 } }
    ]
  }
  ```

---

### 62. `plcsim_run_mixing_sequence`
- **Description:** Executes an automated multi-parameter batch sequence test. Sets recipe parameters, triggers start, tracks step transitions, records 80ms telemetry, and generates an execution report.
- **Parameters:**
  | Parameter | Type | Required | Default | Description |
  | :--- | :---: | :---: | :---: | :--- |
  | `recipe_id` | string | No | `"RECIPE_01"` | Recipe identifier stored in DB. |
  | `water_liters` | number | No | `100.0` | Target water filling volume in liters. |
  | `additive_type` | integer | No | `1` | Additive flavor index (1, 2, 3). |
  | `additive_amount` | number | No | `20.0` | Target additive volume in liters. |
  | `sugar_kg` | number | No | `10.0` | Target sugar dosing mass in kg. |
  | `mix_time_seconds` | number | No | `3.0` | Agitation time setpoint in seconds. |
  | `mix_speed_rpm` | number | No | `1200.0` | Agitator drive speed in RPM. |
  | `scale_factor` | number | No | `1.0` | Simulation time speed multiplier. |
  | `timeout_seconds` | integer | No | `60` | Max timeout before declaring test failure. |
- **Sample Response:**
  ```json
  {
    "Success": true,
    "Recipe": {
      "RecipeID": "RECIPE_B_STANDARD",
      "WaterLiters": 160.0,
      "AdditiveType": 2,
      "AdditiveAmount": 25.0,
      "SugarKg": 12.0,
      "MixTimeSeconds": 3.5,
      "MixSpeedRpm": 1350.0
    },
    "TotalDurationSeconds": 18.91,
    "PeakTankLevel": 345.24,
    "PeakMixerSpeed": 1350.0,
    "StepTransitions": [
      { "StepNumber": 20, "StepName": "Stage 1: Infeed", "DurationSeconds": 2.76 },
      { "StepNumber": 30, "StepName": "Stage 2: Dosing", "DurationSeconds": 3.21 },
      { "StepNumber": 50, "StepName": "Stage 3: Blending", "DurationSeconds": 4.63 },
      { "StepNumber": 60, "StepName": "Stage 4: Transfer", "DurationSeconds": 5.55 },
      { "StepNumber": 65, "StepName": "Stage 5: Finalize", "DurationSeconds": 2.47 },
      { "StepNumber": 0, "StepName": "Idle / Ready", "DurationSeconds": 0.0 }
    ]
  }
  ```

