using System;
using System.IO;
using System.Text.Json;
using TiaOpennessMcp.Protocol;
using TiaOpennessMcp.Tools;

namespace TiaOpennessMcp
{
    public class McpServer
    {
        private readonly ToolRegistry _toolRegistry;
        private readonly JsonSerializerOptions _jsonOptions;

        public McpServer(ToolRegistry? toolRegistry = null)
        {
            _toolRegistry = toolRegistry ?? new ToolRegistry();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public ToolRegistry Tools => _toolRegistry;

        public void Run()
        {
            Console.Error.WriteLine("[McpServer] Siemens TIA Portal V21 Openness MCP Server started.");
            Console.Error.WriteLine("[McpServer] Listening on stdio for JSON-RPC messages...");

            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                try
                {
                    HandleMessage(line);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[McpServer] Unhandled exception processing message: {ex}");
                }
            }

            Console.Error.WriteLine("[McpServer] Stdio closed. Shutting down.");
        }

        private void HandleMessage(string rawJson)
        {
            JsonRpcRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<JsonRpcRequest>(rawJson, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[McpServer] JSON parse error: {ex.Message}");
                SendResponse(JsonRpcResponse.Fail(null, -32700, "Parse error"));
                return;
            }

            if (req == null) return;

            // Handle notifications (no response needed)
            if (req.Method.StartsWith("notifications/") || req.Method == "exit")
            {
                Console.Error.WriteLine($"[McpServer] Received notification: {req.Method}");
                return;
            }

            switch (req.Method)
            {
                case "initialize":
                    var initResult = new InitializeResult
                    {
                        ProtocolVersion = "2024-11-05",
                        ServerInfo = new ServerInfo
                        {
                            Name = "tia-portal-openness-v21",
                            Version = "1.0.0"
                        },
                        Capabilities = new ServerCapabilities
                        {
                            Tools = new System.Collections.Generic.Dictionary<string, object>()
                        }
                    };
                    SendResponse(JsonRpcResponse.Success(req.Id, initResult));
                    break;

                case "ping":
                    SendResponse(JsonRpcResponse.Success(req.Id, new object()));
                    break;

                case "tools/list":
                    var toolsResult = new ToolsListResult
                    {
                        Tools = _toolRegistry.GetTools()
                    };
                    SendResponse(JsonRpcResponse.Success(req.Id, toolsResult));
                    break;

                case "tools/call":
                    HandleToolCall(req);
                    break;

                default:
                    Console.Error.WriteLine($"[McpServer] Method not found: {req.Method}");
                    SendResponse(JsonRpcResponse.Fail(req.Id, -32601, $"Method '{req.Method}' not found."));
                    break;
            }
        }

        private void HandleToolCall(JsonRpcRequest req)
        {
            if (!req.Params.HasValue)
            {
                SendResponse(JsonRpcResponse.Fail(req.Id, -32602, "Invalid params: expected object with 'name'."));
                return;
            }

            string toolName = "";
            JsonElement? args = null;

            if (req.Params.Value.TryGetProperty("name", out var nameProp))
            {
                toolName = nameProp.GetString() ?? "";
            }

            if (req.Params.Value.TryGetProperty("arguments", out var argsProp))
            {
                args = argsProp;
            }

            if (string.IsNullOrEmpty(toolName))
            {
                SendResponse(JsonRpcResponse.Fail(req.Id, -32602, "Missing 'name' in tools/call params."));
                return;
            }

            Console.Error.WriteLine($"[McpServer] Calling tool '{toolName}'...");
            var callResult = _toolRegistry.Execute(toolName, args);
            SendResponse(JsonRpcResponse.Success(req.Id, callResult));
        }

        private void SendResponse(JsonRpcResponse response)
        {
            string json = JsonSerializer.Serialize(response, _jsonOptions);
            Console.Out.WriteLine(json);
            Console.Out.Flush();
        }
    }
}
