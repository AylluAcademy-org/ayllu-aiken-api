using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Serializers
{
    /// <summary>
    /// Representa el parámetro de acuñación (`MintParam`) requerido por el validador.
    /// </summary>
    public class MintParam
    {
        [JsonPropertyName("constructor")]
        public int Constructor => 0;

        [JsonPropertyName("fields")]
        public object[] Fields => new object[]
        {
            new { bytes = RegistrarPkh },
            new { bytes = TokenName },
            new
            {
                constructor = 0,
                fields = new object[]
                {
                    new { bytes = UtxoTxId },
                    new { int_ = UtxoIndex }
                }
            },
            new { bytes = StudentPkh }
        };

        public string RegistrarPkh { get; set; } = string.Empty;
        public string TokenName { get; set; } = string.Empty;
        public string UtxoTxId { get; set; } = string.Empty;
        public int UtxoIndex { get; set; }
        public string StudentPkh { get; set; } = string.Empty;
    }
}
