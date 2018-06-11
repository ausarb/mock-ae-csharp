using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.Repoistories;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mattersight.mock.ba.ae.Tasks
{
    public class CallTranscriptRepublishTask
    {
        private readonly ITranscriptRepository _transcriptRepository;
        private readonly IConnection _rabbitConnection;
        private readonly ITranscriptRepublishExchange _transcriptRepublishExchange;

        public CallTranscriptRepublishTask(ITranscriptRepository transcriptRepository, IConnection rabbitConnection, ITranscriptRepublishExchange transcriptRepublishExchange)
        {
            _transcriptRepository = transcriptRepository;
            _rabbitConnection = rabbitConnection;
            _transcriptRepublishExchange = transcriptRepublishExchange;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
                {
                    using (var channel = _rabbitConnection.CreateModel())
                    {
                        channel.QueueDeclare(queue: StreamNamespaces.CallTranscriptRepublishRequest,
                            durable: true, exclusive: false, autoDelete: false, arguments: null);

                        channel.BasicQos(prefetchSize: 0, prefetchCount: 500, global: false);

                        var consumer = new EventingBasicConsumer(channel);

                        consumer.Received += async (model, ea) =>
                        {
                            try
                            {
                                // The only difference between this an publishing a "new" transcript is the exhange we publish to.
                                // We needs something that will take a callId and a transcrit and publish it.  routingKey is likely ignored on PubSub so could just make it optional.
                                var request = JsonConvert.DeserializeObject<CallTranscriptRepublishRequest>(Encoding.UTF8.GetString(ea.Body));
                                
                                //Resolving a grain is *relatively* slow, when compared to just getting the information.
                                //Since we likely don't have the transcript grain already activate (cached) anyways, get it straight from the DB.
                                var transcript = await _transcriptRepository.ForCallId(request.CallId);
                                var message = new Dictionary<string, object>
                                {
                                    {"callId", request.CallId },
                                    {"transcript", transcript}
                                };

                                await _transcriptRepublishExchange.Publish(message, request.RoutingKey);

                                // ReSharper disable once AccessToDisposedClosure
                                channel.BasicAck(ea.DeliveryTag, false);
                                await Task.CompletedTask;
                            }
                            catch
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                            }
                        };

                        channel.BasicConsume(queue: StreamNamespaces.CallTranscriptRepublishRequest, autoAck: false, consumer: consumer);

                        cancellationToken.WaitHandle.WaitOne();
                    }
                }
                , cancellationToken);
        }

    }
}
