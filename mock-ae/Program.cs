using System;

namespace Mattersight.mock.ba.ae
{
    class Program
    {
        static void Main(string[] args)
        {
            var ae = new AnalyticEngine();
            ae.Start();

            Console.ReadKey(true);
            ae.Stop();

        }
    }
}
