using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Entities
{
    public class TransferTokenRequest
    {
        public string ReceiverAddress { get; set; } = string.Empty;
        public uint Amount { get; set; }
    }
}
