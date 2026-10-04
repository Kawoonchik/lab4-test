using System;
using Xunit;
using Npgsql; // Додаємо для роботи з підключенням PostgreSQL
// Переконайся, що тут є using твого проєкту, якщо класи в іншому namespace
// using Delivery; 

namespace Delivery.Tests
{
    public class CustomerRepositoryTests : IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly CustomerRepository _repository;
        private Customer _createdCustomer;

        public CustomerRepositoryTests()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Host=localhost;Port=5432;Database=delivery_variant16_test;Username=delivery_student;Password=delivery_student";
            
            // Створюємо та відкриваємо з'єднання, як вимагає README
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            _repository = new CustomerRepository(_connection);
        }

        [Fact]
        public void Create_And_FindById_ShouldSaveAndRetrieveData_repositoryState()
        {
            // Arrange: Використовуємо конструктор з параметрами (Id, Code, Regular). Id = 0, бо БД сама його згенерує.
            var newCustomer = new Customer(0, $"CUST-{Guid.NewGuid().ToString().Substring(0, 8)}", true);

            // Act
            _createdCustomer = _repository.Create(newCustomer);

            // Assert
            var retrievedCustomer = _repository.FindById(_createdCustomer.Id);

            Assert.NotNull(retrievedCustomer);
            Assert.Equal(_createdCustomer.Id, retrievedCustomer.Id);
            Assert.Equal(newCustomer.Code, retrievedCustomer.Code);
            Assert.True(retrievedCustomer.Regular);
        }

        public void Dispose()
        {
            if (_createdCustomer != null && _createdCustomer.Id > 0)
            {
                // Очищаємо БД від тестових даних
                _repository.Delete(_createdCustomer.Id);
            }
            
            // Власник повинен закрити з'єднання
            _connection.Dispose();
        }
    }
}