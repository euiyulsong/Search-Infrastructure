using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerLibrary
{
    public static class Storage
    {
        public static Dashboard dashboard = new Dashboard();
        public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
        public static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        public static CloudQueue linkQueue = queueClient.GetQueueReference("linkqueue");
        public static CloudQueue htmlQueue = queueClient.GetQueueReference("htmlqueue");
        public static CloudQueue commandQueue = queueClient.GetQueueReference("commandqueue");
        public static CloudQueue disallowedQueue = queueClient.GetQueueReference("disallowedqueue");

        public static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        public static CloudTable linkTable = tableClient.GetTableReference("linktable");
        public static CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");

        public static object CommandQueue { get; set; }

        public static void Initiate()
        {
            linkQueue.CreateIfNotExists();
            commandQueue.CreateIfNotExists();
            htmlQueue.CreateIfNotExists();
            disallowedQueue.CreateIfNotExists();
            dashboardTable.CreateIfNotExists();
            linkTable.CreateIfNotExists();
        }

        public static void Clear()
        {
            linkQueue.Clear();
            commandQueue.Clear();
            htmlQueue.Clear();
            disallowedQueue.Clear();
        }
    }
}
