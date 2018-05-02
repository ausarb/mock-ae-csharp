using Orleans;

namespace Mattersight.mock.ba.ae.Grains.Transcription
{
    public interface ICallTranscriptGrain : IGrainWithGuidKey
    {
    }

    public class CallTranscriptGrain : Grain<CallTranscriptState>, ICallTranscriptGrain
    {
    }
}
