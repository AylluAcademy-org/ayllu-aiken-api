using Ayllu.Backend.Application.Interfaces;
using Ayllu.Backend.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Ayllu.Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentAylluController: ControllerBase
    {
        private readonly ICardanoTransactionService _cardanoService;
        public EnrollmentAylluController(ICardanoTransactionService cardanoService)
        {
            _cardanoService = cardanoService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentAddress) ||
            string.IsNullOrWhiteSpace(request.StudentPKH) ||
            string.IsNullOrWhiteSpace(request.TxHash))
            {
                return BadRequest("Datos incompletos.");
            }

            var (success, message) = await _cardanoService.RegisterStudentAsync(request);

            return success ? Ok(message) : StatusCode(500, message);
        }
    }
}
