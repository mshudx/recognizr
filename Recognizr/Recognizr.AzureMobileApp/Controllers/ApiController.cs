using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Recognizr.AzureMobileApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Recognizr.AzureMobileApp.Controllers
{
    public class ApiController : Controller
    {
        public string GetLatestAssignment()
        {
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference("assignments");
            table.CreateIfNotExists();

            var query = new TableQuery<Assignment>()
                .Where(TableQuery.GenerateFilterCondition(nameof(Assignment.PartitionKey), QueryComparisons.Equal, nameof(Assignment)));
            var executedQuery = table.ExecuteQuery(query);
            var results = executedQuery.OrderByDescending(a => a.TimeCreated).Take(1);

            if (results.Any())
            {
                return JsonConvert.SerializeObject(results.First());
            }
            else
            {
                return null;
            }
        }
    }
}