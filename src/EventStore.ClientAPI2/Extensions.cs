using System;
using System.Collections.Generic;
using System.IO;

namespace EventStore.ClientAPI
{
    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static ArraySegment<byte> GetArraySegment(this MemoryStream stream)
        {
            stream.ToArray();
            ArraySegment<byte> result;
            if (stream.TryGetBuffer(out result))
            {
                return result;
            }
            throw new Exception(
                @"TryGetBuffer returned false. Getting ArraySegment<byte> from MemoryStream 
                  that was not constructed with either 
                  
                  - MemoryStream()
                  - MemoryStream(int capacity)
                  - MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) with publiclyVisible: true

                  is not supported.");
        }

        public static byte[] GetBuffer(this MemoryStream stream)
        {
            ArraySegment<byte> resultSegment = stream.GetArraySegment();

            var result = new byte[resultSegment.Count];
            for (var i = resultSegment.Offset; i < resultSegment.Count; i++)
            {
                result[i] = resultSegment.Array[i];
            }
            return result;
        }
    }
}
