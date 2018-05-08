namespace Mattersight.mock.ba.ae.Serialization
{
    public interface IDeserializer<in TIn, out TOut>
    {
        TOut Deserialize(TIn toBeDeserialized);
    }
}
