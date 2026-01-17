using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class AtkinClient
{
    private readonly HttpClient client;
    private readonly string serverUrl = "http://localhost:5000";
    private string? authToken;
    private int currentUserId;

    public AtkinClient()
    {
        client = new HttpClient
        {
            BaseAddress = new Uri(serverUrl)
        };
    }

    public async Task Run()
    {
        Console.WriteLine("=== Решето Аткина - Клиентское приложение ===");
        Console.WriteLine("Алгоритм поиска простых чисел\n");

        await ShowHelp();

        bool isAuthenticated = false;
        while (!isAuthenticated)
        {
            Console.WriteLine("\n=== Аутентификация ===");
            Console.WriteLine("1. Вход");
            Console.WriteLine("2. Регистрация");
            Console.WriteLine("3. Продолжить без входа (ограниченный функционал)");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    isAuthenticated = await Login();
                    break;
                case "2":
                    await Register();
                    break;
                case "3":
                    isAuthenticated = true; // Анонимный доступ
                    break;
                default:
                    Console.WriteLine("Неверный выбор!");
                    break;
            }
        }

        bool continueRunning = true;
        while (continueRunning)
        {
            Console.WriteLine("\n=== Главное меню ===");
            Console.WriteLine("1. Поиск простых чисел");
            if (authToken != null)
            {
                Console.WriteLine("2. Моя история поисков");
                Console.WriteLine("3. Удалить историю поиска");
                Console.WriteLine("4. Сохранить текущий поиск в историю");
            }
            Console.WriteLine("5. Информация об алгоритме");
            Console.WriteLine("6. Выход");
            Console.WriteLine("7. Управление аккаунтом");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    await SearchPrimes();
                    break;
                case "2" when authToken != null:
                    await ViewMyHistory();
                    break;
                case "3" when authToken != null: 
                    await DeleteAllHistory();
                    break;
                case "4" when authToken != null:
                    Console.WriteLine("Функция сохранения вызывается автоматически после поиска");
                    break;
                case "5":
                    await GetAlgorithmInfo();
                    break;
                case "6":
                    continueRunning = false;
                    break;
                case "7" when authToken != null:
                    await ManageAccount();
                    break;
                default:
                    Console.WriteLine("Неверный выбор!");
                    break;
            }
        }

        
    }
private async Task ManageAccount()
{
    Console.WriteLine("=== Главное меню ===");
    Console.WriteLine("1. Изменение пароля");
    if (authToken != null)
    {
        Console.WriteLine("2. Удаление аккаунта");
        Console.WriteLine("3. Остаться здесь");
        Console.WriteLine("4. Назад");

    }
    
    var choice = Console.ReadLine();
    switch (choice)
    {
        case "1":
            await ChangePassword();
            break;
        case "2":
            await DeleteAccount();
            break;
        case "3" when authToken != null:
            Console.WriteLine("*Вы остались здесь*");
            await ManageAccount();
            break;
        case "3":
            Console.WriteLine("Вы не можете здесь оставаться");
            return;
        case "4":
            return;
        default:
            Console.WriteLine("Неверный выбор!");
            break;
    }
}

private async Task ChangePassword()
{
    Console.WriteLine("\n=== Изменение пароля ===");
    
    Console.Write("Введите старый пароль: ");
    var oldPassword = Console.ReadLine();
    
    Console.Write("Введите новый пароль: ");
    var newPassword = Console.ReadLine();
    
    if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
    {
        Console.WriteLine("Пароль не может быть пустым!");
        return;
    }
    
    try
    {
        var request = new { OldPassword = oldPassword, NewPassword = newPassword };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        
        var response = await client.PostAsync("/auth/change-password", content);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Пароль успешно изменен!");
            authToken = null;
            Console.WriteLine("Пожалуйста, войдите снова.");
            await Login();
        }
        else
        {
            Console.WriteLine("Ошибка при изменении пароля. Проверьте старый пароль.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

    private async Task DeleteAccount()
    {
        Console.WriteLine("\n=== Удаление аккаунта ===");
        Console.WriteLine("ВНИМАНИЕ: Это действие необратимо!");
        Console.Write("Введите ваш пароль для подтверждения: ");
        var password = Console.ReadLine();
        
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Пароль не может быть пустым!");
            return;
        }
        
        try
        {
            var request = new { Password = password };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );
            
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            
            var response = await client.PostAsync("/auth/delete-account", content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Аккаунт успешно удален.");
                authToken = null;
            }
            else
            {
                Console.WriteLine("Ошибка при удалении аккаунта. Проверьте пароль.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
    private async Task ShowHelp()
    {
        Console.WriteLine("\n=== Справка по алгоритму 'Решето Аткина' ===");
        Console.WriteLine("Решето Аткина — это современный алгоритм для нахождения");
        Console.WriteLine("всех простых чисел до заданного целого числа.");
        Console.WriteLine("\nОсобенности алгоритма:");
        Console.WriteLine("• Использует квадратичные формы для предварительной маркировки");
        Console.WriteLine("• Более эффективен чем Решето Эратосфена для больших пределов");
        Console.WriteLine("• Временная сложность: O(N / log log N)");
        Console.WriteLine("• Пространственная сложность: O(N)");
        Console.WriteLine("\nИспользование:");
        Console.WriteLine("1. Введите два натуральных числа N1 и N2");
        Console.WriteLine("2. N1 - начало диапазона поиска");
        Console.WriteLine("3. N2 - конец диапазона поиска");
        Console.WriteLine("4. N1 должно быть меньше N2");
    }

    private async Task<bool> Login()
    {
        Console.Write("Введите логин: ");
        var login = Console.ReadLine();
        Console.Write("Введите пароль: ");
        var password = Console.ReadLine();

        try
        {
            var request = new { Login = login, Password = password };
            var response = await client.PostAsJsonAsync("/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                authToken = result.GetProperty("token").GetString();
                Console.WriteLine("Вход выполнен успешно!");
                return true;
            }
            else
            {
                Console.WriteLine("Ошибка входа. Проверьте логин и пароль.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> Register()
    {
        Console.Write("Введите новый логин: ");
        var login = Console.ReadLine();
        Console.Write("Введите пароль: ");
        var password = Console.ReadLine();

        try
        {
            var request = new { Login = login, Password = password };
            var response = await client.PostAsJsonAsync("/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Регистрация успешна! Теперь выполните вход.");
                return true;
            }
            else
            {
                Console.WriteLine("Ошибка регистрации. Возможно, логин уже занят.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения: {ex.Message}");
            return false;
        }
    }

    private async Task SearchPrimes()
    {
        Console.WriteLine("\n=== Поиск простых чисел ===");
        
        try
        {
            Console.Write("Введите N1 (начало диапазона): ");
            if (!int.TryParse(Console.ReadLine(), out int n1) || n1 <= 0)
            {
                Console.WriteLine("N1 должно быть натуральным числом!");
                return;
            }

            Console.Write("Введите N2 (конец диапазона): ");
            if (!int.TryParse(Console.ReadLine(), out int n2) || n2 <= 0)
            {
                Console.WriteLine("N2 должно быть натуральным числом!");
                return;
            }

            if (n1 >= n2)
            {
                Console.WriteLine("N1 должно быть меньше N2!");
                return;
            }

            if (authToken != null)
            {
                var request = new { N1 = n1, N2 = n2 };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );
                
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                
                var response = await client.PostAsync("/atkin/find", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    DisplayResult(result);
                    
                    // await SaveToHistory(n1, n2, result);
                    Console.WriteLine("\nХотите сохранить этот поиск в историю? (да/нет)");
                    var answer = Console.ReadLine()?.ToLower();
                    if (answer == "да" || answer == "yes" || answer == "y")
                    {
                        await SaveToHistory(n1, n2, result);
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка при поиске простых чисел.");
                }
            }
            else
            {
                // Анонимный доступ
                var response = await client.GetAsync($"/atkin/range?n1={n1}&n2={n2}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    DisplayResult(result);
                }
                else
                {
                    Console.WriteLine("Ошибка при поиске простых чисел.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private async Task SaveToHistory(int n1, int n2, JsonElement result)
{
    try
    {
        Console.WriteLine("=== SaveToHistory called ===");
        
        if (result.TryGetProperty("primesCount", out var countProp) &&
            result.TryGetProperty("executionTimeMs", out var timeProp))
        {
            int primesCount = countProp.GetInt32();
            long executionTime = timeProp.GetInt64();
            
            Console.WriteLine($"Data extracted: primesCount={primesCount}, executionTime={executionTime}");

            var request = new 
            { 
                n1 = n1,
                n2 = n2,
                primesCount = primesCount,
                executionTimeMs = executionTime
            };
            
            string jsonRequest = JsonSerializer.Serialize(request);
            Console.WriteLine($"Sending JSON: {jsonRequest}");
            Console.WriteLine($"Range: {n1}-{n2}, Primes found: {primesCount}");
            
            var content = new StringContent(
                jsonRequest,
                Encoding.UTF8,
                "application/json"
            );
            
            if (authToken != null)
            {
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                Console.WriteLine($"Authorization header set with token");
            }
            else
            {
                Console.WriteLine("No auth token available");
                return;
            }
            
            Console.WriteLine($"Sending POST to /history/save");
            var response = await client.PostAsync("/history/save", content);
            
            Console.WriteLine($"Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Результат сохранен в историю! Ответ: {responseContent}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка сохранения: {response.StatusCode}");
                Console.WriteLine($"Error content: {errorContent}");
                
                // Выведем отправленный JSON для отладки
                Console.WriteLine($"Sent JSON was: {jsonRequest}");
            }
        }
        else
        {
            Console.WriteLine("Не удалось получить данные для сохранения в историю");
            
            // Отладочная информация
            Console.WriteLine("Available properties in result:");
            foreach (var prop in result.EnumerateObject())
            {
                Console.WriteLine($"  - {prop.Name}: {prop.Value}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка сохранения в историю: {ex.Message}");
    }
}

private async Task DeleteAllHistory()
{
    Console.WriteLine("\n=== Удаление всей истории поисков ===");
    Console.WriteLine("ВНИМАНИЕ: Это действие удалит ВСЮ вашу историю поисков!");
    Console.WriteLine("Это действие необратимо.");
    Console.Write("Вы уверены, что хотите продолжить? (да/нет): ");
    
    var confirm = Console.ReadLine()?.ToLower();
    if (confirm != "да" && confirm != "yes" && confirm != "y")
    {
        Console.WriteLine("Удаление отменено.");
        return;
    }
    
    try
    {
        if (authToken != null)
        {
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }
        else
        {
            Console.WriteLine("Требуется авторизация!");
            return;
        }
        
        var response = await client.PostAsync("/history/delete-all", null);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Вся история поисков успешно удалена!");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Ошибка удаления: {response.StatusCode}");
            Console.WriteLine($"Детали: {errorContent}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

    private async Task ViewMyHistory()
{
    try
    {
        if (authToken != null)
        {
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }
        else
        {
            Console.WriteLine("Требуется авторизация для просмотра истории");
            return;
        }
        
        var response = await client.PostAsync("/history/get", null);
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var history = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            Console.WriteLine("\n=== Ваша история поисков ===");
            
            if (history == null || history.Count == 0)
            {
                Console.WriteLine("История поисков пуста.");
                return;
            }

            foreach (var record in history)
            {
                if (record.TryGetProperty("id", out var idProp) &&
                    record.TryGetProperty("n1", out var n1Prop) &&
                    record.TryGetProperty("n2", out var n2Prop) &&
                    record.TryGetProperty("primes_count", out var countProp) &&
                    record.TryGetProperty("execution_time_ms", out var timeProp) &&
                    record.TryGetProperty("search_time", out var dateProp))
                {
                    string dateStr = dateProp.GetString();
                    DateTime searchTime;
                    
                    if (DateTime.TryParse(dateStr, out searchTime) ||
                        DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out searchTime))
                    {
                        Console.WriteLine($"ID: {idProp.GetInt32()}, " +
                                        $"Диапазон: {n1Prop.GetInt32()}-{n2Prop.GetInt32()}, " +
                                        $"Чисел: {countProp.GetInt32()}, " +
                                        $"Время: {timeProp.GetInt64()}мс, " +
                                        $"Дата: {searchTime:dd.MM.yyyy HH:mm:ss}");
                    }
                    else
                    {
                        Console.WriteLine($"ID: {idProp.GetInt32()}, " +
                                        $"Диапазон: {n1Prop.GetInt32()}-{n2Prop.GetInt32()}, " +
                                        $"Чисел: {countProp.GetInt32()}, " +
                                        $"Время: {timeProp.GetInt64()}мс, " +
                                        $"Дата: {dateStr}"); // Выводим как есть, если не удалось распарсить
                    }
                }
                else
                {
                    Console.WriteLine("Неверный формат записи истории");
                }
            }
        }
        else
        {
            Console.WriteLine($"Ошибка получения истории: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

    private void DisplayResult(JsonElement result)
{
    try
    {
        Console.WriteLine("\n=== Результаты поиска ===");
        
        
        if (result.TryGetProperty("n1", out var n1Prop) &&
            result.TryGetProperty("n2", out var n2Prop) &&
            result.TryGetProperty("primesCount", out var countProp) &&
            result.TryGetProperty("executionTimeMs", out var timeProp))
        {
            int n1 = n1Prop.GetInt32();
            int n2 = n2Prop.GetInt32();
            int primesCount = countProp.GetInt32();
            long executionTime = timeProp.GetInt64();
            
            Console.WriteLine($"Диапазон поиска: {n1} - {n2}");
            Console.WriteLine($"Время выполнения: {executionTime} мс");
            Console.WriteLine($"Найдено простых чисел: {primesCount}");

            if (result.TryGetProperty("searchTime", out var timeProp2))
            {
                DateTime searchTime = timeProp2.GetDateTime();
                Console.WriteLine($"Время завершения: {searchTime:HH:mm:ss}");
            }

            if (primesCount > 0 && result.TryGetProperty("primes", out var primesProp))
            {
                var primes = primesProp.EnumerateArray()
                    .Select(p => p.GetInt32())
                    .ToList();
                
                if (primesCount <= 50)
                {
                    Console.WriteLine($"Простые числа: {string.Join(", ", primes)}");
                }
                else
                {
                    Console.WriteLine($"Первые 50 чисел: {string.Join(", ", primes.Take(50))}");
                    Console.WriteLine($"... и еще {primesCount - 50} чисел");
                }
            }
        }
        else
        {
            Console.WriteLine("Ошибка: неверный формат ответа от сервера");
            
            Console.WriteLine("Доступные поля в JSON:");
            foreach (var prop in result.EnumerateObject())
            {
                Console.WriteLine($"  - {prop.Name}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при выводе результатов: {ex.Message}");
    }
}

public void SaveSearchToFile(int userId, int n1, int n2, List<int> primes, long executionTimeMs)
{
    var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                   $"User: {userId} | " +
                   $"Range: {n1}-{n2} | " +
                   $"Primes found: {primes.Count} | " +
                   $"Execution time: {executionTimeMs}ms | " +
                   $"Primes: {string.Join(", ", primes.Take(10))}..." +
                   Environment.NewLine;
    
    File.AppendAllText("search_logs.txt", logEntry);
}

    private async Task GetAlgorithmInfo()
{
    try
    {
        var response = await client.GetAsync("/algorithm/info");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<JsonElement>(json);
            
            Console.WriteLine("\n=== Информация об алгоритме ===");
            
            if (info.TryGetProperty("name", out var nameProp) &&
                info.TryGetProperty("description", out var descProp))
            {
                Console.WriteLine($"Название: {nameProp}");
                Console.WriteLine($"Описание: {descProp}");
                
                if (info.TryGetProperty("features", out var featuresProp))
                {
                    Console.WriteLine("Особенности:");
                    
                    foreach (var feature in featuresProp.EnumerateArray())
                    {
                        Console.WriteLine($" • {feature}");
                    }
                }
            }
            else
            {
                if (info.TryGetProperty("Name", out var nameProp2) &&
                    info.TryGetProperty("Description", out var descProp2))
                {
                    Console.WriteLine($"Название: {nameProp2}");
                    Console.WriteLine($"Описание: {descProp2}");
                    
                    if (info.TryGetProperty("Features", out var featuresProp2))
                    {
                        Console.WriteLine("Особенности:");
                        
                        foreach (var feature in featuresProp2.EnumerateArray())
                        {
                            Console.WriteLine($" - {feature}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Неверный формат информации об алгоритме");
                }
            }
        }
        else
        {
            Console.WriteLine($"Ошибка получения информации: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

}

class Program
{
    static async Task Main(string[] args)
    {
        var client = new AtkinClient();
        await client.Run();
    }
}