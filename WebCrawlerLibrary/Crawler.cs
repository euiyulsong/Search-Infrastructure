using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Collections.Concurrent;
using HtmlAgilityPack;
using System.Xml.Linq;
using WebCrawlerLibrary;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace WebCrawlerLibrary
{
    public class Crawler
    {
        private string state = "Idle";
        private HashSet<string> disallowedSetLocal = new HashSet<string>();
        private HashSet<string> tableSetLocal;
        private HashSet<string> titleSetLocal;
        private FixedSizedQueue<string> lastTenUrlLocal;
        private List<string> errorUrlLocal;
        private List<string> error;
        private int count;
        private int countIndex;
        private int countUrl;
        private Dashboard dashboard;

        public Crawler()
        {
            this.titleSetLocal = new HashSet<string>();
            this.state = "Idle";
            this.disallowedSetLocal = new HashSet<string>();
            this.tableSetLocal = new HashSet<string>();
            this.lastTenUrlLocal = new FixedSizedQueue<string>();
            this.lastTenUrlLocal.Limit = 10;
            this.errorUrlLocal = new List<string>();
            this.error = new List<string>();
            this.count = 0;
            this.countUrl = 0;
            this.countIndex = 0;
            this.dashboard = new Dashboard();
        }
        public string GetState()
        {
            return this.state;
        }

        public void SetState(string state)
        {
            this.state = state;
            Dashboard();
        }

        public void Load()
        {
            this.state = "Load";
            Dashboard();
        }

        public void Crawl()
        {
            this.state = "Crawl";
            Dashboard();
        }

        public void Idle()
        {
            this.state = "Idle";
            Dashboard();
        }

        public void Crawling()
        {
            while (GetState().Equals("Load"))
            {
                Thread.Sleep(200);

                CloudQueueMessage linkMessage = Storage.linkQueue.GetMessage();
                if (linkMessage != null)
                {
                    if (!disallowedSetLocal.Contains(linkMessage.AsString))
                    {
                        count++;
                        string link = linkMessage.AsString;
                        disallowedSetLocal.Add(link);

                        if (link.EndsWith("robots.txt"))
                        {
                            GetRobot(link);
                        }
                        else
                        {
                            Crawling(link);
                        }
                        if (count % 10 == 0)
                        {
                            Dashboard();
                        }
                        
                    }
                    Storage.linkQueue.DeleteMessage(linkMessage);
                }
                else
                {
                    Crawl();
                    Storage.commandQueue.AddMessage(new CloudQueueMessage("Crawl"));
                }
            }
        }

        private void Dashboard()
        {   try
            {
                PerformanceCounter mem = new PerformanceCounter("Memory", "Available MBytes", null);
                PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                float perfCounterValue = cpu.NextValue();

                System.Threading.Thread.Sleep(1000);
                double CpuUsage = (double)cpu.NextValue();
                double RamAvailable = (double)mem.NextValue();

                TableOperation tableOperation = TableOperation.Retrieve<Dashboard>("Dashboard", "Dashboard");
                TableResult tableResult = Storage.dashboardTable.Execute(tableOperation);
                dashboard.state = GetState();
                dashboard.cpu = CpuUsage;
                dashboard.ram = RamAvailable;
                dashboard.countUrl = this.countUrl;
                dashboard.lastUrl = JsonConvert.SerializeObject(lastTenUrlLocal.Output());
                dashboard.sizeQueue = count;
                dashboard.sizeIndex = countIndex;
                dashboard.error = JsonConvert.SerializeObject(error);
                dashboard.errorUrl = JsonConvert.SerializeObject(errorUrlLocal);
                TableOperation insertOperation = TableOperation.InsertOrReplace(dashboard);
                Storage.dashboardTable.Execute(insertOperation);
            } catch (Exception e)
            {

            }
        }

        public void ReadHtml()
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument html = new HtmlDocument();
            Thread.Sleep(200);
            while (GetState().Equals("Crawl"))
            {
                Thread.Sleep(200);
                CloudQueueMessage htmlMessage = Storage.htmlQueue.GetMessage();
                if (htmlMessage != null)
                {
                    if (!tableSetLocal.Contains(htmlMessage.AsString))
                    {

                        tableSetLocal.Add(htmlMessage.AsString);
                        using (var client = new WebClient())
                        {
                            try
                            {
                                html.LoadHtml(client.DownloadString(htmlMessage.AsString));
                                DateTime publish;
                                HtmlNode htmlNode = html.DocumentNode.SelectSingleNode("//head/meta[@name='lastmod']");
                                if (htmlNode == null)
                                {
                                    publish = DateTime.Today;
                                }
                                else
                                {
                                    publish = DateTime.Parse(htmlNode.Attributes["content"].Value);
                                }

                                Uri uri = new Uri(htmlMessage.AsString);
                                try
                                {
                                    string temp = html.DocumentNode.SelectSingleNode("//head/title").InnerText ?? "";
                                    string[] splitTitle = temp.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                    if (!this.titleSetLocal.Contains(temp))
                                    {
                                        titleSetLocal.Add(temp);
                                        lastTenUrlLocal.Enqueue("http://" + uri.Host + uri.PathAndQuery);

                                        foreach (string word in splitTitle)
                                        {
                                            string tempWord = RemoveSpecialCharacters(word).ToLower();
                                            Page page = new Page(tempWord, temp, "http://" + uri.Host + uri.PathAndQuery, publish);
                                            try
                                            {
                                                Storage.linkTable.Execute(TableOperation.InsertOrReplace(page));
                                            }
                                            catch (Exception e)
                                            {   if (errorUrlLocal.Count < 10 && !errorUrlLocal.Contains("http://" + uri.Host + uri.PathAndQuery))
                                                {
                                                    errorUrlLocal.Add("http://" + uri.Host + uri.PathAndQuery);
                                                }
                                                if (error.Count < 10 && !error.Contains(e+ ""))
                                                {
                                                    error.Add(e + "");
                                                }
                                                Dashboard();
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (errorUrlLocal.Count < 10 && !errorUrlLocal.Contains("http://" + uri.Host + uri.PathAndQuery))
                                    {
                                        errorUrlLocal.Add("http://" + uri.Host + uri.PathAndQuery);
                                    }
                                    if (error.Count < 10 && !error.Contains(e + ""))
                                    {
                                        error.Add(e + "");
                                    }
                                }
                                Dashboard();

                            }
                            catch (Exception e)
                            {
                                if (errorUrlLocal.Count < 10 && !errorUrlLocal.Contains(htmlMessage.AsString))
                                {
                                    errorUrlLocal.Add(htmlMessage.AsString);
                                }
                                if (error.Count < 10 && !error.Contains(e + ""))
                                {
                                    error.Add(e + "");
                                }
                                Dashboard();
                            }
                        }
                        if (count % 10 == 0)
                        {
                            Dashboard();
                        }
                    }
                    Thread.Sleep(200);

                    Storage.htmlQueue.DeleteMessage(htmlMessage);
                }
                else
                {
                    Idle();
                    Storage.commandQueue.AddMessage(new CloudQueueMessage("Idle"));

                }
            }
        }

        private string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private string RemoveSpecialCharacters(string input)
        {
            Regex r = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return r.Replace(input, String.Empty);
        }

        public int GetCount()
        {
            return this.count;
        }

        public void Crawling(string link)
        {
            XElement linkElement = XElement.Load(link);
            XName getCrawl;
            XName getLoc;
            XName getLastMod = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");
            DateTime original = new DateTime(2018, 1, 1);
            DateTime publish = new DateTime(2016, 1, 01);

            if (link.Contains("-index"))
            {
                getCrawl = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                getLoc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                try
                {
                    foreach (var temp in linkElement.Elements(getCrawl))
                    {
                        string linkElementLoc = temp.Element(getLoc).Value;
                        publish = DateTime.Parse(temp.Element(getLastMod).Value);
                        if (publish != null && publish > original)
                        {
                            countIndex++;
                            Storage.linkQueue.AddMessage(new CloudQueueMessage(linkElementLoc));
                        }
                    }
                }
                catch (Exception e)
                {
                    if (errorUrlLocal.Count < 10 && !errorUrlLocal.Contains(link))
                    {
                        errorUrlLocal.Add(link);
                    }
                    if (error.Count < 10 && !error.Contains(e + ""))
                    {
                        error.Add(e + "");
                    }


                    Dashboard();


                }
            }
            else
            {
                if (link.Contains("bleacherreport.com"))
                {
                    getCrawl = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                    getLoc = XName.Get("loc", "http://www.google.com/schemas/sitemap/0.9");
                    publish = DateTime.Today;
                }
                else
                {
                    getCrawl = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                    getLoc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                }
                try
                {
                    foreach (var temp in linkElement.Elements(getCrawl))
                    {
                        string linkElementLoc = temp.Element(getLoc).Value;
                        if (temp != null && !link.Contains("bleacherreport.com"))
                        {
                            publish = DateTime.Parse(temp.Element(getLastMod).Value);
                        }
                        if (publish > original && publish != null && !this.disallowedSetLocal.Contains(linkElementLoc))
                        {
                            disallowedSetLocal.Add(linkElementLoc);
                            countUrl++;
                            Storage.htmlQueue.AddMessage(new CloudQueueMessage(linkElementLoc));
                        }
                    }
                }
                catch (Exception e)
                {
                    if (errorUrlLocal.Count < 10 && !errorUrlLocal.Contains(link))
                    {
                        errorUrlLocal.Add(link);
                    }
                    if (error.Count < 10 && !error.Contains(e + ""))
                    {
                        error.Add(e + "");
                    }
                    Dashboard();

                }
            }
        }

        public void GetRobot(string url)
        {
            Uri uri = new Uri(url);

            WebRequest request = WebRequest.Create(uri.AbsoluteUri);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            while (!reader.EndOfStream)
            {
                Thread.Sleep(200);
                string temp = reader.ReadLine();
                string[] temp2 = temp.Split(' ');
                if (temp2[0].Equals("Sitemap:"))
                {
                    if (uri.Authority.Equals("bleacherreport.com"))
                    {
                        if (temp2[1].Contains("nba") || temp2[1].Contains("articles"))
                        {
                            countIndex++;
                            lastTenUrlLocal.Enqueue(temp2[1]);

                            Storage.linkQueue.AddMessage(new CloudQueueMessage(temp2[1]));
                        }
                    }
                    else
                    {
                        countIndex++;
                        lastTenUrlLocal.Enqueue(temp2[1]);

                        Storage.linkQueue.AddMessage(new CloudQueueMessage(temp2[1]));
                    }
                }
                else if (temp2[0].Equals("Disallow:"))
                {
                    this.disallowedSetLocal.Add("https://" + uri.Authority + temp2[1]);
                    Storage.disallowedQueue.AddMessage(new CloudQueueMessage("https://" + uri.Authority + temp2[1]));
                }
            }
        }
    }
}
