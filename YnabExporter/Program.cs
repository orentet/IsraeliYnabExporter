using System;
using System.Linq;
using System.Threading.Tasks;
using IsraeliFinancialImporter;
using IsraeliFinancialImporter.Importers;
using Microsoft.Extensions.Configuration;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using YNAB.Rest;
using YnabExporter.Utils;

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

            var ynabApi = ApiClientFactory.Create(config.GetValue<string>("YnabAccessToken"));
            var budgetId = config.GetValue<string>("YnabBudget");

            Parallel.ForEach(config.GetSection("ImportAccounts").GetChildren(), async importAccount =>
            {
                await Retry.Do(async () =>
                {
                    using var importer = GetImporterByType(importAccount);
                    var importedTransactions = importer.Import(FromInclusive, ToInclusive).ToList();
                    var ynabTransactions = importedTransactions.Select(transaction => new Transaction
                        {
                            AccountId = importAccount.GetValue<string>("YnabAccount"),
                            Date = transaction.OccuredAt,
                            Amount = GetMilliUnitAmount(transaction.Amount),
                            PayeeName = transaction.Payee,
                            Memo = transaction.Memo,
                            ImportId = $"{transaction.AccountId}::{transaction.Id}"
                        })
                        .ToList();

                    Console.WriteLine(
                        $"{importAccount.GetValue<string>("ImporterType")}: sending {ynabTransactions.Count} transactions to YNAB");
                    await ynabApi.PostBulkTransactions(budgetId,
                        new PostBulkTransactions { Transactions = ynabTransactions });
                }, TimeSpan.FromSeconds(30), 10);
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

        private static int GetMilliUnitAmount(decimal amount)
        {
            return Convert.ToInt32(decimal.Truncate(amount * 1000));
        }
    }
}