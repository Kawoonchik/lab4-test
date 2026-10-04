using System;
using Xunit;
using Npgsql;
// using Delivery; // Рокоментуй, якщо потрібно

namespace Delivery.Tests
{
    public class ParcelRepositoryTests : IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly ParcelRepository _repository;
        private Parcel _createdParcel;

        public ParcelRepositoryTests()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Host=localhost;Port=5432;Database=delivery_variant16_test;Username=delivery_student;Password=delivery_student";
            
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
            _repository = new ParcelRepository(_connection);
        }

        // Перевіряємо створення, читання та оновлення (суфікс _repositoryState обов'язковий)
        [Fact]
        public void Create_AndUpdate_ShouldChangeDataInDb_repositoryState()
        {
            // Arrange: створюємо посилку (Вага: 1000 г, Вартість: 5000 коп), як вимагає варіант 16
            var newParcel = new Parcel(0, $"P-{Guid.NewGuid().ToString().Substring(0, 8)}", 1000, 5000);

            // Act 1: Зберігаємо в БД
            _createdParcel = _repository.Create(newParcel);
            
            // Act 2: Змінюємо дані (наприклад, вагу збільшили до 2000, вартість до 7000)
            var parcelToUpdate = new Parcel(_createdParcel.Id, _createdParcel.Code, 2000, 7000);
            _repository.Update(parcelToUpdate);

            // Assert: звертаємось до БД, щоб перевірити фактичний стан після Update
            var retrievedParcel = _repository.FindById(_createdParcel.Id);

            Assert.NotNull(retrievedParcel);
            Assert.Equal(2000, retrievedParcel.WeightGrams); // Або просто Weight (глянь у Models.cs)
            Assert.Equal(7000, retrievedParcel.DeclaredValue); // Або просто Value (глянь у Models.cs)
        }

        public void Dispose()
        {
            if (_createdParcel != null && _createdParcel.Id > 0)
            {
                // Видаляємо посилку після тесту для незалежності
                _repository.Delete(_createdParcel.Id);
            }
            _connection.Dispose();
        }
    }
}