using System;
using Xunit;
using Npgsql;
using Moq;
// using Delivery; // Розкоментуй за потреби

namespace Delivery.Tests
{
    public class ShippingServiceTests : IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly CustomerRepository _customerRepo;
        private readonly ParcelRepository _parcelRepo;
        private readonly ShipmentRepository _shipmentRepo;
        private readonly Mock<IShippingPolicy> _policyMock;
        private readonly ShippingService _service;

        private Customer _testCustomer;
        private Parcel _testParcel;
        private long _createdShipmentId;

        public ShippingServiceTests()
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Host=localhost;Port=5432;Database=delivery_variant16_test;Username=delivery_student;Password=delivery_student";
            
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            // Ініціалізуємо РЕАЛЬНІ репозиторії
            _customerRepo = new CustomerRepository(_connection);
            _parcelRepo = new ParcelRepository(_connection);
            _shipmentRepo = new ShipmentRepository(_connection);

            // Ініціалізуємо MOCK для зовнішньої HTTP-політики
            _policyMock = new Mock<IShippingPolicy>();

            // Створюємо РЕАЛЬНИЙ сервіс, передаючи йому реальні репозиторії та mock-політику
            _service = new ShippingService(_customerRepo, _parcelRepo, _shipmentRepo, _policyMock.Object);
        }

        [Fact]
        public void CreateShipment_RegularCustomerAndPickup_ShouldApply15PercentDiscount_serviceState()
        {
            // Arrange
            // 1. Готуємо реальні дані в БД
            _testCustomer = _customerRepo.Create(new Customer(0, $"C-{Guid.NewGuid().ToString().Substring(0, 5)}", true)); // Regular = true (5% знижки)
            _testParcel = _parcelRepo.Create(new Parcel(0, $"P-{Guid.NewGuid().ToString().Substring(0, 5)}", 1000, 5000)); // Вага 1000 г

            // 2. Налаштовуємо Mock політики на дозвіл (true)
            var distanceKm = 100;
            _policyMock.Setup(p => p.IsAllowed(_testCustomer, _testParcel, distanceKm)).Returns(true);

            // 3. Формуємо заявку (Pickup = true дає ще 10% знижки)
            var request = new ShipmentRequest($"REF-{Guid.NewGuid().ToString().Substring(0, 5)}", _testCustomer.Id, _testParcel.Id, distanceKm, false, true);

            // Act
            var shipment = _service.CreateShipment(request);
            _createdShipmentId = shipment.Id; // Зберігаємо для видалення

            // Assert
            // Розрахунок: 
            // База: 50 грн (5000 коп) + 1 кг * 10 грн (1000 коп) + 100 км * 0.10 грн (1000 коп) = 7000 коп.
            // Знижка 15% від 7000 = 1050 коп.
            // Adjustment має бути -1050 (від'ємна знижка). Total = 5950.
            
            // 1. Перевіряємо фактичний стан у БД (вимога 3.6 лаби)
            var savedShipment = _shipmentRepo.FindById(shipment.Id);
            
            Assert.NotNull(savedShipment);
            Assert.Equal(ShipmentStatus.Registered, savedShipment.Status);
            Assert.Equal(7000, savedShipment.BaseFee);
            Assert.Equal(-1050, savedShipment.Adjustment);
            Assert.Equal(5950, savedShipment.Total);
        }

        public void Dispose()
        {
            // Видаляємо дані у правильному порядку (спочатку відправлення, потім клієнта/посилку)
            if (_createdShipmentId > 0) _shipmentRepo.Delete(_createdShipmentId);
            if (_testCustomer != null) _customerRepo.Delete(_testCustomer.Id);
            if (_testParcel != null) _parcelRepo.Delete(_testParcel.Id);
            
            _connection.Dispose();
        }
    }
}