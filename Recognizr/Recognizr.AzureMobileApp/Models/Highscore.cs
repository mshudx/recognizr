using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recognizr.AzureMobileApp.Models
{
    public class Highscore : TableEntity
    {
        public Highscore()
        {
            // Because the object is derived from TableEntity,
            // it automatically comes with a PartitionKey and a RowKey field.
            PartitionKey = Guid.NewGuid().ToString();
            RowKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// At what time was this score achieved?
        /// </summary>
        public DateTimeOffset TimeCreated { get; set; }

        /// <summary>
        /// User who achieved the score
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// What was the task that the user solved?
        /// </summary>
        public string AssignmentText { get; set; }

        /// <summary>
        /// How long did it take for the user to complete the task?
        /// </summary>
        public int SecondsElapsed { get; set; }
    }
}
