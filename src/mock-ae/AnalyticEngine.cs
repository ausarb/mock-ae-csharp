using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mattersight.mock.ba.ae
{
    public class AnalyticEngine
    {
        private readonly CancellationTokenSource _ctx = new CancellationTokenSource();
        private Task _worker;

        public Task Start()
        {
            lock (this)
            {
                if (_worker != null)
                {
                    throw new InvalidOperationException("I've already been started.");
                }

                _worker = Task.Run(() =>
                {
                    Console.WriteLine($"{DateTime.Now} - Press any key to terminate.");
                    do
                    {
                        Console.WriteLine($"{DateTime.Now} - Working hard...");
                    } while (!_ctx.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
                    Console.WriteLine($"{DateTime.Now} - Terminating.");
                }, _ctx.Token);

                return _worker;
            }
        }

        public Task Stop()
        {
            _ctx.Cancel();
            return _worker;
        }
    }
}
