using Ayllu.Backend.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ayllu.Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidatorController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ValidatorController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("addresses")]
        public async Task<IActionResult> GetValidatorAddresses()
        {
            try
            {
                var network = _config["Cardano:Network"] ?? "--testnet-magic 2";
                var blueprintService = new AikenBlueprintService();
                var result = await blueprintService.GenerateValidatorAddressesAsync(network);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
