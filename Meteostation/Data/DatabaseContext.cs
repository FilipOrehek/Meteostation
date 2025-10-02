using System;
using System.Data.SQLite;
using System.Text.Json;

public class DatabaseSaver
{
    private readonly string _connectionString;

    public DatabaseSaver(string databaseFile = "meteo.db")
    {
        // DB soubor se vytvoří automaticky, pokud neexistuje
        _connectionString = $"Data Source={databaseFile};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SQLiteConnection(_connectionString);
        conn.Open();

        string createTable = @"
        CREATE TABLE IF NOT EXISTS MeteoData (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            JsonData TEXT NOT NULL,
            DownloadedAt TEXT NOT NULL
        );";

        using var cmd = new SQLiteCommand(createTable, conn);
        cmd.ExecuteNonQuery();
    }

    public void Save(string jsonData)
    {
        try
        {
            // vytáhneme čas ze JSONu, pokud je tam DownloadedAt
            string downloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                if (doc.RootElement.TryGetProperty("DownloadedAt", out var dt))
                {
                    downloadedAt = dt.GetString();
                }
            }
            catch { }

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sql = "INSERT INTO MeteoData (JsonData, DownloadedAt) VALUES (@json, @time)";
            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@json", jsonData);
            cmd.Parameters.AddWithValue("@time", downloadedAt);

            cmd.ExecuteNonQuery();

            Console.WriteLine("Data byla uložena do SQLite databáze.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při ukládání do SQLite: {ex.Message}");
        }
    }
}
