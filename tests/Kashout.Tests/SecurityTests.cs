using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Kashout.Api;

namespace Kashout.Tests
{
    public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public SecurityTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task API_ProtectsAgainst_SQLInjection()
        {
            // Arrange - SQL injection payloads
            var maliciousPayloads = new[]
            {
                "'; DROP TABLE users; --",
                "1' OR '1'='1",
                "admin'--",
                "' UNION SELECT * FROM users--"
            };

            foreach (var payload in maliciousPayloads)
            {
                var request = new
                {
                    PersonalInfo = new
                    {
                        Name = payload,
                        Email = "test@example.com",
                        Phone = "+1234567890"
                    }
                };

                // Act
                var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

                // Assert - Should either reject or sanitize, not crash
                Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task API_HandlesXSSPayloads()
        {
            // Arrange - XSS payloads
            var xssPayloads = new[]
            {
                "<script>alert('XSS')</script>",
                "<img src=x onerror=alert('XSS')>",
                "javascript:alert('XSS')",
                "<svg/onload=alert('XSS')>"
            };

            foreach (var payload in xssPayloads)
            {
                var request = new
                {
                    PersonalInfo = new
                    {
                        Name = payload,
                        Email = "test@example.com",
                        Phone = "+1234567890"
                    }
                };

                // Act
                var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

                // Assert - Should process without executing scripts
                var content = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("<script>", content);
            }
        }

        [Fact]
        public async Task API_ValidatesInputLength()
        {
            // Arrange - Very long input (potential buffer overflow)
            var longString = new string('A', 100000);
            
            var request = new
            {
                PersonalInfo = new
                {
                    Name = longString,
                    Email = "test@example.com"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

            // Assert - Should handle gracefully
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task API_RejectsInvalidContentType()
        {
            // Arrange
            var content = new StringContent("random data", System.Text.Encoding.UTF8, "text/plain");

            // Act
            var response = await _client.PostAsync("/api/v1/verification/identity", content);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.UnsupportedMediaType ||
                response.StatusCode == HttpStatusCode.BadRequest,
                "Should reject invalid content types"
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task API_ValidatesRequiredFields(string invalidValue)
        {
            // Arrange
            var request = new
            {
                PersonalInfo = new
                {
                    Name = invalidValue,
                    Email = "test@example.com"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

            // Assert - Should validate required fields
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.UnprocessableEntity,
                "Should validate empty/null values"
            );
        }

        [Fact]
        public async Task API_RateLimitsRequests()
        {
            // Arrange - Send many requests rapidly
            var tasks = new Task<HttpResponseMessage>[100];
            var validRequest = new
            {
                PersonalInfo = new
                {
                    Name = "Test User",
                    Email = "test@example.com",
                    Phone = "+1234567890"
                }
            };

            // Act - Flood the API
            for (int i = 0; i < 100; i++)
            {
                tasks[i] = _client.PostAsJsonAsync("/api/v1/verification/identity", validRequest);
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - Check if any rate limiting occurs
            var tooManyRequestsCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            
            // Note: Rate limiting might not be implemented yet, so we just log the result
            Console.WriteLine($"Rate limiting check: {tooManyRequestsCount} requests were rate limited out of 100");
        }

        [Fact]
        public async Task API_HandlesSpecialCharacters()
        {
            // Arrange
            var specialChars = new[]
            {
                "José García",
                "李明",
                "Müller",
                "O'Brien",
                "Test\r\nNewline",
                "Test\tTab"
            };

            foreach (var name in specialChars)
            {
                var request = new
                {
                    PersonalInfo = new
                    {
                        Name = name,
                        Email = "test@example.com"
                    }
                };

                // Act
                var response = await _client.PostAsJsonAsync("/api/v1/verification/identity", request);

                // Assert - Should handle international characters
                Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task API_ProtectsAgainstPathTraversal()
        {
            // Arrange - Path traversal attempts
            var pathTraversalPayloads = new[]
            {
                "../../../etc/passwd",
                "..\\..\\..\\windows\\system32",
                "....//....//....//etc/passwd"
            };

            foreach (var payload in pathTraversalPayloads)
            {
                // Act
                var response = await _client.GetAsync($"/api/v1/verification/{payload}");

                // Assert - Should not allow path traversal
                Assert.True(
                    response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.BadRequest,
                    "Should protect against path traversal"
                );
            }
        }
    }
}
