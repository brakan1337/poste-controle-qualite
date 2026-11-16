using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ControleQualite.Donnees;

/// <summary>
/// Choisit la base selon la configuration : SQL Server chez le client, SQLite pour essayer
/// le poste sans serveur. Le modèle et le code sont les mêmes ; seules les migrations
/// (le schéma de production) sont écrites pour SQL Server.
/// </summary>
public static class FabriqueBase
{
    public static DbContextOptions<BaseQualite> Options(string fournisseur, string chaineConnexion)
    {
        var options = new DbContextOptionsBuilder<BaseQualite>();
        _ = fournisseur.ToUpperInvariant() switch
        {
            "SQLSERVER" => options.UseSqlServer(chaineConnexion),
            "SQLITE" => options.UseSqlite(chaineConnexion),
            _ => throw new ArgumentException($"Base inconnue : {fournisseur} (SqlServer ou Sqlite)."),
        };
        return options.Options;
    }

    /// <summary>Applique les migrations sur SQL Server ; crée directement le schéma sur SQLite.</summary>
    public static void Preparer(BaseQualite db)
    {
        if (db.Database.IsSqlServer())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
    }
}

/// <summary>Utilisée par « dotnet ef migrations add » pour générer le schéma SQL Server.</summary>
public sealed class FabriqueConception : IDesignTimeDbContextFactory<BaseQualite>
{
    public BaseQualite CreateDbContext(string[] args) =>
        new(FabriqueBase.Options("SqlServer", "Server=(localdb)\\MSSQLLocalDB;Database=ControleQualite;Trusted_Connection=True"));
}
