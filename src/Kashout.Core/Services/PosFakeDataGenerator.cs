using System;
using System.Collections.Generic;
using System.Linq;

namespace Kashout.Core.Services
{
    public class PosFakeDataGenerator
    {
        private static readonly Random _random = new Random();
        private static readonly string[] _merchantNames = {
            "Starbucks", "Whole Foods Market", "Walmart", "Target", "Amazon", "Costco",
            "McDonald's", "Chipotle", "Uber", "DoorDash", "Shell", "Chevron",
            "CVS Pharmacy", "Walgreens", "Home Depot", "Lowe's", "Best Buy"
        };

        private static readonly string[] _merchantCategories = {
            "Food & Beverage", "Grocery", "Retail", "E-commerce", "Gas Station",
            "Pharmacy", "Home Improvement", "Electronics", "Ride Sharing", "Delivery"
        };

        private static readonly string[] _paymentMethods = {
            "credit_card", "debit_card", "cash", "digital_wallet", "contactless"
        };

        private static readonly string[] _transactionStatuses = {
            "approved", "declined", "pending", "refunded", "voided"
        };

        public class PosMerchant
        {
            public string MerchantId { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
            public string Phone { get; set; }
            public string TaxId { get; set; }
            public bool IsActive { get; set; }
        }

        public class PosTransaction
        {
            public string TransactionId { get; set; }
            public string MerchantId { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string PaymentMethod { get; set; }
            public string Status { get; set; }
            public DateTime Timestamp { get; set; }
            public string Description { get; set; }
            public string AuthorizationCode { get; set; }
            public string CardLastFour { get; set; }
            public string CardType { get; set; }
            public bool IsRefund { get; set; }
            public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        }

        public class PosSettlement
        {
            public string SettlementId { get; set; }
            public string MerchantId { get; set; }
            public decimal TotalAmount { get; set; }
            public string Currency { get; set; }
            public DateTime SettlementDate { get; set; }
            public int TransactionCount { get; set; }
            public decimal Fee { get; set; }
            public decimal NetAmount { get; set; }
            public string Status { get; set; }
        }

        public class PosRefund
        {
            public string RefundId { get; set; }
            public string OriginalTransactionId { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string Reason { get; set; }
            public DateTime Timestamp { get; set; }
            public string Status { get; set; }
        }

        public static PosMerchant GenerateMerchant()
        {
            var name = _merchantNames[_random.Next(_merchantNames.Length)];
            var category = _merchantCategories[_random.Next(_merchantCategories.Length)];

            return new PosMerchant
            {
                MerchantId = $"mer_{Guid.NewGuid().ToString().Replace("-", "")}",
                Name = name,
                Category = category,
                Address = $"{_random.Next(100, 9999)} Commerce St",
                City = "San Francisco",
                State = "CA",
                ZipCode = "94105",
                Phone = $"+1-555-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}",
                TaxId = $"TAX{_random.Next(100000000, 999999999)}",
                IsActive = _random.NextDouble() > 0.05 // 95% active
            };
        }

        public static List<PosMerchant> GenerateMerchants(int count = 5)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateMerchant())
                .ToList();
        }

        public static PosTransaction GenerateTransaction(string merchantId = null)
        {
            var amount = Math.Round((decimal)(_random.NextDouble() * 200 + 1), 2); // $1 to $201
            var paymentMethod = _paymentMethods[_random.Next(_paymentMethods.Length)];
            var status = _transactionStatuses[_random.Next(_transactionStatuses.Length)];
            var cardTypes = new[] { "visa", "mastercard", "amex", "discover" };
            var cardType = cardTypes[_random.Next(cardTypes.Length)];

            return new PosTransaction
            {
                TransactionId = $"txn_{Guid.NewGuid().ToString().Replace("-", "")}",
                MerchantId = merchantId ?? $"mer_{Guid.NewGuid().ToString().Replace("-", "")}",
                Amount = amount,
                Currency = "USD",
                PaymentMethod = paymentMethod,
                Status = status,
                Timestamp = DateTime.Now.AddMinutes(-_random.Next(0, 1440)), // Last 24 hours
                Description = $"Purchase at {GenerateMerchant().Name}",
                AuthorizationCode = _random.Next(100000, 999999).ToString(),
                CardLastFour = _random.Next(1000, 9999).ToString(),
                CardType = cardType,
                IsRefund = false,
                Metadata = new Dictionary<string, object>
                {
                    ["terminal_id"] = $"term_{_random.Next(1000, 9999)}",
                    ["location_id"] = $"loc_{_random.Next(100, 999)}",
                    ["employee_id"] = $"emp_{_random.Next(1000, 9999)}"
                }
            };
        }

        public static List<PosTransaction> GenerateTransactions(string merchantId = null, int count = 20)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateTransaction(merchantId))
                .OrderByDescending(t => t.Timestamp)
                .ToList();
        }

        public static PosRefund GenerateRefund(string originalTransactionId)
        {
            var refundReasons = new[] {
                "customer_request", "damaged_goods", "duplicate_charge", "service_issue", "wrong_item"
            };

            return new PosRefund
            {
                RefundId = $"ref_{Guid.NewGuid().ToString().Replace("-", "")}",
                OriginalTransactionId = originalTransactionId,
                Amount = Math.Round((decimal)(_random.NextDouble() * 100 + 1), 2),
                Currency = "USD",
                Reason = refundReasons[_random.Next(refundReasons.Length)],
                Timestamp = DateTime.Now.AddHours(-_random.Next(1, 24)),
                Status = "completed"
            };
        }

        public static PosSettlement GenerateSettlement(string merchantId, List<PosTransaction> transactions)
        {
            var approvedTransactions = transactions.Where(t => t.Status == "approved").ToList();
            var totalAmount = approvedTransactions.Sum(t => t.Amount);
            var fee = Math.Round(totalAmount * 0.029m + approvedTransactions.Count * 0.30m, 2); // 2.9% + $0.30 per transaction
            var netAmount = totalAmount - fee;

            return new PosSettlement
            {
                SettlementId = $"set_{Guid.NewGuid().ToString().Replace("-", "")}",
                MerchantId = merchantId,
                TotalAmount = totalAmount,
                Currency = "USD",
                SettlementDate = DateTime.Now.AddDays(-1),
                TransactionCount = approvedTransactions.Count,
                Fee = fee,
                NetAmount = netAmount,
                Status = "completed"
            };
        }

        public static List<PosSettlement> GenerateSettlements(List<PosMerchant> merchants, int days = 7)
        {
            var settlements = new List<PosSettlement>();

            foreach (var merchant in merchants)
            {
                for (int i = 0; i < days; i++)
                {
                    var transactions = GenerateTransactions(merchant.MerchantId, _random.Next(5, 50));
                    var settlement = GenerateSettlement(merchant.MerchantId, transactions);
                    settlement.SettlementDate = DateTime.Now.AddDays(-i);
                    settlements.Add(settlement);
                }
            }

            return settlements.OrderByDescending(s => s.SettlementDate).ToList();
        }

        public static Dictionary<string, object> GeneratePosReport(string merchantId, DateTime startDate, DateTime endDate)
        {
            var transactions = GenerateTransactions(merchantId, _random.Next(50, 200))
                .Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
                .ToList();

            var totalVolume = transactions.Where(t => t.Status == "approved").Sum(t => t.Amount);
            var transactionCount = transactions.Count(t => t.Status == "approved");
            var averageTicket = transactionCount > 0 ? totalVolume / transactionCount : 0;
            var refundAmount = transactions.Where(t => t.IsRefund).Sum(t => t.Amount);

            return new Dictionary<string, object>
            {
                ["merchant_id"] = merchantId,
                ["period_start"] = startDate,
                ["period_end"] = endDate,
                ["total_volume"] = totalVolume,
                ["transaction_count"] = transactionCount,
                ["average_ticket"] = Math.Round(averageTicket, 2),
                ["refund_amount"] = refundAmount,
                ["net_volume"] = totalVolume - refundAmount,
                ["transactions"] = transactions.Select(t => new Dictionary<string, object>
                {
                    ["id"] = t.TransactionId,
                    ["amount"] = t.Amount,
                    ["status"] = t.Status,
                    ["timestamp"] = t.Timestamp,
                    ["payment_method"] = t.PaymentMethod
                }).ToList()
            };
        }
    }
}