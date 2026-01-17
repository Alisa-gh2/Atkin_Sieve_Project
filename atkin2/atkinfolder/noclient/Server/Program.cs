using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Инициализация БД
var dbManager = new DBManager();
dbManager.InitializeDatabase();

// Основные endpointы
app.MapGet("/", () => "Решето Аткина - API для поиска простых чисел");

// Поиск простых чисел
app.MapPost("/atkin/find", (HttpRequest request) => 
{
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Unauthorized();
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    if (!dbManager.ValidateToken(token))
        return Results.Unauthorized();
    
    // Получаем данные из тела запроса
    using var reader = new StreamReader(request.Body);
    var body = reader.ReadToEndAsync().Result;
    var data = JsonSerializer.Deserialize<FindRequest>(body);
    
    if (data == null)
        return Results.BadRequest("Неверный формат запроса");
    
    return FindPrimesInRange(data.N1, data.N2, dbManager);
});

// Упрощенный поиск
app.MapGet("/atkin/range", (int n1, int n2) => FindPrimesInRange(n1, n2, dbManager));

// Аутентификация
app.MapPost("/auth/register", ([FromBody] AuthRequest request) => 
{
    if (string.IsNullOrEmpty(request.Login) || string.IsNullOrEmpty(request.Password))
        return Results.BadRequest("Логин и пароль не могут быть пустыми");
    
    if (dbManager.RegisterUser(request.Login, request.Password))
        return Results.Ok(new { message = "Пользователь зарегистрирован" });
    
    return Results.Conflict("Пользователь с таким логином уже существует");
});

app.MapPost("/auth/login", ([FromBody] AuthRequest request) => 
{
    var token = dbManager.AuthenticateUser(request.Login, request.Password);
    if (token != null)
        return Results.Ok(new { token = token });
    
    return Results.Unauthorized();
});

app.MapPost("/auth/change-password", async (HttpRequest request) => 
{
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Unauthorized();
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var userId = dbManager.GetUserIdByToken(token);
    
    if (userId == null)
        return Results.Unauthorized();
    
    // Получаем данные из тела запроса
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<ChangePasswordRequest>(body);
    
    if (data == null || string.IsNullOrEmpty(data.OldPassword) || string.IsNullOrEmpty(data.NewPassword))
        return Results.BadRequest("Неверный формат запроса");
    
    if (dbManager.ChangePassword(userId.Value, data.OldPassword, data.NewPassword))
        return Results.Ok(new { message = "Пароль успешно изменен" });
    
    return Results.BadRequest("Неверный старый пароль");
});

// Удаление аккаунта
app.MapPost("/auth/delete-account", async (HttpRequest request) => 
{
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Unauthorized();
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var userId = dbManager.GetUserIdByToken(token);
    
    if (userId == null)
        return Results.Unauthorized();
    
    // Получаем данные из тела запроса
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<DeleteAccountRequest>(body);
    
    if (data == null || string.IsNullOrEmpty(data.Password))
        return Results.BadRequest("Неверный формат запроса");
    
    if (dbManager.DeleteAccount(userId.Value, data.Password))
        return Results.Ok(new { message = "Аккаунт успешно удален" });
    
    return Results.BadRequest("Неверный пароль");
});

// История поисков пользователя
app.MapPost("/history/get", (HttpRequest request) => 
{
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Unauthorized();
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    var userId = dbManager.GetUserIdByToken(token);
    
    if (userId == null)
        return Results.Unauthorized();
    
    var history = dbManager.GetUserSearchHistory(userId.Value);
    return Results.Ok(history);
});

// Сохранение поиска в историю
app.MapPost("/history/save", async (HttpRequest request) => 
{
    Console.WriteLine("=== /history/save endpoint called ===");
    
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        Console.WriteLine("No authorization header");
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    Console.WriteLine($"Token received: {token}");
    
    var userId = dbManager.GetUserIdByToken(token);
    Console.WriteLine($"User ID from token: {userId}");
    
    if (userId == null)
    {
        Console.WriteLine("Invalid token or user not found");
        return Results.Unauthorized();
    }
    
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    Console.WriteLine($"Raw request body: {body}");
    
    try
    {
        // Используем JsonDocument для более гибкого парсинга
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        
        // Пробуем получить значения разными способами
        if (!root.TryGetProperty("n1", out var n1Prop) || !n1Prop.TryGetInt32(out int n1))
        {
            Console.WriteLine("Missing or invalid n1 property");
            return Results.BadRequest("Отсутствует или неверное свойство n1");
        }
        
        if (!root.TryGetProperty("n2", out var n2Prop) || !n2Prop.TryGetInt32(out int n2))
        {
            Console.WriteLine("Missing or invalid n2 property");
            return Results.BadRequest("Отсутствует или неверное свойство n2");
        }
        
        if (!root.TryGetProperty("primesCount", out var countProp) || !countProp.TryGetInt32(out int primesCount))
        {
            Console.WriteLine("Missing or invalid primesCount property");
            return Results.BadRequest("Отсутствует или неверно свойство primesCount");
        }
        
        long executionTimeMs = 0;
        if (root.TryGetProperty("executionTimeMs", out var timePropMs))
        {
            executionTimeMs = timePropMs.GetInt64();
        }
        else if (root.TryGetProperty("executionTime", out var timeProp))
        {
            executionTimeMs = timeProp.GetInt64();
            Console.WriteLine($"Using 'executionTime' instead of 'executionTimeMs'");
        }
        else
        {
            Console.WriteLine("Missing executionTimeMs property");
            return Results.BadRequest("Отсутствует свойство executionTimeMs");
        }
        
        Console.WriteLine($"Parsed data: n1={n1}, n2={n2}, count={primesCount}, time={executionTimeMs}");
        
        // Проверка диапазона
        if (n1 < 1 || n2 < 1)
        {
            Console.WriteLine($"Range validation failed: n1={n1}, n2={n2} (must be >= 1)");
            return Results.BadRequest("N1 и N2 должны быть натуральными числами (>= 1)");
        }
        
        if (n1 >= n2)
        {
            Console.WriteLine($"Range validation failed: n1={n1} >= n2={n2}");
            return Results.BadRequest("N1 должно быть меньше N2");
        }
        
        Console.WriteLine($"Range validation passed: {n1}-{n2}");
        
        // Сохраняем историю
        dbManager.SaveSearchHistory(userId.Value, n1, n2, primesCount, executionTimeMs);
        Console.WriteLine($"History saved for user {userId}");
        
        return Results.Ok(new { message = "Поиск сохранен в историю" });
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON parsing error: {ex.Message}");
        return Results.BadRequest($"Ошибка формата JSON: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
        return Results.Problem($"Неожиданная ошибка: {ex.Message}");
    }
});

app.MapPost("/history/delete-all", async (HttpRequest request) => 
{
    Console.WriteLine("=== /history/delete-all endpoint called ===");
    
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        Console.WriteLine("No authorization header");
        return Results.Unauthorized();
    }
    
    var token = authHeader.ToString().Replace("Bearer ", "");
    Console.WriteLine($"Token received: {token}");
    
    var userId = dbManager.GetUserIdByToken(token);
    Console.WriteLine($"User ID from token: {userId}");
    
    if (userId == null)
    {
        Console.WriteLine("Invalid token or user not found");
        return Results.Unauthorized();
    }
    
    try
    {
        // Удаляем всю историю пользователя
        bool success = dbManager.DeleteAllSearchHistory(userId.Value);
        
        if (success)
        {
            Console.WriteLine($"All history deleted for user {userId}");
            return Results.Ok(new { message = "Вся история поисков успешно удалена" });
        }
        else
        {
            Console.WriteLine($"Failed to delete history for user {userId}");
            return Results.Problem("Не удалось удалить историю поисков");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deleting history: {ex.Message}");
        return Results.Problem($"Ошибка при удалении истории: {ex.Message}");
    }
});

app.MapGet("/algorithm/info", () => 
{
    return new 
    {
        Name = "Решето Аткина",
        Description = "Современный алгоритм для поиска всех простых чисел",
        Features = new List<string>
        {
            "Использует квадратичные формы",
            "Эффективен для больших чисел",
            "Сложность O(N / log log N)"
        }
    };
});

app.Run();

// Метод поиска простых чисел
IResult FindPrimesInRange(int n1, int n2, DBManager dbManager = null, int? userId = null)
{
    if (n1 <= 0 || n2 <= 0)
        return Results.BadRequest("N1 и N2 должны быть натуральными числами");
    
    if (n1 >= n2)
        return Results.BadRequest("N1 должно быть меньше N2");

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var primes = GeneratePrimesInRange(n1, n2);
    stopwatch.Stop();

    long executionTime = stopwatch.ElapsedMilliseconds;
    if (executionTime == 0)
    {
        executionTime = stopwatch.ElapsedTicks * 1000 / System.Diagnostics.Stopwatch.Frequency;
        if (executionTime == 0) executionTime = 1;
    }

    var result = new
    {
        n1 = n1,
        n2 = n2,
        primes = primes,
        primesCount = primes.Count,
        executionTimeMs = executionTime,
        searchTime = DateTime.Now
    };
    
    return Results.Ok(result);

    // if (userId.HasValue && dbManager != null)
    // {
    //     dbManager.SaveSearchHistory(userId.Value, n1, n2, primes.Count, stopwatch.ElapsedMilliseconds);
    //     Console.WriteLine($" Лог сохранен для пользователя {userId}: {n1}-{n2}");
    // }
    // else
    // {
    //     Console.WriteLine($" Лог НЕ сохранен: userId={userId}, dbManager={dbManager != null}");
    // }
    
    //return Results.Ok(result);
}

// endpoint для поиска с авторизацией
app.MapPost("/atkin/find", (HttpRequest request, DBManager dbManager) => 
{
    // Проверяем авторизацию
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Unauthorized();
    
    var authHeaderValue = authHeader.ToString();
    Console.WriteLine($"Auth header: {authHeaderValue}");
    
    if (!authHeaderValue.StartsWith("Bearer "))
        return Results.Unauthorized();
    
    var token = authHeaderValue.Substring("Bearer ".Length);
    Console.WriteLine($"Token: {token}");
    
    var userId = dbManager.GetUserIdByToken(token);
    Console.WriteLine($"User ID from token: {userId}");
    
    if (userId == null)
        return Results.Unauthorized();
    
    // Получаем данные из тела запроса
    using var reader = new StreamReader(request.Body);
    var body = reader.ReadToEndAsync().Result;
    Console.WriteLine($"Request body: {body}");
    
    try
    {
        var data = JsonSerializer.Deserialize<FindRequest>(body);
        
        if (data == null)
            return Results.BadRequest("Неверный формат запроса");
        
        return FindPrimesInRange(data.N1, data.N2, dbManager, userId);
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"JSON error: {ex.Message}");
        return Results.BadRequest($"Ошибка JSON: {ex.Message}");
    }
});
// Алгоритм проверки простых чисел
List<int> GeneratePrimesInRange(int from, int to)
{
    var primes = new List<int>();
    
    for (int num = Math.Max(2, from); num <= to; num++)
    {
        if (IsPrime(num))
            primes.Add(num);
    }
    
    return primes;
}

bool IsPrime(int n)
{
    if (n < 2) return false;
    if (n == 2) return true;
    if (n % 2 == 0) return false;
    
    for (int i = 3; i * i <= n; i += 2)
    {
        if (n % i == 0) return false;
    }
    
    return true;
}

// Классы для запросов
public record AuthRequest(string Login, string Password);
public record FindRequest(int N1, int N2);
public record SaveHistoryRequest(int N1, int N2, int PrimesCount, long ExecutionTimeMs);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record DeleteAccountRequest(string Password);