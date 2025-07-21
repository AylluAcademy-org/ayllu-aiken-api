using Ayllu.Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Interfaces
{
    public interface ICardanoTransactionService
    {
        Task<(bool Success, string Message)> TransferTokenAsync(TransferTokenRequest request);
        Task<(bool success, string message)> RegisterStudentAsync(RegisterStudentRequest request);

    }
}
