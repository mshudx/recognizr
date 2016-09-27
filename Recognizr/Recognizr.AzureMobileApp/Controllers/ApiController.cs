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

        public string SubmitPhoto(string userName, string assignmentRowKey)
        {
            var submissionDate = DateTimeOffset.UtcNow; // Save this now because later processes can take several seconds

            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            var assignmentsTable = tableClient.GetTableReference("assignments");
            assignmentsTable.CreateIfNotExists();
            var highscoresTable = tableClient.GetTableReference("highscores");
            highscoresTable.CreateIfNotExists();

            bool isMatched = true;
            int secondsElapsed = 0;
            string cognitiveServicesResults = "apple, orange, bowl, 2 smiling people";

            // Check uploaded file with MS Cognitive Services
            // TODO
            // Send back tags that MSCS found for fun!

            if (isMatched)
            {
                // Fish out the assignment this was submitted for
                var query = new TableQuery<Assignment>()
                    .Where(TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition(nameof(Assignment.PartitionKey), QueryComparisons.Equal, nameof(Assignment)),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition(nameof(Assignment.RowKey), QueryComparisons.Equal, assignmentRowKey)));
                var executedQuery = assignmentsTable.ExecuteQuery(query);

                // If we found the assignment, calculate elapsed seconds and save the record
                if (executedQuery.Any())
                {
                    var assignment = executedQuery.First();

                    var highscore = new Highscore();
                    highscore.TimeCreated = DateTimeOffset.UtcNow;
                    highscore.UserName = userName;
                    secondsElapsed = (int)Math.Round((submissionDate - assignment.TimeCreated).TotalSeconds);
                    highscore.SecondsElapsed = secondsElapsed;
                    highscore.AssignmentText = assignment.AssignmentText;

                    var operation = TableOperation.Insert(highscore);
                    highscoresTable.Execute(operation);
                }
            }

            return JsonConvert.SerializeObject(new
            {
                isMatched = isMatched,
                secondsElapsed = secondsElapsed,
                cognitiveServicesResults = cognitiveServicesResults
            });
        }

        public string GetHighscores()
        {
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference("highscores");
            table.CreateIfNotExists();

            var query = new TableQuery<Highscore>();
            var executedQuery = table.ExecuteQuery(query);
            var results = executedQuery.OrderByDescending(h => h.SecondsElapsed).Take(10).ToList();

            if (results.Any())
            {
                return JsonConvert.SerializeObject(results);
            }
            else
            {
                return null;
            }
        }
    }
}