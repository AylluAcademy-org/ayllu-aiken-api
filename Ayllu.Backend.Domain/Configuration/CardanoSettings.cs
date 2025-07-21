using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Configuration
{
    public class CardanoSettings
    {
        public string? BaseDir { get; set; }
        public string? WalletAddressPath { get; set; }
        public string? SigningKeyPath { get; set; }
        public string? PolicyScriptPath { get; set; }
        public string? PolicyId { get; set; }
        public string? TokenHex { get; set; }
        public string? BlueprintPath { get; set; }
        public string? Network { get; set; }

        public string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            if (string.IsNullOrEmpty(BaseDir))
                throw new InvalidOperationException("BaseDir no está configurado y se requiere para rutas relativas.");

            return Path.Combine(BaseDir, relativePath);
        }

    }
}
