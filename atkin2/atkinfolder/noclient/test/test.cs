using Xunit;

public class AtkinSieveTests
{
    private readonly AtkinSieve _sieve = new AtkinSieve();

    [Fact]
    public void GeneratePrimesUpTo_LimitLessThan2_ReturnsEmptyList()
    {
        // Arrange
        int limit = 1;

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit2_ReturnsOnly2()
    {
        // Arrange
        int limit = 2;

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit3_Returns2And3()
    {
        // Arrange
        int limit = 3;

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(2, result);
        Assert.Contains(3, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit10_ReturnsCorrectPrimes()
    {
        // Arrange
        int limit = 10;
        int[] expected = { 2, 3, 5, 7 };

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit20_ReturnsCorrectPrimes()
    {
        // Arrange
        int limit = 20;
        int[] expected = { 2, 3, 5, 7, 11, 13, 17, 19 };

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit30_ReturnsCorrectPrimes()
    {
        // Arrange
        int limit = 30;
        int[] expected = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit50_ReturnsCorrectPrimes()
    {
        // Arrange
        int limit = 50;
        int[] expected = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit100_ReturnsCorrectPrimes()
    {
        // Arrange
        int limit = 100;
        int[] expected = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 
                          53, 59, 61, 67, 71, 73, 79, 83, 89, 97 };

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_Limit500_ReturnsCorrectCount()
    {
        // Arrange
        int limit = 500;
        int expectedCount = 95; // Количество простых чисел до 500

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Equal(expectedCount, result.Count);
        Assert.Equal(2, result[0]); // Первое простое
        Assert.Equal(499, result[^1]); // Последнее простое до 500
    }

    [Fact]
    public void GeneratePrimesInRange_FromLessThan2_StartsFrom2()
    {
        // Arrange
        int from = 1;
        int to = 10;
        int[] expected = { 2, 3, 5, 7 };

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesInRange_From5To20_ReturnsCorrectPrimes()
    {
        // Arrange
        int from = 5;
        int to = 20;
        int[] expected = { 5, 7, 11, 13, 17, 19 };

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesInRange_From20To30_ReturnsCorrectPrimes()
    {
        // Arrange
        int from = 20;
        int to = 30;
        int[] expected = { 23, 29 };

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesInRange_NoPrimesInRange_ReturnsEmptyList()
    {
        // Arrange
        int from = 8;
        int to = 10;

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GeneratePrimesInRange_SameNumberPrime_ReturnsThatNumber()
    {
        // Arrange
        int from = 13;
        int to = 13;

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Single(result);
        Assert.Equal(13, result[0]);
    }

    [Fact]
    public void GeneratePrimesInRange_SameNumberNotPrime_ReturnsEmpty()
    {
        // Arrange
        int from = 15;
        int to = 15;

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GeneratePrimesInRange_LargeRange_ReturnsCorrectPrimes()
    {
        // Arrange
        int from = 990;
        int to = 1000;
        int[] expected = { 991, 997 };

        // Act
        var result = _sieve.GeneratePrimesInRange(from, to);

        // Assert
        Assert.Equal(expected.Length, result.Count);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_LimitIsPrimeNumber_IncludesThatNumber()
    {
        // Arrange
        int limit = 97; // Простое число

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Contains(limit, result);
    }

    [Fact]
    public void GeneratePrimesUpTo_LimitIsNotPrimeNumber_ExcludesThatNumber()
    {
        // Arrange
        int limit = 100; // Не простое число

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.DoesNotContain(limit, result);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(23)]
    [InlineData(29)]
    public void GeneratePrimesUpTo_VariousLimits_IncludesAllSmallPrimes(int limit)
    {
        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        // Все простые числа до лимита должны быть в результате
        foreach (var prime in result)
        {
            Assert.True(IsPrimeManual(prime));
        }
    }

    [Fact]
    public void GeneratePrimesUpTo_ZeroLimit_ReturnsEmptyList()
    {
        // Arrange
        int limit = 0;

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GeneratePrimesUpTo_NegativeLimit_ReturnsEmptyList()
    {
        // Arrange
        int limit = -10;

        // Act
        var result = _sieve.GeneratePrimesUpTo(limit);

        // Assert
        Assert.Empty(result);
    }

    // Вспомогательный метод для проверки простоты числа (для тестов)
    private bool IsPrimeManual(int n)
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
}