using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Kashout.Core.Models;
using Kashout.Api;

namespace Kashout.Tests
{
    public class VerificationControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public VerificationControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task VerifyIdentity_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var request = CreateValidVerificationRequest();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadFromJsonAsync<VerificationResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.TransactionId);
            Assert.Equal("completed", result.Status);
            Assert.NotEmpty(result.Results);
        }

        [Fact]
        public async Task GetTestScenarios_ReturnsMultipleScenarios()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/verification/test-scenarios");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var scenarios = await response.Content.ReadFromJsonAsync<object[]>();
            Assert.NotNull(scenarios);
            Assert.NotEmpty(scenarios);
        }

        [Theory]
        [InlineData("low_risk")]
        [InlineData("medium_risk")]
        [InlineData("high_risk")]
        [InlineData("corporate")]
        public async Task GenerateTestData_WithDifferentScenarios_ReturnsAppropriateData(string scenario)
        {
            // Arrange
            var request = new { Scenario = scenario };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/test-data", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var result = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task VerifyIdentity_WithMissingData_ReturnsBadRequest()
        {
            // Arrange
            var request = new { }; // Empty request

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task VerifyIdentity_ProcessesMultipleServices()
        {
            // Arrange
            var request = CreateValidVerificationRequest();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);
            var result = await response.Content.ReadFromJsonAsync<VerificationResponse>();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Results.Count >= 3, "Should process multiple verification services");
        }

        private VerificationRequest CreateValidVerificationRequest()
        {
            return new VerificationRequest
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    Phone = "+1234567890",
                    SSN = "123-45-6789",
                    Address = new Address
                    {
                        Street = "123 Main St",
                        City = "New York",
                        State = "NY",
                        ZipCode = "10001",
                        Country = "USA"
                    }
                },
                Documents = new List<DocumentData>
                {
                    new DocumentData
                    {
                        Type = "passport",
                        ImageData = new byte[] { 1, 2, 3, 4 }
                    }
                },
                Biometric = new BiometricData
                {
                    FaceImage = new byte[] { 1, 2, 3, 4 }
                }
            };
        }
    }

    public class VerificationRequest
    {
        public PersonalInfo PersonalInfo { get; set; }
        public List<DocumentData> Documents { get; set; }
        public BiometricData Biometric { get; set; }
    }

    public class VerificationResponse
    {
        public string TransactionId { get; set; }
        public List<VerificationResult> Results { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    [Fact]
        public void PlaidFakeDataGenerator_GeneratesValidAccount()
        {
            // Act
            var account = Kashout.Core.Services.PlaidFakeDataGenerator.GenerateAccount();

            // Assert
            Assert.NotNull(account);
            Assert.NotNull(account.AccountId);
            Assert.NotNull(account.Name);
            Assert.NotNull(account.OfficialName);
            Assert.NotNull(account.Type);
            Assert.NotNull(account.Subtype);
            Assert.NotNull(account.Mask);
            Assert.True(account.Balance >= 0);
            Assert.Equal("USD", account.Currency);
            Assert.NotNull(account.InstitutionId);
            Assert.NotNull(account.InstitutionName);
        }

        [Fact]
        public void PlaidFakeDataGenerator_GeneratesValidTransactions()
        {
            // Arrange
            var accountId = "test_account_123";

            // Act
            var transactions = Kashout.Core.Services.PlaidFakeDataGenerator.GenerateTransactions(accountId, 5);

            // Assert
            Assert.NotNull(transactions);
            Assert.Equal(5, transactions.Count);
            foreach (var transaction in transactions)
            {
                Assert.Equal(accountId, transaction.AccountId);
                Assert.NotNull(transaction.TransactionId);
                Assert.True(transaction.Amount != 0);
                Assert.Equal("USD", transaction.Currency);
                Assert.NotNull(transaction.Description);
                Assert.NotNull(transaction.Category);
            }
        }

        [Fact]
        public void PosFakeDataGenerator_GeneratesValidMerchant()
        {
            // Act
            var merchant = Kashout.Core.Services.PosFakeDataGenerator.GenerateMerchant();

            // Assert
            Assert.NotNull(merchant);
            Assert.NotNull(merchant.MerchantId);
            Assert.NotNull(merchant.Name);
            Assert.NotNull(merchant.Category);
            Assert.NotNull(merchant.Address);
            Assert.NotNull(merchant.City);
            Assert.NotNull(merchant.State);
            Assert.NotNull(merchant.ZipCode);
            Assert.NotNull(merchant.Phone);
            Assert.NotNull(merchant.TaxId);
        }

        [Fact]
        public void PosFakeDataGenerator_GeneratesValidTransaction()
        {
            // Act
            var transaction = Kashout.Core.Services.PosFakeDataGenerator.GenerateTransaction();

            // Assert
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.TransactionId);
            Assert.NotNull(transaction.MerchantId);
            Assert.True(transaction.Amount > 0);
            Assert.Equal("USD", transaction.Currency);
            Assert.NotNull(transaction.PaymentMethod);
            Assert.NotNull(transaction.Status);
            Assert.NotNull(transaction.Description);
            Assert.NotNull(transaction.AuthorizationCode);
            Assert.NotNull(transaction.CardLastFour);
            Assert.NotNull(transaction.CardType);
        }
    }

    public class PosFakeDataGeneratorTests
    {
        [Fact]
        public void PosFakeDataGenerator_GeneratesValidMerchant()
        {
            // Act
            var merchant = Kashout.Core.Services.PosFakeDataGenerator.GenerateMerchant();

            // Assert
            Assert.NotNull(merchant);
            Assert.NotNull(merchant.MerchantId);
            Assert.NotNull(merchant.Name);
            Assert.NotNull(merchant.Category);
            Assert.NotNull(merchant.Address);
            Assert.NotNull(merchant.City);
            Assert.NotNull(merchant.State);
            Assert.NotNull(merchant.ZipCode);
            Assert.NotNull(merchant.Phone);
            Assert.NotNull(merchant.TaxId);
        }

        [Fact]
        public void PosFakeDataGenerator_GeneratesValidTransaction()
        {
            // Act
            var transaction = Kashout.Core.Services.PosFakeDataGenerator.GenerateTransaction();

            // Assert
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.TransactionId);
            Assert.NotNull(transaction.MerchantId);
            Assert.True(transaction.Amount > 0);
            Assert.Equal("USD", transaction.Currency);
            Assert.NotNull(transaction.PaymentMethod);
            Assert.NotNull(transaction.Status);
            Assert.NotNull(transaction.Description);
            Assert.NotNull(transaction.AuthorizationCode);
            Assert.NotNull(transaction.CardLastFour);
            Assert.NotNull(transaction.CardType);
        }
    }
}
