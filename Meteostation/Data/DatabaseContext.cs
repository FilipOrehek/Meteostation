using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Meteostation.Data
{
    public class DatabaseContext : DbContext
    {
        public DbSet<WeatherRecord> WeatherRecords { get; set; } = null!;

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        // Encapsulate save logic for WeatherRecord
        public async Task SaveWeatherRecordAsync(WeatherRecord record)
        {
            WeatherRecords.Add(record);
            await SaveChangesAsync();
        }
    }
}
