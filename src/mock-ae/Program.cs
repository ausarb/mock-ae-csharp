using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams.RabbitMQ;
using Mattersight.mock.ba.ae.Serialization;
using RabbitMQ.Client;

namespace Mattersight.mock.ba.ae
{
    public class Program
    {
        private readonly ConnectionFactory _connectionFactory;

        public Program(string rabbitHostName, int rabbitPort = AmqpTcpEndpoint.UseDefaultPort)
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = rabbitHostName,
                Port = rabbitPort
            };
        }

        public static void Main()
        {
            Console.WriteLine($"Version = v{Assembly.GetExecutingAssembly().GetName().Version}.");

            var ctx = new CancellationTokenSource();
            var workerTask = new Program("rabbit", 5672).Run(ctx.Token);

            if (Console.IsInputRedirected)
            {
                Console.WriteLine($"{DateTime.Now} - No console detected.  I will run forever.");
                workerTask.Wait(ctx.Token);  //Just wait forever.
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} - Press any key to terminate.");
                Console.ReadKey(true);
                ctx.Cancel();
            }

            workerTask.Wait(TimeSpan.FromSeconds(30));
        }

        public Task Run(CancellationToken cancellationToken)
        {
            var ctx = new CancellationTokenSource();

            var incomingStream = new ConsumingStream<CallEvent>(new QueueConfiguration {Name="ti"} , _connectionFactory, new ByteArrayEncodedJsonDeserializer<CallEvent>());
            var outgoingStream = new ProducingStream<CallTranscript>(new QueueConfiguration {Name="transcript"}, _connectionFactory, new CallTranscriptSerializer());

            //Started is when the methods return, not when the tasks from them complete.  
            //Without the { } inside the Task.Run, it will grab the task returned by these method.  Those won't complete until the program ends.
            var allStarted = Task
                .WhenAll(
                    // ReSharper disable ImplicitlyCapturedClosure
                    Task.Run(() => { incomingStream.Start(ctx.Token); }, cancellationToken),
                    Task.Run(() => { outgoingStream.Start(ctx.Token); }, cancellationToken))
                    // ReSharper restore ImplicitlyCapturedClosure
                .Wait(TimeSpan.FromMinutes(1));

            if (!allStarted)
            {
                throw new Exception("At least one stream did not start within 1 minute.");
            }

            var ae = new Ae(incomingStream, outgoingStream);
            return ae.Start(cancellationToken);
        }
    }
}
