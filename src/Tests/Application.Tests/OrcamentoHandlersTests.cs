using Application.Interfaces;
using Application.Exceptions;
using Application.Models.Requests;
using Application.UseCases.Orcamentos;
using Domain.Aggregates;
using Domain.Exceptions;
using Moq;

namespace Application.Tests;

public class OrcamentoHandlersTests
{
    [Fact]
    public async Task Adicionar_DeveCriarOrcamentoQuandoNaoExisteAberto()
    {
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ExisteOrcamentoEmAbertoParaOrdemdeSerivico(12))
            .ReturnsAsync(false);
        Orcamento? adicionado = null;
        repository.Setup(r => r.Adicionar(It.IsAny<Orcamento>()))
            .Callback<Orcamento>(orcamento => adicionado = orcamento)
            .Returns(Task.CompletedTask);
        var request = new AddOrcamentoRequest
        {
            OrdemServicoId = 12,
            ValorTotal = 150m,
            Observacao = "Troca de peças"
        };

        var response = await new AdicionarOrcamentoHandler(repository.Object).Handle(request);

        Assert.True(response.Success);
        Assert.Equal("Criando com sucesso", response.Message);
        Assert.NotNull(adicionado);
        Assert.Equal(request.OrdemServicoId, adicionado.OrdemServicoId);
        Assert.Equal(request.ValorTotal, adicionado.ValorTotal);
        Assert.Equal(request.Observacao, adicionado.Observacao);
        Assert.Equal(adicionado.Id, response.Data);
        repository.Verify(r => r.Adicionar(It.IsAny<Orcamento>()), Times.Once);
    }

    [Fact]
    public async Task Adicionar_DeveFalharQuandoOrdemJaTemOrcamentoAberto()
    {
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ExisteOrcamentoEmAbertoParaOrdemdeSerivico(12))
            .ReturnsAsync(true);
        var handler = new AdicionarOrcamentoHandler(repository.Object);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new AddOrcamentoRequest
        {
            OrdemServicoId = 12,
            ValorTotal = 150m,
            Observacao = "Troca de peças"
        }));

        repository.Verify(r => r.Adicionar(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task Aprovar_DeveAtualizarOrcamentoEncontrado()
    {
        var orcamento = NovoOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        var response = await new AprovarOrcamentoHandler(repository.Object).Handle(orcamento.Id);

        Assert.True(response.Success);
        Assert.Equal("Orcamento Aprovado com sucesso", response.Message);
        Assert.Equal(orcamento.Id, Assert.IsType<Guid>(response.Data));
        Assert.True(orcamento.OrcamentoAprovado);
        repository.Verify(r => r.Atualizar(orcamento), Times.Once);
    }

    [Fact]
    public async Task Aprovar_DeveFalharQuandoOrcamentoNaoEncontrado()
    {
        var id = Guid.NewGuid();
        var repository = RepositoryComOrcamento(id, null);

        await Assert.ThrowsAsync<DomainException>(() =>
            new AprovarOrcamentoHandler(repository.Object).Handle(id));
        repository.Verify(r => r.Atualizar(It.IsAny<Orcamento>()), Times.Never);
    }

    [Fact]
    public async Task Aprovar_DeveFalharQuandoOrcamentoEstaInativo()
    {
        var orcamento = NovoOrcamento();
        orcamento.Inativar();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new AprovarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
    }

    [Fact]
    public async Task Aprovar_DeveFalharQuandoPagamentoJaFoiDecidido()
    {
        var orcamento = NovoOrcamento();
        orcamento.MarcarOrcamentoPago();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new AprovarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
    }

    [Fact]
    public async Task Aprovar_DeveFalharQuandoAprovacaoJaFoiDecidida()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new AprovarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
    }

    [Fact]
    public async Task Negar_DeveAtualizarOrcamentoEncontrado()
    {
        var orcamento = NovoOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        var response = await new NegarOrcamentoHandler(repository.Object).Handle(orcamento.Id);

        Assert.True(response.Success);
        Assert.Equal("Orçamento negado com sucesso.", response.Message);
        Assert.False(orcamento.OrcamentoAprovado);
        repository.Verify(r => r.Atualizar(orcamento), Times.Once);
    }

    [Fact]
    public async Task Pagar_DeveExigirMesmaChaveQuandoPagamentoEstaPendente()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();
        gateway.Setup(g => g.CreatePaymentAsync(
                It.IsAny<MercadoPagoPaymentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoPaymentResult(
                "order-id",
                "payment-id",
                "pending",
                "waiting_for_payment"));
        var handler = new PagarOrcamentoHandler(repository.Object, gateway.Object);
        var primeiraTentativa = NovaSolicitacaoPagamento(orcamento.Id);

        await handler.Handle(orcamento.Id, primeiraTentativa, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                orcamento.Id,
                primeiraTentativa with { IdempotencyKey = Guid.NewGuid() },
                CancellationToken.None));

        gateway.Verify(g => g.CreatePaymentAsync(
            It.IsAny<MercadoPagoPaymentRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pagar_DevePermitirNovaChaveQuandoMercadoPagoRecusaRequisicao()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();
        gateway.SetupSequence(g => g.CreatePaymentAsync(
                It.IsAny<MercadoPagoPaymentRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentProviderException(400))
            .ReturnsAsync(new MercadoPagoPaymentResult(
                "order-id",
                "payment-id",
                "processed",
                "accredited"));
        var handler = new PagarOrcamentoHandler(repository.Object, gateway.Object);
        var request = NovaSolicitacaoPagamento(orcamento.Id);

        await Assert.ThrowsAsync<PaymentProviderException>(() =>
            handler.Handle(orcamento.Id, request, CancellationToken.None));

        var result = await handler.Handle(
            orcamento.Id,
            request with { IdempotencyKey = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.Pago);
        gateway.Verify(g => g.CreatePaymentAsync(
            It.IsAny<MercadoPagoPaymentRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Negar_DeveFalharQuandoOrcamentoNaoEncontrado()
    {
        var id = Guid.NewGuid();
        var repository = RepositoryComOrcamento(id, null);

        await Assert.ThrowsAsync<DomainException>(() =>
            new NegarOrcamentoHandler(repository.Object).Handle(id));
    }

    [Fact]
    public async Task Negar_DeveFalharQuandoPagamentoJaFoiDecidido()
    {
        var orcamento = NovoOrcamento();
        orcamento.MarcarOrcamentoPago();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new NegarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
    }

    [Fact]
    public async Task Negar_DeveFalharQuandoAprovacaoJaFoiDecidida()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new NegarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
    }

    [Fact]
    public async Task Pagar_DeveAtualizarOrcamentoEncontrado()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();
        gateway.Setup(g => g.CreatePaymentAsync(
                It.IsAny<MercadoPagoPaymentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoPaymentResult(
                "order-id",
                "payment-id",
                "processed",
                "accredited"));
        var request = NovaSolicitacaoPagamento(orcamento.Id);

        var response = await new PagarOrcamentoHandler(repository.Object, gateway.Object)
            .Handle(orcamento.Id, request, CancellationToken.None);

        Assert.Equal(orcamento.Id, response.OrcamentoId);
        Assert.Equal(250m, response.Valor);
        Assert.Equal("order-id", response.MercadoPagoOrderId);
        Assert.Equal("payment-id", response.MercadoPagoPaymentId);
        Assert.Equal("processed", response.Status);
        Assert.True(response.Pago);
        Assert.True(orcamento.OrcamentoPago);
        Assert.Equal("order-id", orcamento.MercadoPagoOrderId);
        gateway.Verify(g => g.CreatePaymentAsync(
            It.Is<MercadoPagoPaymentRequest>(payment =>
                payment.Amount == orcamento.ValorTotal &&
                payment.ExternalReference == orcamento.Id.ToString() &&
                payment.CardToken == request.CardToken),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.Atualizar(orcamento), Times.Exactly(2));
    }

    [Fact]
    public async Task Pagar_DeveRegistrarRecusaSemMarcarOrcamentoComoPago()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();
        gateway.Setup(g => g.CreatePaymentAsync(
                It.IsAny<MercadoPagoPaymentRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MercadoPagoPaymentResult(
                "order-id",
                "payment-id",
                "failed",
                "cc_rejected_other_reason"));

        var response = await new PagarOrcamentoHandler(repository.Object, gateway.Object)
            .Handle(orcamento.Id, NovaSolicitacaoPagamento(orcamento.Id), CancellationToken.None);

        Assert.False(response.Pago);
        Assert.Null(orcamento.OrcamentoPago);
        Assert.Null(orcamento.DataDecisaoClientePagamento);
        Assert.Equal("failed", orcamento.MercadoPagoPaymentStatus);
        repository.Verify(r => r.Atualizar(orcamento), Times.Exactly(2));
    }

    [Fact]
    public async Task Pagar_DeveFalharQuandoOrcamentoNaoEncontrado()
    {
        var id = Guid.NewGuid();
        var repository = RepositoryComOrcamento(id, null);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();

        await Assert.ThrowsAsync<OrcamentoNaoEncontradoException>(() =>
            new PagarOrcamentoHandler(repository.Object, gateway.Object)
                .Handle(id, NovaSolicitacaoPagamento(id), CancellationToken.None));

        gateway.Verify(g => g.CreatePaymentAsync(
            It.IsAny<MercadoPagoPaymentRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pagar_DeveFalharQuandoOrcamentoNaoFoiAprovado()
    {
        var orcamento = NovoOrcamento();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();

        await Assert.ThrowsAsync<DomainException>(() =>
            new PagarOrcamentoHandler(repository.Object, gateway.Object)
                .Handle(orcamento.Id, NovaSolicitacaoPagamento(orcamento.Id), CancellationToken.None));

        gateway.Verify(g => g.CreatePaymentAsync(
            It.IsAny<MercadoPagoPaymentRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pagar_DeveFalharQuandoPagamentoJaFoiDecidido()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        orcamento.MarcarOrcamentoNaoPago();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);
        var gateway = new Mock<IMercadoPagoPaymentGateway>();

        await Assert.ThrowsAsync<DomainException>(() =>
            new PagarOrcamentoHandler(repository.Object, gateway.Object)
                .Handle(orcamento.Id, NovaSolicitacaoPagamento(orcamento.Id), CancellationToken.None));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(null, false, false)]
    [InlineData(true, true, false)]
    public async Task ConsultarAprovacao_DeveIndicarSeOrcamentoEstaAprovadoENaoPago(
        bool? aprovado,
        bool pago,
        bool esperado)
    {
        var orcamento = NovoOrcamento();
        if (aprovado == true)
            orcamento.AprovarOrcamento();
        else if (aprovado == false)
            orcamento.NegarOrcamento();
        if (pago)
            orcamento.MarcarOrcamentoPago();

        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.EncontrarOrcamentoPorOrdemServico(orcamento.OrdemServicoId))
            .ReturnsAsync(orcamento);

        var response = await new OrcamentoAprovadoHandler(repository.Object)
            .Handle(orcamento.OrdemServicoId);

        Assert.True(response.Success);
        Assert.Equal("Ok", response.Message);
        var aprovadoResultante = (bool)response.Data!.GetType()
            .GetProperty("OrcamentoAprovado")!.GetValue(response.Data)!;
        Assert.Equal(esperado, aprovadoResultante);
    }

    [Fact]
    public async Task Listar_DeveMapearOrcamentosAtivos()
    {
        var orcamento = NovoOrcamento();
        orcamento.AprovarOrcamento();
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ListarAtivos()).ReturnsAsync(new List<Orcamento> { orcamento });

        var response = await new ListarOrcamentoHandler(repository.Object).Handle();

        Assert.True(response.Success);
        Assert.Equal("Listado com sucesso", response.Message);
        var item = Assert.Single(Assert.IsType<List<Application.Models.Responses.ListOrcamentoResponse>>(
            response.Data));
        Assert.Equal(orcamento.Id, item.Id);
        Assert.Equal(orcamento.OrdemServicoId, item.OrdemServicoId);
        Assert.Equal(orcamento.ValorTotal, item.Valor);
        Assert.Equal(orcamento.OrcamentoAprovado, item.OrcamentoAprovado);
        Assert.Equal(orcamento.OrcamentoPago, item.OrcamentoPago);
        Assert.Equal(orcamento.DataCadastro, item.DataCadastro);
    }

    [Fact]
    public async Task Listar_DeveRetornarListaVaziaQuandoNaoHaOrcamentosAtivos()
    {
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ListarAtivos()).ReturnsAsync(new List<Orcamento>());

        var response = await new ListarOrcamentoHandler(repository.Object).Handle();

        Assert.Empty(Assert.IsType<List<Application.Models.Responses.ListOrcamentoResponse>>(
            response.Data));
    }

    private static Orcamento NovoOrcamento() => new(42, 250m, "Serviço");

    private static PagarOrcamentoRequest NovaSolicitacaoPagamento(Guid orcamentoId) => new()
    {
        OrcamentoId = orcamentoId,
        CardToken = "card-token-from-sdk",
        PaymentMethodId = "visa",
        Installments = 1,
        PayerEmail = "buyer@example.com",
        IdempotencyKey = Guid.NewGuid()
    };

    private static Mock<OrcamentoRepository> RepositoryComOrcamento(Guid id, Orcamento? orcamento)
    {
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ObterPorId(id)).ReturnsAsync(orcamento!);
        repository.Setup(r => r.Atualizar(It.IsAny<Orcamento>())).Returns(Task.CompletedTask);
        return repository;
    }
}
