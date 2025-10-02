using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;
using System.Collections.Generic;

public class WeatherRecord
{
    private readonly string _url;

    public WeatherRecord(string url)
    {
        _url = url;
    }

    public async Task<string> GetDataAsJsonAsync()
    {
        try
        {
            using HttpClient client = new HttpClient();
            string xmlData = await client.GetStringAsync(_url);

            XDocument doc = XDocument.Parse(xmlData);

            var root = doc.Root;
            if (root == null)
                throw new Exception("Neplatné XML");

            // hlavní atributy wario
            var warioAttributes = new Dictionary<string, string>();
            foreach (var attr in root.Attributes())
            {
                warioAttributes[attr.Name.LocalName] = attr.Value;
            }

            // input senzory
            var inputSensors = new List<object>();
            foreach (var sensor in root.Element("input").Elements("sensor"))
            {
                inputSensors.Add(new
                {
                    type = sensor.Element("type")?.Value,
                    id = sensor.Element("id")?.Value,
                    name = sensor.Element("name")?.Value,
                    place = sensor.Element("place")?.Value,
                    value = sensor.Element("value")?.Value
                });
            }

            // output senzory
            var outputSensors = new List<object>();
            foreach (var sensor in root.Element("output").Elements("sensor"))
            {
                outputSensors.Add(new
                {
                    type = sensor.Element("type")?.Value,
                    id = sensor.Element("id")?.Value,
                    name = sensor.Element("name")?.Value,
                    place = sensor.Element("place")?.Value,
                    value = sensor.Element("value")?.Value
                });
            }

            // variable
            var variables = new Dictionary<string, string>();
            foreach (var el in root.Element("variable").Elements())
            {
                variables[el.Name.LocalName] = el.Value;
            }

            // minmax
            var minmax = new List<object>();
            foreach (var el in root.Element("minmax").Elements("s"))
            {
                minmax.Add(new
                {
                    id = el.Attribute("id")?.Value,
                    min = el.Attribute("min")?.Value,
                    max = el.Attribute("max")?.Value
                });
            }

            // final JSON objekt
            var result = new
            {
                Station = warioAttributes,
                Input = inputSensors,
                Output = outputSensors,
                Variables = variables,
                MinMax = minmax,
                DownloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                Error = "Meteostanice není dostupná",
                Message = ex.Message,
                DownloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
