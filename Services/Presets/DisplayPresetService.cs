using System.IO;
using System.Text.Json;
using BorderlessWindowApp.Services.Display.implement;
using Microsoft.Extensions.Logging;

// Reference the preset model

namespace BorderlessWindowApp.Services.Presets
{
    public class DisplayPresetService : IDisplayPresetService
    {
        private readonly string _presetsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<DisplayPresetService> _logger;
        public DisplayPresetService(ILogger<DisplayPresetService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Define where to store the presets file (e.g., %LOCALAPPDATA%\YourAppName)
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "BorderlessWindowApp"); // Use your actual app name
            Directory.CreateDirectory(appFolder); // Ensure the directory exists
            _presetsFilePath = Path.Combine(appFolder, "display_presets.json");

            // Configure JSON options (optional, but good practice)
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // Make the JSON file readable
                PropertyNameCaseInsensitive = true // More robust loading
            };
        }

        public async Task<List<DisplayPreset>> LoadPresetsAsync()
        {
            if (!File.Exists(_presetsFilePath))
            {
                return new List<DisplayPreset>(); // Return empty list if file doesn't exist
            }

            try
            {
                string jsonContent = await File.ReadAllTextAsync(_presetsFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<DisplayPreset>(); // Handle empty file case
                }

                var presets = JsonSerializer.Deserialize<List<DisplayPreset>>(jsonContent, _jsonOptions);
                return presets ?? new List<DisplayPreset>(); // Return empty list if deserialization fails
            }
            catch (Exception ex)
            {
                // Log the error (replace Console.WriteLine with proper logging)
                Console.WriteLine($"Error loading presets from {_presetsFilePath}: {ex.Message}");
                // Decide error handling: return empty list, throw, etc.
                return new List<DisplayPreset>(); // Return empty list on error
            }
        }

        public async Task SavePresetsAsync(IEnumerable<DisplayPreset> presets)
        {
            if (presets == null)
            {
                presets = new List<DisplayPreset>(); // Ensure we don't save null
            }

            try
            {
                // Ensure we are saving a List<T> if the deserializer expects that.
                var listToSave = presets.ToList();
                string jsonContent = JsonSerializer.Serialize(listToSave, _jsonOptions);
                await File.WriteAllTextAsync(_presetsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error saving presets to {_presetsFilePath}: {ex.Message}");
                // Decide error handling: throw, notify user, etc.
                // Consider adding retry logic or backup mechanisms
            }
        }
    }
}