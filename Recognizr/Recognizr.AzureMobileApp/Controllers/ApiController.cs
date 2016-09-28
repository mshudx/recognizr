using Microsoft.ProjectOxford.Vision;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Recognizr.AzureMobileApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
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

        public async Task<string> SubmitPhoto(string userName, string assignmentRowKey, HttpPostedFileBase photo)
        {
            // Save elapsed time now because later processes can take several seconds
            var submissionDate = DateTimeOffset.UtcNow;

            // Fetch Azure tables
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            var assignmentsTable = tableClient.GetTableReference("assignments");
            assignmentsTable.CreateIfNotExists();
            var highscoresTable = tableClient.GetTableReference("highscores");
            highscoresTable.CreateIfNotExists();

            // Prepare result variables
            string cognitiveServicesResults = "No photo submitted, or no assignment with this ID";
            bool isMatched = false;
            int secondsElapsed = 0;

            // Fish out the assignment this was submitted for
            var query = new TableQuery<Assignment>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(Assignment.PartitionKey), QueryComparisons.Equal, nameof(Assignment)),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(Assignment.RowKey), QueryComparisons.Equal, assignmentRowKey)));
            var executedQuery = assignmentsTable.ExecuteQuery(query);

            // If the assignment was found and there is a photo, perform check
            if (photo != null && executedQuery.Any())
            {
                var assignment = executedQuery.First();

                // Check uploaded file with MS Cognitive Services
                var client = new VisionServiceClient(ConfigurationManager.AppSettings["ComputerVisionApiKey"]);
                var photoStream = photo.InputStream;
                photoStream.Seek(0, SeekOrigin.Begin);
                var visionResults = await client.GetTagsAsync(photoStream);
                var tags = visionResults.Tags.Select(t => t.Name).ToList();

                cognitiveServicesResults = string.Join(", ", tags);
                isMatched = tags.Contains(assignment.AssignmentText);
                secondsElapsed = (int)Math.Round((submissionDate - assignment.TimeCreated).TotalSeconds);

                // If there is a match, save the highscore
                if (isMatched)
                {
                    var highscore = new Highscore();
                    highscore.TimeCreated = DateTimeOffset.UtcNow;
                    highscore.UserName = userName;
                    highscore.SecondsElapsed = secondsElapsed;
                    highscore.AssignmentText = assignment.AssignmentText;

                    var operation = TableOperation.Insert(highscore);
                    highscoresTable.Execute(operation);
                }
            }

            return JsonConvert.SerializeObject(new
            {
                cognitiveServicesResults = cognitiveServicesResults,
                isMatched = isMatched,
                secondsElapsed = secondsElapsed
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
            var results = executedQuery.OrderBy(h => h.SecondsElapsed).Take(10).ToList();

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