using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

public class DBManager
{
    private SqliteConnection? _connection;
    private readonly Dictionary<string, int> _activeTokens = new();
    private const string DB_PATH = "atkin_sieve.db";

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // Генерация токена
    private string GenerateToken()
    {
        return Guid.NewGuid().ToString() + DateTime.Now.Ticks.ToString();
    }

    // Инициализация БД
    public void InitializeDatabase()
{
    try
    {
        _connection = new SqliteConnection($"Data Source={DB_PATH}");
        _connection.Open();

        // Таблица пользователей
        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                login TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        // Таблица истории поисков
        var createHistoryTable = @"
            CREATE TABLE IF NOT EXISTS search_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                n1 INTEGER NOT NULL,
                n2 INTEGER NOT NULL,
                primes_count INTEGER NOT NULL,
                execution_time_ms INTEGER NOT NULL,
                search_time DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            )";

        using var cmd1 = new SqliteCommand(createUsersTable, _connection);
        cmd1.ExecuteNonQuery();

        using var cmd2 = new SqliteCommand(createHistoryTable, _connection);
        cmd2.ExecuteNonQuery();

        Console.WriteLine("База данных инициализирована");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
    }
}

    // Регистрация пользователя
    public bool RegisterUser(string login, string password)
    {
        try
        {
            var passwordHash = HashPassword(password);
            var query = "INSERT INTO users (login, password_hash) VALUES (@login, @passwordHash)";
            
            using var cmd = new SqliteCommand(query, _connection);
            cmd.Parameters.AddWithValue("@login", login);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            
            return cmd.ExecuteNonQuery() == 1;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка регистрации: {ex.Message}");
            return false;
        }
    }

    // Аутентификация пользователя
    public string? AuthenticateUser(string login, string password)
    {
        try
        {
            var passwordHash = HashPassword(password);
            var query = "SELECT id FROM users WHERE login = @login AND password_hash = @passwordHash";
            
            using var cmd = new SqliteCommand(query, _connection);
            cmd.Parameters.AddWithValue("@login", login);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var userId = reader.GetInt32(0);
                var token = GenerateToken();
                _activeTokens[token] = userId;
                return token;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка аутентификации: {ex.Message}");
            return null;
        }
    }

    // Проверка токена
    public bool ValidateToken(string token)
    {
        return _activeTokens.ContainsKey(token);
    }

    // Получение ID пользователя по токену
    public int? GetUserIdByToken(string token)
    {
        return _activeTokens.TryGetValue(token, out var userId) ? userId : null;
    }

    // Сохранение истории поиска
    public void SaveSearchHistory(int userId, int n1, int n2, int primesCount, long executionTimeMs)
{
    try
    {
        Console.WriteLine($"Saving history for user {userId}");
        
        var query = @"
            INSERT INTO search_history (user_id, n1, n2, primes_count, execution_time_ms) 
            VALUES (@userId, @n1, @n2, @primesCount, @executionTime)";
        
        using var cmd = new SqliteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@n1", n1);
        cmd.Parameters.AddWithValue("@n2", n2);
        cmd.Parameters.AddWithValue("@primesCount", primesCount);
        cmd.Parameters.AddWithValue("@executionTime", executionTimeMs);
        
        cmd.ExecuteNonQuery();
        Console.WriteLine($"History saved for user {userId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving history: {ex.Message}");
    }
}

public bool DeleteAllSearchHistory(int userId)
{
    try
    {
        Console.WriteLine($"Deleting all search history for user {userId}");
        
        var query = "DELETE FROM search_history WHERE user_id = @userId";
        
        using var cmd = new SqliteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        int rowsAffected = cmd.ExecuteNonQuery();
        
        Console.WriteLine($"Rows deleted: {rowsAffected}");
        
        return true; 
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting all search history: {ex.Message}");
        return false;
    }
}


    // Получение истории поисков пользователя
    public List<Dictionary<string, object>> GetUserSearchHistory(int userId)
{
    var history = new List<Dictionary<string, object>>();
    
    try
    {
        var query = @"
            SELECT id, n1, n2, primes_count, execution_time_ms, 
                   datetime(search_time, 'localtime') as search_time 
            FROM search_history 
            WHERE user_id = @userId 
            ORDER BY search_time DESC 
            LIMIT 50";
        
        using var cmd = new SqliteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var record = new Dictionary<string, object>
            {
                ["id"] = reader.GetInt32(0),
                ["n1"] = reader.GetInt32(1),
                ["n2"] = reader.GetInt32(2),
                ["primes_count"] = reader.GetInt32(3),
                ["execution_time_ms"] = reader.GetInt64(4),
                ["search_time"] = reader.GetString(5) 
            };
            history.Add(record);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка получения истории: {ex.Message}");
    }
    
    return history;
}

    // Изменение пароля
    public bool ChangePassword(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var oldPasswordHash = HashPassword(oldPassword);
            var newPasswordHash = HashPassword(newPassword);
            
            // проверяем старый пароль
            var checkQuery = "SELECT id FROM users WHERE id = @userId AND password_hash = @oldPasswordHash";
            using var checkCmd = new SqliteCommand(checkQuery, _connection);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            checkCmd.Parameters.AddWithValue("@oldPasswordHash", oldPasswordHash);
            
            using var reader = checkCmd.ExecuteReader();
            if (!reader.Read())
            {
                return false; // Старый пароль неверный
            }
            
            // Обновляем пароль
            var updateQuery = "UPDATE users SET password_hash = @newPasswordHash WHERE id = @userId";
            using var updateCmd = new SqliteCommand(updateQuery, _connection);
            updateCmd.Parameters.AddWithValue("@userId", userId);
            updateCmd.Parameters.AddWithValue("@newPasswordHash", newPasswordHash);
            
            var result = updateCmd.ExecuteNonQuery() == 1;
            
            if (result)
            {
                Console.WriteLine($"Пароль изменен для пользователя {userId}");
                
                // Удаляем все активные токены пользователя
                var tokensToRemove = _activeTokens
                    .Where(kv => kv.Value == userId)
                    .Select(kv => kv.Key)
                    .ToList();
                
                foreach (var token in tokensToRemove)
                {
                    _activeTokens.Remove(token);
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка изменения пароля: {ex.Message}");
            return false;
        }
    }

// Удаление аккаунта
    public bool DeleteAccount(int userId, string password)
    {
        try
        {
            var passwordHash = HashPassword(password);
            
            // Сначала проверяем пароль
            var checkQuery = "SELECT id FROM users WHERE id = @userId AND password_hash = @passwordHash";
            using var checkCmd = new SqliteCommand(checkQuery, _connection);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            checkCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            
            using var reader = checkCmd.ExecuteReader();
            if (!reader.Read())
            {
                return false; // Пароль неверный
            }
            
            // Начинаем транзакцию
            using var transaction = _connection.BeginTransaction();
            
            try
            {
                // 1. Удаляем историю поисков пользователя
                var deleteHistoryQuery = "DELETE FROM search_history WHERE user_id = @userId";
                using var deleteHistoryCmd = new SqliteCommand(deleteHistoryQuery, _connection, transaction);
                deleteHistoryCmd.Parameters.AddWithValue("@userId", userId);
                deleteHistoryCmd.ExecuteNonQuery();
                
                // 2. Удаляем пользователя
                var deleteUserQuery = "DELETE FROM users WHERE id = @userId";
                using var deleteUserCmd = new SqliteCommand(deleteUserQuery, _connection, transaction);
                deleteUserCmd.Parameters.AddWithValue("@userId", userId);
                var result = deleteUserCmd.ExecuteNonQuery() == 1;
                
                // 3. Удаляем все активные токены пользователя
                var tokensToRemove = _activeTokens
                    .Where(kv => kv.Value == userId)
                    .Select(kv => kv.Key)
                    .ToList();
                
                foreach (var token in tokensToRemove)
                {
                    _activeTokens.Remove(token);
                }
                
                transaction.Commit();
                
                if (result)
                {
                    Console.WriteLine($"Аккаунт пользователя {userId} удален");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Ошибка при удалении аккаунта: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка проверки пароля: {ex.Message}");
            return false;
        }
    }
    // Получение всех пользователей
    public List<Dictionary<string, object>> GetAllUsers()
    {
        var users = new List<Dictionary<string, object>>();
        
        try
        {
            var query = "SELECT id, login, created_at FROM users ORDER BY created_at DESC";
            using var cmd = new SqliteCommand(query, _connection);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var user = new Dictionary<string, object>
                {
                    ["id"] = reader.GetInt32(0),
                    ["login"] = reader.GetString(1),
                    ["created_at"] = reader.GetDateTime(2)
                };
                users.Add(user);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения пользователей: {ex.Message}");
        }
        
        return users;
    }
}