# AI Agent Skills, Tools & Prompt Engineering Guide
## Industrial Automation with Siemens TIA Portal V21 & WinCC Unified

**Scope:** Architectural relationship between Tools and Skills, Industrial Prompt Engineering Guide, and the Complete Catalog of WinCC Unified Runtime System Functions.

---

## 1. How Tools and Skills Work Together

In modern agentic AI architectures (such as Google Antigravity, Claude, and autonomous coding agents), **Tools** and **Skills** represent two distinct, complementary halves of the agent's cognition:

```
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                                   HUMAN USER                                    │
  │                    Prompt: "Add an emergency stop toggle on Screen_2"           │
  └────────────────────────────────────────┬────────────────────────────────────────┘
                                           │
                                           ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                                    AI AGENT                                     │
  │                                                                                 │
  │   ┌─────────────────────────────────────────────────────────────────────────┐   │
  │   │                    SKILL: `tia-portal-openness`                         │   │
  │   │   - Runbook: In-Place Editing > Destructive Re-creation                 │   │
  │   │   - Guideline: Use `Tags.SysFct.InvertBitInTag("...", 0)`               │   │
  │   │   - Guideline: HTML text wrapping `<body><p>...</p></body>`             │   │
  │   │   - Sequence: Inspect Items -> Update Script -> Compile Target          │   │
  │   └────────────────────────────────────┬────────────────────────────────────┘   │
  │                                        │ Decides which tool & exact parameters  │
  │                                        ▼                                        │
  │   ┌─────────────────────────────────────────────────────────────────────────┐   │
  │   │                    MCP TOOLS (Executable Primitives)                    │   │
  │   │   1. `hmi_get_screen_items`      --> Check existing items                   │   │
  │   │   2. `hmi_update_button_scripts` --> Apply InvertBitInTag in-place          │   │
  │   │   3. `hmi_compile`               --> Verify ICompilable with 0 errors       │   │
  │   └────────────────────────────────────┬────────────────────────────────────┘   │
  └────────────────────────────────────────┼────────────────────────────────────────┘
                                           │ JSON-RPC via Port 5001 / stdio
                                           ▼
  ┌─────────────────────────────────────────────────────────────────────────────────┐
  │                    Persistent MCP Connector (TiaOpennessMcp)                    │
  │                  Siemens TIA Portal V21 & WinCC Unified Runtime                 │
  └─────────────────────────────────────────────────────────────────────────────────┘
```

### The Roles Defined:
1. **Tools are the "Hands" (Functional Primitives)**:
   - Tools are atomic, stateful API calls defined with JSON Schemas (e.g. `hmi_add_button`, `tia_compile`, `hmi_update_button_scripts`).
   - They execute actions on TIA Portal via Openness COM interfaces, return raw diagnostic data, and enforce strict parameter types.
   - *Tools do not know business rules*: `hmi_add_button` does not know whether a button should momentary-set or toggle a bit; it simply places what it is told.

2. **Skills are the "Brain" (Procedural Knowledge & Heuristics)**:
   - Skills teach the AI agent the **engineering standards, sequential runbooks, and anti-patterns** of the target environment.
   - They define *when* to call tools, *in what sequence*, *how to recover* from compilation errors, and *which syntax* to write inside script bodies.
   - *Example:* The skill instructs the agent: *"Never delete and recreate a button just to change its script; use `hmi_update_button_scripts` to preserve visual layout and object GUIDs."*

3. **Why Persistence (`connexion must stay`) is Critical**:
   - Because TIA Portal Openness projects are massive COM objects that take minutes to initialize, the persistent MCP server keeps the connection permanently open across agent turns.
   - The Skill informs the agent that the server is persistent, eliminating redundant attach/reconnect cycles and enabling sub-second tool execution.

---

## 2. Industrial Prompt Engineering: Maximizing Tool Strength

Generic software prompts (e.g., *"Make a screen for the motors"*) produce suboptimal industrial HMIs because they lack deterministic constraints on tags, coordinates, and behavior. 

To maximize tool effectiveness, use the **P.A.C.T.S. Framework**:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               P.A.C.T.S. Framework                              │
├─────────────────┬───────────────────────────────────────────────────────────────┤
│ P - Target Path │ Specific device, CPU, or HMI target (e.g. PC-System_1/HMI_RT_1)│
│ A - Action Type │ Precise operational behavior (Toggle, Momentary, Range Dyn)   │
│ C - Constraints │ Non-destructive edits, layout bounds, coordinate envelopes    │
│ T - Tags & DBs  │ Exact PLC addresses or HMI tags (e.g. HMI_MotorInfeed_Start)  │
│ S - Self-Check  │ Explicit requirement to compile and report error diagnostics  │
└─────────────────┴───────────────────────────────────────────────────────────────┘
```

---

### 2.1 Bad vs. High-Strength Prompt Comparison

#### Scenario 1: Modifying an Existing Screen Button
- ❌ **Poor Prompt:**  
  *"Change the button on screen 2 so it toggles instead of set bit."*  
  *(Risk: The agent might delete the button, create a new one with wrong coordinates, or use an invalid JavaScript function).*
- ✅ **High-Strength Prompt:**  
  ```text
  On Screen_2 of HMI_RT_1, update all motor Start/Stop buttons in-place using hmi_update_button_scripts.
  Do NOT delete or re-create screen elements.
  Replace any Tags.SysFct.SetBitInTag calls with Tags.SysFct.InvertBitInTag("...", 0).
  After updating, compile the HMI target and verify State == Success with 0 errors.
  ```

#### Scenario 2: Adding a Motor Detail Indicator Card
- ❌ **Poor Prompt:**  
  *"Put an indicator for motor 101 on the screen."*
- ✅ **High-Strength Prompt:**  
  ```text
  On HMI_RT_1, Screen_2:
  1. Create a background card rectangle 'rect_M101' at left: 40, top: 120, width: 340, height: 260 with back_color: '#1E293B', border_color: '#334155'.
  2. Add a title label 'lbl_M101_Title' with text 'M101 - INFEED CONVEYOR', font_size: 14, is_bold: true, fore_color: 'white'.
  3. Add a single status circle 'circ_M101_Status' at center_x: 100, center_y: 200, radius: 22.
  4. Dynamize circ_M101_Status.BackColor using single-object range dynamization bound to tag 'HMI_MotorInfeed_RunFb' (from 0 to 0 = '#64748B' gray, from 1 to 1 = '#22C55E' green). Do not create overlapping objects.
  5. Add an InvertBit button 'btn_M101_Toggle' at left: 160, top: 185, width: 100, height: 34 calling Tags.SysFct.InvertBitInTag("HMI_MotorInfeed_Start", 0).
  6. Compile the HMI and confirm 0 errors.
  ```

---

### 2.2 Reusable Prompt Templates

#### Template 1: Non-Destructive Script Refactoring
```text
In [Target_HMI], screen '[Screen_Name]':
- Inspect existing screen items using hmi_get_screen_items.
- Update the button scripts matching find_text: "[Old_Function]" to replace_text: "[New_Function]".
- Use in-place updating without deleting or changing coordinates of existing widgets.
- Compile [Target_HMI] and report compilation state and error counts.
```

#### Template 2: New Subsystem Generation
```text
Generate a detail monitoring card for [Subsystem_Name] on [Screen_Name]:
- Bounds: Left [X], Top [Y], Width [W], Height [H].
- Bound Tags: Status feedback tag '[Feedback_Tag]', Command tag '[Cmd_Tag]'.
- Indicator Rule: Use single-object BackColor range dynamization (0=gray, 1=green, 2=red).
- Controls: Toggle button calling Tags.SysFct.InvertBitInTag("[Cmd_Tag]", 0).
- Run hmi_compile upon completion to verify syntax check.
```

---

## 3. Complete WinCC Unified System Functions Catalog

Siemens SIMATIC WinCC Unified Runtime provides a rich suite of built-in JavaScript system functions accessible under the global namespaces `Tags.SysFct.*` and `HMIRuntime.*.SysFct.*`.

These functions can be executed directly inside button event handlers (e.g. `Tapped`), screen events (e.g. `Loaded`), or scheduled tasks.

---

### 3.1 Tag System Functions (`Tags.SysFct.*`)

These functions operate directly on process tags and trigger automatic synchronization between the HMI Runtime and the PLC.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`InvertBitInTag`** | `(TagName, BitNumber)` | Inverts (toggles) a specific bit in a tag and writes the new value to the PLC. Ideal for Start/Stop toggle buttons. | `Tags.SysFct.InvertBitInTag("HMI_MotorInfeed_Start", 0);` |
| **`SetBitInTag`** | `(TagName, BitNumber)` | Sets a specific bit to `1` (true). Used for momentary latching or set commands. | `Tags.SysFct.SetBitInTag("HMI_Cmd_StartBatch", 0);` |
| **`ResetBitInTag`** | `(TagName, BitNumber)` | Resets a specific bit to `0` (false). Used for fault resets and stop commands. | `Tags.SysFct.ResetBitInTag("HMI_Cmd_StartBatch", 0);` |
| **`SetTagValue`** | `(TagName, Value)` | Writes an explicit value (numeric, string, or boolean) to an HMI tag. | `Tags.SysFct.SetTagValue("HMI_Setpoint_Speed", 1500);` |
| **`IncreaseTag`** | `(TagName, Value)` | Increments the numeric value of a tag by the specified step amount. | `Tags.SysFct.IncreaseTag("HMI_BatchCounter", 1);` |
| **`DecreaseTag`** | `(TagName, Value)` | Decrements the numeric value of a tag by the specified step amount. | `Tags.SysFct.DecreaseTag("HMI_BatchCounter", 1);` |
| **`LinearScaling`** | `(SourceTag, SMin, SMax, TargetTag, TMin, TMax)` | Performs linear scaling calculation between two tag ranges and stores the result. | `Tags.SysFct.LinearScaling("RawADC", 0, 27648, "SpeedRPM", 0, 1800);` |
| **`ShiftAndMask`** | `(Tag, Shift, Mask, TargetTag)` | Performs bitwise shift and bitwise AND masking operations for packing/unpacking words. | `Tags.SysFct.ShiftAndMask("RawStatusWord", 4, 0x0F, "MotorState");` |
| **`UpdateTag`** | `(UpdateID)` | Triggers a one-shot read request for tags mapped to a specific cyclic update cycle ID. | `Tags.SysFct.UpdateTag(1);` |

---

### 3.2 Screen Navigation & Window Control (`HMIRuntime.UI.SysFct.*`)

Controls viewport navigation, screen windows, popups, and zooming.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`ChangeScreen`** | `(ScreenName, ScreenWindowPath)` | Changes the screen displayed in the specified screen window. **Note:** Use `"~"` to refer to the current window. | `HMIRuntime.UI.SysFct.ChangeScreen("Screen_2", "~");` |
| **`ChangeScreenAsync`**| `(ScreenName, ScreenWindowPath)` | Asynchronously changes the screen without blocking script thread execution. | `HMIRuntime.UI.SysFct.ChangeScreenAsync("Screen_1", "~");` |
| **`ChangeScreenAsyncByNumber`** | `(ScreenNumber, ScreenWindowPath)` | Navigates to a screen by its configured numeric index. | `HMIRuntime.UI.SysFct.ChangeScreenAsyncByNumber(2, "~");` |
| **`OpenScreenInPopup`** | `(PopupName, ScreenName, HeaderText, Left, Top, ShowHeader, Flags)` | Displays a screen inside a floating modal or non-modal popup window. | `HMIRuntime.UI.SysFct.OpenScreenInPopup("MotorPop", "Screen_MotorDetail", "Motor M101", 300, 200, true);` |
| **`ClosePopup`** | `(PopupPath)` | Closes an active popup window by path name. | `HMIRuntime.UI.SysFct.ClosePopup("MotorPop");` |
| **`ZoomIn`** | `(ScreenWindowPath, Factor)` | Zooms into the target screen window by a scaling factor. | `HMIRuntime.UI.SysFct.ZoomIn("~", 1.25);` |
| **`ZoomOut`** | `(ScreenWindowPath, Factor)` | Zooms out of the target screen window. | `HMIRuntime.UI.SysFct.ZoomOut("~", 0.8);` |
| **`ResetZoom`** | `(ScreenWindowPath)` | Resets the zoom level of the screen window to 100% (1.0). | `HMIRuntime.UI.SysFct.ResetZoom("~");` |

---

### 3.3 Screen Item Properties & Interaction (`HMIRuntime.UI.SysFct.*`)

Dynamically inspects and manipulates widgets on the active screen.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`SetPropertyValue`** | `(ItemPath, PropertyName, Value)` | Programmatically assigns a new value to any valid property of an element. | `HMIRuntime.UI.SysFct.SetPropertyValue("rect_Card", "BackColor", "#1E293B");` |
| **`GetPropertyValue`** | `(ItemPath, PropertyName)` | Reads the current runtime value of an element's property. | `let col = HMIRuntime.UI.SysFct.GetPropertyValue("btn_Start", "BackColor");` |
| **`SetFocusOnElement`**| `(ItemName, ScreenWindowPath)` | Directs keyboard and mouse focus to a specific input field or button. | `HMIRuntime.UI.SysFct.SetFocusOnElement("io_Setpoint", "~");` |

---

### 3.4 Alarms & Event Functions (`HMIRuntime.Alarming.SysFct.*`)

Manages alarm acknowledgments, alarm states, and historical alarm exports.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`AcknowledgeAlarm`** | `(AlarmID)` | Acknowledges an active incoming alarm message. | `HMIRuntime.Alarming.SysFct.AcknowledgeAlarm(1001);` |
| **`ResetAlarm`** | `(AlarmID)` | Resets an acknowledged alarm that has returned to normal state. | `HMIRuntime.Alarming.SysFct.ResetAlarm(1001);` |
| **`ExportAlarmLog`** | `(LogName, FilePath, TimeRange, Format)` | Exports archived alarm history to a CSV or TXT file on disk. | `HMIRuntime.Alarming.SysFct.ExportAlarmLog("AlarmLog1", "/media/logs/alarms.csv", 1, 0);` |
| **`RestoreAlarmLog`**| `(LogName, FilePath)` | Restores an archived alarm backup database into runtime. | `HMIRuntime.Alarming.SysFct.RestoreAlarmLog("AlarmLog1", "/media/backup.db");` |

---

### 3.5 Historical Logging & Process Data (`HMIRuntime.Logging.SysFct.*`)

Controls historical data acquisition and file backup exports.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`StartTagLogging`** | `(LogName)` | Resumes historical data acquisition for the specified tag log. | `HMIRuntime.Logging.SysFct.StartTagLogging("ProcessDataLog");` |
| **`StopTagLogging`** | `(LogName)` | Pauses historical data acquisition for maintenance or file locking. | `HMIRuntime.Logging.SysFct.StopTagLogging("ProcessDataLog");` |
| **`ExportTagLog`** | `(LogName, FilePath, TimeRange, Format)` | Exports archived process tag values to CSV format. | `HMIRuntime.Logging.SysFct.ExportTagLog("SpeedLog", "/logs/speed.csv", 0, 0);` |
| **`RestoreTagLog`** | `(LogName, FilePath)` | Restores a segment of historical tag logs from an external backup. | `HMIRuntime.Logging.SysFct.RestoreTagLog("SpeedLog", "/backup/speed.db");` |
| **`WriteManualValue`** | `(LogName, TagName, Timestamp, Value)` | Manually inserts an audit record or calibrated value into the historical log. | `HMIRuntime.Logging.SysFct.WriteManualValue("LabLog", "Moisture", new Date(), 12.4);` |

---

### 3.6 User Management & Security (`HMIRuntime.UserManagement.SysFct.*`)

Controls operator authentication, user rights, and session security.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`LogOff`** | `()` | Immediately logs out the currently logged-in user and returns to default rights. | `HMIRuntime.UserManagement.SysFct.LogOff();` |
| **`ShowLogonDialog`** | `()` | Prompts the native WinCC Unified operator login dialog on screen. | `HMIRuntime.UserManagement.SysFct.ShowLogonDialog();` |
| **`ChangePassword`** | `(UserName, OldPassword, NewPassword)` | Programmatically updates the password for a registered user account. | `HMIRuntime.UserManagement.SysFct.ChangePassword("Operator1", "old#1", "new#2");` |

---

### 3.7 System Programs, Diagnostics & Connections (`HMIRuntime.*`)

Interacts with the underlying operating system and hardware communication channels.

| Function | Signature | Description | Example Usage |
| :--- | :--- | :--- | :--- |
| **`StartProgram`** | `(ProgramPath, Arguments)` | Launches an external executable or batch script on the runtime PC host. *(Requires SIMATIC Runtime Manager permission)* | `HMIRuntime.UI.SysFct.StartProgram("notepad.exe", "C:/report.txt");` |
| **`ShowSoftwareVersion`** | `()` | Displays the runtime build and patch version information dialog. | `HMIRuntime.UI.SysFct.ShowSoftwareVersion();` |
| **`ShowControlPanel`** | `()` | Opens the SIMATIC Unified Control Panel on Unified Comfort Panels or PC. | `HMIRuntime.UI.SysFct.ShowControlPanel();` |
| **`ChangeConnection`** | `(ConnName, IP, Slot, Rack)` | Dynamically re-routes an S7 connection to a redundant or standby PLC controller IP. | `HMIRuntime.Connections.SysFct.ChangeConnection("HMI_Connection_1", "192.168.0.10", 1, 0);` |

---

## 4. Summary: How to Ask the Agent for Maximum Quality

When directing your AI coding partner to engineer WinCC Unified HMIs:
1. Specify whether actions should be **momentary** (`SetBitInTag`) or **toggle** (`InvertBitInTag`).
2. Require **single-object range dynamization** for state coloring.
3. Command **in-place updating** (`hmi_update_button_scripts`) to avoid layout disruption.
4. Always conclude tasks with **`hmi_compile`** to verify syntax checks pass with 0 errors.
