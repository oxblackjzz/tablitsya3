using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using таблиц€3.Models;

namespace таблиц€3.Services
{
    public class DataStorageService
    {
        private readonly string _dataFilePath;
        private readonly ILogger<DataStorageService> _logger;

        public DataStorageService(IWebHostEnvironment environment, ILogger<DataStorageService> logger)
        {
            _logger = logger;
            var dataFolder = Path.Combine(environment.ContentRootPath, "Data");

            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            _dataFilePath = Path.Combine(dataFolder, "workshop-data.json");
            _logger.LogInformation($"Data file path: {_dataFilePath}");
        }

        public async Task SaveWorkshopDataAsync(WorkshopData data)
        {
            try
            {
                data.LastUpdated = DateTime.Now;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(data, options);

                await File.WriteAllTextAsync(_dataFilePath, json, Encoding.UTF8);

                _logger.LogInformation("Workshop data saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving workshop data");
                throw;
            }
        }

        public async Task<WorkshopData?> LoadWorkshopDataAsync()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    _logger.LogInformation("Data file not found, returning null");
                    return null;
                }

                var json = await File.ReadAllTextAsync(_dataFilePath, Encoding.UTF8);

                json = CleanCorruptedText(json);

                var data = JsonSerializer.Deserialize<WorkshopData>(json);

                if (data != null)
                {
                    CleanOrderNames(data);
                }

                _logger.LogInformation($"Workshop data loaded successfully. Orders count: {data?.WorkshopOrders.Sum(x => x.Value.Count)}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading workshop data");
                return null;
            }
        }

        private string CleanCorruptedText(string json)
        {
            try
            {
                json = System.Text.RegularExpressions.Regex.Replace(json, @"\x3F+", "");
                json = json.Replace("?", "");
                json = System.Text.RegularExpressions.Regex.Replace(json, @"\uFFFD+", "");

                _logger.LogInformation("JSON cleaned from corrupted characters");
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning corrupted text");
                return json;
            }
        }

        private void CleanOrderNames(WorkshopData data)
        {
            if (data.WorkshopOrderNames == null || !data.WorkshopOrderNames.Any())
                return;

            int totalCleaned = 0;

            foreach (var workshopPair in data.WorkshopOrderNames)
            {
                var workshopNumber = workshopPair.Key;
                var names = workshopPair.Value;

                for (int i = 0; i < names.Count; i++)
                {
                    var name = names[i];

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    try
                    {
                        var cleaned = name;

                        cleaned = cleaned.Replace("?", "");
                        cleaned = cleaned.Replace("?", "");

                        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[\x3F\uFFFD]+", "");

                        cleaned = cleaned.Trim();

                        if (string.IsNullOrWhiteSpace(cleaned))
                        {
                            names[i] = string.Empty;
                            totalCleaned++;
                            _logger.LogWarning($"Cleared corrupted order name in workshop {workshopNumber}, order {i}: '{name}' -> (empty)");
                        }
                        else if (cleaned != name)
                        {
                            names[i] = cleaned;
                            totalCleaned++;
                            _logger.LogInformation($"Cleaned order name in workshop {workshopNumber}, order {i}: '{name}' -> '{cleaned}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error cleaning order name in workshop {workshopNumber}, order {i}");
                        names[i] = string.Empty;
                    }
                }
            }

            if (totalCleaned > 0)
            {
                _logger.LogInformation($"Total cleaned order names: {totalCleaned}");
            }
        }

        public async Task<bool> MigrateDataEncodingAsync()
        {
            try
            {
                _logger.LogInformation("Starting data migration...");

                var data = await LoadWorkshopDataAsync();

                if (data == null)
                {
                    _logger.LogWarning("No data to migrate");
                    return false;
                }

                var backupPath = _dataFilePath + ".backup";
                if (File.Exists(_dataFilePath))
                {
                    File.Copy(_dataFilePath, backupPath, true);
                    _logger.LogInformation($"Backup created: {backupPath}");
                }

                await SaveWorkshopDataAsync(data);

                _logger.LogInformation("Data migration completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data migration");
                return false;
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    File.Delete(_dataFilePath);
                    _logger.LogInformation("All workshop data cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing workshop data");
                throw;
            }

            await Task.CompletedTask;
        }

        public bool HasSavedData()
        {
            return File.Exists(_dataFilePath);
        }
    }
}
