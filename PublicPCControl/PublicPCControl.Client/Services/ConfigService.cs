// File: PublicPCControl.Client/Services/ConfigService.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PublicPCControl.Client.Models;

namespace PublicPCControl.Client.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

        public ConfigService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _configPath = Path.Combine(localAppData, "PublicPCControl", "Config", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        }

        public AppConfig Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    var defaultConfig = new AppConfig();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json, _options) ?? new AppConfig();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                BackupCorruptConfig();
                var fallback = new AppConfig();
                Save(fallback);
                return fallback;
            }
        }

        public void Save(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config, _options);
            File.WriteAllText(_configPath, json);
        }

        public static string HashPassword(string plain)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(plain);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        private void BackupCorruptConfig()
        {
            if (!File.Exists(_configPath))
            {
                return;
            }

            var backupPath = _configPath + ".corrupt.bak";
            File.Copy(_configPath, backupPath, overwrite: true);
        }
    }
}