using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Serializers
{
    /// <summary>
    /// Representa los parámetros del validador de registro (`RegParam` en Aiken).
    /// Define el PKH del registrador autorizado.
    /// </summary>
    public class RegParam
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new { bytes = RegistrarPkh }
        };

        public string RegistrarPkh { get; set; } = string.Empty;
    }
}
