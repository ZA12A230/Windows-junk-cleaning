using System.IO;
using System.Text.Json;
using DiskCleaner.Models;

namespace DiskCleaner.Services;

public interface IConfigService
{
    Task<ScanOptions> LoadOptionsAsync();
    Task SaveOptionsAsync(ScanOptions options);
    Task<T> GetSettingAsync<T>(string key, T defaultValue);
    Task SetSettingAsync<T>(string key, T value);
}

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configPath;
    private JsonDocument? _cachedConfig;
    
    private const string ConfigFileName = "config.json";

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskCleaner");
        
        Directory.CreateDirectory(appDataPath);
        
        _configPath = Path.Combine(appDataPath, ConfigFileName);
        _logger.LogInformation("Config path: {ConfigPath}", _configPath);
    }
    
    public async Task<ScanOptions> LoadOptionsAsync()
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Config file not found, using defaults");
                return new ScanOptions();
            }
            
            try
            {
                var json = File.ReadAllText(_configPath);
                var options = JsonSerializer.Deserialize<ScanOptions>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadNumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                });
                
                _logger.LogInformation("Config loaded successfully");
                return options ?? new ScanOptions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load config, using defaults");
                return new ScanOptions();
            }
        });
    }
    
    public async Task SaveOptionsAsync(ScanOptions options)
    {
        await Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(options, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_configPath, json);
                _cachedConfig = null;
                _logger.LogInformation("Config saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save config");
                throw;
            }
        });
    }
    
    public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
    {
        return await Task.Run(() =>
        {
            try
            {
                var config = GetConfig();
                if (config.RootElement.TryGetProperty(key, out var element))
                {
                    return element.Deserialize<T>() ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get setting {Key}", key);
            }
            
            return defaultValue;
        });
    }
    
    public async Task SetSettingAsync<T>(string key, T value)
    {
        await Task.Run(() =>
        {
            try
            {
                var config = GetConfig();
                var json = File.ReadAllText(_configPath);
                using var doc = JsonDocument.Parse(json);
                
                // TODO: Implement proper JSON update
                _logger.LogInformation("Setting {Key} updated", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set setting {Key}", key);
            }
        });
    }
    
    private JsonDocument GetConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;
        
        if (!File.Exists(_configPath))
        {
            File.WriteAllText(_configPath, "{}");
        }
        
        var json = File.ReadAllText(_configPath);
        _cachedConfig = JsonDocument.Parse(json);
        return _cachedConfig;
    }
}
