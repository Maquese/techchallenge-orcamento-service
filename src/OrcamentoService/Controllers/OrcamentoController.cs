using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.UseCases.Orcamentos;
using Application.Models.Requests;

namespace AutoReparaAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    // [Authorize]
    public class OrcamentoController : ControllerBase
    {
        public OrcamentoController()
        {
            
        }

        [HttpPost]
        public async Task<IActionResult> CriarOrcamento([FromServices] AdicionarOrcamentoHandler adicionarOrcamentoHandler, [FromBody] AddOrcamentoRequest model)
        {
            var id = await adicionarOrcamentoHandler.Handle(model);
            return Ok(id);
        }

        [HttpPost]
        public async Task<IActionResult> AprovarOrcamento([FromServices] AprovarOrcamentoHandler aprovarOrcamentoHandler, [FromBody] int orcamentoId)
        {
            
            return Ok(await aprovarOrcamentoHandler.Handle(orcamentoId));
        }

            [HttpPost]
        public async Task<IActionResult> NegarOrcamento([FromServices] NegarOrcamentoHandler negarOrcamentoHandler, [FromBody] int orcamentoId)
        {
            await negarOrcamentoHandler.Handle(orcamentoId);
            return Ok("Orçamento negado com sucesso");
        }

        [HttpPost]
        public async Task<IActionResult> PagamentoEfetuado([FromServices] PagarOrcamentoHandler pagarOrcamentoHandler, [FromBody] int orcamentoId)
        {
            
            return Ok(await pagarOrcamentoHandler.Handle(orcamentoId));
        }

        [HttpGet]
        public async Task<IActionResult> ListarOrcamentos([FromServices] ListarOrcamentoHandler listarOrcamentoHandler)
        {
            var orcamentos = await listarOrcamentoHandler.Handle();
            return Ok(orcamentos);
        }
    }
}
