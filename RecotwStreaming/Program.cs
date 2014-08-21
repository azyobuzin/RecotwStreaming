using System;
using System.Threading;
using Microsoft.Owin.Hosting;

namespace RecotwStreaming
{
    class Program
    {
        static void Main(string[] args)
        {
            var ev = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, e) =>
            {
                ev.Set();
                e.Cancel = true;
            };

            using (RecotwReceiver.Start())
            using (WebApp.Start<Startup>("http://*:" + args[0]))
                ev.WaitOne();
        }
    }
}
