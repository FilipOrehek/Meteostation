using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Meteostation.Data;

// Nastavení URL meteostanice (lze načíst z konfigurace)
const string weatherUrl = "https://pastebin.com/raw/PMQueqDV";

var services = new ServiceCollection();
services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite("Data Source=weather.db")); // SQLite pro jednoduchost

var provider = services.BuildServiceProvider();

while (true)
{
    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();

    WeatherRecord record;
    try
    {
        // Use the shared HttpClient for the operation
        using var http = new HttpClient();
        record = await WeatherRecord.FromUrlAsync(weatherUrl, httpClient: http);
    }
    catch (Exception ex)
    {
        // Shouldn't usually happen because FromUrlAsync handles exceptions, but be defensive
        record = new WeatherRecord
        {
            DownloadedAt = DateTime.UtcNow,
            JsonData = null,
            IsStationOnline = false,
            ErrorMessage = ex.Message
        };
    }

    try
    {
        await db.SaveWeatherRecordAsync(record);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Chyba při ukládání do databáze: {ex.Message}");
    }

    Console.WriteLine($"[{DateTime.Now}] Záznam uložen. Online: {record.IsStationOnline}");
    await Task.Delay(TimeSpan.FromHours(1));
}
