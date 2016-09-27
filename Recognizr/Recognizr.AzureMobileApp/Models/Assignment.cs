using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Recognizr.AzureMobileApp.Models
{
    public class Assignment : TableEntity
    {
        public Assignment()
        {
            // Because the object is derived from TableEntity,
            // it automatically comes with a PartitionKey and a RowKey field.
            PartitionKey = nameof(Assignment); // put everything in one partition for easy querying
            RowKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// At what time was this task created?
        /// </summary>
        public DateTimeOffset TimeCreated { get; set; }

        /// <summary>
        /// What is the object's name that the user has to photograph?
        /// </summary>
        public string AssignmentText { get; set; }
    }
}