using Domain.Entidades;
using Domain.Exceptions;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Amazon.DynamoDBv2.DataModel;

namespace Infra;

public class BaseRepositoryImp<T> : BaseRepository<T> where T : IEntity
{
    //protected readonly EFContext _context;
    protected readonly IDynamoDBContext _context;
    public BaseRepositoryImp(IDynamoDBContext context)
    {
        _context = context;
    }

    
    public async Task Adicionar(T entity)
    {
        try
        {
            await _context.SaveAsync(entity);
        }
        catch (DbUpdateException ex)
        {
            throw TraducaoErroUnicidade(ex);
        }
    }

    private DomainException TraducaoErroUnicidade(DbUpdateException ex)
    {
        var mensagem = ex.InnerException?.Message ?? ex.Message;

        if (mensagem.Contains("Duplicate entry") && mensagem.Contains("Cpf"))
            return new DomainException("CPF já cadastrado");

        if (mensagem.Contains("Duplicate entry") && mensagem.Contains("Email"))
            return new DomainException("Email já cadastrado");

        if (mensagem.Contains("Duplicate entry") && mensagem.Contains("Celular"))
            return new DomainException("Celular já cadastrado");
        if (mensagem.Contains("Duplicate entry") && mensagem.Contains("Placa"))
            return new DomainException("Placa já cadastrado");
        if (mensagem.Contains("Duplicate entry") && mensagem.Contains("Documento"))
            return new DomainException("Documento já cadastrado");

        if (mensagem.Contains("Duplicate entry"))
            return new DomainException("Dados duplicados no banco de dados");

        return new DomainException("Erro ao salvar dados");
    }

    public async Task Atualizar(T entity)
    {
        await _context.SaveAsync(entity);
    }

    public async Task Inativar(T entity)
    {
        entity.Inativar();
        await _context.SaveAsync(entity);
    }

    public async Task<List<T>> ListarAtivos()
    {
        return await _context.ScanAsync<T>(Array.Empty<ScanCondition>()).GetRemainingAsync();
    }

    public virtual async Task<T> ObterPorId(Guid id)
    {
        var search = _context.QueryAsync<T>(id);
        var itens = await search.GetRemainingAsync();

        return itens.FirstOrDefault();
    }

    public async Task<List<T>> ListarPorIds(List<Guid> ids)
    {
        var batch = _context.CreateBatchGet<T>();

        foreach (var id in ids)
            batch.AddKey(id);

        await batch.ExecuteAsync();

        return batch.Results
            .Where(item => item.Ativo)
            .ToList();
    }

    public async Task Remover(T entity)
    {
        await _context.DeleteAsync<T>(entity);
    }
}
