using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Serializers
{
    public class RegistrationRedeemer
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new { constructor = Action, fields = Array.Empty<object>() }, // Action: 0=Register, 1=Cancel
            new { bytes = StudentPkh },
            new
            {
                constructor = 0,
                fields = new object[]
                {
                    new { bytes = TxHash },
                    new { int_ = OutputIndex }
                }
            }
        };

        // Datos requeridos para rellenar desde el controller
        public int Action { get; set; }              // 0: Register, 1: Cancel
        public string StudentPkh { get; set; } = string.Empty;
        public string TxHash { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }
}
