using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TiaOpennessMcp.Config
{
    public class ServerConfig
    {
        public ServerSettings Server { get; set; } = new ServerSettings();
        public TiaSettings Tia { get; set; } = new TiaSettings();

        public static ServerConfig Load(string? configPath = null)
        {
            string path = ResolveConfigPath(configPath);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<ServerConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                    if (cfg != null)
                    {
                        Console.Error.WriteLine($"[ServerConfig] Loaded configuration from '{path}'.");
                        return cfg;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ServerConfig] Warning: Failed to parse '{path}': {ex.Message}. Using defaults.");
                }
            }

            var defaultCfg = new ServerConfig();
            try
            {
                // Write default config file for easy editing
                string json = JsonSerializer.Serialize(defaultCfg, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                Console.Error.WriteLine($"[ServerConfig] Created default configuration file at '{path}'.");
            }
            catch { }

            return defaultCfg;
        }

        private static string ResolveConfigPath(string? explicitPath)
        {
            if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath))
                return Path.GetFullPath(explicitPath);

            string localDir = AppDomain.CurrentDomain.BaseDirectory;
            string localConfig = Path.Combine(localDir, "config.json");
            if (File.Exists(localConfig))
                return localConfig;

            string cwdConfig = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            if (File.Exists(cwdConfig))
                return cwdConfig;

            return localConfig;
        }
    }

    public class ServerSettings
    {
        public string Host { get; set; } = "http://localhost:5001/";
        public int Port { get; set; } = 5001;
        public string Mode { get; set; } = "HttpAndStdio"; // Http, Stdio, or HttpAndStdio
        public bool EnableCors { get; set; } = true;
    }

    public class TiaSettings
    {
        public bool AutoConnect { get; set; } = true;
        public int? PreferredProcessId { get; set; } = null;
        public bool KeepAliveConnection { get; set; } = true;
        public string ApiPath { get; set; } = @"C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\net48";
    }
}
