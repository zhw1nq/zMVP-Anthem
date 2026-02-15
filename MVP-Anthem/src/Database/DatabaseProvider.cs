using MySqlConnector;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;

namespace MVPAnthem.Database;

public interface IDatabaseProvider
{
    Task InitializeAsync();
    Task<PlayerPreference?> GetPlayerPreferenceAsync(ulong steamId);
    Task SavePlayerPreferenceAsync(ulong steamId, string? mvpName, string? mvpSound);
    Task<bool> TestConnectionAsync();
}

public class MySqlDatabaseProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private readonly ILogger? _logger;

    public MySqlDatabaseProvider(string connectionString, ILogger? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS mvp_player_preferences (
                steam_id BIGINT UNSIGNED PRIMARY KEY,
                mvp_name VARCHAR(255) NULL,
                mvp_sound VARCHAR(255) NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX idx_steam_id (steam_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
        ";

        await using var command = new MySqlCommand(createTableQuery, connection);
        await command.ExecuteNonQueryAsync();

        _logger?.LogInformation("[MVP-Anthem] Database table created/verified successfully");
    }

    public async Task<PlayerPreference?> GetPlayerPreferenceAsync(ulong steamId)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT steam_id, mvp_name, mvp_sound
                FROM mvp_player_preferences 
                WHERE steam_id = @steamId
            ";

            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@steamId", steamId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var steamIdFromDb = reader.GetUInt64("steam_id");
                var mvpName = reader.IsDBNull(reader.GetOrdinal("mvp_name")) ? null : reader.GetString("mvp_name");
                var mvpSound = reader.IsDBNull(reader.GetOrdinal("mvp_sound")) ? null : reader.GetString("mvp_sound");

                return new PlayerPreference
                {
                    SteamId = steamIdFromDb,
                    MVPName = mvpName,
                    MVPSound = mvpSound
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to get player preference for {steamId}: {ex.Message}");
            return null;
        }
    }

    public async Task SavePlayerPreferenceAsync(ulong steamId, string? mvpName, string? mvpSound)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO mvp_player_preferences (steam_id, mvp_name, mvp_sound)
                VALUES (@steamId, @mvpName, @mvpSound)
                ON DUPLICATE KEY UPDATE
                    mvp_name = @mvpName,
                    mvp_sound = @mvpSound,
                    updated_at = CURRENT_TIMESTAMP
            ";

            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@steamId", steamId);
            command.Parameters.AddWithValue("@mvpName", mvpName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@mvpSound", mvpSound ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to save player preference for {steamId}: {ex.Message}");
        }
    }
}

public class PlayerPreference
{
    public ulong SteamId { get; set; }
    public string? MVPName { get; set; }
    public string? MVPSound { get; set; }
}
