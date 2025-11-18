using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Kashout.Core.Models;
using Kashout.Core.Services;
using Kashout.Core.Services.Verification;

namespace Kashout.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly List<IVerificationService> _verificationServices;
        private readonly PaymentDecisionEngine _decisionEngine;
        private readonly WeightedScorer _scorer;

        public VerificationController()
        {
            // Initialize services for MVP testing
            _verificationServices = new List<IVerificationService>
            {
                new PlaidVerificationService(null, "test_client_id", "test_secret"),
                new StripeVerificationService(null, "test_secret_key"),
                new AdvancedFraudDetectionService(),
                new MLRiskScoringService(),
                new WebAuthnVerificationService()
            };
            
            _scorer = new WeightedScorer();
            _decisionEngine = new PaymentDecisionEngine(_scorer, new ComplianceChecker());
        }

        [HttpPost("identity")]
        public async Task<ActionResult<VerificationResponse>> VerifyIdentity([FromBody] VerificationRequest request)
        {
            try
            {
                var verificationData = MapToVerificationData(request);
                var results = new List<VerificationResult>();

                // Run all verification services in parallel
                var verificationTasks = _verificationServices.Select(service => 
                    service.VerifyAsync(verificationData));

                var verificationResults = await Task.WhenAll(verificationTasks);
                results.AddRange(verificationResults);

                var response = new VerificationResponse
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Results = results,
                    Timestamp = DateTime.UtcNow,
                    Status = "completed"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-scenarios")]
        public ActionResult<object> GetTestScenarios()
        {
            var scenarios = TestDataGenerator.GenerateTestSuite();
            return Ok(scenarios.Select(s => new { 
                scenario = s.Scenario,
                userData = s.User,
                paymentData = s.Payment 
            }));
        }

        [HttpPost("test-data")]
        public ActionResult<object> GenerateTestData([FromBody] TestDataRequest request)
        {
            try
            {
                var scenario = request?.Scenario?.ToLower() ?? "random";
                
                VerificationData userData = scenario switch
                {
                    "low_risk" or "high_trust" => TestDataGenerator.TestScenarios.HighTrustUser,
                    "medium_risk" => TestDataGenerator.TestScenarios.MediumRiskUser,
                    "high_risk" or "fraudulent" or "suspicious" => TestDataGenerator.TestScenarios.HighRiskUser,
                    "corporate" => TestDataGenerator.TestScenarios.CorporateUser,
                    _ => TestDataGenerator.GenerateRandomUser()
                };

                var paymentData = scenario switch
                {
                    "low_risk" or "high_trust" => TestDataGenerator.PaymentScenarios.SmallPurchase,
                    "high_risk" or "corporate" => TestDataGenerator.PaymentScenarios.LargePurchase,
                    "fraudulent" or "suspicious" => TestDataGenerator.PaymentScenarios.SuspiciousTransaction,
                    _ => TestDataGenerator.GenerateRandomPayment()
                };

                return Ok(new
                {
                    scenario = scenario,
                    userData = userData,
                    paymentData = paymentData,
                    generatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("test-scenario/{scenarioName}")]
        public async Task<ActionResult<object>> RunTestScenario(string scenarioName)
        {
            try
            {
                VerificationData userData = scenarioName.ToLower() switch
                {
                    "high-trust" => TestDataGenerator.TestScenarios.HighTrustUser,
                    "medium-risk" => TestDataGenerator.TestScenarios.MediumRiskUser,
                    "high-risk" => TestDataGenerator.TestScenarios.HighRiskUser,
                    "corporate" => TestDataGenerator.TestScenarios.CorporateUser,
                    _ => TestDataGenerator.GenerateRandomUser()
                };

                var paymentData = TestDataGenerator.PaymentScenarios.SmallPurchase;

                // Run verification
                var verificationTasks = _verificationServices.Select(service => 
                    service.VerifyAsync(userData));
                var verificationResults = await Task.WhenAll(verificationTasks);

                // Get score
                var score = _scorer.CalculateScore(verificationResults.ToList());

                // Make decision
                var decision = await _decisionEngine.ProcessPaymentRequestAsync(
                    paymentData, verificationResults.ToList());

                return Ok(new
                {
                    scenario = scenarioName,
                    userData = userData,
                    paymentData = paymentData,
                    verificationResults = verificationResults,
                    score = score,
                    decision = decision
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("comprehensive-test")]
        public async Task<ActionResult<object>> RunComprehensiveTest()
        {
            try
            {
                var testSuite = TestDataGenerator.GenerateTestSuite();
                var results = new List<object>();

                foreach (var (user, payment, scenario) in testSuite)
                {
                    // Run verification
                    var verificationTasks = _verificationServices.Select(service => 
                        service.VerifyAsync(user));
                    var verificationResults = await Task.WhenAll(verificationTasks);

                    // Get score and decision
                    var score = _scorer.CalculateScore(verificationResults.ToList());
                    var decision = await _decisionEngine.ProcessPaymentRequestAsync(
                        payment, verificationResults.ToList());

                    results.Add(new
                    {
                        scenario = scenario,
                        score = score.FinalScore,
                        riskLevel = score.RiskLevel,
                        recommendation = score.Recommendation,
                        approved = decision.Approved,
                        allowedMethods = decision.AllowedMethods,
                        limits = decision.Limits,
                        verificationBreakdown = score.Breakdown,
                        riskFlags = verificationResults.SelectMany(r => r.RiskFlags).Distinct()
                    });
                }

                return Ok(new { 
                    testResults = results,
                    summary = new {
                        totalTests = results.Count,
                        approvalRate = results.Count(r => (bool)r.GetType().GetProperty("approved").GetValue(r)) / (double)results.Count,
                        averageScore = results.Average(r => (double)r.GetType().GetProperty("score").GetValue(r))
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("payment-decision")]
        public async Task<ActionResult<PaymentDecisionResponse>> ProcessPaymentDecision(
            [FromBody] PaymentDecisionRequest request)
        {
            try
            {
                var paymentRequest = MapToPaymentRequest(request);
                var verificationResults = request.VerificationResults ?? new List<VerificationResult>();

                var decision = await _decisionEngine.ProcessPaymentRequestAsync(
                    paymentRequest, verificationResults);

                var response = new PaymentDecisionResponse
                {
                    TransactionId = decision.AuditTrail.TransactionId,
                    Approved = decision.Approved,
                    AllowedMethods = decision.AllowedMethods.Select(m => m.ToString()).ToList(),
                    Limits = decision.Limits,
                    AdditionalVerificationRequired = decision.AdditionalVerificationRequired,
                    RiskLevel = decision.RiskLevel,
                    Score = decision.AuditTrail.Score,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("verification/{id}/status")]
        public async Task<ActionResult<VerificationStatusResponse>> GetVerificationStatus(string id)
        {
            try
            {
                // In a real implementation, this would query a database
                var response = new VerificationStatusResponse
                {
                    TransactionId = id,
                    Status = "completed",
                    Timestamp = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow.AddMinutes(-5)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("risk/assess")]
        public async Task<ActionResult<RiskAssessmentResponse>> AssessRisk([FromBody] RiskAssessmentRequest request)
        {
            try
            {
                // Simulate risk assessment
                var riskScore = CalculateRiskScore(request);
                
                var response = new RiskAssessmentResponse
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    RiskScore = riskScore,
                    RiskLevel = GetRiskLevel(riskScore),
                    RiskFactors = GetRiskFactors(request),
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("compliance/status")]
        public async Task<ActionResult<ComplianceStatusResponse>> GetComplianceStatus()
        {
            try
            {
                var response = new ComplianceStatusResponse
                {
                    BSACompliance = true,
                    AMLCompliance = true,
                    KYCCompliance = true,
                    FFIECCompliance = true,
                    NISTCompliance = true,
                    LastAudit = DateTime.UtcNow.AddDays(-30),
                    NextAudit = DateTime.UtcNow.AddDays(60)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private VerificationData MapToVerificationData(VerificationRequest request)
        {
            return new VerificationData
            {
                PersonalInfo = new PersonalInfo
                {
                    Name = request.PersonalInfo?.Name,
                    Email = request.PersonalInfo?.Email,
                    Phone = request.PersonalInfo?.Phone,
                    SSN = request.PersonalInfo?.SSN,
                    Address = request.PersonalInfo?.Address != null ? new Address
                    {
                        Street = request.PersonalInfo.Address.Street,
                        City = request.PersonalInfo.Address.City,
                        State = request.PersonalInfo.Address.State,
                        ZipCode = request.PersonalInfo.Address.ZipCode,
                        Country = request.PersonalInfo.Address.Country ?? "US"
                    } : null
                },
                Documents = request.Documents?.Select(d => new DocumentData
                {
                    Type = d.Type,
                    ImageData = d.ImageData,
                    ExtractedData = d.ExtractedData ?? new Dictionary<string, object>()
                }).ToList() ?? new List<DocumentData>(),
                Biometric = request.Biometric != null ? new BiometricData
                {
                    FaceImage = request.Biometric.FaceImage,
                    FingerprintData = request.Biometric.FingerprintData,
                    Features = request.Biometric.Features ?? new Dictionary<string, object>()
                } : null,
                DeviceInfo = request.DeviceInfo != null ? new DeviceInfo
                {
                    DeviceId = request.DeviceInfo.DeviceId,
                    UserAgent = request.DeviceInfo.UserAgent,
                    IPAddress = request.DeviceInfo.IPAddress,
                    Location = request.DeviceInfo.Location,
                    Fingerprint = request.DeviceInfo.Fingerprint ?? new Dictionary<string, object>()
                } : null
            };
        }

        private PaymentRequest MapToPaymentRequest(PaymentDecisionRequest request)
        {
            return new PaymentRequest
            {
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                RequestedMethod = Enum.Parse<PaymentMethod>(request.RequestedMethod),
                MerchantId = request.MerchantId,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };
        }

        private double CalculateRiskScore(RiskAssessmentRequest request)
        {
            var baseRisk = 20.0; // Low base risk

            // Simulate risk factors
            if (request.Amount > 10000) baseRisk += 30;
            if (request.Amount > 1000) baseRisk += 15;
            if (request.IsFirstTimeUser) baseRisk += 25;
            if (request.IsHighRiskLocation) baseRisk += 40;
            if (request.HasVelocityRisk) baseRisk += 35;

            return Math.Min(baseRisk, 100);
        }

        private string GetRiskLevel(double riskScore)
        {
            return riskScore switch
            {
                < 30 => "LOW",
                < 60 => "MEDIUM",
                < 85 => "HIGH",
                _ => "VERY_HIGH"
            };
        }

        private List<string> GetRiskFactors(RiskAssessmentRequest request)
        {
            var factors = new List<string>();

            if (request.Amount > 10000) factors.Add("high_value_transaction");
            if (request.IsFirstTimeUser) factors.Add("new_user");
            if (request.IsHighRiskLocation) factors.Add("high_risk_geography");
            if (request.HasVelocityRisk) factors.Add("velocity_risk");

            return factors;
        }
    }

    // Request/Response DTOs
    public class VerificationRequest
    {
        public PersonalInfoDto PersonalInfo { get; set; }
        public List<DocumentDto> Documents { get; set; }
        public BiometricDto Biometric { get; set; }
        public DeviceInfoDto DeviceInfo { get; set; }
    }

    public class PersonalInfoDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string SSN { get; set; }
        public AddressDto Address { get; set; }
    }

    public class AddressDto
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }

    public class DocumentDto
    {
        public string Type { get; set; }
        public byte[] ImageData { get; set; }
        public Dictionary<string, object> ExtractedData { get; set; }
    }

    public class BiometricDto
    {
        public byte[] FaceImage { get; set; }
        public byte[] FingerprintData { get; set; }
        public Dictionary<string, object> Features { get; set; }
    }

    public class DeviceInfoDto
    {
        public string DeviceId { get; set; }
        public string UserAgent { get; set; }
        public string IPAddress { get; set; }
        public string Location { get; set; }
        public Dictionary<string, object> Fingerprint { get; set; }
    }

    public class VerificationResponse
    {
        public string TransactionId { get; set; }
        public List<VerificationResult> Results { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    public class PaymentDecisionRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string RequestedMethod { get; set; }
        public string MerchantId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public List<VerificationResult> VerificationResults { get; set; }
    }

    public class PaymentDecisionResponse
    {
        public string TransactionId { get; set; }
        public bool Approved { get; set; }
        public List<string> AllowedMethods { get; set; }
        public TransactionLimits Limits { get; set; }
        public List<string> AdditionalVerificationRequired { get; set; }
        public string RiskLevel { get; set; }
        public ScoreResult Score { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VerificationStatusResponse
    {
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class RiskAssessmentRequest
    {
        public decimal Amount { get; set; }
        public bool IsFirstTimeUser { get; set; }
        public bool IsHighRiskLocation { get; set; }
        public bool HasVelocityRisk { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class RiskAssessmentResponse
    {
        public string TransactionId { get; set; }
        public double RiskScore { get; set; }
        public string RiskLevel { get; set; }
        public List<string> RiskFactors { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ComplianceStatusResponse
    {
        public bool BSACompliance { get; set; }
        public bool AMLCompliance { get; set; }
        public bool KYCCompliance { get; set; }
        public bool FFIECCompliance { get; set; }
        public bool NISTCompliance { get; set; }
        public DateTime LastAudit { get; set; }
        public DateTime NextAudit { get; set; }
    }

    public class TestDataRequest
    {
        public string Scenario { get; set; }
    }
}