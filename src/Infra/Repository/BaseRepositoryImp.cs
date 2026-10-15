using Domain.Entidades;
using Domain.Exceptions;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infra;

public class BaseRepositoryImp<T> : BaseRepository<T> where T : IEntity
{
    protected readonly EFContext _context;
    public BaseRepositoryImp(EFContext context)
    {
        _context = context;
    }

    
    public async Task Adicionar(T entity)
    {
        try
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
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
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task Inativar(T entity)
    {
        entity.Inativar();
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<T>> ListarAtivos()
    {
        return await _context.Set<T>().Where(e => e.Ativo).ToListAsync();
    }

    public virtual async Task<T> ObterPorId(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> ListarPorIds(List<int> ids)
    {
        return await _context.Set<T>().Where(e => ids.Contains(e.Id) && e.Ativo).ToListAsync();
    }

    public async Task Remover(T entity)
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
