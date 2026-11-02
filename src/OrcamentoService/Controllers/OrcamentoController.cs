using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.UseCases.Orcamentos;
using Application.Models.Requests;
using Application.Exceptions;
using Domain.Exceptions;

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
        public async Task<IActionResult> AprovarOrcamento([FromServices] AprovarOrcamentoHandler aprovarOrcamentoHandler, [FromBody] Guid orcamentoId)
        {
            
            return Ok(await aprovarOrcamentoHandler.Handle(orcamentoId));
        }

        [HttpPost]
        public async Task<IActionResult> NegarOrcamento([FromServices] NegarOrcamentoHandler negarOrcamentoHandler, [FromBody] Guid orcamentoId)
        {
            await negarOrcamentoHandler.Handle(orcamentoId);
            return Ok("Orçamento negado com sucesso");
        }

        /// <summary>Processa o pagamento de um orçamento aprovado com cartão tokenizado.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Application.Models.Responses.PagamentoOrcamentoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> PagamentoEfetuado(
            [FromServices] PagarOrcamentoHandler pagarOrcamentoHandler,
            [FromBody] PagarOrcamentoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await pagarOrcamentoHandler.Handle(
                    request.OrcamentoId,
                    request,
                    cancellationToken);
                return Ok(result);
            }
            catch (OrcamentoNaoEncontradoException)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Orçamento não encontrado",
                    detail: "O orçamento informado não foi encontrado.");
            }
            catch (DomainException exception)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Não foi possível iniciar o pagamento",
                    detail: exception.Message);
            }
            catch (PaymentProviderException exception)
            {
                var statusCode = exception.StatusCode is StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status502BadGateway;

                return Problem(
                    statusCode: statusCode,
                    title: "Não foi possível processar o pagamento",
                    detail: statusCode == StatusCodes.Status400BadRequest
                        ? "Verifique os dados de pagamento e tente novamente."
                        : "O provedor de pagamento está indisponível. Tente novamente.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListarOrcamentos([FromServices] ListarOrcamentoHandler listarOrcamentoHandler)
        {
            var orcamentos = await listarOrcamentoHandler.Handle();
            return Ok(orcamentos);
        }

        [HttpGet]
        public async Task<IActionResult> OrcamentoAprovado([FromServices] OrcamentoAprovadoHandler listarOrcamentoHandler, [FromQuery]int ordemServicoId)
        {
            var orcamentos = await listarOrcamentoHandler.Handle(ordemServicoId);
            return Ok(orcamentos);
        }
    }
}
