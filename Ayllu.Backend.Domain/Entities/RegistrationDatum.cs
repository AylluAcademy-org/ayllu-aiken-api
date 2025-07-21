using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Entities
{
    /// <summary>
    /// Representa el datum de registro para el validador.
    /// Coincide con `RegistrationDatum` definido en el blueprint de Aiken.
    /// </summary>
    public class RegistrationDatum
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new { bytes = StudentPkh }
        };

        public string StudentPkh { get; set; } = string.Empty;
    }
}
