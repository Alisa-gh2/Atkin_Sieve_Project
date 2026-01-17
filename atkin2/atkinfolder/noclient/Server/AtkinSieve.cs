public class AtkinSieve
{
    public List<int> GeneratePrimesUpTo(int limit)
    {
        if (limit < 2) 
            return new List<int>();
        
        // Создаем массив для отметки простых чисел
        bool[] isPrime = new bool[limit + 1];
        
        // Инициализируем маленькие простые числа
        if (limit >= 2) isPrime[2] = true;
        if (limit >= 3) isPrime[3] = true;
        
        // Алгоритм Решето Аткина
        int sqrtLimit = (int)Math.Sqrt(limit);
        
        for (int x = 1; x <= sqrtLimit; x++)
        {
            for (int y = 1; y <= sqrtLimit; y++)
            {
                int n = 4 * x * x + y * y;
                if (n <= limit && (n % 12 == 1 || n % 12 == 5))
                    isPrime[n] = !isPrime[n];
                
                n = 3 * x * x + y * y;
                if (n <= limit && n % 12 == 7)
                    isPrime[n] = !isPrime[n];
                
                if (x > y)
                {
                    n = 3 * x * x - y * y;
                    if (n <= limit && n % 12 == 11)
                        isPrime[n] = !isPrime[n];
                }
            }
        }
        
        // Исключаем квадраты простых чисел
        for (int i = 5; i <= sqrtLimit; i++)
        {
            if (isPrime[i])
            {
                int square = i * i;
                for (int j = square; j <= limit; j += square)
                {
                    isPrime[j] = false;
                }
            }
        }
        
        // Собираем результат
        List<int> primes = new List<int>();
        for (int i = 2; i <= limit; i++)
        {
            if (isPrime[i])
                primes.Add(i);
        }
        
        return primes;
    }

    public List<int> GeneratePrimesInRange(int from, int to)
    {
        if (from < 2) from = 2;
        var allPrimes = GeneratePrimesUpTo(to);
        return allPrimes.Where(p => p >= from).ToList();
    }
}