using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ayllu.Backend.Domain.Entities
{
    public class RegisterStudentRequest
    {
        public string StudentAddress { get; set; } = string.Empty;
        public string StudentPKH { get; set; } = string.Empty;

        public string TxHash { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }
}
