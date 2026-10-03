using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TiaOpennessMcp.Config;
using TiaOpennessMcp.Protocol;
using TiaOpennessMcp.Tia;
using TiaOpennessMcp.Tools;

namespace TiaOpennessMcp.Server
{
    public class HttpMcpServer : IDisposable
    {
        private readonly ServerConfig _config;
        private readonly ToolRegistry _toolRegistry;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, SseSession> _sessions = new ConcurrentDictionary<string, SseSession>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly DateTime _startTime = DateTime.UtcNow;
        private bool _isRunning;

        public HttpMcpServer(ServerConfig config, ToolRegistry toolRegistry)
        {
            _config = config;
            _toolRegistry = toolRegistry;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _listener = new HttpListener();
            string prefix = _config.Server.Host;
            if (!prefix.EndsWith("/")) prefix += "/";
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;
                Console.Error.WriteLine($"[HttpMcpServer] Listening on {_config.Server.Host} (Port {_config.Server.Port})...");
                Task.Run(() => AcceptLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[HttpMcpServer] Failed to start HTTP listener on {_config.Server.Host}: {ex.Message}");
                // Fallback attempt to 127.0.0.1 if localhost has reservation issues
                if (!_config.Server.Host.Contains("127.0.0.1"))
                {
                    try
                    {
                        string fallback = $"http://127.0.0.1:{_config.Server.Port}/";
                        Console.Error.WriteLine($"[HttpMcpServer] Retrying with fallback: {fallback}...");
                        _listener.Prefixes.Clear();
                        _listener.Prefixes.Add(fallback);
                        _listener.Start();
                        _isRunning = true;
                        Console.Error.WriteLine($"[HttpMcpServer] Listening on {fallback}...");
                        Task.Run(() => AcceptLoopAsync(_cts.Token));
                    }
                    catch (Exception ex2)
                    {
                        Console.Error.WriteLine($"[HttpMcpServer] Fallback listener failed: {ex2.Message}");
                    }
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            foreach (var kvp in _sessions)
            {
                kvp.Value.Dispose();
            }
            _sessions.Clear();
            Console.Error.WriteLine("[HttpMcpServer] Server stopped.");
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleRequestAsync(context), ct);
                }
                catch (HttpListenerException) when (!isRunning())
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.Error.WriteLine($"[HttpMcpServer] Accept error: {ex.Message}");
                    }
                }
            }
        }

        private bool isRunning() => _isRunning && !_cts.IsCancellationRequested;

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            // Apply CORS headers
            if (_config.Server.EnableCors)
            {
                res.AddHeader("Access-Control-Allow-Origin", "*");
                res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, Accept");
            }

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = (int)HttpStatusCode.NoContent;
                res.Close();
                return;
            }

            string rawPath = req.Url?.AbsolutePath ?? "/";
            string path = rawPath.TrimEnd('/').ToLowerInvariant();

            try
            {
                // Health & Status endpoints
                if (path == "" || path == "/status" || path == "/health")
                {
                    await HandleStatusAsync(context).ConfigureAwait(false);
                    return;
                }

                // SSE transport endpoints
                if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    (path == "/sse" || path == "/mcp" || path == "/events"))
                {
                    await HandleSseConnectAsync(context).ConfigureAwait(false);
                    return;
                }

                // SSE incoming message endpoint: POST /messages?sessionId=...
                if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) && path == "/messages")
                {
                    await HandleSsePostMessageAsync(context).ConfigureAwait(false);
                    return;
                }

                // Direct HTTP JSON-RPC endpoint: POST /mcp or POST /rpc
                if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                    (path == "/mcp" || path == "/rpc" || path == "/tools/call"))
                {
                    await HandleDirectJsonRpcAsync(context).ConfigureAwait(false);
                    return;
                }

                // Not found
                res.StatusCode = (int)HttpStatusCode.NotFound;
                byte[] notFoundBytes = Encoding.UTF8.GetBytes("Not Found");
                res.ContentType = "text/plain";
                res.ContentLength64 = notFoundBytes.Length;
                await res.OutputStream.WriteAsync(notFoundBytes, 0, notFoundBytes.Length).ConfigureAwait(false);
                res.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[HttpMcpServer] Error handling {req.HttpMethod} {rawPath}: {ex}");
                try
                {
                    res.StatusCode = (int)HttpStatusCode.InternalServerError;
                    byte[] errBytes = Encoding.UTF8.GetBytes($"Server Error: {ex.Message}");
                    res.ContentType = "text/plain";
                    res.ContentLength64 = errBytes.Length;
                    await res.OutputStream.WriteAsync(errBytes, 0, errBytes.Length).ConfigureAwait(false);
                    res.Close();
                }
                catch { }
            }
        }

        private async Task HandleStatusAsync(HttpListenerContext context)
        {
            var res = context.Response;
            var tia = TiaManager.Instance;

            var statusObj = new
            {
                server = "TiaOpennessMcp",
                version = "2.0.0",
                mode = _config.Server.Mode,
                port = _config.Server.Port,
                connected = tia.IsConnected,
                attachedPid = tia.AttachedProcessId,
                projectName = tia.CurrentProject?.Name ?? "",
                projectPath = tia.CurrentProject?.Path?.FullName ?? "",
                toolCount = _toolRegistry.GetTools().Count,
                activeSseSessions = _sessions.Count,
                uptimeSeconds = (int)(DateTime.UtcNow - _startTime).TotalSeconds
            };

            string json = JsonSerializer.Serialize(statusObj, new JsonSerializerOptions { WriteIndented = true });
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            res.Close();
        }

        private async Task HandleSseConnectAsync(HttpListenerContext context)
        {
            var res = context.Response;
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "text/event-stream";
            res.AddHeader("Cache-Control", "no-cache");
            res.AddHeader("Connection", "keep-alive");

            string sessionId = Guid.NewGuid().ToString("N");
            var session = new SseSession(sessionId, context);
            _sessions[sessionId] = session;

            Console.Error.WriteLine($"[HttpMcpServer] SSE client connected. SessionId: {sessionId}");

            // Send initial endpoint event
            string endpointMsg = $"event: endpoint\r\ndata: /messages?sessionId={sessionId}\r\n\r\n";
            byte[] endpointBytes = Encoding.UTF8.GetBytes(endpointMsg);
            await res.OutputStream.WriteAsync(endpointBytes, 0, endpointBytes.Length).ConfigureAwait(false);
            await res.OutputStream.FlushAsync().ConfigureAwait(false);

            // Keep SSE channel open until disconnected
            try
            {
                while (session.IsAlive && !_cts.IsCancellationRequested)
                {
                    if (session.TryDequeueMessage(out var msg, 5000))
                    {
                        string sseChunk = $"event: message\r\ndata: {msg}\r\n\r\n";
                        byte[] chunkBytes = Encoding.UTF8.GetBytes(sseChunk);
                        await res.OutputStream.WriteAsync(chunkBytes, 0, chunkBytes.Length).ConfigureAwait(false);
                        await res.OutputStream.FlushAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        // Ping comment to keep socket open through proxies
                        byte[] pingBytes = Encoding.UTF8.GetBytes(": ping\r\n\r\n");
                        await res.OutputStream.WriteAsync(pingBytes, 0, pingBytes.Length).ConfigureAwait(false);
                        await res.OutputStream.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[HttpMcpServer] SSE session {sessionId} disconnected: {ex.Message}");
            }
            finally
            {
                _sessions.TryRemove(sessionId, out _);
                session.Dispose();
                try { res.Close(); } catch { }
            }
        }

        private async Task HandleSsePostMessageAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            string? sessionId = req.QueryString["sessionId"];
            if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
            {
                res.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] err = Encoding.UTF8.GetBytes("Invalid or missing sessionId query parameter.");
                res.ContentType = "text/plain";
                res.ContentLength64 = err.Length;
                await res.OutputStream.WriteAsync(err, 0, err.Length).ConfigureAwait(false);
                res.Close();
                return;
            }

            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            // Respond 202 Accepted immediately to POST
            res.StatusCode = (int)HttpStatusCode.Accepted;
            res.Close();

            // Process JSON-RPC request asynchronously and deliver result via SSE stream
            _ = Task.Run(() =>
            {
                try
                {
                    string responseJson = ProcessJsonRpcRequest(body);
                    if (!string.IsNullOrEmpty(responseJson))
                    {
                        session.EnqueueMessage(responseJson);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[HttpMcpServer] Error processing SSE RPC for session {sessionId}: {ex}");
                }
            });
        }

        private async Task HandleDirectJsonRpcAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            string responseJson = ProcessJsonRpcRequest(body);
            byte[] bytes = Encoding.UTF8.GetBytes(responseJson);

            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            res.Close();
        }

        public string ProcessJsonRpcRequest(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return JsonSerializer.Serialize(JsonRpcResponse.Fail(null, -32700, "Empty request"), _jsonOptions);
            }

            JsonRpcRequest? req;
            try
            {
                req = JsonSerializer.Deserialize<JsonRpcRequest>(rawJson, _jsonOptions);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(JsonRpcResponse.Fail(null, -32700, $"Parse error: {ex.Message}"), _jsonOptions);
            }

            if (req == null)
            {
                return JsonSerializer.Serialize(JsonRpcResponse.Fail(null, -32600, "Invalid Request"), _jsonOptions);
            }

            // Notifications
            if (req.Method.StartsWith("notifications/") || req.Method == "exit")
            {
                return "";
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
                            Version = "2.0.0"
                        },
                        Capabilities = new ServerCapabilities
                        {
                            Tools = new System.Collections.Generic.Dictionary<string, object>()
                        }
                    };
                    return JsonSerializer.Serialize(JsonRpcResponse.Success(req.Id, initResult), _jsonOptions);

                case "ping":
                    return JsonSerializer.Serialize(JsonRpcResponse.Success(req.Id, new object()), _jsonOptions);

                case "tools/list":
                    var toolsResult = new ToolsListResult
                    {
                        Tools = _toolRegistry.GetTools()
                    };
                    return JsonSerializer.Serialize(JsonRpcResponse.Success(req.Id, toolsResult), _jsonOptions);

                case "tools/call":
                    return HandleToolCall(req);

                default:
                    return JsonSerializer.Serialize(JsonRpcResponse.Fail(req.Id, -32601, $"Method not found: '{req.Method}'"), _jsonOptions);
            }
        }

        private string HandleToolCall(JsonRpcRequest req)
        {
            if (req.Params == null)
            {
                return JsonSerializer.Serialize(JsonRpcResponse.Fail(req.Id, -32602, "Missing params"), _jsonOptions);
            }

            string? toolName = null;
            JsonElement? toolArgs = null;

            if (req.Params.Value.ValueKind == JsonValueKind.Object)
            {
                if (req.Params.Value.TryGetProperty("name", out var nameProp))
                    toolName = nameProp.GetString();

                if (req.Params.Value.TryGetProperty("arguments", out var argsProp))
                    toolArgs = argsProp;
            }

            if (string.IsNullOrEmpty(toolName))
            {
                return JsonSerializer.Serialize(JsonRpcResponse.Fail(req.Id, -32602, "Missing 'name' in tools/call params"), _jsonOptions);
            }

            var result = _toolRegistry.Execute(toolName!, toolArgs);
            return JsonSerializer.Serialize(JsonRpcResponse.Success(req.Id, result), _jsonOptions);
        }

        public void Dispose()
        {
            Stop();
        }

        private class SseSession : IDisposable
        {
            public string SessionId { get; }
            public HttpListenerContext Context { get; }
            private readonly BlockingCollection<string> _queue = new BlockingCollection<string>();
            private bool _isDisposed;

            public SseSession(string sessionId, HttpListenerContext context)
            {
                SessionId = sessionId;
                Context = context;
            }

            public bool IsAlive => !_isDisposed;

            public void EnqueueMessage(string message)
            {
                if (!_isDisposed)
                {
                    try { _queue.Add(message); } catch { }
                }
            }

            public bool TryDequeueMessage(out string message, int timeoutMs)
            {
                return _queue.TryTake(out message, timeoutMs);
            }

            public void Dispose()
            {
                _isDisposed = true;
                _queue.CompleteAdding();
                _queue.Dispose();
            }
        }
    }
}
