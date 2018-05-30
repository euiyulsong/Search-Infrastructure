using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using WebCrawlerLibrary;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService2 : System.Web.Services.WebService
    {
        private static HybridTrie trie = new HybridTrie();
        static bool found = false;

        public WebService2()
        {
            if (!found)
            {
                Build();
                found = true;
            }
        }

        [WebMethod]
        public void Download()
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("yorih");

                if (container.Exists())
                {
                    foreach (IListBlobItem item in container.ListBlobs(null, false))
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            blob.DownloadToFile(System.IO.Path.GetTempPath() + "//wiki.txt", System.IO.FileMode.Create);
                        }
                    }
                }
            }
            catch (IOException e)
            {
            }
        }

        [WebMethod]
        public void Build()
        {
            try
            {
                StreamReader file = new StreamReader(System.IO.Path.GetTempPath() + "//wiki.txt");
                PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
                Regex regex = new Regex(@"^[a-zA-Z ]+$");
                file.ReadLine();

                while (!file.EndOfStream && memProcess.NextValue() > 40)
                {
                    string temp = file.ReadLine();
                    temp = temp.TrimEnd().Replace("_", " ");
                    if (regex.IsMatch(temp) && temp.ToLower()[0] <= 'c')
                    {
                        trie.Add(temp.ToLower());
                    }
                }
            }
            catch (IOException e)
            {
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Search(string word)
        {
            if (!found)
            {
                Download();
                Build();
                found = true;
            }

            List<string> list = trie.Search(word);

            return new JavaScriptSerializer().Serialize(list);
        }

    }
}
