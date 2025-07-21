using Ayllu.Backend.Application.Interfaces;
using Ayllu.Backend.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Ayllu.Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimAylluController: ControllerBase
    {
        private readonly ICardanoTransactionService _cardanoService;

        public ClaimAylluController(ICardanoTransactionService cardanoService)
        {
            _cardanoService = cardanoService;
        }

        [HttpPost("claim")]
        public async Task<IActionResult> ClaimAyllu([FromBody] TransferTokenRequest request)
        {
            if (request.Amount <= 0 || request.Amount > 10)
            {
                return BadRequest("La cantidad de recompensas solicitdas es incorrecta");
            }
            if (IsValidCardanoAddress(request.ReceiverAddress)==false)
            {

                return BadRequest("Dirección incorrecta");
            }

            var (success, message) = await _cardanoService.TransferTokenAsync(request);
            return success ? Ok(message) : StatusCode(500, message);
        }
        public static bool IsValidCardanoAddress(string address)
        {
            // Shelley-style testnet: starts with addr_test... (mainnet: addr1...)
            var regex = new Regex(@"^addr(_test)?1[0-9a-z]{58,}$", RegexOptions.IgnoreCase);
            return regex.IsMatch(address);

        }
    }
}
