using System;
using System.Collections.Generic;
using System.Text;

namespace Mattersight.mock.ba.ae.Serialization
{
    /// <summary>
    /// Simply converts a string to a UTF8 byte[].
    /// </summary>
    public class StringSerializer : ISerializer<string, byte[]>
    {
        public byte[] Serialize(string toBeSerialized)
        {
            return Encoding.UTF8.GetBytes(toBeSerialized);
        }
    }
}
