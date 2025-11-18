using System;
using System.Collections.Generic;
using Kashout.Core.Models;

namespace Kashout.Core.Services
{
    public class TestDataGenerator
    {
        private static readonly Random _random = new Random();
        
        public static class TestScenarios
        {
            // High-trust user profiles
            public static VerificationData HighTrustUser => new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = "John Doe",
                    Email = "john.doe@gmail.com",
                    Phone = "+1-555-123-4567",
                    SSN = "123-45-6789",
                    Address = new Address
                    {
                        Street = "123 Main St",
                        City = "San Francisco",
                        State = "CA",
                        ZipCode = "94105",
                        Country = "US"
                    }
                },
                Documents = new List<DocumentData>
                {
                    new DocumentData 
                    { 
                        Type = "drivers_license", 
                        ImageData = GenerateImageData(2048)
                    }
                },
                Biometric = new BiometricData
                {
                    FaceImage = GenerateImageData(1536),
                    Features = new Dictionary<string, object>
                    {
                        { "liveness_score", 0.95 },
                        { "face_match_score", 0.92 },
                        { "head_movement", true }
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = "device_abc123",
                    UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0)",
                    IPAddress = "203.0.113.42",
                    Location = "San Francisco, CA"
                }
            };

            // Medium-risk user
            public static VerificationData MediumRiskUser => new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = "Jane Smith",
                    Email = "jane.smith@tempmail.com",
                    Phone = "+1-555-987-6543",
                    SSN = "987-65-4321",
                    Address = new Address
                    {
                        Street = "456 Oak Ave",
                        City = "Austin",
                        State = "TX",
                        ZipCode = "78701",
                        Country = "US"
                    }
                },
                Documents = new List<DocumentData>
                {
                    new DocumentData 
                    { 
                        Type = "passport", 
                        ImageData = GenerateImageData(1024)
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = "device_xyz789",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                    IPAddress = "192.168.1.100", // Private IP - slight risk
                    Location = "Austin, TX"
                }
            };

            // High-risk/fraudulent user
            public static VerificationData HighRiskUser => new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = "test user",
                    Email = "fraud@tempmail.org",
                    Phone = "+1-000-000-0000",
                    SSN = "000-00-0000",
                    Address = new Address
                    {
                        Street = "123 Fake St",
                        City = "Nowhere",
                        State = "XX",
                        ZipCode = "00000",
                        Country = "XX"
                    }
                },
                Documents = new List<DocumentData>(), // No documents
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = "suspicious_device",
                    UserAgent = "bot/1.0",
                    IPAddress = "127.0.0.1", // Suspicious localhost
                    Location = "Unknown"
                }
            };

            // Corporate user
            public static VerificationData CorporateUser => new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = "Michael Johnson",
                    Email = "m.johnson@techcorp.com",
                    Phone = "+1-555-444-3333",
                    SSN = "555-44-3333",
                    Address = new Address
                    {
                        Street = "789 Business Blvd",
                        City = "New York",
                        State = "NY",
                        ZipCode = "10001",
                        Country = "US"
                    }
                },
                Documents = new List<DocumentData>
                {
                    new DocumentData 
                    { 
                        Type = "drivers_license", 
                        ImageData = GenerateImageData(2048)
                    },
                    new DocumentData 
                    { 
                        Type = "business_license", 
                        ImageData = GenerateImageData(1536)
                    }
                },
                Biometric = new BiometricData
                {
                    FaceImage = GenerateImageData(2048),
                    Features = new Dictionary<string, object>
                    {
                        { "liveness_score", 0.88 },
                        { "face_match_score", 0.94 }
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = "corporate_device_001",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                    IPAddress = "198.51.100.42",
                    Location = "New York, NY"
                }
            };
        }

        public static class PaymentScenarios
        {
            public static PaymentRequest SmallPurchase => new PaymentRequest
            {
                Amount = 25.99m,
                Currency = "USD",
                RequestedMethod = PaymentMethod.CreditCard,
                MerchantId = "merchant_123",
                Metadata = new Dictionary<string, object>
                {
                    { "item_category", "digital_goods" },
                    { "customer_tenure", "6_months" }
                }
            };

            public static PaymentRequest LargePurchase => new PaymentRequest
            {
                Amount = 2500.00m,
                Currency = "USD",
                RequestedMethod = PaymentMethod.BankTransfer,
                MerchantId = "merchant_456",
                Metadata = new Dictionary<string, object>
                {
                    { "item_category", "electronics" },
                    { "shipping_required", true }
                }
            };

            public static PaymentRequest SuspiciousTransaction => new PaymentRequest
            {
                Amount = 9999.99m,
                Currency = "USD",
                RequestedMethod = PaymentMethod.DebitCard,
                MerchantId = "merchant_suspicious",
                Metadata = new Dictionary<string, object>
                {
                    { "item_category", "gift_cards" },
                    { "unusual_amount", true }
                }
            };
        }

        public static VerificationData GenerateRandomUser()
        {
            var names = new[] { "Alice Johnson", "Bob Smith", "Carol Wilson", "David Brown", "Eva Davis" };
            var domains = new[] { "gmail.com", "yahoo.com", "outlook.com", "example.com" };
            var cities = new[] { "San Francisco", "New York", "Chicago", "Austin", "Seattle" };
            var states = new[] { "CA", "NY", "IL", "TX", "WA" };

            var name = names[_random.Next(names.Length)];
            var email = $"{name.ToLower().Replace(" ", ".")}@{domains[_random.Next(domains.Length)]}";
            
            return new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = name,
                    Email = email,
                    Phone = $"+1-555-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}",
                    SSN = $"{_random.Next(100, 999)}-{_random.Next(10, 99)}-{_random.Next(1000, 9999)}",
                    Address = new Address
                    {
                        Street = $"{_random.Next(100, 9999)} {new[] { "Main", "Oak", "Pine", "Elm" }[_random.Next(4)]} St",
                        City = cities[_random.Next(cities.Length)],
                        State = states[_random.Next(states.Length)],
                        ZipCode = _random.Next(10000, 99999).ToString(),
                        Country = "US"
                    }
                },
                Documents = new List<DocumentData>
                {
                    new DocumentData 
                    { 
                        Type = "drivers_license", 
                        ImageData = GenerateImageData(_random.Next(1024, 3072))
                    }
                },
                DeviceInfo = new DeviceInfo
                {
                    DeviceId = $"device_{Guid.NewGuid().ToString()[..8]}",
                    UserAgent = "Mozilla/5.0 (compatible browser)",
                    IPAddress = $"{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}.{_random.Next(1, 255)}",
                    Location = $"{cities[_random.Next(cities.Length)]}, {states[_random.Next(states.Length)]}"
                }
            };
        }

        public static PaymentRequest GenerateRandomPayment()
        {
            var amounts = new[] { 15.99m, 49.99m, 199.99m, 599.99m, 1299.99m };
            var methods = new[] { PaymentMethod.CreditCard, PaymentMethod.DebitCard, PaymentMethod.BankTransfer };
            
            return new PaymentRequest
            {
                Amount = amounts[_random.Next(amounts.Length)],
                Currency = "USD",
                RequestedMethod = methods[_random.Next(methods.Length)],
                MerchantId = $"merchant_{_random.Next(100, 999)}",
                Metadata = new Dictionary<string, object>
                {
                    { "transaction_id", Guid.NewGuid().ToString() },
                    { "timestamp", DateTime.UtcNow }
                }
            };
        }

        private static byte[] GenerateImageData(int size)
        {
            var data = new byte[size];
            _random.NextBytes(data);
            return data;
        }

        // Method to create a comprehensive test suite
        public static List<(VerificationData User, PaymentRequest Payment, string Scenario)> GenerateTestSuite()
        {
            return new List<(VerificationData, PaymentRequest, string)>
            {
                (TestScenarios.HighTrustUser, PaymentScenarios.SmallPurchase, "High Trust + Small Purchase"),
                (TestScenarios.HighTrustUser, PaymentScenarios.LargePurchase, "High Trust + Large Purchase"),
                (TestScenarios.MediumRiskUser, PaymentScenarios.SmallPurchase, "Medium Risk + Small Purchase"),
                (TestScenarios.MediumRiskUser, PaymentScenarios.LargePurchase, "Medium Risk + Large Purchase"),
                (TestScenarios.HighRiskUser, PaymentScenarios.SuspiciousTransaction, "High Risk + Suspicious Transaction"),
                (TestScenarios.CorporateUser, PaymentScenarios.LargePurchase, "Corporate + Large Purchase"),
                (GenerateRandomUser(), GenerateRandomPayment(), "Random User + Random Payment")
            };
        }
    }
}