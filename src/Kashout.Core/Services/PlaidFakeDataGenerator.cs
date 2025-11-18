using System;
using System.Collections.Generic;
using System.Linq;

namespace Kashout.Core.Services
{
    public class PlaidFakeDataGenerator
    {
        private static readonly Random _random = new Random();
        private static readonly string[] _bankNames = {
            "Chase", "Bank of America", "Wells Fargo", "Citibank", "Capital One",
            "US Bank", "PNC Bank", "TD Bank", "BB&T", "SunTrust"
        };

        private static readonly string[] _accountTypes = { "checking", "savings", "credit" };
        private static readonly string[] _accountSubtypes = { "checking", "savings", "credit card" };

        public class PlaidAccount
        {
            public string AccountId { get; set; }
            public string Name { get; set; }
            public string OfficialName { get; set; }
            public string Type { get; set; }
            public string Subtype { get; set; }
            public string Mask { get; set; }
            public decimal Balance { get; set; }
            public string Currency { get; set; }
            public string InstitutionId { get; set; }
            public string InstitutionName { get; set; }
        }

        public class PlaidTransaction
        {
            public string TransactionId { get; set; }
            public string AccountId { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
            public string MerchantName { get; set; }
            public string Category { get; set; }
            public List<string> CategoryDetailed { get; set; } = new List<string>();
            public bool Pending { get; set; }
        }

        public class PlaidLinkToken
        {
            public string LinkToken { get; set; }
            public DateTime Expiration { get; set; }
            public string RequestId { get; set; }
        }

        public class PlaidAccessToken
        {
            public string AccessToken { get; set; }
            public string ItemId { get; set; }
            public string RequestId { get; set; }
        }

        public static PlaidAccount GenerateAccount(string userName = null)
        {
            var bankName = _bankNames[_random.Next(_bankNames.Length)];
            var accountType = _accountTypes[_random.Next(_accountTypes.Length)];
            var accountSubtype = _accountSubtypes[_random.Next(_accountSubtypes.Length)];

            return new PlaidAccount
            {
                AccountId = $"acc_{Guid.NewGuid().ToString().Replace("-", "")}",
                Name = $"{bankName} {accountSubtype}",
                OfficialName = $"{bankName} {accountSubtype} Account",
                Type = accountType,
                Subtype = accountSubtype,
                Mask = _random.Next(1000, 9999).ToString(),
                Balance = Math.Round((decimal)(_random.NextDouble() * 10000 + 100), 2),
                Currency = "USD",
                InstitutionId = $"ins_{bankName.ToLower().Replace(" ", "_")}",
                InstitutionName = bankName
            };
        }

        public static List<PlaidAccount> GenerateAccounts(int count = 3, string userName = null)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateAccount(userName))
                .ToList();
        }

        public static PlaidTransaction GenerateTransaction(string accountId)
        {
            var descriptions = new[]
            {
                "Grocery Store Purchase", "Gas Station", "Restaurant Payment", "Online Shopping",
                "Utility Bill Payment", "ATM Withdrawal", "Direct Deposit", "Transfer",
                "Subscription Service", "Medical Payment", "Entertainment", "Transportation"
            };

            var categories = new[]
            {
                "Food and Drink", "Transportation", "Shops", "Service", "Healthcare",
                "Entertainment", "Bills and Utilities", "Transfer", "Income"
            };

            var amount = Math.Round((decimal)(_random.NextDouble() * 500 - 250), 2); // -250 to +250
            var description = descriptions[_random.Next(descriptions.Length)];
            var category = categories[_random.Next(categories.Length)];

            return new PlaidTransaction
            {
                TransactionId = $"txn_{Guid.NewGuid().ToString().Replace("-", "")}",
                AccountId = accountId,
                Amount = amount,
                Currency = "USD",
                Date = DateTime.Now.AddDays(-_random.Next(0, 90)),
                Description = description,
                MerchantName = description.Contains(" ") ? description.Split(' ')[0] : description,
                Category = category,
                CategoryDetailed = new List<string> { category },
                Pending = _random.NextDouble() < 0.1 // 10% chance of being pending
            };
        }

        public static List<PlaidTransaction> GenerateTransactions(string accountId, int count = 10)
        {
            return Enumerable.Range(0, count)
                .Select(_ => GenerateTransaction(accountId))
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public static PlaidLinkToken GenerateLinkToken()
        {
            return new PlaidLinkToken
            {
                LinkToken = $"link-token-{Guid.NewGuid().ToString()}",
                Expiration = DateTime.Now.AddHours(4),
                RequestId = $"req_{Guid.NewGuid().ToString().Replace("-", "")}"
            };
        }

        public static PlaidAccessToken GenerateAccessToken()
        {
            return new PlaidAccessToken
            {
                AccessToken = $"access-token-{Guid.NewGuid().ToString()}",
                ItemId = $"item_{Guid.NewGuid().ToString().Replace("-", "")}",
                RequestId = $"req_{Guid.NewGuid().ToString().Replace("-", "")}"
            };
        }

        public static Dictionary<string, object> GenerateIdentityMatch(string userName, string userEmail)
        {
            var firstName = userName?.Split(' ')[0] ?? "John";
            var lastName = userName?.Split(' ').Last() ?? "Doe";

            return new Dictionary<string, object>
            {
                ["names"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["first_name"] = firstName,
                        ["last_name"] = lastName,
                        ["middle_name"] = "",
                        ["suffix"] = ""
                    }
                },
                ["addresses"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["street"] = $"{_random.Next(100, 9999)} Main St",
                        ["city"] = "San Francisco",
                        ["region"] = "CA",
                        ["postal_code"] = "94105",
                        ["country"] = "US"
                    }
                },
                ["emails"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["email"] = userEmail,
                        ["primary"] = true,
                        ["type"] = "primary"
                    }
                },
                ["phone_numbers"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["phone_number"] = $"+1-555-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}",
                        ["primary"] = true,
                        ["type"] = "mobile"
                    }
                }
            };
        }
    }
}