using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml.Serialization;
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
    public class WebService1 : System.Web.Services.WebService
    {
        private static Crawler crawler = new Crawler();
        private static Dictionary<string, int> count = new Dictionary<string, int>();
        private static Dictionary<string, List<Page>> cache = new Dictionary<string, List<Page>>();

        static bool start = false;
        public WebService1()
        {
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Dashboard()
        {
            //TableOperation retrieveOperation = TableOperation.Retrieve<Dashboard>("Dashboard", "Dashboard");
            //TableResult query = Storage.dashboardTable.Execute(retrieveOperation);
            List<Dashboard> list = new List<Dashboard>();
            var querytest = Storage.dashboardTable.CreateQuery<Dashboard>()
            .Where(x => x.PartitionKey == "Dashboard")
                .ToList();

            foreach (Dashboard url in querytest)
            {
                list.Add(url);
            }
            return new JavaScriptSerializer().Serialize(list);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StartCrawling()
        {
            if (!start)
            {
                Storage.Clear();
                Storage.Initiate();
                start = true;
            }
            Storage.linkQueue.AddMessage(new CloudQueueMessage("http://bleacherreport.com/robots.txt"));
            Storage.linkQueue.AddMessage(new CloudQueueMessage("http://www.cnn.com/robots.txt"));
            //Storage.linkQueue.AddMessage(new CloudQueueMessage("http://www.cnn.com/sitemaps/sitemap-show-2018-03.xml"));

            //crawler.SetState("Load");
            Storage.commandQueue.AddMessage(new CloudQueueMessage("Load"));

            return Dashboard();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StopCrawling()
        {
            //crawler.SetState("Idle");
            Storage.commandQueue.AddMessage(new CloudQueueMessage("Idle"));
            return Dashboard();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearCrawling()
        {
            //crawler.SetState("Idle");
            Storage.Initiate();
            Storage.Clear();
            Storage.Initiate();
            return new JavaScriptSerializer().Serialize("stop");
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetTitles(string search)
        {   if (cache.ContainsKey(search.ToLower()))
            {
                return new JavaScriptSerializer().Serialize(cache[search.ToLower()].ToArray());
            } else
            {
                List<Page> result = new List<Page>();
                string[] split = search.ToLower().Split(' ');
                foreach (string word in split)
                {

                    var querytest = Storage.linkTable.CreateQuery<Page>()
                        .Where(x => x.PartitionKey == word)
                        .ToList();

                    foreach (Page url in querytest)
                    {
                        result.Add(url);
                    }
                };
                var results = result
                    .GroupBy(x => x.Path)
                    .Select(x => new Tuple<Page, int>(x.First(), x.ToList().Count()))
                    .OrderByDescending(x => x.Item2)
                    .ThenByDescending(x => x.Item1.PubDate)
                    .Select(x => x.Item1)
                    .ToList();

                if (cache.Count > 50)
                {
                    cache.Clear();
                }
                if (results.Count > 20)
                {
                    results = results.GetRange(0, 21);
                }
                if (results.Count > 10)
                {
                    if (!cache.ContainsKey(search.ToLower()))
                    {
                        cache.Add(search, results);
                    }
                }
                return new JavaScriptSerializer().Serialize(results.ToArray());
            }
        }

        [WebMethod]
        public void ResetCache()
        {
            cache.Clear();
        }
    }
}
