using Mattersight.mock.ae.csharp.Interfaces.Transcription;
using Orleans;

namespace Mattersight.mock.ba.ae.csharp.Grains.Transcription
{
    public class CallTranscriptGrain : Grain<CallTranscriptState>, ICallTranscriptGrain
    {
    }
}
