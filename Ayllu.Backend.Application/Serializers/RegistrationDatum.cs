using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Serializers
{
    public class RegistrationDatum
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new { bytes = StudentPkh }
        };

        // Este valor lo llenas desde el request
        public string StudentPkh { get; set; } = string.Empty;
    }
}
