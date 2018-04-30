using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class Title : TableEntity
    {
        public Title()
        {
            this.PartitionKey = "1";
            this.RowKey = "1";
        }
        public int numberOfTitle { get; set; }
        public string lastTitle { get; set; }
    }
}