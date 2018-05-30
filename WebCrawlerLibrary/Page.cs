using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawlerLibrary
{
    public class Page : TableEntity
    {
        public string path;
        public string title;
        private string pubDate;
        public string Path { get => path; set => path = value; }
        public string Title { get => title; set => title = value; }
        public string PubDate { get => pubDate; set => pubDate = value; }

        public Page() { }

        public Page(string partitionKey, string rowKey, string path, DateTime pubDate)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Path = path;
            this.Title = rowKey;
            this.PubDate = pubDate.ToString("yyyy-MM-dd");
        }
    }
}
