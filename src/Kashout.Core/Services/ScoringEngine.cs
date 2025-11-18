using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kashout.Core.Models;

namespace Kashout.Core.Services
{
    public class WeightedScorer
    {
        private readonly Dictionary<string, double> _weights = new Dictionary<string, double>
        {
            { "plaid_identity", 0.15 },
            { "stripe_identity", 0.15 },
            { "advanced_fraud_detection", 0.25 },
            { "ml_risk_scoring", 0.30 },
            { "webauthn", 0.10 },
            { "nist_aal", 0.05 }
        };

        public ScoreResult CalculateScore(List<VerificationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return new ScoreResult
                {
                    FinalScore = 0,
                    Recommendation = PaymentRecommendation.Decline,
                    RiskLevel = "HIGH"
                };
            }

            double totalScore = 0;
            double totalWeight = 0;
            var breakdown = new Dictionary<string, double>();

            foreach (var result in results)
            {
                if (_weights.TryGetValue(result.Provider, out double weight))
                {
                    var weightedScore = result.Score * weight * result.Confidence;
                    totalScore += weightedScore;
                    totalWeight += weight * result.Confidence;
                    breakdown[result.Provider] = weightedScore;
                }
            }

            var finalScore = totalWeight > 0 ? totalScore / totalWeight : 0;

            return new ScoreResult
            {
                FinalScore = finalScore,
                Breakdown = breakdown,
                Recommendation = GetRecommendation(finalScore),
                RiskLevel = GetRiskLevel(finalScore)
            };
        }

        private PaymentRecommendation GetRecommendation(double score)
        {
            return score switch
            {
                >= 90 => PaymentRecommendation.Approve,
                >= 75 => PaymentRecommendation.ApproveWithLimits,
                >= 45 => PaymentRecommendation.RequireAdditionalVerification,
                _ => PaymentRecommendation.Decline
            };
        }

        private string GetRiskLevel(double score)
        {
            return score switch
            {
                >= 90 => "LOW",
                >= 75 => "MEDIUM",
                >= 45 => "HIGH",
                _ => "VERY_HIGH"
            };
        }
    }

    public class PaymentDecisionEngine
    {
        private readonly WeightedScorer _scorer;
        private readonly IComplianceChecker _complianceChecker;

        public PaymentDecisionEngine(WeightedScorer scorer, IComplianceChecker complianceChecker)
        {
            _scorer = scorer;
            _complianceChecker = complianceChecker;
        }

        public async Task<PaymentDecision> ProcessPaymentRequestAsync(
            PaymentRequest paymentRequest,
            List<VerificationResult> verificationResults)
        {
            var score = _scorer.CalculateScore(verificationResults);
            var compliance = await _complianceChecker.ValidateAsync(paymentRequest);

            var decision = new PaymentDecision
            {
                Approved = ShouldApprove(score, compliance),
                AllowedMethods = DetermineAllowedMethods(score, paymentRequest),
                Limits = CalculateLimits(score, paymentRequest),
                AdditionalVerificationRequired = GetAdditionalVerificationRequirements(score),
                RiskLevel = score.RiskLevel,
                AuditTrail = new AuditTrail
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    VerificationResults = verificationResults,
                    Score = score,
                    UserId = "user_placeholder"
                }
            };

            decision.AuditTrail.Decision = decision;
            return decision;
        }

        private bool ShouldApprove(ScoreResult score, ComplianceResult compliance)
        {
            return score.FinalScore >= 45 && compliance.Passed;
        }

        private List<PaymentMethod> DetermineAllowedMethods(ScoreResult score, PaymentRequest payment)
        {
            var methods = new List<PaymentMethod>();

            switch (score.FinalScore)
            {
                case >= 90:
                    methods.AddRange(new[] { PaymentMethod.CreditCard, PaymentMethod.DebitCard, 
                                           PaymentMethod.BankTransfer, PaymentMethod.ACH });
                    break;
                case >= 75:
                    methods.AddRange(new[] { PaymentMethod.CreditCard, PaymentMethod.DebitCard, 
                                           PaymentMethod.BankTransferVerified });
                    break;
                case >= 60:
                    methods.AddRange(new[] { PaymentMethod.CreditCard, PaymentMethod.DebitCard });
                    break;
                case >= 45:
                    methods.AddRange(new[] { PaymentMethod.CreditCardLimited, PaymentMethod.DebitCardLimited });
                    break;
            }

            return methods;
        }

        private TransactionLimits CalculateLimits(ScoreResult score, PaymentRequest payment)
        {
            return score.FinalScore switch
            {
                >= 90 => new TransactionLimits
                {
                    MaxAmount = 10000m,
                    DailyLimit = 25000m,
                    MonthlyLimit = 100000m,
                    MaxTransactionsPerDay = 50
                },
                >= 75 => new TransactionLimits
                {
                    MaxAmount = 5000m,
                    DailyLimit = 10000m,
                    MonthlyLimit = 50000m,
                    MaxTransactionsPerDay = 25
                },
                >= 60 => new TransactionLimits
                {
                    MaxAmount = 2500m,
                    DailyLimit = 5000m,
                    MonthlyLimit = 25000m,
                    MaxTransactionsPerDay = 15
                },
                _ => new TransactionLimits
                {
                    MaxAmount = 500m,
                    DailyLimit = 1000m,
                    MonthlyLimit = 5000m,
                    MaxTransactionsPerDay = 5
                }
            };
        }

        private List<string> GetAdditionalVerificationRequirements(ScoreResult score)
        {
            var requirements = new List<string>();

            if (score.FinalScore < 75)
            {
                requirements.Add("phone_verification");
            }

            if (score.FinalScore < 60)
            {
                requirements.Add("email_verification");
                requirements.Add("document_upload");
            }

            if (score.FinalScore < 45)
            {
                requirements.Add("manual_review");
                requirements.Add("enhanced_due_diligence");
            }

            return requirements;
        }
    }

    public interface IComplianceChecker
    {
        Task<ComplianceResult> ValidateAsync(PaymentRequest request);
    }

    public class ComplianceChecker : IComplianceChecker
    {
        public async Task<ComplianceResult> ValidateAsync(PaymentRequest request)
        {
            var result = new ComplianceResult();
            
            // Simulate compliance checks
            await CheckBSACompliance(request, result);
            await CheckAMLScreening(request, result);
            await CheckKYCRequirements(request, result);
            await CheckFFIECGuidelines(request, result);
            await CheckNISTStandards(request, result);

            result.Passed = result.RequirementsFailed.Count == 0;

            return result;
        }

        private async Task CheckBSACompliance(PaymentRequest request, ComplianceResult result)
        {
            // Bank Secrecy Act compliance
            if (request.Amount > 10000m)
            {
                result.RequirementsMet.Add("CTR_reporting_threshold_check");
            }
            
            if (request.Amount > 3000m)
            {
                result.RequirementsMet.Add("suspicious_activity_monitoring");
            }

            result.ComplianceData["BSA_check_performed"] = true;
            await Task.Delay(1); // Simulate async operation
        }

        private async Task CheckAMLScreening(PaymentRequest request, ComplianceResult result)
        {
            // Anti-Money Laundering screening
            result.RequirementsMet.Add("sanctions_list_screening");
            result.RequirementsMet.Add("pep_screening");
            result.RequirementsMet.Add("adverse_media_check");

            result.ComplianceData["AML_screening_performed"] = true;
            await Task.Delay(1); // Simulate async operation
        }

        private async Task CheckKYCRequirements(PaymentRequest request, ComplianceResult result)
        {
            // Know Your Customer requirements
            result.RequirementsMet.Add("customer_identification");
            result.RequirementsMet.Add("beneficial_ownership");
            result.RequirementsMet.Add("ongoing_monitoring");

            result.ComplianceData["KYC_check_performed"] = true;
            await Task.Delay(1); // Simulate async operation
        }

        private async Task CheckFFIECGuidelines(PaymentRequest request, ComplianceResult result)
        {
            // Federal Financial Institutions Examination Council guidelines
            result.RequirementsMet.Add("risk_based_authentication");
            result.RequirementsMet.Add("transaction_monitoring");
            result.RequirementsMet.Add("audit_trail");

            result.ComplianceData["FFIEC_compliance_checked"] = true;
            await Task.Delay(1); // Simulate async operation
        }

        private async Task CheckNISTStandards(PaymentRequest request, ComplianceResult result)
        {
            // NIST SP 800-63 standards
            result.RequirementsMet.Add("identity_assurance_level");
            result.RequirementsMet.Add("authenticator_assurance_level");
            result.RequirementsMet.Add("federation_assurance_level");

            result.ComplianceData["NIST_standards_checked"] = true;
            await Task.Delay(1); // Simulate async operation
        }
    }
}