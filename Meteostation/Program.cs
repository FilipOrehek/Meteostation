using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string url = config["MeteoStationUrl"];

        var meteo = new WeatherRecord(url);
        var dbSaver = new DatabaseSaver("meteoDatabase.db");

        Console.WriteLine("Meteo downloader started");

        while (true)
        {
            string json = await meteo.GetDataAsJsonAsync();
            Console.WriteLine(json);
            dbSaver.SaveToDatabase(json);
            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
}
