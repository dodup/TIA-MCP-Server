---
name: figma-to-wincc-unified
description: Convert Figma visual designs and design tokens into functional Siemens WinCC Unified V21 HMI screens using TIA Openness.
---

# Figma to WinCC Unified HMI Skill

This skill guides the agent in ingesting UI/UX layouts exported from Figma (as SVG, PNG, or JSON) and constructing native, compiled, and tag-bound WinCC Unified screens in Siemens TIA Portal V21.

## When to Use This Skill
- The user provides an SVG or PNG export from Figma and asks to recreate it as a WinCC Unified screen.
- The user requests to apply custom branding, exact color palettes, or card layouts from a UI design tool to an HMI device.
- The user wants to build complex multi-widget HMI screens with custom styling, buttons, indicators, and alarm controls.

---

## Workflow Steps

### 1. Ingest & Inspect Figma Export
- Check for SVG files in `HMI Figma/` or user workspace.
- Parse SVG dimensions, viewBox, and coordinate offsets.
- Extract embedded images (e.g. base64 PNGs) to inspect UI elements, icons, gauges, or buttons.
- Extract design tokens:
  - Background color (e.g. `#000028`)
  - Container/Card colors (e.g. `#0B132B`, `#334155`)
  - Accent / Primary action color (e.g. `#00D7A0` Teal)
  - Emergency / Stop color (e.g. `#DC2626` Red)
  - Neutral / Tab colors (e.g. `#8C8C8C`, `#313030`)

### 2. Map Layout Components to WinCC Unified Types
Map visual elements into their corresponding Openness SDK classes:
- Containers / Panels $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Shapes.HmiRectangle`
- Text Labels / Headers $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Widgets.HmiTextBox`
- Buttons $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Widgets.HmiButton`
- IO Fields / Numeric Displays $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Widgets.HmiIoField`
- Status Indicator Lamps $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Shapes.HmiCircle`
- Alarms View $\rightarrow$ `Siemens.Engineering.HmiUnified.UI.Controls.HmiAlarmControl`

### 3. Openness API Rules & Best Practices
- **Rich Text Wrapper**: Text strings on buttons and textboxes MUST be wrapped in `<body><p>...</p></body>` with XML escaped text (`&`, `<`, `>`, etc.).
- **Button Scripts**: Assign JavaScripts via Openness event interfaces:
  - Navigation: `HMIRuntime.UI.SysFct.ChangeScreen("TargetScreen", "~");`
  - Tag toggle: `Tags.SysFct.InvertBitInTag("TagName");`
  - Tag set: `Tags.SysFct.SetBitInTag("TagName");`
- **Range Dynamizations**: Configure range conditions on `Dynamizations.Create<TagDynamization>("BackColor")`.
- **Compiling**: Always run a compile check on the device or software container and ensure `project.Save()` commits changes.

---

## Reference Layer Naming Convention
When guiding users to structure Figma files:
- `Btn_Nav_[ScreenName]`: Automatic navigation button
- `Btn_Toggle_[TagName]`: Toggle bit button
- `IO_In_[TagName]`: Editable input/output field
- `IO_Out_[TagName]`: Read-only output field
- `Ind_Dyn_[TagName]`: Pilot circle indicator with range dynamization
- `Card_[Name]`: Container rectangle
- `Ctrl_Alarm`: Embedded HmiAlarmControl
