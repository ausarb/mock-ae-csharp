using System;
using System.Collections.Generic;
using System.Text;

namespace Mattersight.mock.ba.ae.Serialization
{
    /// <summary>
    /// Doesn't do any real deserialization.  It just returns the item to be deserialized.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NoopDeserializer<T> : IDeserializer<T, T>
    {
        public T Deserialize(T toBeDeserialized)
        {
            return toBeDeserialized;
        }
    }
}
