using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kashout.Core.Models
{
    public interface IVerificationService
    {
        string Provider { get; }
        double Weight { get; }
        Task<VerificationResult> VerifyAsync(VerificationData data);
    }

    public class VerificationData
    {
        public PersonalInfo PersonalInfo { get; set; }
        public List<DocumentData> Documents { get; set; } = new List<DocumentData>();
        public BiometricData Biometric { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
    }

    public class PersonalInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Address Address { get; set; }
        public string SSN { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }

    public class DocumentData
    {
        public string Type { get; set; }
        public byte[] ImageData { get; set; }
        public Dictionary<string, object> ExtractedData { get; set; } = new Dictionary<string, object>();
    }

    public class BiometricData
    {
        public byte[] FaceImage { get; set; }
        public byte[] FingerprintData { get; set; }
        public Dictionary<string, object> Features { get; set; } = new Dictionary<string, object>();
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string UserAgent { get; set; }
        public string IPAddress { get; set; }
        public string Location { get; set; }
        public Dictionary<string, object> Fingerprint { get; set; } = new Dictionary<string, object>();
    }

    public class VerificationResult
    {
        public string Provider { get; set; }
        public double Score { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public List<string> RiskFlags { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ScoreResult
    {
        public double FinalScore { get; set; }
        public Dictionary<string, double> Breakdown { get; set; } = new Dictionary<string, double>();
        public PaymentRecommendation Recommendation { get; set; }
        public string RiskLevel { get; set; }
    }

    public enum PaymentRecommendation
    {
        Approve,
        ApproveWithLimits,
        RequireAdditionalVerification,
        Decline
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        BankTransfer,
        ACH,
        CreditCardLimited,
        DebitCardLimited,
        BankTransferVerified
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod RequestedMethod { get; set; }
        public string MerchantId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class PaymentDecision
    {
        public bool Approved { get; set; }
        public List<PaymentMethod> AllowedMethods { get; set; } = new List<PaymentMethod>();
        public TransactionLimits Limits { get; set; }
        public List<string> AdditionalVerificationRequired { get; set; } = new List<string>();
        public string RiskLevel { get; set; }
        public AuditTrail AuditTrail { get; set; }
    }

    public class TransactionLimits
    {
        public decimal MaxAmount { get; set; }
        public decimal DailyLimit { get; set; }
        public decimal MonthlyLimit { get; set; }
        public int MaxTransactionsPerDay { get; set; }
    }

    public class AuditTrail
    {
        public string TransactionId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VerificationResult> VerificationResults { get; set; } = new List<VerificationResult>();
        public ScoreResult Score { get; set; }
        public PaymentDecision Decision { get; set; }
        public string UserId { get; set; }
    }

    public class ComplianceResult
    {
        public bool Passed { get; set; }
        public List<string> RequirementsMet { get; set; } = new List<string>();
        public List<string> RequirementsFailed { get; set; } = new List<string>();
        public Dictionary<string, object> ComplianceData { get; set; } = new Dictionary<string, object>();
    }

    public interface IComplianceChecker
    {
        Task<ComplianceResult> ValidateAsync(PaymentRequest request);
    }

    // Request/Response Models for API
    public class VerificationRequest
    {
        public PersonalInfo PersonalInfo { get; set; }
        public List<DocumentData> Documents { get; set; } = new List<DocumentData>();
        public BiometricData Biometric { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
    }

    public class VerificationResponse
    {
        public string TransactionId { get; set; }
        public List<VerificationResult> Results { get; set; } = new List<VerificationResult>();
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    public class PaymentDecisionRequest
    {
        public VerificationRequest VerificationData { get; set; }
        public PaymentRequest PaymentRequest { get; set; }
    }

    public class RiskAssessmentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public bool IsFirstTimeUser { get; set; }
        public bool IsHighRiskLocation { get; set; }
        public bool HasVelocityRisk { get; set; }
        public Dictionary<string, object> AdditionalFactors { get; set; } = new Dictionary<string, object>();
    }
}