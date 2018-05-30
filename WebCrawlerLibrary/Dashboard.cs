using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerLibrary
{
    public class Dashboard : TableEntity
    {
        public string state;
        public double cpu;
        public double ram;
        public int countUrl; 
        public string lastUrl; 
        public int sizeQueue; 
        public int sizeIndex; 
        public string error;
        public string errorUrl;
        public Dashboard()
        {
            this.PartitionKey = "Dashboard";
            this.RowKey = "Dashboard";
        }
        public string State { get => state; set => state = value; }
        public double Cpu { get => cpu; set => cpu = value; }
        public double Ram { get => ram; set => ram = value; }
        public int CountUrl { get => countUrl; set => countUrl = value; }
        public int SizeQueue { get => sizeQueue; set => sizeQueue = value; }
        public int SizeIndex { get => sizeIndex; set => sizeIndex = value; }
        public string Error { get => error; set => error = value; }
        public string ErrorUrl { get => errorUrl; set => errorUrl = value; }
        public string LastUrl { get => lastUrl; set => lastUrl = value; }
    }
}
