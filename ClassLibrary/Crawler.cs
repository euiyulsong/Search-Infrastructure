using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Globalization;
using System.IO.Compression;

namespace ClassLibrary {
    public class Crawler
    {
        private Dashboard dashboard;
        private ConcurrentDictionary<string, string> visitedLinks;
        private ConcurrentDictionary<string, string> uniqueUrlInSiteMaps;
        private ConcurrentBag<string> disallowedUrls;
        private string CrawlerState;
        private FixedSizedQueue<string> last10Urls;
        private FixedSizedQueue<string> errorsUrl;
        private int counterTable;
        private int numberOfUrlsCrawled;
        private static EventWaitHandle myWaitHandle;
        private int errorNumber;
        private string error;

        public Crawler()
        {
            myWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            visitedLinks = new ConcurrentDictionary<string, string>();
            uniqueUrlInSiteMaps = new ConcurrentDictionary<string, string>();
            disallowedUrls = new ConcurrentBag<string>();
            CrawlerState = "Idle";
            last10Urls = new FixedSizedQueue<string>();
            last10Urls.Limit = 10;
            errorsUrl = new FixedSizedQueue<string>();
            errorsUrl.Limit = 10;
            counterTable = 0;
            numberOfUrlsCrawled = 0;
            errorNumber = 0;
            error = "";
            dashboard = new Dashboard();
            ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
        }

        /// <summary>
        /// Get the state of the crawler (loading, idle, crawling)
        /// </summary>
        /// <returns></returns>
        public string GetCrawlerState()
        {
            return CrawlerState;
        }

        /// <summary>
        /// Start the crawling process
        /// </summary>
        public void Start()
        {
            CrawlerState = "Loading";
            DateTime begin = DateTime.Now;
            TimeSpan time = DateTime.Now.Subtract(begin);
            dashboard.CrawlingFor = time.ToString();
            dashboard.BeganCrawlingAt = begin.ToString();
        }

        /// <summary>
        /// Stop the crawling process
        /// </summary>
        public void Stop()
        {
            CrawlerState = "Idle";
        }


        public void Resume()
        {
            CrawlerState = "Crawling";
            DateTime begin = DateTime.Now;
            TimeSpan time = DateTime.Now.Subtract(begin);
            dashboard.CrawlingFor = time.ToString();
            dashboard.BeganCrawlingAt = begin.ToString();
            TableOperation retrieveOperation = TableOperation.Retrieve<Dashboard>("1", "1");
            TableResult retrievedResult = Storage.DashboardTable.Execute(retrieveOperation);
            Dashboard dashboardRetrieved = (Dashboard)retrievedResult.Result;
            counterTable = dashboardRetrieved.SizeOfTable;
            numberOfUrlsCrawled = dashboardRetrieved.NumberOfUrlsCrawled;
            errorNumber = dashboardRetrieved.errorNumber;
            string[] disallowedUrlsArray = JsonConvert.DeserializeObject<string[]>(dashboardRetrieved.disallowedUrls);
            foreach(string url in disallowedUrlsArray)
            {
                if(!disallowedUrls.Contains(url))
                    disallowedUrls.Add(url);
            }
        }

        /// <summary>
        /// Method will crawl the url
        /// </summary>
        /// <param name="url">string urk</param>
        public void CrawlUrl(string url)
        {
            if (CrawlerState.Equals("Idle"))
            {
                return;
            }
            Uri uri = new Uri(url);
            if (url.EndsWith("robots.txt"))
            {
                CrawlRobots(uri);
            }
            else
            {
                CrawlerState = "Crawling";
                ThreadPool.QueueUserWorkItem(o => CrawlPage(uri.AbsoluteUri));
            }
        }


        /// <summary>
        /// Method check if we are done with sitesmap before proceed to the crawling state
        /// </summary>
        private void ProceedThreading()
        {
            if(disallowedUrls.Count >= 43)
            {
                myWaitHandle.Set();
                CrawlerState = "Crawling";
            }
            
        }

        /// <summary>
        /// Hashing method used for url
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Crawl robot sites
        /// </summary>
        /// <param name="link"></param>
        public void CrawlRobots(Uri link)
        {
            StreamReader contentReader = GetStreamFromUri(link.AbsoluteUri);
            while (!contentReader.EndOfStream)
            {
                string line = contentReader.ReadLine();
                string[] components = line.Split(':');
                if (components[0].Equals("Sitemap")) 
                {
                    if (link.Authority.Equals("bleacherreport.com"))
                    {
                        if (components[2].Contains("nba"))
                        {
                            ProcessSiteMaps(components[1] + ":" + components[2]);
                        }
                    }else
                    {
                        ProcessSiteMaps(components[1] + ":" + components[2]);
                    }
                    
                }else if (components[0].Equals("Disallow"))
                {
                    disallowedUrls.Add("http://" + link.Authority + components[1].Substring(1)); //add disallowedUrls so crawlers wont crawl them
                }
            }
            ProceedThreading();
        }


        /// <summary>
        /// Method will determine what to do with a site map, if it contains -index or not
        /// </summary>
        /// <param name="url"></param>
        private void ProcessSiteMaps(string url)
        {
            if (CrawlerState.Equals("Idle"))
            {
                return;
            }
            if (url.Contains("-index"))
            {
                CrawlSiteMapIndex(url);
            }
            else
            {
                CrawlSiteMap(url);
            }
        }

        /// <summary>
        /// Method will crawl site map that has index in it
        /// </summary>
        /// <param name="url"></param>
        private void CrawlSiteMapIndex(string url)
        {
            HtmlDocument htmlContent = GetWebText(url);
            if (htmlContent == null)
                return;
            HtmlNodeCollection sitemaps = htmlContent.DocumentNode.SelectNodes("//sitemap");
            if(sitemaps != null)
            {
                foreach(HtmlNode sitemap in sitemaps)
                {
                    HtmlNode loc = sitemap.SelectSingleNode("//loc");
                    string link = loc.InnerText;
                    if (link.Contains("www.cnn.com"))
                    {
                        try
                        {
                            DateTime sitemapDate = Convert.ToDateTime(sitemap.LastChild.InnerText);
                            int difference = ((DateTime.Now.Year - sitemapDate.Year) * 12) + DateTime.Now.Month - sitemapDate.Month;
                            if (difference <= 2)
                            {
                                ProcessSiteMaps(link);
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e);
                            error = e.ToString();
                            updateDashboard();
                        };
                    }
                    else if (link.Contains("bleacherreport.com"))
                    {
                        if (link.Contains("nba"))
                        {
                            ProcessSiteMaps(link);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method will clean up the url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        private string CleanUrl(string url, string authority)
        {
            string cleanUrl = "";
            if (!url.Contains("realestate.money.cnn.com"))
            {
                if ((url.StartsWith("http://") || url.StartsWith("https://")))
                {
                    if(url.Contains("cnn.com") || url.Contains("bleacherreport.com"))
                        cleanUrl = url;
                }
                else
                {
                    if (url.Contains("cnn.com") || url.Contains("bleacherreport.com"))
                    {
                        if (url.StartsWith("/"))
                        {
                            cleanUrl = "http:" + url;
                        }
                        else
                        {
                            cleanUrl = "http://" + url;
                        }
                    }
                    else
                    {
                        if (url.StartsWith("/"))
                        {
                            cleanUrl = "http://" + authority + url;
                        }
                    }
                }
            }
            return cleanUrl;
        }

        /// <summary>
        /// Crawl "regular" sitemap (without index keyword)
        /// </summary>
        /// <param name="url"></param>
        private void CrawlSiteMap(string url)
        {
            HtmlDocument htmlContent = GetWebText(url);
            if (htmlContent == null)
                return;
            HtmlNodeCollection links = htmlContent.DocumentNode.SelectNodes("//url");
            if (links == null)
                return;
            else
            {
                foreach (HtmlNode link in links)
                {
                    HtmlNode loc = link.SelectSingleNode("//loc");
                    if (!uniqueUrlInSiteMaps.ContainsKey(loc.InnerText))
                    {
                        uniqueUrlInSiteMaps.TryAdd(loc.InnerText, "");
                        Storage.LinkQueue.AddMessage(new CloudQueueMessage(loc.InnerText));
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a url is allowed to be crawled 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool IsNotAllowed (string url)
        {
            bool result = false;
            foreach(string disallowedUrl in disallowedUrls)
            {
                if (url.Contains(disallowedUrl))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Method will the crawl the page getting out the tile and all the new links to be crawled
        /// </summary>
        /// <param name="url"></param>
        public void CrawlPage(string url)
        {
            if(!url.Contains("cnn") && !url.Contains("bleacherreport"))
            {
                return;
            }
            if (CrawlerState.Equals("Idle"))
            {
                return;
            }
            if (url == null)
            {
                return;
            }
            if (IsNotAllowed(url))
            {
                return;
            }
            Interlocked.Add(ref numberOfUrlsCrawled, 1);
            last10Urls.Enqueue(url);
            if (!visitedLinks.ContainsKey(url))
            {
                visitedLinks.TryAdd(url, "");
                HtmlDocument htmlText = GetWebText(url);
                if (htmlText == null)
                    return;

                HtmlNode title = htmlText.DocumentNode.SelectSingleNode("//title");
                HtmlNodeCollection linkNodes = htmlText.DocumentNode.SelectNodes("//a");
                HtmlNode mdnode = htmlText.DocumentNode.SelectSingleNode("//meta[@name='lastmod']");
                DateTime lastModified = DateTime.Now;
                if (mdnode != null)
                {
                    HtmlAttribute desc;
                    desc = mdnode.Attributes["content"];
                    string fulldescription = desc.Value;
                    lastModified = DateTime.Parse(fulldescription);
                }

                if (linkNodes == null)
                {
                    return;
                }
                if (title != null)
                {
                    string pageTitle = title.InnerText;
                    string[] titleArray = pageTitle.Split(' ');
                    Uri link = new Uri(url);
                    string textBody = GetPlainTextFromHtml(htmlText);
                    int byteCount = System.Text.ASCIIEncoding.Unicode.GetByteCount(textBody);
                    if(byteCount >= 60000)
                    {
                        textBody = textBody.Substring(0, 2000);
                    }
                    foreach (string titleWord in titleArray)
                    {
                        string pk = RemoveSpecialCharacters(titleWord);
                        pk = pk.ToLower();
                        WebPageEntity newPage = new WebPageEntity(pk, Hash(url));
                        newPage.Title = pageTitle;
                        newPage.Url = url;
                        newPage.Text = Compress(textBody);
                        newPage.Date = lastModified.ToString("MM/dd/yyyy HH:mmtt", new CultureInfo("en-US"));
                        TableOperation insertOperation = TableOperation.InsertOrReplace(newPage);
                        try
                        {
                            Interlocked.Add(ref counterTable, 1);
                            Storage.LinkTable.Execute(insertOperation);
                        }
                        catch (Exception exc)
                        {
                            System.Diagnostics.Debug.WriteLine(exc);
                            error = exc.ToString();
                            updateDashboard();
                        }
                    }
                }
                //Find all the link in the current page and add them to the queue
                foreach (HtmlNode link in linkNodes)
                {
                    if (CrawlerState.Equals("Idle"))
                    {
                        return;
                    }
                    HtmlAttribute href = link.Attributes["href"];
                    try
                    {
                        if (href != null)
                        {
                            string pageLink = href.Value;
                            Uri FullUrl = new Uri(url);
                            pageLink = CleanUrl(pageLink, FullUrl.Authority);
                            if (pageLink != "")
                            {
                                if (!visitedLinks.ContainsKey(pageLink))
                                {
                                    CloudQueueMessage msg = new CloudQueueMessage(pageLink);
                                    Storage.LinkQueue.AddMessage(msg);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Debug.WriteLine(exc);
                        error = exc.ToString();
                        updateDashboard();
                    }
                }
            }
        }

        public string Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }

            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return Convert.ToBase64String(gzBuffer);
        }

        /// <summary>
        /// Method will filter out all the html tags, script tags, and style tags and just get the plain html text
        /// </summary>
        /// <param name="htmlString">html string</param>
        /// <returns></returns>
        private string GetPlainTextFromHtml(HtmlDocument htmlText)
        {
            HtmlNode body = null;
            try
            {
                body = htmlText.DocumentNode.SelectSingleNode("//body");
                if (body == null)
                {
                    return "";
                }
                HtmlNode header = body.SelectSingleNode("//header");
                HtmlNode footer = body.SelectSingleNode("//footer");
                if (header != null)
                    header.Remove();
                if (footer != null)
                    footer.Remove();
                var divs = body.SelectNodes("//div");
                if (divs != null)
                {
                    foreach (var tag in divs)
                    {
                        if (tag.Attributes["class"] != null && tag.Attributes["class"].Value.Contains("nav"))
                        {
                            tag.Remove();
                        }
                        else if (tag.Attributes["id"] != null && tag.Attributes["id"].Value.Contains("breaking-news"))
                        {
                            tag.Remove();
                        }
                        else if (tag.Attributes["id"] != null && tag.Attributes["id"].Value.Contains("nav"))
                        {
                            tag.Remove();
                        }
                        else if (tag.Attributes["class"] != null && tag.Attributes["class"].Value.Contains("header"))
                        {
                            tag.Remove();
                        }
                        else if (tag.Attributes["id"] != null && tag.Attributes["id"].Value.Contains("header"))
                        {
                            tag.Remove();
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            string htmlString = "";
            try
            {
                htmlString = body.OuterHtml;
                string htmlTagPattern = "<.*?>";
                var regexCss = new Regex("(\\<script(.+?)\\</script\\>)|(\\<style(.+?)\\</style\\>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlString = regexCss.Replace(htmlString, string.Empty);
                htmlString = Regex.Replace(htmlString, htmlTagPattern, string.Empty);
                htmlString = Regex.Replace(htmlString, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
                htmlString = Regex.Replace(htmlString, "<!--.*?-->", String.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                htmlString = htmlString.Replace("&nbsp;", string.Empty);
                htmlString = htmlString.Replace("\t", string.Empty);
                htmlString = htmlString.Replace("\n", string.Empty);
            }catch(Exception e)
            {

            }
            return htmlString;
        }

        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Return the summary of the current dahsboard
        /// </summary>  
        /// <returns></returns>
        public void updateDashboard()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Dashboard>("1", "1");
            TableResult retrievedResult = Storage.DashboardTable.Execute(retrieveOperation);
            Dashboard dashboard1 = (Dashboard)retrievedResult.Result;
            GetPerfCounters();
            CloudQueue link = Storage.LinkQueue;
            link.FetchAttributes();
            dashboard.CrawlingState = GetCrawlerState();
            dashboard.last10Urls = JsonConvert.SerializeObject(last10Urls.GetSnapshot());
            dashboard.errorUris = JsonConvert.SerializeObject(errorsUrl.GetSnapshot());
            dashboard.errorNumber = errorNumber;
            dashboard.SizeOfQueue = (int)link.ApproximateMessageCount;
            dashboard.SizeOfTable = counterTable;
            dashboard.NumberOfUrlsCrawled = numberOfUrlsCrawled;
            dashboard.error = error;
            dashboard.lastTitle = dashboard1.lastTitle;
            dashboard.numberOfTitle = dashboard1.numberOfTitle;
            string[] disallowUrlsSnapShot = disallowedUrls.ToArray();
            dashboard.disallowedUrls = JsonConvert.SerializeObject(disallowUrlsSnapShot);
            TableOperation insertOperation = TableOperation.InsertOrReplace(dashboard);
            Storage.DashboardTable.Execute(insertOperation);
        }


        /// <summary>
        /// Get system configuration 
        /// </summary>
        public void GetPerfCounters()
        {
            PerformanceCounter mem = new PerformanceCounter("Memory", "Available MBytes", null);
            PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            float perfCounterValue = cpu.NextValue();

            //Thread has to sleep for at least 1 sec for accurate value.
            System.Threading.Thread.Sleep(1000);

            perfCounterValue = cpu.NextValue();
            dashboard.CpuUsage = perfCounterValue;
            dashboard.RamAvailable = mem.NextValue();

            if (GetCrawlerState().Equals("Crawling") || GetCrawlerState().Equals("Loading"))
            {
                DateTime begin = Convert.ToDateTime(dashboard.BeganCrawlingAt);
                TimeSpan timespan = DateTime.Now.Subtract(begin);
                string fmt = "hh\\:mm\\:ss";
                dashboard.CrawlingFor = timespan.Days + ":" + timespan.ToString(fmt);
            }
        }

        public HtmlDocument GetWebText(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLimit = 1;
            request.Proxy = null;
            request.UserAgent = "A Web Crawler";
            request.Timeout = 3600000;
            HtmlDocument doc = null;
            try
            {
                WebResponse response = request.GetResponse();
                Stream streamResponse = response.GetResponseStream();
                StreamReader sreader = new StreamReader(streamResponse);
                string s = "";
                try
                {
                    s = sreader.ReadToEnd();
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
                doc = new HtmlDocument();
                doc.LoadHtml(s);
            }
            catch (WebException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                Interlocked.Add(ref errorNumber, 1);
                errorsUrl.Enqueue(url + " | " + "Error code: " + e);
            }
            return doc;
        }

        /// <summary>
        /// Method for reading robots.text
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private StreamReader GetStreamFromUri(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "A Web Crawler";
            WebResponse response = request.GetResponse();
            Stream streamResponse = response.GetResponseStream();
            StreamReader sreader = new StreamReader(streamResponse);
            return sreader;
        }
    }
}