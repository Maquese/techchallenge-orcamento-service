namespace Domain.Entidades;

public abstract class IEntity
{
    public int Id { get; private set; }
    public bool Ativo {  get; protected set; } 

    public void Inativar()
    {
        Ativo = false;
    }

    public bool EstaAtivo()
    {
        return Ativo;
    }
}
