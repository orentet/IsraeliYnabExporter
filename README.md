# IsraeliYnabExporter
This is an Israeli YNAB exporter.
uses [israeli-financial-importer](https://github.com/orentet/israeli-financial-importer)

# Usage
in the **appsettings.json**, specify the following:
* **YnabAccessToken**: YNAB access token, any YNAB user can get it following these instructions: https://api.youneedabudget.com/#personal-access-tokens
* **YnabBudget**: The UUID of your YNAB budget. when entering the account - it is part of the URL.
* **ImportAccounts**: a list of objects for each importer. in the following format:
```
{
  "YnabAccount": "663503eb-6feb-4d83-b834-6c94f617e8fb",
  "ImporterType": "UnionBank",
  "Username": "",
  "Password": ""
}
```
* **YnabAccount**: the UUID of the account you want to import the transactions to.
* **ImporterType**: the type of importer you with to use. for a complete list, visit the [israeli-financial-importer project](https://github.com/orentet/israeli-financial-importer)
