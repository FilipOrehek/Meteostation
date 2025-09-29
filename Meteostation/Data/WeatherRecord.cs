using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Meteostation.Data
{
    public class WeatherRecord
    {
        public int Id { get; set; }
        public DateTime DownloadedAt { get; set; }
        public string? JsonData { get; set; }
        public bool IsStationOnline { get; set; }
        public string? ErrorMessage { get; set; }

        // Fetch XML from the specified URL, transform it into JSON and return a populated WeatherRecord.
        public static async Task<WeatherRecord> FromUrlAsync(string url, HttpClient? httpClient = null)
        {
            var record = new WeatherRecord
            {
                DownloadedAt = DateTime.UtcNow
            };

            var disposeClient = false;
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                disposeClient = true;
            }

            try
            {
                var xml = await httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(xml);

                // Convert XML to a POCO structure so it serializes nicely to JSON
                object? ConvertElement(XElement el)
                {
                    // If element has no child elements, return its value (string)
                    if (!el.HasElements)
                    {
                        return el.Value;
                    }

                    var dict = new Dictionary<string, object?>();

                    // Include attributes
                    foreach (var attr in el.Attributes())
                    {
                        dict[$"@{attr.Name.LocalName}"] = attr.Value;
                    }

                    // Process child elements
                    foreach (var child in el.Elements())
                    {
                        var key = child.Name.LocalName;
                        var value = ConvertElement(child);

                        if (!dict.ContainsKey(key))
                        {
                            dict[key] = value;
                        }
                        else
                        {
                            // If existing value is not a list, convert it to a list
                            if (dict[key] is List<object?> existingList)
                            {
                                existingList.Add(value);
                            }
                            else
                            {
                                var first = dict[key];
                                dict[key] = new List<object?> { first, value };
                            }
                        }
                    }

                    return dict;
                }

                var root = doc.Root;
                object? result = root != null ? new Dictionary<string, object?> { [root.Name.LocalName] = ConvertElement(root) } : null;

                record.JsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                record.IsStationOnline = true;
                record.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                // If anything fails (network, parse), mark station as offline and record the error
                record.JsonData = null;
                record.IsStationOnline = false;
                record.ErrorMessage = ex.Message;
            }
            finally
            {
                if (disposeClient)
                {
                    httpClient?.Dispose();
                }
            }

            return record;
        }
    }
}
