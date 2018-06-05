using System;
using System.Threading.Tasks;
using Mattersight.mock.ba.ae.Domain.CallTranscript.Publishing;
using Mattersight.mock.ba.ae.Grains.Transcription;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.TestKit;
using Shouldly;
using Xbehave;

namespace Mattersight.mock.ba.ae.Tests.Domain.CallTranscript.Publishing.CallTranscriptRepublishGrainTests
{
    public class CallTranscriptRepublishRequestScenerios : TestKitBase
    {
        [Scenario]
        public void When_processing_a_RepublishRequest(CallTranscriptRepublishGrain sut)
        {
            ICallTranscriptGrain grainThatWasPublished = null;
            ICallTranscriptGrain grainThatShouldBePublished = null;

            string rountingKeyToPublishWith = null;

            var expectedGrainKey = Guid.NewGuid().ToString();

            "Given a CallTranscriptRepublishGrain".x(() =>
            {
                sut = Silo.CreateGrain<CallTranscriptRepublishGrain>(Guid.Empty);

                // Explicitly adding a probe will give us the same grain GrainFactory will return when it later asks for the grain with the same key.
                grainThatShouldBePublished = Silo.AddProbe<ICallTranscriptGrain>(expectedGrainKey).Object;

                var exchange = Silo.ServiceProvider.GetService<ICallTranscriptRepublishExchange>();

                // Save off the grain that gets passed to the exchange.  This is how we'll test the grain loads the grain needed.
                Mock.Get(exchange)
                    .Setup(x => x.OnNext(It.IsAny<ICallTranscriptGrain>(), It.IsAny<string>()))
                    .Callback((ICallTranscriptGrain grain, string routingKey) =>
                    {
                        grainThatWasPublished = grain;
                        rountingKeyToPublishWith = routingKey;
                    })
                    .Returns(Task.CompletedTask);
            });

            "that receives a CallTranscriptRepublishRequest".x(async () =>
            {
                await sut.OnNextAsync(new CallTranscriptRepublishRequest(expectedGrainKey, routingKey: "12345"));
            });

            "Should get correct grain and pass it to the republish grain".x(() =>
                grainThatWasPublished.ShouldBe(grainThatShouldBePublished));

            "Should use the routing key specified in the republish request".x(() =>
                rountingKeyToPublishWith.ShouldBe("12345"));
        }
    }
}
