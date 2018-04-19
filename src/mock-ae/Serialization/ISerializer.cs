namespace Mattersight.mock.ba.ae.Serialization
{
    public interface ISerializer<in TIn, out TOut>
    {
        TOut Serialize(TIn toBeSerialized);
    }
}
