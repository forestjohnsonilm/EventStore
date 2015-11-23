using System;
using System.Collections.Generic;
using System.Text;

namespace EventStore.ClientAPI
{
    // This class mimics the behaviour of the old code which was not compatible with .NET core:
    // var builder = new DbConnectionStringBuilder(false) { ConnectionString = cs};
    // var result = from object key in builder.Keys
    //          select new KeyValuePair<string, string>(key.ToString(), builder[key.ToString()].ToString());
    class DbConnectionStringKeyValueEnumerator : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly string ConnectionString;
        private int readPosition = 0;
        private readonly char[] keyTrimChars;

        public DbConnectionStringKeyValueEnumerator(string connectionString)
        {
            ConnectionString = connectionString.Trim();
            keyTrimChars = new char[] { ' ', ';' };
        }

        private enum ReadMode
        {
            Key = 0,
            Value = 1
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var keyBuilder = new StringBuilder();
            var valueBuilder = new StringBuilder();
            var mode = ReadMode.Key;
            var returnedCount = 0;

            while (readPosition < ConnectionString.Length)
            {

                char currentChar = ConnectionString[readPosition];
                var atLastCharacter = readPosition == ConnectionString.Length - 1;
                char nextChar = atLastCharacter ? 'x' : ConnectionString[readPosition + 1];
                readPosition += 1;

                if (mode == ReadMode.Key && currentChar == '=')
                {
                    Console.Write(nextChar);
                    if (nextChar == '=')
                    {
                        keyBuilder.Append('=');
                        readPosition += 1;
                    }
                    else
                    {
                        mode = ReadMode.Value;
                    }
                }
                else if (mode == ReadMode.Value && (currentChar == ';' || atLastCharacter))
                {
                    if (atLastCharacter)
                    {
                        valueBuilder.Append(currentChar);
                    }
                    mode = ReadMode.Key;
                    var result = new KeyValuePair<string, string>(keyBuilder.ToString().ToLower().Trim(keyTrimChars), valueBuilder.ToString().Trim());
                    keyBuilder.Clear();
                    valueBuilder.Clear();
                    returnedCount += 1;
                    yield return result;
                }
                else
                {
                    (mode == ReadMode.Key ? keyBuilder : valueBuilder).Append(currentChar);
                }
            }

            if (returnedCount == 0)
            {
                throw new ArgumentException("Format of the initialization string does not conform to specification starting at index 0.");
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }


}
