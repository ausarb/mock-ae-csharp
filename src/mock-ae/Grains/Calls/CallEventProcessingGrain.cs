using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain;
using Mattersight.mock.ba.ae.Domain.Calls;
using Mattersight.mock.ba.ae.Domain.Ti;
using Mattersight.mock.ba.ae.Domain.Transcription;
using Mattersight.mock.ba.ae.ProcessingStreams;
using Mattersight.mock.ba.ae.Serialization;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Mattersight.mock.ba.ae.Grains.Calls
{
    public interface ICallEventProcessingGrain : IGrainWithGuidKey, IAsyncObserver<byte[]>
    {
    }

    [StatelessWorker]
    [ImplicitStreamSubscription(StreamNamespaces.TiProducedCallEvents)]
    public class CallEventProcessingGrain : Grain, ICallEventProcessingGrain
    {
        private readonly IProducingStream<CallTranscript> _outgoingStream;
        private readonly IDeserializer<byte[], CallEvent> _deserializer;

        public CallEventProcessingGrain(IProducingStream<CallTranscript> outgoinStream, IDeserializer<byte[], CallEvent> deserializer)
        {
            _outgoingStream = outgoinStream;
            _deserializer = deserializer;
        }

        public override async Task OnActivateAsync()
        {
            var guid = this.GetPrimaryKey();
            var streamProvider = GetStreamProvider(Configuration.OrleansStreamProviderName);
            var stream = streamProvider.GetStream<byte[]>(guid, StreamNamespaces.TiProducedCallEvents);

            await stream.SubscribeAsync(this);
            await base.OnActivateAsync();
        }

        // Need so we can mock it for unit testing
        public new virtual IStreamProvider GetStreamProvider(string name)
        {
            return base.GetStreamProvider(name);
        }

        public Task OnNextAsync(byte[] item, StreamSequenceToken token = null)
        {
            var callEvent = _deserializer.Deserialize(item);

            Console.Write($"Received '{callEvent.AcdEvent.EventType}' event for callId {callEvent.AcdEvent.CallId}.   ");

            if (callEvent.AcdEvent.EventType != "end call")
            {
                Console.WriteLine("Ignoring....");
                return Task.CompletedTask;
            }

            Console.WriteLine("Creating a transcript.");

            var mediumId = MediumId.Next();
            var transcript = new CallTranscript
            {
                Call = new Call(mediumId)
                {
                    CallMetaData = new CallMetaData { TiCallId = callEvent.AcdEvent.CallId }
                },
                Transcript = new Transcript($"random transcript for call with MediumId of {mediumId.Value}.")
            };

            // The stream must know how to serialze the transcript (via dependency injection), not *this* class.  
            // This allows multiple producers to write to the same stream.
            _outgoingStream.OnNext(transcript);

            return Task.CompletedTask;
        }

        public Task OnCompletedAsync()
        {
            Console.WriteLine("OnCompletedAsync!!!");
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            Console.WriteLine("OnErrorAsync!!!");
            return Task.CompletedTask;
        }
    }
}
