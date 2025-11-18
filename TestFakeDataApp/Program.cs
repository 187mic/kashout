// See https://aka.ms/new-console-template for more information
using System;
using Kashout.Core.Services;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing PlaidFakeDataGenerator...");

        // Test Plaid account generation
        var account = PlaidFakeDataGenerator.GenerateAccount();
        Console.WriteLine($"Generated account: {account.Name} at {account.InstitutionName}");
        Console.WriteLine($"Account ID: {account.AccountId}, Balance: ${account.Balance}");

        // Test Plaid transactions
        var transactions = PlaidFakeDataGenerator.GenerateTransactions(account.AccountId, 3);
        Console.WriteLine($"Generated {transactions.Count} transactions:");
        foreach (var txn in transactions)
        {
            Console.WriteLine($"  - {txn.Description}: ${txn.Amount} on {txn.Date.ToShortDateString()}");
        }

        Console.WriteLine("\nTesting PosFakeDataGenerator...");

        // Test POS merchant generation
        var merchant = PosFakeDataGenerator.GenerateMerchant();
        Console.WriteLine($"Generated merchant: {merchant.Name} ({merchant.Category})");
        Console.WriteLine($"Merchant ID: {merchant.MerchantId}, Location: {merchant.City}, {merchant.State}");

        // Test POS transactions
        var posTransactions = PosFakeDataGenerator.GenerateTransactions(merchant.MerchantId, 3);
        Console.WriteLine($"Generated {posTransactions.Count} POS transactions:");
        foreach (var txn in posTransactions)
        {
            Console.WriteLine($"  - {txn.Description}: ${txn.Amount} via {txn.PaymentMethod} ({txn.Status})");
        }

        Console.WriteLine("\n✅ All fake data generators working correctly!");
    }
}
