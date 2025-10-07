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
                throw new Exception("Invalid XML");

            //Main attributes
            var warioAttributes = new Dictionary<string, string>();
            foreach (var attr in root.Attributes())
            {
                warioAttributes[attr.Name.LocalName] = attr.Value;
            }

            //Input sensors
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

            //Output sensors
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

            //Variable
            var variables = new Dictionary<string, string>();
            foreach (var el in root.Element("variable").Elements())
            {
                variables[el.Name.LocalName] = el.Value;
            }

            //MinMax
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

            //final JSON object
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
                Error = "Meteostation is not available",
                Message = ex.Message,
                DownloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
