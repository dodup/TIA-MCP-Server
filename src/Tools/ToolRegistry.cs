using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaOpennessMcp.Protocol;
using TiaOpennessMcp.Tia;

namespace TiaOpennessMcp.Tools
{
    public class ToolRegistry
    {
        private readonly List<ToolDefinition> _tools = new List<ToolDefinition>();
        private readonly Dictionary<string, Func<JsonElement?, CallToolResult>> _handlers =
            new Dictionary<string, Func<JsonElement?, CallToolResult>>(StringComparer.OrdinalIgnoreCase);

        public ToolRegistry()
        {
            RegisterAllTools();
        }

        public List<ToolDefinition> GetTools() => _tools;

        public CallToolResult Execute(string toolName, JsonElement? args)
        {
            if (_handlers.TryGetValue(toolName, out var handler))
            {
                try
                {
                    return handler(args);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ToolRegistry] Error executing '{toolName}': {ex}");
                    return CallToolResult.Error($"Error executing '{toolName}': {ex.Message}");
                }
            }
            return CallToolResult.Error($"Unknown tool: '{toolName}'");
        }

        private void RegisterAllTools()
        {
            // 1. tia_list_processes
            Register("tia_list_processes", "List all running Siemens TIA Portal instances on the machine with PID, open project path, and mode.",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.ListProcesses()));

            // 2. tia_connect
            Register("tia_connect", "Connect/attach to a running TIA Portal instance. If process_id is omitted, connects to the first running instance.",
                schema =>
                {
                    schema.Properties["process_id"] = new ToolProperty
                    {
                        Type = "integer",
                        Description = "Process ID (PID) of the TIA Portal instance to attach to (optional)."
                    };
                },
                args =>
                {
                    int? pid = GetIntProp(args, "process_id");
                    var res = TiaManager.Instance.Connect(pid);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 3. tia_disconnect
            Register("tia_disconnect", "Disconnect / detach from the active TIA Portal instance.",
                schema => { },
                args =>
                {
                    TiaManager.Instance.Disconnect();
                    return CallToolResult.Text("Disconnected from TIA Portal.");
                });

            // 4. tia_get_project_info
            Register("tia_get_project_info", "Get detailed metadata for the currently open TIA Portal project (name, path, author, comments, timestamps).",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.GetProjectInfo()));

            // 5. tia_open_project
            Register("tia_open_project", "Open a TIA project file (.ap21, .ap20) from disk in a new TIA Portal instance.",
                schema =>
                {
                    schema.Properties["project_path"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Full path to the project file (e.g. C:\\Projects\\MyProj.ap21)."
                    };
                    schema.Properties["with_gui"] = new ToolProperty
                    {
                        Type = "boolean",
                        Description = "Whether to start TIA Portal with user interface (true) or headless (false). Default: true.",
                        Default = true
                    };
                    schema.Required = new List<string> { "project_path" };
                },
                args =>
                {
                    string path = GetStringProp(args, "project_path") ?? throw new ArgumentException("project_path is required.");
                    bool withGui = GetBoolProp(args, "with_gui", true);
                    var res = TiaManager.Instance.OpenProject(path, withGui);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 6. tia_save_project
            Register("tia_save_project", "Save changes to the active TIA Portal project.",
                schema => { },
                args =>
                {
                    TiaManager.Instance.SaveProject();
                    return CallToolResult.Text("Project successfully saved.");
                });

            // 7. tia_close_project
            Register("tia_close_project", "Close the active TIA Portal project.",
                schema => { },
                args =>
                {
                    TiaManager.Instance.CloseProject();
                    return CallToolResult.Text("Project successfully closed.");
                });

            // 8. tia_list_devices
            Register("tia_list_devices", "List all devices in the project (PLCs, HMIs, Drives, distributed I/O) with their type identifiers and group folders.",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.ListDevices()));

            // 9. tia_get_device_tree
            Register("tia_get_device_tree", "Get hierarchical hardware tree for a device (racks, slots, CPU, modules, subnets, interfaces).",
                schema =>
                {
                    schema.Properties["device_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the device to inspect."
                    };
                    schema.Required = new List<string> { "device_name" };
                },
                args =>
                {
                    string name = GetStringProp(args, "device_name") ?? throw new ArgumentException("device_name is required.");
                    return CallToolResult.Json(TiaManager.Instance.GetDeviceTree(name));
                });

            // 10. tia_list_plcs
            Register("tia_list_plcs", "List all PLCs detected in the project with software summaries (block counts, UDT counts, tag tables).",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.ListAllPlcs()));

            // 11. tia_list_blocks
            Register("tia_list_blocks", "List PLC software program blocks (OB, FB, FC, DB) with language, number, consistency, and group folder.",
                schema =>
                {
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Properties["block_type"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Filter by block type: OB, FB, FC, GlobalDB, or InstanceDB (optional)."
                    };
                    schema.Properties["folder"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Filter by folder path (optional)."
                    };
                },
                args =>
                {
                    string? plc = GetStringProp(args, "plc_name");
                    string? btype = GetStringProp(args, "block_type");
                    string? folder = GetStringProp(args, "folder");
                    return CallToolResult.Json(TiaManager.Instance.ListBlocks(plc, btype, folder));
                });

            // 12. tia_get_block_code
            Register("tia_get_block_code", "Export and read the SimaticML XML and SCL source code for a specified PLC block.",
                schema =>
                {
                    schema.Properties["block_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the block to export (e.g. 'Main', 'PumpControl', 'DB_Config')."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Required = new List<string> { "block_name" };
                },
                args =>
                {
                    string block = GetStringProp(args, "block_name") ?? throw new ArgumentException("block_name is required.");
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Json(TiaManager.Instance.GetBlockCode(plc, block));
                });

            // 13. tia_import_block
            Register("tia_import_block", "Import or update a PLC block from SimaticML XML.",
                schema =>
                {
                    schema.Properties["xml_content"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "The SimaticML XML string containing the block definition."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Properties["overwrite"] = new ToolProperty
                    {
                        Type = "boolean",
                        Description = "Whether to overwrite existing block if it exists (default true).",
                        Default = true
                    };
                    schema.Properties["folder"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Target group folder path for the block (optional)."
                    };
                    schema.Required = new List<string> { "xml_content" };
                },
                args =>
                {
                    string xml = GetStringProp(args, "xml_content") ?? throw new ArgumentException("xml_content is required.");
                    string? plc = GetStringProp(args, "plc_name");
                    bool overwrite = GetBoolProp(args, "overwrite", true);
                    string? folder = GetStringProp(args, "folder");
                    string msg = TiaManager.Instance.ImportBlock(plc, xml, overwrite, folder);
                    return CallToolResult.Text(msg);
                });

            // 14. tia_delete_block
            Register("tia_delete_block", "Delete a PLC program block from the project.",
                schema =>
                {
                    schema.Properties["block_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the block to delete."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Required = new List<string> { "block_name" };
                },
                args =>
                {
                    string block = GetStringProp(args, "block_name") ?? throw new ArgumentException("block_name is required.");
                    string? plc = GetStringProp(args, "plc_name");
                    TiaManager.Instance.DeleteBlock(plc, block);
                    return CallToolResult.Text($"Block '{block}' deleted successfully.");
                });

            // 15. tia_list_plc_types
            Register("tia_list_plc_types", "List PLC user data types (UDTs) with consistency state and folder path.",
                schema =>
                {
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                },
                args =>
                {
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Json(TiaManager.Instance.ListPlcTypes(plc));
                });

            // 16. tia_get_plc_type
            Register("tia_get_plc_type", "Export and read the SimaticML XML definition of a PLC User Data Type (UDT).",
                schema =>
                {
                    schema.Properties["type_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the PLC User Data Type (UDT)."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Required = new List<string> { "type_name" };
                },
                args =>
                {
                    string type = GetStringProp(args, "type_name") ?? throw new ArgumentException("type_name is required.");
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Text(TiaManager.Instance.GetPlcType(plc, type));
                });

            // 17. tia_import_plc_type
            Register("tia_import_plc_type", "Import or update a PLC User Data Type (UDT) from SimaticML XML.",
                schema =>
                {
                    schema.Properties["xml_content"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "The SimaticML XML string containing the UDT definition."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                    schema.Properties["overwrite"] = new ToolProperty
                    {
                        Type = "boolean",
                        Description = "Whether to overwrite existing UDT (default true).",
                        Default = true
                    };
                    schema.Required = new List<string> { "xml_content" };
                },
                args =>
                {
                    string xml = GetStringProp(args, "xml_content") ?? throw new ArgumentException("xml_content is required.");
                    string? plc = GetStringProp(args, "plc_name");
                    bool overwrite = GetBoolProp(args, "overwrite", true);
                    string msg = TiaManager.Instance.ImportPlcType(plc, xml, overwrite);
                    return CallToolResult.Text(msg);
                });

            // 18. tia_list_tag_tables
            Register("tia_list_tag_tables", "List all PLC tag tables and folder groups.",
                schema =>
                {
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                },
                args =>
                {
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Json(TiaManager.Instance.ListTagTables(plc));
                });

            // 19. tia_get_tags
            Register("tia_get_tags", "Read tags from a tag table (or all tag tables) with data types, logical addresses (%I, %Q, %M), and comments.",
                schema =>
                {
                    schema.Properties["table_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of specific tag table (optional, defaults to all tables)."
                    };
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC (optional, defaults to first PLC)."
                    };
                },
                args =>
                {
                    string? table = GetStringProp(args, "table_name");
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Json(TiaManager.Instance.GetTags(plc, table));
                });

            // 20. tia_compile
            Register("tia_compile", "Compile PLC software to check syntax, consistency, and generate machine code. Returns errors, warnings, and messages.",
                schema =>
                {
                    schema.Properties["plc_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the target PLC to compile (optional, defaults to first PLC)."
                    };
                },
                args =>
                {
                    string? plc = GetStringProp(args, "plc_name");
                    return CallToolResult.Json(TiaManager.Instance.Compile(plc));
                });

            // 21. tia_get_status
            Register("tia_get_status", "Get the current TIA Openness connection status, attached process ID, open project name and path.",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.GetStatus()));

            // 22. hmi_list_targets
            Register("hmi_list_targets", "List all HMI targets (WinCC Unified runtime devices/software) in the active project.",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.ListHmiTargets()));

            // 23. hmi_list_screens
            Register("hmi_list_screens", "List all screens in the specified HMI software target with dimensions and element counts.",
                schema =>
                {
                    schema.Properties["target_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the HMI device or software target (optional, defaults to first found)."
                    };
                },
                args =>
                {
                    string? target = GetStringProp(args, "target_name");
                    return CallToolResult.Json(TiaManager.Instance.ListHmiScreens(target));
                });

            // 24. hmi_create_screen
            Register("hmi_create_screen", "Create or ensure existence of an HMI screen with specified dimensions.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the screen to create (e.g. Screen_1, Screen_2)."
                    };
                    schema.Properties["width"] = new ToolProperty
                    {
                        Type = "integer",
                        Description = "Width of the screen in pixels (optional)."
                    };
                    schema.Properties["height"] = new ToolProperty
                    {
                        Type = "integer",
                        Description = "Height of the screen in pixels (optional)."
                    };
                    schema.Properties["target_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Target HMI software name (optional)."
                    };
                    schema.Required = new List<string> { "screen_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name is required.");
                    uint? width = GetUIntProp(args, "width");
                    uint? height = GetUIntProp(args, "height");
                    string? target = GetStringProp(args, "target_name");
                    return CallToolResult.Json(TiaManager.Instance.CreateHmiScreen(screen, width, height, target));
                });

            // 25. hmi_delete_screen
            Register("hmi_delete_screen", "Delete an HMI screen from the HMI software target.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the screen to delete."
                    };
                    schema.Properties["target_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Target HMI software name (optional)."
                    };
                    schema.Required = new List<string> { "screen_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name is required.");
                    string? target = GetStringProp(args, "target_name");
                    TiaManager.Instance.DeleteHmiScreen(screen, target);
                    return CallToolResult.Text($"Screen '{screen}' deleted successfully.");
                });

            // 26. hmi_get_screen_items
            Register("hmi_get_screen_items", "Get all screen items (buttons, textboxes, shapes, IO fields) on a screen with positions, colors, scripts, and dynamizations.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the screen to inspect."
                    };
                    schema.Properties["target_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Target HMI software name (optional)."
                    };
                    schema.Required = new List<string> { "screen_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name is required.");
                    string? target = GetStringProp(args, "target_name");
                    return CallToolResult.Json(TiaManager.Instance.GetHmiScreenItems(screen, target));
                });

            // 27. hmi_delete_screen_items
            Register("hmi_delete_screen_items", "Delete specific screen items by name list or by name prefix (e.g. 'btn_', 'lbl_').",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Name of the screen containing the items."
                    };
                    schema.Properties["item_names"] = new ToolProperty
                    {
                        Type = "array",
                        Description = "List of item names to delete."
                    };
                    schema.Properties["prefix"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Prefix of item names to delete (e.g. 'M101_')."
                    };
                    schema.Properties["target_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Target HMI software name (optional)."
                    };
                    schema.Required = new List<string> { "screen_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name is required.");
                    var items = GetStringListProp(args, "item_names");
                    string? prefix = GetStringProp(args, "prefix");
                    string? target = GetStringProp(args, "target_name");
                    int deleted = TiaManager.Instance.DeleteHmiScreenItems(screen, items, prefix, target);
                    return CallToolResult.Text($"Deleted {deleted} screen item(s) from '{screen}'.");
                });

            // 28. hmi_add_label
            Register("hmi_add_label", "Add or update a label / text box on a screen with text, position, font size, bold, alignment, and colors.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["name"] = new ToolProperty { Type = "string", Description = "Item name." };
                    schema.Properties["text"] = new ToolProperty { Type = "string", Description = "Display text." };
                    schema.Properties["left"] = new ToolProperty { Type = "integer", Description = "X position." };
                    schema.Properties["top"] = new ToolProperty { Type = "integer", Description = "Y position." };
                    schema.Properties["width"] = new ToolProperty { Type = "integer", Description = "Width." };
                    schema.Properties["height"] = new ToolProperty { Type = "integer", Description = "Height." };
                    schema.Properties["font_size"] = new ToolProperty { Type = "number", Description = "Font size in pt (default: 11)." };
                    schema.Properties["is_bold"] = new ToolProperty { Type = "boolean", Description = "Bold text." };
                    schema.Properties["align"] = new ToolProperty { Type = "string", Description = "Horizontal alignment ('Left', 'Center', 'Right')." };
                    schema.Properties["fore_color"] = new ToolProperty { Type = "string", Description = "Text color ('#FFFFFF', 'white', etc.)." };
                    schema.Properties["back_color"] = new ToolProperty { Type = "string", Description = "Background color ('transparent', '#000000', etc.)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name", "name", "text", "left", "top", "width", "height" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string name = GetStringProp(args, "name") ?? throw new ArgumentException("name required.");
                    string text = GetStringProp(args, "text") ?? throw new ArgumentException("text required.");
                    int left = GetIntProp(args, "left") ?? 0;
                    int top = GetIntProp(args, "top") ?? 0;
                    uint width = GetUIntProp(args, "width") ?? 100;
                    uint height = GetUIntProp(args, "height") ?? 30;
                    float fontSize = GetFloatProp(args, "font_size", 11f);
                    bool isBold = GetBoolProp(args, "is_bold", false);
                    string align = GetStringProp(args, "align") ?? "Center";
                    string? foreColor = GetStringProp(args, "fore_color");
                    string? backColor = GetStringProp(args, "back_color");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.AddOrUpdateLabel(screen, name, text, left, top, width, height, fontSize, isBold, align, foreColor, backColor, target);
                    return CallToolResult.Text(res);
                });

            // 29. hmi_add_button
            Register("hmi_add_button", "Add or update a button on a screen with text, position, colors, pressed state tag, and OnClick/Tapped JavaScript script.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["name"] = new ToolProperty { Type = "string", Description = "Button item name." };
                    schema.Properties["text"] = new ToolProperty { Type = "string", Description = "Button caption." };
                    schema.Properties["left"] = new ToolProperty { Type = "integer", Description = "X position." };
                    schema.Properties["top"] = new ToolProperty { Type = "integer", Description = "Y position." };
                    schema.Properties["width"] = new ToolProperty { Type = "integer", Description = "Width." };
                    schema.Properties["height"] = new ToolProperty { Type = "integer", Description = "Height." };
                    schema.Properties["back_color"] = new ToolProperty { Type = "string", Description = "Background color." };
                    schema.Properties["fore_color"] = new ToolProperty { Type = "string", Description = "Text color." };
                    schema.Properties["pressed_tag"] = new ToolProperty { Type = "string", Description = "HMI tag to bind to button pressed state (optional)." };
                    schema.Properties["tapped_script"] = new ToolProperty { Type = "string", Description = "JavaScript code to execute on button tap (e.g. Tags.SysFct.InvertBitInTag(\"tag\", 0);)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name", "name", "text", "left", "top", "width", "height" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string name = GetStringProp(args, "name") ?? throw new ArgumentException("name required.");
                    string text = GetStringProp(args, "text") ?? throw new ArgumentException("text required.");
                    int left = GetIntProp(args, "left") ?? 0;
                    int top = GetIntProp(args, "top") ?? 0;
                    uint width = GetUIntProp(args, "width") ?? 100;
                    uint height = GetUIntProp(args, "height") ?? 30;
                    string? backColor = GetStringProp(args, "back_color");
                    string? foreColor = GetStringProp(args, "fore_color");
                    string? pressedTag = GetStringProp(args, "pressed_tag");
                    string? tappedScript = GetStringProp(args, "tapped_script");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.AddOrUpdateButton(screen, name, text, left, top, width, height, backColor, foreColor, pressedTag, tappedScript, target);
                    return CallToolResult.Json(res);
                });

            // 30. hmi_add_shape
            Register("hmi_add_shape", "Add or update a shape ('Rectangle' or 'Circle') with dimensions, center/radius, fill and border colors.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["name"] = new ToolProperty { Type = "string", Description = "Shape item name." };
                    schema.Properties["shape_type"] = new ToolProperty { Type = "string", Description = "'Rectangle' or 'Circle'." };
                    schema.Properties["left"] = new ToolProperty { Type = "integer", Description = "X position (for Rectangle)." };
                    schema.Properties["top"] = new ToolProperty { Type = "integer", Description = "Y position (for Rectangle)." };
                    schema.Properties["width"] = new ToolProperty { Type = "integer", Description = "Width (for Rectangle)." };
                    schema.Properties["height"] = new ToolProperty { Type = "integer", Description = "Height (for Rectangle)." };
                    schema.Properties["center_x"] = new ToolProperty { Type = "integer", Description = "Center X (for Circle)." };
                    schema.Properties["center_y"] = new ToolProperty { Type = "integer", Description = "Center Y (for Circle)." };
                    schema.Properties["radius"] = new ToolProperty { Type = "integer", Description = "Radius (for Circle)." };
                    schema.Properties["back_color"] = new ToolProperty { Type = "string", Description = "Fill / background color." };
                    schema.Properties["border_color"] = new ToolProperty { Type = "string", Description = "Border color." };
                    schema.Properties["border_width"] = new ToolProperty { Type = "integer", Description = "Border width in px (default: 1)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name", "name", "shape_type" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string name = GetStringProp(args, "name") ?? throw new ArgumentException("name required.");
                    string shapeType = GetStringProp(args, "shape_type") ?? throw new ArgumentException("shape_type required.");
                    int? left = GetIntProp(args, "left");
                    int? top = GetIntProp(args, "top");
                    uint? width = GetUIntProp(args, "width");
                    uint? height = GetUIntProp(args, "height");
                    int? centerX = GetIntProp(args, "center_x");
                    int? centerY = GetIntProp(args, "center_y");
                    uint? radius = GetUIntProp(args, "radius");
                    string? backColor = GetStringProp(args, "back_color");
                    string? borderColor = GetStringProp(args, "border_color");
                    uint borderWidth = GetUIntProp(args, "border_width") ?? 1;
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.AddOrUpdateShape(screen, name, shapeType, left, top, width, height, centerX, centerY, radius, backColor, borderColor, borderWidth, target);
                    return CallToolResult.Text(res);
                });

            // 31. hmi_add_io_field
            Register("hmi_add_io_field", "Add or update an IO field on a screen bound to a process value tag.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["name"] = new ToolProperty { Type = "string", Description = "IO Field item name." };
                    schema.Properties["left"] = new ToolProperty { Type = "integer", Description = "X position." };
                    schema.Properties["top"] = new ToolProperty { Type = "integer", Description = "Y position." };
                    schema.Properties["width"] = new ToolProperty { Type = "integer", Description = "Width." };
                    schema.Properties["height"] = new ToolProperty { Type = "integer", Description = "Height." };
                    schema.Properties["mode"] = new ToolProperty { Type = "string", Description = "'Output' or 'InputOutput' (default: Output)." };
                    schema.Properties["process_tag"] = new ToolProperty { Type = "string", Description = "HMI tag to bind to ProcessValue." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name", "name", "left", "top", "width", "height" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string name = GetStringProp(args, "name") ?? throw new ArgumentException("name required.");
                    int left = GetIntProp(args, "left") ?? 0;
                    int top = GetIntProp(args, "top") ?? 0;
                    uint width = GetUIntProp(args, "width") ?? 80;
                    uint height = GetUIntProp(args, "height") ?? 24;
                    string mode = GetStringProp(args, "mode") ?? "Output";
                    string? processTag = GetStringProp(args, "process_tag");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.AddOrUpdateIOField(screen, name, left, top, width, height, mode, processTag, target);
                    return CallToolResult.Text(res);
                });

            // 32. hmi_set_tag_dynamization
            Register("hmi_set_tag_dynamization", "Set tag dynamization on a screen item property (e.g. BackColor range dynamization linked to a tag with range entries).",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["item_name"] = new ToolProperty { Type = "string", Description = "Item name." };
                    schema.Properties["property_name"] = new ToolProperty { Type = "string", Description = "Property to dynamize (e.g. 'BackColor', 'Visibility')." };
                    schema.Properties["tag_name"] = new ToolProperty { Type = "string", Description = "HMI tag name to evaluate." };
                    schema.Properties["condition_type"] = new ToolProperty { Type = "string", Description = "'None' or 'Range'." };
                    schema.Properties["ranges"] = new ToolProperty { Type = "array", Description = "List of ranges: [{from: 0, to: 0, value: 'gray'}, {from: 1, to: 1, value: 'green'}]." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name", "item_name", "property_name", "tag_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string item = GetStringProp(args, "item_name") ?? throw new ArgumentException("item_name required.");
                    string prop = GetStringProp(args, "property_name") ?? throw new ArgumentException("property_name required.");
                    string tag = GetStringProp(args, "tag_name") ?? throw new ArgumentException("tag_name required.");
                    string condition = GetStringProp(args, "condition_type") ?? "None";
                    var ranges = GetRangesProp(args, "ranges");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.SetTagDynamization(screen, item, prop, tag, condition, ranges, target);
                    return CallToolResult.Text(res);
                });

            // 33. hmi_update_button_scripts
            Register("hmi_update_button_scripts", "Update button scripts in place on a screen matching text search/replace or list of button names without deleting items.",
                schema =>
                {
                    schema.Properties["screen_name"] = new ToolProperty { Type = "string", Description = "Screen name." };
                    schema.Properties["find_text"] = new ToolProperty { Type = "string", Description = "Substring to find in button scripts (e.g. 'SetBitInTag')." };
                    schema.Properties["replace_text"] = new ToolProperty { Type = "string", Description = "Replacement string (e.g. 'InvertBitInTag')." };
                    schema.Properties["new_script"] = new ToolProperty { Type = "string", Description = "Exact new script code to set unconditionally (optional)." };
                    schema.Properties["button_names"] = new ToolProperty { Type = "array", Description = "Filter by specific button names (optional)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "screen_name" };
                },
                args =>
                {
                    string screen = GetStringProp(args, "screen_name") ?? throw new ArgumentException("screen_name required.");
                    string? find = GetStringProp(args, "find_text");
                    string? rep = GetStringProp(args, "replace_text");
                    string? newScript = GetStringProp(args, "new_script");
                    var btnNames = GetStringListProp(args, "button_names");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.UpdateHmiButtonScripts(screen, find, rep, newScript, btnNames, target);
                    return CallToolResult.Json(res);
                });

            // 34. hmi_list_tag_tables
            Register("hmi_list_tag_tables", "List all HMI tag tables in the HMI software target.",
                schema =>
                {
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                },
                args =>
                {
                    string? target = GetStringProp(args, "target_name");
                    return CallToolResult.Json(TiaManager.Instance.ListHmiTagTables(target));
                });

            // 35. hmi_list_tags
            Register("hmi_list_tags", "List HMI tags with their table, connection, PLC tag binding, and data type.",
                schema =>
                {
                    schema.Properties["table_name"] = new ToolProperty { Type = "string", Description = "Tag table name (optional, all tables if omitted)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                },
                args =>
                {
                    string? table = GetStringProp(args, "table_name");
                    string? target = GetStringProp(args, "target_name");
                    return CallToolResult.Json(TiaManager.Instance.ListHmiTags(table, target));
                });

            // 36. hmi_create_tag
            Register("hmi_create_tag", "Create or update an HMI tag linked to a PLC tag with connection name and table.",
                schema =>
                {
                    schema.Properties["tag_name"] = new ToolProperty { Type = "string", Description = "HMI Tag name." };
                    schema.Properties["plc_tag"] = new ToolProperty { Type = "string", Description = "PLC tag path (e.g. 'DB_MixingLine.Line.MotorInfeed.RunFb')." };
                    schema.Properties["connection"] = new ToolProperty { Type = "string", Description = "HMI connection name (default: 'HMI_Connection_1')." };
                    schema.Properties["table_name"] = new ToolProperty { Type = "string", Description = "Tag table name (default: 'Default tag table')." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "tag_name", "plc_tag" };
                },
                args =>
                {
                    string tag = GetStringProp(args, "tag_name") ?? throw new ArgumentException("tag_name required.");
                    string plcTag = GetStringProp(args, "plc_tag") ?? throw new ArgumentException("plc_tag required.");
                    string conn = GetStringProp(args, "connection") ?? "HMI_Connection_1";
                    string? table = GetStringProp(args, "table_name");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.CreateOrUpdateHmiTag(tag, plcTag, conn, table, target);
                    return CallToolResult.Text(res);
                });

            // 37. hmi_update_tag_acquisition_cycles
            Register("hmi_update_tag_acquisition_cycles", "Update acquisition cycle (e.g. 'T100ms', 'T1s') for HMI tags.",
                schema =>
                {
                    schema.Properties["cycle"] = new ToolProperty { Type = "string", Description = "Target acquisition cycle name or interval (e.g. 'T100ms', 'T1s')." };
                    schema.Properties["plc_only"] = new ToolProperty { Type = "boolean", Description = "Only update PLC-connected tags (default: true)." };
                    schema.Properties["table_name"] = new ToolProperty { Type = "string", Description = "Tag table name (optional, all tables if omitted)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "cycle" };
                },
                args =>
                {
                    string cycle = GetStringProp(args, "cycle") ?? throw new ArgumentException("cycle required.");
                    bool plcOnly = GetBoolProp(args, "plc_only", true);
                    string? table = GetStringProp(args, "table_name");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.UpdateHmiTagAcquisitionCycles(cycle, plcOnly, table, target);
                    return CallToolResult.Json(res);
                });

            // 38. hmi_set_tag_range
            Register("hmi_set_tag_range", "Configure minimum and maximum range limits (constant values) on an HMI tag.",
                schema =>
                {
                    schema.Properties["tag_name"] = new ToolProperty { Type = "string", Description = "HMI tag name." };
                    schema.Properties["min_value"] = new ToolProperty { Type = "number", Description = "Minimum limit value (optional)." };
                    schema.Properties["max_value"] = new ToolProperty { Type = "number", Description = "Maximum limit value (optional)." };
                    schema.Properties["table_name"] = new ToolProperty { Type = "string", Description = "Tag table name (optional)." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "tag_name" };
                },
                args =>
                {
                    string tag = GetStringProp(args, "tag_name") ?? throw new ArgumentException("tag_name required.");
                    double? min = GetDoubleProp(args, "min_value");
                    double? max = GetDoubleProp(args, "max_value");
                    string? table = GetStringProp(args, "table_name");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.SetHmiTagRange(tag, min, max, table, target);
                    return CallToolResult.Text(res);
                });

            // 39. hmi_create_data_log
            Register("hmi_create_data_log", "Create a new DataLog in WinCC Unified HMI software.",
                schema =>
                {
                    schema.Properties["log_name"] = new ToolProperty { Type = "string", Description = "Name of the DataLog to create." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "log_name" };
                },
                args =>
                {
                    string logName = GetStringProp(args, "log_name") ?? throw new ArgumentException("log_name required.");
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.CreateDataLog(logName, target);
                    return CallToolResult.Text(res);
                });

            // 40. hmi_assign_logging_tags
            Register("hmi_assign_logging_tags", "Assign multiple HMI tags to a DataLog with logging mode and cycle.",
                schema =>
                {
                    schema.Properties["log_name"] = new ToolProperty { Type = "string", Description = "Target DataLog name." };
                    schema.Properties["tag_names"] = new ToolProperty { Type = "array", Description = "List of HMI tag names to log." };
                    schema.Properties["cycle"] = new ToolProperty { Type = "string", Description = "Logging cycle (e.g. 'T1s', 'T500ms', default: 'T1s')." };
                    schema.Properties["mode"] = new ToolProperty { Type = "string", Description = "Logging mode ('Cyclic', 'OnChange', 'OnDemand', default: 'Cyclic')." };
                    schema.Properties["target_name"] = new ToolProperty { Type = "string", Description = "Target HMI software name (optional)." };
                    schema.Required = new List<string> { "log_name", "tag_names" };
                },
                args =>
                {
                    string logName = GetStringProp(args, "log_name") ?? throw new ArgumentException("log_name required.");
                    var tags = GetStringListProp(args, "tag_names") ?? throw new ArgumentException("tag_names array required.");
                    string cycle = GetStringProp(args, "cycle") ?? "T1s";
                    string mode = GetStringProp(args, "mode") ?? "Cyclic";
                    string? target = GetStringProp(args, "target_name");

                    var res = TiaManager.Instance.AssignTagsToDataLog(logName, tags, cycle, mode, target);
                    return CallToolResult.Json(res);
                });

            // 41. hmi_compile
            Register("hmi_compile", "Compile HMI software / runtime device using ICompilable. Returns errors, warnings, and messages.",
                schema =>
                {
                    schema.Properties["target_device"] = new ToolProperty { Type = "string", Description = "Device name (e.g. 'PC-System_1', optional)." };
                },
                args =>
                {
                    string? target = GetStringProp(args, "target_device");
                    return CallToolResult.Json(TiaManager.Instance.CompileHmi(target));
                });

            // 42. tia_testsuite_list_tests
            Register("tia_testsuite_list_tests", "List TestSuite test cases configured in the current project.",
                schema => { },
                args => CallToolResult.Json(TiaManager.Instance.ListTestCases()));

            // 43. tia_testsuite_load_test
            Register("tia_testsuite_load_test", "Load/import a TestSuite .tat test file into the project with Override option.",
                schema =>
                {
                    schema.Properties["file_path"] = new ToolProperty { Type = "string", Description = "Path to the .tat file on disk." };
                    schema.Required = new List<string> { "file_path" };
                },
                args =>
                {
                    string file = GetStringProp(args, "file_path") ?? throw new ArgumentException("file_path required.");
                    return CallToolResult.Json(TiaManager.Instance.LoadTestCase(file));
                });

            // 44. hmi_list_connections
            Register("hmi_list_connections", "List all HMI connections in the WinCC Unified HMI software (name, communication driver, partner station, partner node, disabled status).",
                schema =>
                {
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Device or HMI software name (optional, defaults to first HMI target)." };
                },
                args =>
                {
                    string? target = GetStringProp(args, "device_name");
                    return CallToolResult.Json(TiaManager.Instance.ListHmiConnections(target));
                });

            // 45. hmi_create_connection
            Register("hmi_create_connection", "Create or configure an integrated HMI connection between an HMI target and a partner PLC device/CPU.",
                schema =>
                {
                    schema.Properties["connection_name"] = new ToolProperty { Type = "string", Description = "Name of the HMI connection to create (e.g. 'HMI_PLC_1_Connection')." };
                    schema.Properties["partner_plc_name"] = new ToolProperty { Type = "string", Description = "Name of the partner PLC device in the project (e.g. 'PLC_1')." };
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional, defaults to first HMI target)." };
                    schema.Properties["communication_driver"] = new ToolProperty { Type = "string", Description = "Communication driver (default: 'SIMATIC S7 1200/1500').", Default = "SIMATIC S7 1200/1500" };
                    schema.Properties["disabled_at_startup"] = new ToolProperty { Type = "boolean", Description = "Whether the connection is disabled at startup (default: false).", Default = false };
                    schema.Properties["comment"] = new ToolProperty { Type = "string", Description = "Optional comment describing the connection." };
                    schema.Required = new List<string> { "connection_name", "partner_plc_name" };
                },
                args =>
                {
                    string connName = GetStringProp(args, "connection_name") ?? throw new ArgumentException("connection_name required.");
                    string plcName = GetStringProp(args, "partner_plc_name") ?? throw new ArgumentException("partner_plc_name required.");
                    string? devName = GetStringProp(args, "device_name");
                    string? driver = GetStringProp(args, "communication_driver");
                    bool disabled = GetBoolProp(args, "disabled_at_startup", false);
                    string? comment = GetStringProp(args, "comment");

                    var res = TiaManager.Instance.CreateHmiConnection(devName, connName, plcName, driver, disabled, comment);
                    return CallToolResult.Json(res);
                });

            // 46. hmi_delete_connection
            Register("hmi_delete_connection", "Delete an HMI connection from WinCC Unified software and/or hardware communication management.",
                schema =>
                {
                    schema.Properties["connection_name"] = new ToolProperty { Type = "string", Description = "Name of the HMI connection to delete (e.g. 'HMI_Connection_2')." };
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional, defaults to first HMI target)." };
                    schema.Required = new List<string> { "connection_name" };
                },
                args =>
                {
                    string connName = GetStringProp(args, "connection_name") ?? throw new ArgumentException("connection_name required.");
                    string? devName = GetStringProp(args, "device_name");

                    var res = TiaManager.Instance.DeleteHmiConnection(devName, connName);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 47. hmi_list_text_lists
            Register("hmi_list_text_lists", "List HMI text lists on a WinCC Unified device with their entries and values.",
                schema =>
                {
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional)." };
                },
                args =>
                {
                    string? devName = GetStringProp(args, "device_name");
                    var res = TiaManager.Instance.ListTextLists(devName);
                    return CallToolResult.Json(res);
                });

            // 48. hmi_create_text_list
            Register("hmi_create_text_list", "Create a new text list on a WinCC Unified device with specified entries and values/ranges.",
                schema =>
                {
                    schema.Properties["list_name"] = new ToolProperty { Type = "string", Description = "Name of the text list to create." };
                    schema.Properties["entries"] = new ToolProperty
                    {
                        Type = "array",
                        Description = "List of text entries. Each entry: { value, from_value (optional), to_value (optional), text, multilingual_texts (optional dict of lang->text) }."
                    };
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional)." };
                    schema.Required = new List<string> { "list_name", "entries" };
                },
                args =>
                {
                    string listName = GetStringProp(args, "list_name") ?? throw new ArgumentException("list_name required.");
                    var entries = GetTextListEntries(args, "entries");
                    string? devName = GetStringProp(args, "device_name");

                    var res = TiaManager.Instance.CreateTextList(listName, entries, devName);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 49. hmi_delete_text_list
            Register("hmi_delete_text_list", "Delete an HMI text list from a WinCC Unified device.",
                schema =>
                {
                    schema.Properties["list_name"] = new ToolProperty { Type = "string", Description = "Name of the text list to delete." };
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional)." };
                    schema.Required = new List<string> { "list_name" };
                },
                args =>
                {
                    string listName = GetStringProp(args, "list_name") ?? throw new ArgumentException("list_name required.");
                    string? devName = GetStringProp(args, "device_name");

                    var res = TiaManager.Instance.DeleteTextList(listName, devName);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 50. hmi_export_text_lists
            Register("hmi_export_text_lists", "Export all HMI text lists to YAML (.hmi.yml) in a destination folder.",
                schema =>
                {
                    schema.Properties["destination_folder"] = new ToolProperty { Type = "string", Description = "Full path to destination folder." };
                    schema.Properties["base_filename"] = new ToolProperty { Type = "string", Description = "Base filename for exported .hmi.yml files." };
                    schema.Properties["device_name"] = new ToolProperty { Type = "string", Description = "Target HMI device or software name (optional)." };
                    schema.Required = new List<string> { "destination_folder", "base_filename" };
                },
                args =>
                {
                    string dest = GetStringProp(args, "destination_folder") ?? throw new ArgumentException("destination_folder required.");
                    string baseFile = GetStringProp(args, "base_filename") ?? throw new ArgumentException("base_filename required.");
                    string? devName = GetStringProp(args, "device_name");

                    var res = TiaManager.Instance.ExportTextLists(dest, baseFile, devName);
                    return CallToolResult.Json(res);
                });

            // ----------------------------------------------------
            // S7-PLCSIM Advanced Tools
            // ----------------------------------------------------

            // 51. plcsim_list_instances
            Register("plcsim_list_instances", "List all registered Siemens S7-PLCSIM Advanced instances with ID, name, operating state, CPU type, and IP.",
                schema => { },
                args => CallToolResult.Json(PlcSimManager.Instance.ListInstances()));

            // 52. plcsim_connect
            Register("plcsim_connect", "Connect to a running Siemens S7-PLCSIM Advanced instance by name (default 'PLC_1').",
                schema =>
                {
                    schema.Properties["instance_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Instance name of the virtual controller (e.g. 'PLC_1'). Default is 'PLC_1'."
                    };
                },
                args =>
                {
                    string instanceName = GetStringProp(args, "instance_name") ?? "PLC_1";
                    var res = PlcSimManager.Instance.Connect(instanceName);
                    return CallToolResult.Json(res, isError: !res.Success);
                });

            // 53. plcsim_disconnect
            Register("plcsim_disconnect", "Disconnect from the currently connected S7-PLCSIM Advanced instance.",
                schema => { },
                args =>
                {
                    PlcSimManager.Instance.Disconnect();
                    return CallToolResult.Success("Disconnected from S7-PLCSIM Advanced.");
                });

            // 54. plcsim_get_status
            Register("plcsim_get_status", "Get live status, operating mode, CPU type, scale factor, and tag count of the connected S7-PLCSIM Advanced instance.",
                schema => { },
                args => CallToolResult.Json(PlcSimManager.Instance.GetStatus()));

            // 55. plcsim_set_operating_state
            Register("plcsim_set_operating_state", "Set the operating state of the connected S7-PLCSIM Advanced instance (Run, Stop, MemoryReset).",
                schema =>
                {
                    schema.Properties["state"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Desired operating state ('Run', 'Stop', or 'MemoryReset')."
                    };
                    schema.Required = new List<string> { "state" };
                },
                args =>
                {
                    string state = GetStringProp(args, "state") ?? throw new ArgumentException("state required.");
                    bool ok = PlcSimManager.Instance.SetOperatingState(state);
                    return CallToolResult.Success($"Operating state set to {state}. Result: {ok}");
                });

            // 56. plcsim_set_scale_factor
            Register("plcsim_set_scale_factor", "Set the simulation time scale factor (e.g. 1.0 = real-time, 2.0 = 2x speed, 5.0 = 5x speed).",
                schema =>
                {
                    schema.Properties["scale_factor"] = new ToolProperty
                    {
                        Type = "number",
                        Description = "Scale factor (> 0.0, e.g. 1.0, 2.0, 5.0)."
                    };
                    schema.Required = new List<string> { "scale_factor" };
                },
                args =>
                {
                    double? factor = GetDoubleProp(args, "scale_factor");
                    if (!factor.HasValue || factor.Value <= 0)
                        throw new ArgumentException("scale_factor must be positive number.");
                    double updated = PlcSimManager.Instance.SetScaleFactor(factor.Value);
                    return CallToolResult.Success($"ScaleFactor set to {updated}");
                });

            // 57. plcsim_read_tag
            Register("plcsim_read_tag", "Read a live variable or tag value from S7-PLCSIM Advanced memory by symbolic tag name.",
                schema =>
                {
                    schema.Properties["tag_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Symbolic tag name (e.g. 'DB_MixingLine.Status_StepNumber', 'DB_MixingLine.Line.MixingTank.CurrentLevel')."
                    };
                    schema.Required = new List<string> { "tag_name" };
                },
                args =>
                {
                    string tagName = GetStringProp(args, "tag_name") ?? throw new ArgumentException("tag_name required.");
                    var res = PlcSimManager.Instance.ReadTag(tagName);
                    return CallToolResult.Json(res);
                });

            // 58. plcsim_write_tag
            Register("plcsim_write_tag", "Write a live value to a variable or tag in S7-PLCSIM Advanced memory by symbolic tag name.",
                schema =>
                {
                    schema.Properties["tag_name"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Symbolic tag name (e.g. 'DB_MixingLine.Cmd_StartBatch', 'DB_MixingLine.BatchRecipe.Water')."
                    };
                    schema.Properties["value"] = new ToolProperty
                    {
                        Description = "Value to write (boolean, integer, float, or string)."
                    };
                    schema.Required = new List<string> { "tag_name", "value" };
                },
                args =>
                {
                    string tagName = GetStringProp(args, "tag_name") ?? throw new ArgumentException("tag_name required.");
                    if (!args.HasValue || !args.Value.TryGetProperty("value", out var pVal))
                    {
                        throw new ArgumentException("value required.");
                    }
                    bool ok = PlcSimManager.Instance.WriteTag(tagName, pVal);
                    return CallToolResult.Success($"Tag '{tagName}' written successfully.");
                });

            // 59. plcsim_read_tags
            Register("plcsim_read_tags", "Read multiple live variable/tag values from S7-PLCSIM Advanced memory.",
                schema =>
                {
                    schema.Properties["tag_names"] = new ToolProperty
                    {
                        Type = "array",
                        Description = "List of symbolic tag names to read."
                    };
                    schema.Required = new List<string> { "tag_names" };
                },
                args =>
                {
                    var names = GetStringListProp(args, "tag_names") ?? throw new ArgumentException("tag_names required.");
                    var res = PlcSimManager.Instance.ReadTags(names);
                    return CallToolResult.Json(res);
                });

            // 60. plcsim_write_tags
            Register("plcsim_write_tags", "Write multiple live values to variables/tags in S7-PLCSIM Advanced memory.",
                schema =>
                {
                    schema.Properties["tags"] = new ToolProperty
                    {
                        Type = "object",
                        Description = "Key-value dictionary mapping symbolic tag names to values."
                    };
                    schema.Required = new List<string> { "tags" };
                },
                args =>
                {
                    var dict = GetObjectDictProp(args, "tags") ?? throw new ArgumentException("tags object required.");
                    int count = PlcSimManager.Instance.WriteTags(dict);
                    return CallToolResult.Success($"Successfully wrote {count} tags.");
                });

            // 61. plcsim_run_mixing_sequence
            Register("plcsim_run_mixing_sequence", "Execute an automated online sequence test on the mixing line in S7-PLCSIM Advanced. Configures recipe parameters, pulses start, monitors step progression, samples telemetry, and returns a detailed execution report.",
                schema =>
                {
                    schema.Properties["recipe_id"] = new ToolProperty { Type = "string", Description = "Recipe identifier (default 'RECIPE_01')." };
                    schema.Properties["water_liters"] = new ToolProperty { Type = "number", Description = "Target water volume in liters (default 100.0)." };
                    schema.Properties["additive_type"] = new ToolProperty { Type = "integer", Description = "Additive flavor type 1..3 (default 1)." };
                    schema.Properties["additive_amount"] = new ToolProperty { Type = "number", Description = "Additive amount in liters (default 20.0)." };
                    schema.Properties["sugar_kg"] = new ToolProperty { Type = "number", Description = "Sugar amount in kg (default 10.0)." };
                    schema.Properties["mix_time_seconds"] = new ToolProperty { Type = "number", Description = "Mixing duration in seconds (default 3.0)." };
                    schema.Properties["mix_speed_rpm"] = new ToolProperty { Type = "number", Description = "Mixer speed in RPM (default 1200.0)." };
                    schema.Properties["scale_factor"] = new ToolProperty { Type = "number", Description = "Simulation speed scale factor (default 1.0)." };
                    schema.Properties["timeout_seconds"] = new ToolProperty { Type = "integer", Description = "Max timeout in seconds (default 60)." };
                },
                args =>
                {
                    var recipe = new MixingRecipeInput
                    {
                        RecipeID = GetStringProp(args, "recipe_id") ?? "RECIPE_01",
                        WaterLiters = GetFloatProp(args, "water_liters", 100.0f),
                        AdditiveType = GetIntProp(args, "additive_type") ?? 1,
                        AdditiveAmount = GetFloatProp(args, "additive_amount", 20.0f),
                        SugarKg = GetFloatProp(args, "sugar_kg", 10.0f),
                        MixTimeSeconds = GetFloatProp(args, "mix_time_seconds", 3.0f),
                        MixSpeedRpm = GetFloatProp(args, "mix_speed_rpm", 1200.0f)
                    };
                    double scale = GetDoubleProp(args, "scale_factor") ?? 1.0;
                    int timeout = GetIntProp(args, "timeout_seconds") ?? 60;

                    var report = PlcSimManager.Instance.RunMixingSequence(recipe, scale, timeout);
                    return CallToolResult.Json(report, isError: !report.Success);
                });

            // 62. plcsim_run_sequence (Generic universal sequence runner)
            Register("plcsim_run_sequence", "Execute a generic automated online sequence test on any PLC sequence/state machine in S7-PLCSIM Advanced. Writes initial/recipe tags, triggers start, tracks step progression, polls monitored tags, and generates an execution telemetry report.",
                schema =>
                {
                    schema.Properties["sequence_name"] = new ToolProperty { Type = "string", Description = "Name or ID of the sequence under test (e.g. 'BottlingCycle', 'PressSequence')." };
                    schema.Properties["start_command_tag"] = new ToolProperty { Type = "string", Description = "Tag name to pulse TRUE to initiate the sequence (e.g. 'DB_Seq.cmdStart')." };
                    schema.Properties["reset_command_tag"] = new ToolProperty { Type = "string", Description = "Optional tag name to pulse TRUE to reset the sequence before starting." };
                    schema.Properties["step_number_tag"] = new ToolProperty { Type = "string", Description = "Tag name holding the current sequence step/state number (e.g. 'DB_Seq.currentStep')." };
                    schema.Properties["step_name_tag"] = new ToolProperty { Type = "string", Description = "Optional string tag name holding current step name." };
                    schema.Properties["done_tag"] = new ToolProperty { Type = "string", Description = "Tag name indicating sequence completion (e.g. 'DB_Seq.done')." };
                    schema.Properties["fault_tag"] = new ToolProperty { Type = "string", Description = "Optional tag name indicating sequence fault/alarm." };
                    schema.Properties["idle_step"] = new ToolProperty { Type = "integer", Description = "Step integer value that indicates idle state (default 0)." };
                    schema.Properties["initial_tags"] = new ToolProperty { Type = "object", Description = "Optional key-value map of tags to initialize before sequence start." };
                    schema.Properties["parameter_tags"] = new ToolProperty { Type = "object", Description = "Optional key-value map of recipe/setpoint parameters to apply." };
                    schema.Properties["monitored_tags"] = new ToolProperty { Type = "array", Description = "Optional array of tag names to sample in real-time telemetry." };
                    schema.Properties["scale_factor"] = new ToolProperty { Type = "number", Description = "Virtual controller speed scale factor (default 1.0)." };
                    schema.Properties["timeout_seconds"] = new ToolProperty { Type = "integer", Description = "Timeout in seconds to wait for sequence completion (default 60)." };
                    schema.Required = new List<string> { "start_command_tag", "step_number_tag", "done_tag" };
                },
                args =>
                {
                    var startTag = GetStringProp(args, "start_command_tag") ?? throw new ArgumentException("start_command_tag is required.");
                    var stepTag = GetStringProp(args, "step_number_tag") ?? throw new ArgumentException("step_number_tag is required.");
                    var doneTag = GetStringProp(args, "done_tag") ?? throw new ArgumentException("done_tag is required.");

                    var seqInput = new SequenceExecutionInput
                    {
                        SequenceName = GetStringProp(args, "sequence_name") ?? "Sequence_01",
                        StartCommandTag = startTag,
                        ResetCommandTag = GetStringProp(args, "reset_command_tag"),
                        StepNumberTag = stepTag,
                        StepNameTag = GetStringProp(args, "step_name_tag"),
                        DoneTag = doneTag,
                        FaultTag = GetStringProp(args, "fault_tag"),
                        IdleStep = GetIntProp(args, "idle_step") ?? 0,
                        InitialTags = GetObjectDictProp(args, "initial_tags"),
                        ParameterTags = GetObjectDictProp(args, "parameter_tags"),
                        MonitoredTags = GetStringListProp(args, "monitored_tags")
                    };

                    double scale = GetDoubleProp(args, "scale_factor") ?? 1.0;
                    int timeout = GetIntProp(args, "timeout_seconds") ?? 60;

                    var report = PlcSimManager.Instance.RunSequence(seqInput, scale, timeout);
                    return CallToolResult.Json(report, isError: !report.Success);
                });
        }

        private void Register(string name, string description, Action<ToolInputSchema> schemaBuilder, Func<JsonElement?, CallToolResult> handler)
        {
            var schema = new ToolInputSchema();
            schemaBuilder(schema);

            var tool = new ToolDefinition
            {
                Name = name,
                Description = description,
                InputSchema = schema
            };

            _tools.Add(tool);
            _handlers[name] = handler;
        }

        private static string? GetStringProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                return prop.GetString();
            }
            return null;
        }

        private static int? GetIntProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int val))
                    return val;
            }
            return null;
        }

        private static uint? GetUIntProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetUInt32(out uint val))
                    return val;
            }
            return null;
        }

        private static float GetFloatProp(JsonElement? element, string propName, float defaultValue)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetSingle(out float val))
                    return val;
            }
            return defaultValue;
        }

        private static double? GetDoubleProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out double val))
                    return val;
            }
            return null;
        }

        private static bool GetBoolProp(JsonElement? element, string propName, bool defaultValue)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True) return true;
                if (prop.ValueKind == JsonValueKind.False) return false;
            }
            return defaultValue;
        }

        private static List<string>? GetStringListProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop) &&
                prop.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in prop.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var s = item.GetString();
                        if (s != null) list.Add(s);
                    }
                }
                return list;
            }
            return null;
        }

        private static List<RangeEntryDto>? GetRangesProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop) &&
                prop.ValueKind == JsonValueKind.Array)
            {
                var list = new List<RangeEntryDto>();
                foreach (var item in prop.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        double from = 0.0, to = 0.0;
                        string val = "";
                        if (item.TryGetProperty("from", out var pFrom) && pFrom.TryGetDouble(out var dFrom)) from = dFrom;
                        if (item.TryGetProperty("to", out var pTo) && pTo.TryGetDouble(out var dTo)) to = dTo;
                        if (item.TryGetProperty("value", out var pVal)) val = pVal.GetString() ?? "";

                        list.Add(new RangeEntryDto { From = from, To = to, Value = val });
                    }
                }
                return list;
            }
            return null;
        }

        private static List<TextListEntryInput> GetTextListEntries(JsonElement? element, string propName)
        {
            var list = new List<TextListEntryInput>();
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop) &&
                prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        int val = 0;
                        int? fromVal = null;
                        int? toVal = null;
                        string text = "";
                        Dictionary<string, string>? ml = null;

                        if (item.TryGetProperty("value", out var pVal) && pVal.TryGetInt32(out var iVal)) val = iVal;
                        if (item.TryGetProperty("from_value", out var pFrom) && pFrom.TryGetInt32(out var iFrom)) fromVal = iFrom;
                        if (item.TryGetProperty("to_value", out var pTo) && pTo.TryGetInt32(out var iTo)) toVal = iTo;
                        if (item.TryGetProperty("text", out var pTxt)) text = pTxt.GetString() ?? "";

                        if (item.TryGetProperty("multilingual_texts", out var pMl) && pMl.ValueKind == JsonValueKind.Object)
                        {
                            ml = new Dictionary<string, string>();
                            foreach (var propMl in pMl.EnumerateObject())
                            {
                                ml[propMl.Name] = propMl.Value.GetString() ?? "";
                            }
                        }

                        list.Add(new TextListEntryInput
                        {
                            Value = val,
                            FromValue = fromVal,
                            ToValue = toVal,
                            Text = text,
                            MultilingualTexts = ml
                        });
                    }
                }
            }
            return list;
        }

        private static Dictionary<string, object>? GetObjectDictProp(JsonElement? element, string propName)
        {
            if (element.HasValue && element.Value.ValueKind == JsonValueKind.Object &&
                element.Value.TryGetProperty(propName, out var prop) &&
                prop.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object>();
                foreach (var p in prop.EnumerateObject())
                {
                    dict[p.Name] = p.Value;
                }
                return dict;
            }
            return null;
        }
    }
}
