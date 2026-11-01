using Application.Interfaces;
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
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        var response = await new PagarOrcamentoHandler(repository.Object).Handle(orcamento.Id);

        Assert.True(response.Success);
        Assert.Equal("Orcamento pago com sucesso", response.Message);
        Assert.Equal(orcamento.Id, Assert.IsType<Guid>(response.Data));
        Assert.True(orcamento.OrcamentoPago);
        repository.Verify(r => r.Atualizar(orcamento), Times.Once);
    }

    [Fact]
    public async Task Pagar_DeveFalharQuandoOrcamentoNaoEncontrado()
    {
        var id = Guid.NewGuid();
        var repository = RepositoryComOrcamento(id, null);

        await Assert.ThrowsAsync<DomainException>(() =>
            new PagarOrcamentoHandler(repository.Object).Handle(id));
    }

    [Fact]
    public async Task Pagar_DeveFalharQuandoPagamentoJaFoiDecidido()
    {
        var orcamento = NovoOrcamento();
        orcamento.MarcarOrcamentoNaoPago();
        var repository = RepositoryComOrcamento(orcamento.Id, orcamento);

        await Assert.ThrowsAsync<DomainException>(() =>
            new PagarOrcamentoHandler(repository.Object).Handle(orcamento.Id));
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

    private static Mock<OrcamentoRepository> RepositoryComOrcamento(Guid id, Orcamento? orcamento)
    {
        var repository = new Mock<OrcamentoRepository>();
        repository.Setup(r => r.ObterPorId(id)).ReturnsAsync(orcamento!);
        repository.Setup(r => r.Atualizar(It.IsAny<Orcamento>())).Returns(Task.CompletedTask);
        return repository;
    }
}
