using Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Domain.VOs;
using Domain.Entidades;
namespace Infra;

public class EFContext : DbContext
{

    public DbSet<Orcamento> Orcamento { get; set; }

    public EFContext(DbContextOptions<EFContext> options) : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);        

        modelBuilder.Entity<Orcamento>(entity =>
        {
            entity.Property(e => e.Ativo).HasDefaultValue(true);
            entity.ToTable("Orcamento");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacao).HasMaxLength(1000);
            entity.Property(e => e.OrcamentoAprovado).IsRequired(false);
            entity.Property(e => e.DataDecisaoClienteAprovacao).IsRequired(false);
            entity.Property(e => e.OrcamentoPago).IsRequired(false);
            entity.Property(e => e.DataDecisaoClientePagamento).IsRequired(false);
            entity.Property(e => e.DataCadastro).IsRequired();
            entity.Property(o => o.OrdemServicoId)
            .IsRequired();
        });
    }
}
