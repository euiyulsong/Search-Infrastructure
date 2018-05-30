using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
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

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static Crawler crawler = new Crawler();
 

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            Storage.Clear();
            Storage.Initiate();
            //Storage.linkQueue.AddMessage(new CloudQueueMessage("http://www.cnn.com/sitemaps/sitemap-profile-2018-02.xml"));
            //Storage.commandQueue.AddMessage(new CloudQueueMessage("Load"));
            // TODO: Replace the following with your own logic.
            while (true)
            {
                Thread.Sleep(100);
                Trace.TraceInformation("Working");
               CloudQueueMessage commandQueueMessage = Storage.commandQueue.GetMessage(TimeSpan.FromMinutes(5));
                //string commandQueueMessage = crawler.GetState();
                if (commandQueueMessage != null)
                {
                    if (commandQueueMessage.AsString == "Load")
                        //commandQueueMessage.AsString == "Load")
                    {
                        crawler.Load();
                        crawler.Crawling();
                        crawler.ReadHtml();
                    }
                    else if (commandQueueMessage.AsString == "Crawl")
                    {
                        crawler.Crawl();
                        crawler.Crawling();
                        crawler.ReadHtml();
                    }
                    else if (commandQueueMessage.AsString == "Idle")
                    {
                        crawler.Idle();
                    }try
                    {
                        Storage.commandQueue.DeleteMessage(commandQueueMessage);
                    } catch (Exception e)
                    {

                    }
                }
                commandQueueMessage = Storage.commandQueue.GetMessage();
            }
            
            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);

            }
        }
    }
}
