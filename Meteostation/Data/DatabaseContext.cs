using System;
using System.Data.SQLite;
using System.Text.Json;

public class DatabaseSaver
{
    private readonly string _connectionString;

    public DatabaseSaver(string databaseFile = "meteo.db")
    {
        _connectionString = $"Data Source={databaseFile};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SQLiteConnection(_connectionString);
        conn.Open();

        string createDownloadTable = @"
        CREATE TABLE IF NOT EXISTS DownloadInfo (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DownloadedAt TEXT NOT NULL,
            Status TEXT,
            Message TEXT
        );";

        string createSensorTable = @"
        CREATE TABLE IF NOT EXISTS Sensor (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Type TEXT,
            SensorId TEXT,
            Name TEXT,
            Place TEXT,
            Value TEXT,
            DownloadId INTEGER,
            FOREIGN KEY(DownloadId) REFERENCES DownloadInfo(Id)
        );";

        string createVariableTable = @"
        CREATE TABLE IF NOT EXISTS Variable (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            Value TEXT,
            DownloadId INTEGER,
            FOREIGN KEY(DownloadId) REFERENCES DownloadInfo(Id)
        );";

        string createMinMaxTable = @"
        CREATE TABLE IF NOT EXISTS MinMax (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SensorId TEXT,
            MinValue TEXT,
            MaxValue TEXT,
            DownloadId INTEGER,
            FOREIGN KEY(DownloadId) REFERENCES DownloadInfo(Id)
        );";

        using var cmd = new SQLiteCommand(createDownloadTable, conn);
        cmd.ExecuteNonQuery();

        cmd.CommandText = createSensorTable;
        cmd.ExecuteNonQuery();

        cmd.CommandText = createVariableTable;
        cmd.ExecuteNonQuery();

        cmd.CommandText = createMinMaxTable;
        cmd.ExecuteNonQuery();
    }

    public void SaveToDatabase(string jsonData)
    {
        using var conn = new SQLiteConnection(_connectionString);
        conn.Open();

        string downloadedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        //Check if meteostation is not available
        if (string.IsNullOrWhiteSpace(jsonData) || jsonData.Contains("Meteostation is not available", StringComparison.OrdinalIgnoreCase))
        {
            string insertError = @"INSERT INTO DownloadInfo (DownloadedAt, Status, Message) 
                                   VALUES (@time, @status, @message)";
            using var cmd = new SQLiteCommand(insertError, conn);
            cmd.Parameters.AddWithValue("@time", downloadedAt);
            cmd.Parameters.AddWithValue("@status", "Error");
            cmd.Parameters.AddWithValue("@message", "Meteostation is not available");
            cmd.ExecuteNonQuery();

            Console.WriteLine("⚠Meteostation is not available — saved error entry into database.");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            if (doc.RootElement.TryGetProperty("DownloadedAt", out var dt))
                downloadedAt = dt.GetString();

            //DownloadInfo
            string insertDownload = @"INSERT INTO DownloadInfo (DownloadedAt, Status, Message)
                                      VALUES (@time, @status, @message);
                                      SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(insertDownload, conn);
            cmd.Parameters.AddWithValue("@time", downloadedAt);
            cmd.Parameters.AddWithValue("@status", "OK");
            cmd.Parameters.AddWithValue("@message", "Data successfully downloaded");
            long downloadId = (long)cmd.ExecuteScalar();

            //Sensors
            if (doc.RootElement.TryGetProperty("Input", out var inputs))
            {
                foreach (var item in inputs.EnumerateArray())
                {
                    string insertSensor = @"INSERT INTO Sensor (Type, SensorId, Name, Place, Value, DownloadId)
                                            VALUES (@type, @id, @name, @place, @value, @downloadId)";
                    using var cmdSensor = new SQLiteCommand(insertSensor, conn);
                    cmdSensor.Parameters.AddWithValue("@type", item.GetProperty("type").GetString());
                    cmdSensor.Parameters.AddWithValue("@id", item.GetProperty("id").GetString());
                    cmdSensor.Parameters.AddWithValue("@name", item.GetProperty("name").GetString());
                    cmdSensor.Parameters.AddWithValue("@place", item.GetProperty("place").GetString());
                    cmdSensor.Parameters.AddWithValue("@value", item.GetProperty("value").GetString());
                    cmdSensor.Parameters.AddWithValue("@downloadId", downloadId);
                    cmdSensor.ExecuteNonQuery();
                }
            }

            //Variables
            if (doc.RootElement.TryGetProperty("Variables", out var vars))
            {
                foreach (var variable in vars.EnumerateObject())
                {
                    string insertVar = @"INSERT INTO Variable (Name, Value, DownloadId)
                                         VALUES (@name, @value, @downloadId)";
                    using var cmdVar = new SQLiteCommand(insertVar, conn);
                    cmdVar.Parameters.AddWithValue("@name", variable.Name);
                    cmdVar.Parameters.AddWithValue("@value", variable.Value.GetString());
                    cmdVar.Parameters.AddWithValue("@downloadId", downloadId);
                    cmdVar.ExecuteNonQuery();
                }
            }

            //MinMax
            if (doc.RootElement.TryGetProperty("MinMax", out var minMaxArr))
            {
                foreach (var item in minMaxArr.EnumerateArray())
                {
                    string insertMM = @"INSERT INTO MinMax (SensorId, MinValue, MaxValue, DownloadId)
                                        VALUES (@id, @min, @max, @downloadId)";
                    using var cmdMM = new SQLiteCommand(insertMM, conn);
                    cmdMM.Parameters.AddWithValue("@id", item.GetProperty("id").GetString());
                    cmdMM.Parameters.AddWithValue("@min", item.GetProperty("min").GetString());
                    cmdMM.Parameters.AddWithValue("@max", item.GetProperty("max").GetString());
                    cmdMM.Parameters.AddWithValue("@downloadId", downloadId);
                    cmdMM.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"Data have been saved into SQLite database.");
        }
        catch (Exception ex)
        {
            string insertError = @"INSERT INTO DownloadInfo (DownloadedAt, Status, Message)
                                   VALUES (@time, @status, @message)";
            using var cmd = new SQLiteCommand(insertError, conn);
            cmd.Parameters.AddWithValue("@time", downloadedAt);
            cmd.Parameters.AddWithValue("@status", "Error");
            cmd.Parameters.AddWithValue("@message", $"Invalid or missing data: {ex.Message}");
            cmd.ExecuteNonQuery();

            Console.WriteLine($"Error while saving data into database: {ex.Message}");
        }
    }
}
