using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

class Program
{
    static async Task Main(string[] args)
    {
        // Načtení konfigurace
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string url = config["MeteoStationUrl"];

        var meteo = new WeatherRecord(url);
        var dbSaver = new DatabaseSaver("meteodata.db");

        Console.WriteLine("Meteo downloader started...");

        while (true)
        {
            string json = await meteo.GetDataAsJsonAsync();
            Console.WriteLine(json);
            dbSaver.Save(json);
            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
}
