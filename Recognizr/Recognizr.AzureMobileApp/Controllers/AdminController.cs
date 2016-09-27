using Microsoft.Azure.NotificationHubs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Recognizr.AzureMobileApp.Models;
using Recognizr.AzureMobileApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Recognizr.AzureMobileApp.Controllers
{
    public class AdminController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(AdminViewModel m)
        {
            if (string.IsNullOrWhiteSpace(m.AssignmentText))
            {
                ViewBag.Message = "Please specify an assignment. Example: apple";
                return View();
            }

            // Insert assignment record into table
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference("assignments");
            table.CreateIfNotExists();

            var assignment = new Assignment();
            assignment.TimeCreated = DateTimeOffset.UtcNow;
            assignment.AssignmentText = m.AssignmentText;

            var operation = TableOperation.Insert(assignment);
            table.Execute(operation);

            // Send out push notification to tell clients
            var assignmentId = assignment.PartitionKey; // Generated as an unique Guid so RowKey is not necessary
            var message = "New Recognizr challenge!";

            var notificationHub = NotificationHubClient.CreateClientFromConnectionString("NotificationHubConnectionString", "recognizr");

            var windowsNotification = string.Format("<toast launch==\"{0}\"><visual><binding template=\"ToastText01\"><text id=\"1\">{1}</text></binding></visual></toast>", assignmentId, message);
            var windowsNotificationResult = await notificationHub.SendWindowsNativeNotificationAsync(windowsNotification);
            var windowsCount = windowsNotificationResult?.Results?.Count ?? 0;

            var androidNotification = string.Format("{\"data\":{\"assignmentId\":\"{0}\", \"message\":\"{1}\"}}", assignmentId, message);
            var androidNotificationResult = await notificationHub.SendGcmNativeNotificationAsync(androidNotification);
            var androidCount = androidNotificationResult?.Results?.Count ?? 0;

            ViewBag.Message = string.Format("Assignment has been successfully created, and a push notification was sent out to {0} Windows and {1} Android users!", windowsCount, androidCount);
            return View();
        }
    }
}