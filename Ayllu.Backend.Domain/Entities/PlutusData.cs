using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Entities
{
    public class PlutusData
    {
        [JsonPropertyName("constructor")]
        public int Constructor { get; set; }

        [JsonPropertyName("fields")]
        public List<object> Fields { get; set; } = new();
    }

    public class PlutusBytes
    {
        [JsonPropertyName("bytes")]
        public string Bytes { get; set; }

        public PlutusBytes(string bytes) => Bytes = bytes;
    }

    public class PlutusInt
    {
        [JsonPropertyName("int")]
        public int Int { get; set; }

        public PlutusInt(int value) => Int = value;
    }
}
