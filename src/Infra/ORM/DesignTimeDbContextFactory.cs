using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infra;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EFContext>
{
    public EFContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EFContext>();
        
        // Connection string padrão (será usada apenas para design-time/migrations)
        var connectionString = "Server=127.0.0.1;Port=3306;Database=Tests;User=root;Password=SuaSenhaSegura;";
        
        optionsBuilder.UseMySQL(connectionString);
        
        return new EFContext(optionsBuilder.Options);
    }
}
