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
            var message = "New Recognizr challenge!";

            var notificationHub = NotificationHubClient.CreateClientFromConnectionString(ConfigurationManager.AppSettings["NotificationHubConnectionString"], "recognizr");

            int windowsCount = -2;
            try
            {
                var windowsNotification = string.Format("<toast><visual><binding template=\"ToastText01\"><text id=\"1\">{0}</text></binding></visual></toast>", message);
                var windowsNotificationResult = await notificationHub.SendWindowsNativeNotificationAsync(windowsNotification);
                windowsCount = windowsNotificationResult?.Results?.Count ?? -1;
            }
            catch { }

            int androidCount = -2;
            try
            {
                var androidNotification = string.Format("{ \"data\" : {\"message\":\"{0}\"}}", message);
                var androidNotificationResult = await notificationHub.SendGcmNativeNotificationAsync(androidNotification);
                androidCount = androidNotificationResult?.Results?.Count ?? -1;
            }
            catch { }

            ViewBag.Message = string.Format("Assignment has been successfully created, and a push notification was sent out to {0} Windows and {1} Android users!", windowsCount, androidCount);
            return View();
        }
    }
}