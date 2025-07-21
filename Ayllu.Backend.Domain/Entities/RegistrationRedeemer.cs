using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Entities
{
    /// <summary>
    /// Representa el redeemer de registro usado para consumir el UTxO de matrícula.
    /// Coincide con el tipo `RegistrationRedeemer` en Aiken.
    /// </summary>
    public class RegistrationRedeemer
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new
            {
                constructor = Action,  // 0 = Register, 1 = Cancel
                fields = Array.Empty<object>()
            },
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

        public int Action { get; set; } = 0;
        public string StudentPkh { get; set; } = string.Empty;
        public string TxHash { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }
}
