using System;

namespace Mattersight.mock.ba.ae
{
    class Program
    {
        static void Main(string[] args)
        {
            var ae = new AnalyticEngine();
            var workerTask = ae.Start();

            TimeSpan timeout;

            try
            {
                Console.ReadKey(true);
                ae.Stop();
                timeout = TimeSpan.FromSeconds(30);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"{DateTime.Now} - No console detected.  I will run for 1 day.");
                timeout = TimeSpan.FromDays(1);
            }

            workerTask.Wait(timeout);

        }
    }
}
