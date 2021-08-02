using System;
using System.Linq;
using System.Threading.Tasks;
using IsraeliFinancialImporter;
using IsraeliFinancialImporter.Importers;
using Microsoft.Extensions.Configuration;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using YNAB.SDK;
using YNAB.SDK.Model;

namespace YnabExporter
{
    internal class Program
    {
        private static readonly DateTime FromInclusive = DateTime.Today.AddMonths(-1);
        private static readonly DateTime ToInclusive = DateTime.Today;

        private static void Main(string[] args)
        {
            new DriverManager().SetUpDriver(new ChromeConfig());

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false).Build();

            Parallel.ForEach(config.GetSection("ImportAccounts").GetChildren(), importAccount =>
            {
                using var importer = GetImporterByType(importAccount);
                var importedTransactions = importer.Import(FromInclusive, ToInclusive).ToList();
                var saveTransactions = importedTransactions.Select(transaction => new SaveTransaction(
                        importAccount.GetValue<Guid>("YnabAccount"),
                        transaction.OccuredAt,
                        GetMilliUnitAmount(transaction.Amount),
                        payeeName: transaction.Payee,
                        memo: transaction.Memo,
                        importId: $"{transaction.AccountId}::{transaction.Id}"))
                    .ToList();

                Console.WriteLine(
                    $"{importAccount.GetValue<string>("ImporterType")}: sending {saveTransactions.Count} transactions to YNAB");
                var ynabApi = new API(config.GetValue<string>("YnabAccessToken"));
                ynabApi.Transactions.CreateTransaction(config.GetValue<string>("YnabBudget"),
                    new SaveTransactionsWrapper(transactions: saveTransactions));
            });
        }

        private static IFinancialImporter GetImporterByType(IConfigurationSection importerConfigurationSection)
        {
            var type = importerConfigurationSection.GetValue<string>("ImporterType");
            switch (type)
            {
                case "UnionBank":
                    return new UnionBankImporter(importerConfigurationSection.GetValue<string>("Username"),
                        importerConfigurationSection.GetValue<string>("Password"));
                case "Max":
                    return new MaxImporter(importerConfigurationSection.GetValue<string>("Username"),
                        importerConfigurationSection.GetValue<string>("Password"));
                case "Cal":
                    return new CalImporter(importerConfigurationSection.GetValue<string>("Username"),
                        importerConfigurationSection.GetValue<string>("Password"));
                default:
                    throw new Exception($"Unknown importer type: {type}");
            }
        }

        private static long GetMilliUnitAmount(decimal amount)
        {
            return Convert.ToInt64(decimal.Truncate(amount * 1000));
        }
    }
}