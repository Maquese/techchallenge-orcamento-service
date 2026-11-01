using System;
using Xunit;
using Domain.Aggregates;

namespace Domain.Tests
{
    public class OrcamentoTests
    {
        [Fact]
        public void Construtor_DeveInicializarCorretamente()
        {
            // Arrange
            int ordemServicoId = 1;
            decimal valorTotal = 1000m;
            string observacao = "Troca de peças";

            // Act
            var orcamento = new Orcamento(ordemServicoId, valorTotal, observacao);

            // Assert
            Assert.Equal(ordemServicoId, orcamento.OrdemServicoId);
            Assert.Equal(valorTotal, orcamento.ValorTotal);
            Assert.Equal(observacao, orcamento.Observacao);
            Assert.Null(orcamento.OrcamentoAprovado);
            Assert.Null(orcamento.DataDecisaoClienteAprovacao);
            Assert.True(orcamento.Ativo);
            Assert.True(orcamento.DataCadastro <= DateTime.UtcNow);
        }

        [Fact]
        public void AprovarOrcamento_DeveDefinirAprovadoComoTrue()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.AprovarOrcamento();

            Assert.True(orcamento.OrcamentoAprovado);
            Assert.NotNull(orcamento.DataDecisaoClienteAprovacao);
        }

        [Fact]
        public void ReprovarOrcamento_DeveDefinirAprovadoComoFalse()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.ReprovarOrcamento();

            Assert.False(orcamento.OrcamentoAprovado);
            Assert.NotNull(orcamento.DataDecisaoClienteAprovacao);
        }

        [Fact]
        public void NegarOrcamento_DeveDefinirAprovadoComoFalse()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.NegarOrcamento();

            Assert.False(orcamento.OrcamentoAprovado);
            Assert.NotNull(orcamento.DataDecisaoClienteAprovacao);
        }

        [Fact]
        public void MarcarOrcamentoPago_DeveDefinirPagoComoTrue()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.MarcarOrcamentoPago();

            Assert.True(orcamento.OrcamentoPago);
            Assert.NotNull(orcamento.DataDecisaoClientePagamento);
        }

        [Fact]
        public void MarcarOrcamentoNaoPago_DeveDefinirPagoComoFalse()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.MarcarOrcamentoNaoPago();

            Assert.False(orcamento.OrcamentoPago);
            Assert.NotNull(orcamento.DataDecisaoClientePagamento);
        }

        [Fact]
        public void Inativar_DeveMarcarOrcamentoComoInativo()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.Inativar();

            Assert.False(orcamento.Ativo);
            Assert.False(orcamento.EstaAtivo());
        }
    }
}
