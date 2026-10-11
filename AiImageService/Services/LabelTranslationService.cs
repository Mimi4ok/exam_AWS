using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AiImageService.Services
{
    public class LabelTranslationService
    {
        private readonly Dictionary<string, string> _mappings = new(StringComparer.OrdinalIgnoreCase);

        public LabelTranslationService()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "translation.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    using var doc = JsonDocument.Parse(json);

                    // Якщо в JSON є кореневий об'єкт "root"
                    JsonElement rootElement = doc.RootElement;
                    if (rootElement.TryGetProperty("root", out var rootObj))
                    {
                        foreach (var prop in rootObj.EnumerateObject())
                        {
                            _mappings[prop.Name] = prop.Value.GetString() ?? prop.Name;
                        }
                    }
                    else
                    {
                        // Якщо об'єкт без обгортки "root"
                        foreach (var prop in rootElement.EnumerateObject())
                        {
                            _mappings[prop.Name] = prop.Value.GetString() ?? prop.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading label mappings: {ex.Message}");
                }
            }
        }

        public string Translate(string rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawLabel))
                return "Unknown";

            return _mappings.TryGetValue(rawLabel.Trim(), out var translated) ? translated : rawLabel;
        }
    }
}