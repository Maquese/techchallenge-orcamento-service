using Domain.Entidades;

namespace Application.Interfaces; 

public interface BaseRepository<T> where T : IEntity
{
    Task Adicionar(T entity);
    Task Atualizar(T entity);
    Task Inativar(T entity);
    Task<List<T>> ListarAtivos();
    Task<T> ObterPorId(Guid id);
    Task<List<T>> ListarPorIds(List<Guid> ids);
    Task Remover(T entity);
}
