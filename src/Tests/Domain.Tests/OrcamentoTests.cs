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

        [Theory]
        [InlineData("APPROVED", "accredited", true)]
        [InlineData("PROCESSED", "ACCREDITED", true)]
        [InlineData("PROCESSED", "pending", null)]
        [InlineData("pending", "accredited", null)]
        public void RegistrarResultadoPagamento_DeveMarcarPagoApenasParaPagamentoAprovado(
            string status,
            string statusDetail,
            bool? esperadoComoPago)
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");

            orcamento.RegistrarResultadoPagamento("payment-123", status, statusDetail);

            Assert.Equal("payment-123", orcamento.MercadoPagoPaymentId);
            Assert.Equal(status, orcamento.MercadoPagoPaymentStatus);
            Assert.Equal(statusDetail, orcamento.MercadoPagoPaymentStatusDetail);
            Assert.Equal(esperadoComoPago, orcamento.OrcamentoPago);
            Assert.Equal(esperadoComoPago.HasValue, orcamento.DataDecisaoClientePagamento.HasValue);
        }

        [Fact]
        public void IniciarPagamento_DeveRegistrarChaveEReiniciarDadosDoProvedor()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");
            orcamento.RegistrarResultadoPagamento("payment-anterior", "approved", "accredited");

            orcamento.IniciarPagamento("idempotency-123");

            Assert.Null(orcamento.MercadoPagoOrderId);
            Assert.Null(orcamento.MercadoPagoPaymentId);
            Assert.Equal("idempotency-123", orcamento.MercadoPagoIdempotencyKey);
            Assert.Equal("processing", orcamento.MercadoPagoPaymentStatus);
            Assert.Equal("awaiting_provider_response", orcamento.MercadoPagoPaymentStatusDetail);
        }

        [Fact]
        public void RegistrarFalhaAoIniciarPagamento_DeveLimparChaveERegistrarFalha()
        {
            var orcamento = new Orcamento(1, 500m, "Revisão geral");
            orcamento.IniciarPagamento("idempotency-123");

            orcamento.RegistrarFalhaAoIniciarPagamento();

            Assert.Null(orcamento.MercadoPagoIdempotencyKey);
            Assert.Equal("failed", orcamento.MercadoPagoPaymentStatus);
            Assert.Equal("provider_rejected_request", orcamento.MercadoPagoPaymentStatusDetail);
        }
    }
}
