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
using ClassLibrary;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static Crawler crawler = new Crawler();
        private static int linkCount = 0;
        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            Storage.CreateStorage();
            while (true)
            {
                CloudQueueMessage command = Storage.CommandQueue.GetMessage(TimeSpan.FromMinutes(5));
                if (command != null)
                {
                    switch (command.AsString)
                    {
                        case "Initialize Crawl":
                            crawler.Start();
                            break;
                        case "Stop Crawl":
                            crawler.Stop();
                            crawler.updateDashboard();
                            break;
                        case "Resume Crawl":
                            crawler.Resume();
                            break;
                        default:
                            break;
                    }
                    Storage.CommandQueue.DeleteMessage(command);
                }
                if (crawler.GetCrawlerState().Equals("Crawling") || crawler.GetCrawlerState().Equals("Loading"))
                {
                    CloudQueueMessage link = Storage.LinkQueue.GetMessage(TimeSpan.FromMinutes(5));
                    if (link != null)
                    {
                        linkCount++;
                        try
                        {
                            crawler.CrawlUrl(link.AsString);
                            Storage.LinkQueue.DeleteMessage(link);
                        }
                        catch { }
                        if (linkCount % 5 == 0)
                        {
                            crawler.updateDashboard();
                        }
                    }
                }
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
