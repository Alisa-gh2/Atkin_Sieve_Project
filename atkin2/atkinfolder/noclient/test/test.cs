using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class BasicFunctionalityTests
    {
        private AtkinSieve _sieve;
        private DBManager _dbManager;

        [TestInitialize]
        public void TestInitialize()
        {
            _sieve = new AtkinSieve();
            _dbManager = new DBManager();
            _dbManager.InitializeDatabase();
        }

        [TestMethod]
        public void Test1_FindPrimes_SmallRange()
        {
            // Arrange
            int n1 = 1;
            int n2 = 20;

            // Act
            List<int> primes = _sieve.GeneratePrimesInRange(n1, n2);

            // Assert
            List<int> expected = new List<int> { 2, 3, 5, 7, 11, 13, 17, 19 };
            CollectionAssert.AreEqual(expected, primes, "Должны быть найдены правильные простые числа");
        }

        [TestMethod]
        public void Test2_FindPrimes_MediumRange()
        {
            // Arrange
            int n1 = 100;
            int n2 = 120;

            // Act
            List<int> primes = _sieve.GeneratePrimesInRange(n1, n2);

            // Assert
            List<int> expected = new List<int> { 101, 103, 107, 109, 113 };
            CollectionAssert.AreEqual(expected, primes, "Должны быть найдены правильные простые числа в среднем диапазоне");
        }

        [TestMethod]
        public void Test3_FindPrimes_LargeRange_ReturnsCorrectCount()
        {
            // Arrange
            int n1 = 1;
            int n2 = 1000;

            // Act
            List<int> primes = _sieve.GeneratePrimesInRange(n1, n2);

            // Assert
            // до 1000 существует 168 простых чисел
            Assert.AreEqual(168, primes.Count, "Должно быть найдено 168 простых чисел до 1000");
        }

        [TestMethod]
        [DataRow(1, 10, 4)] // 2, 3, 5, 7
        [DataRow(10, 20, 4)] // 11, 13, 17, 19
        [DataRow(20, 30, 2)] // 23, 29
        public void Test4_FindPrimes_DataDriven(int n1, int n2, int expectedCount)
        {
            // Act
            List<int> primes = _sieve.GeneratePrimesInRange(n1, n2);

            // Assert
            Assert.AreEqual(expectedCount, primes.Count, 
                $"В диапазоне {n1}-{n2} должно быть {expectedCount} простых чисел");
        }
    }
}