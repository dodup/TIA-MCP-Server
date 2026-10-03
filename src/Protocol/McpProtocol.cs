using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TiaOpennessMcp.Protocol
{
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public JsonElement? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public JsonElement? Id { get; set; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonRpcError? Error { get; set; }

        public static JsonRpcResponse Success(JsonElement? id, object result) =>
            new JsonRpcResponse { Id = id, Result = result };

        public static JsonRpcResponse Fail(JsonElement? id, int code, string message, object? data = null) =>
            new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError { Code = code, Message = message, Data = data }
            };
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }

    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; } = new ServerCapabilities();

        [JsonPropertyName("serverInfo")]
        public ServerInfo ServerInfo { get; set; } = new ServerInfo();
    }

    public class ServerCapabilities
    {
        [JsonPropertyName("tools")]
        public Dictionary<string, object> Tools { get; set; } = new Dictionary<string, object>();

        [JsonPropertyName("resources")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Resources { get; set; }

        [JsonPropertyName("prompts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Prompts { get; set; }
    }

    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "tia-portal-openness-v21";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";
    }

    public class ToolDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("inputSchema")]
        public ToolInputSchema InputSchema { get; set; } = new ToolInputSchema();
    }

    public class ToolInputSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("properties")]
        public Dictionary<string, ToolProperty> Properties { get; set; } = new Dictionary<string, ToolProperty>();

        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Required { get; set; }

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; } = false;
    }

    public class ToolProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Enum { get; set; }

        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Default { get; set; }
    }

    public class ToolsListResult
    {
        [JsonPropertyName("tools")]
        public List<ToolDefinition> Tools { get; set; } = new List<ToolDefinition>();
    }

    public class CallToolResult
    {
        [JsonPropertyName("content")]
        public List<ToolContent> Content { get; set; } = new List<ToolContent>();

        [JsonPropertyName("isError")]
        public bool IsError { get; set; } = false;

        public static CallToolResult Text(string text, bool isError = false)
        {
            var res = new CallToolResult { IsError = isError };
            res.Content.Add(new ToolContent { Type = "text", Text = text });
            return res;
        }

        public static CallToolResult Json(object obj, bool isError = false)
        {
            string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            return Text(json, isError);
        }

        public static CallToolResult Success(string message) => Text(message, isError: false);
        public static CallToolResult Error(string message) => Text(message, isError: true);
    }

    public class ToolContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }
}
