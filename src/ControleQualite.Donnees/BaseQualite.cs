using Microsoft.EntityFrameworkCore;

namespace ControleQualite.Donnees;

public class BaseQualite(DbContextOptions<BaseQualite> options) : DbContext(options)
{
    public DbSet<Reference> References => Set<Reference>();
    public DbSet<CoteEntite> Cotes => Set<CoteEntite>();
    public DbSet<OrdreFabrication> OrdresFabrication => Set<OrdreFabrication>();
    public DbSet<PieceControlee> Pieces => Set<PieceControlee>();
    public DbSet<MesureEntite> Mesures => Set<MesureEntite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reference>(r =>
        {
            r.HasIndex(x => x.Code).IsUnique();
            r.Property(x => x.Code).HasMaxLength(30);
            r.Property(x => x.Designation).HasMaxLength(120);
        });

        modelBuilder.Entity<CoteEntite>(c =>
        {
            c.ToTable("Cotes", t => t.HasCheckConstraint("CK_Cotes_Limites", "[LimiteInferieure] < [LimiteSuperieure]"));
            c.Property(x => x.Nom).HasMaxLength(80);
            c.HasIndex(x => new { x.ReferenceId, x.Ordre }).IsUnique();
            c.HasOne<Reference>().WithMany(r => r.Cotes).HasForeignKey(x => x.ReferenceId);
        });

        modelBuilder.Entity<OrdreFabrication>(o =>
        {
            o.HasIndex(x => x.Numero).IsUnique();
            o.Property(x => x.Numero).HasMaxLength(30);
        });

        modelBuilder.Entity<PieceControlee>(p =>
        {
            // Un même numéro de pièce ne se contrôle qu'une fois par ordre de fabrication.
            p.HasIndex(x => new { x.OrdreFabricationId, x.Numero }).IsUnique();
            p.HasIndex(x => x.ControleeLe);
            p.Property(x => x.Numero).HasMaxLength(40);
            p.Property(x => x.Operateur).HasMaxLength(60);
            p.Property(x => x.Etat).HasConversion<string>().HasMaxLength(15);
            p.Property(x => x.MotifQuarantaine).HasMaxLength(300);
        });

        modelBuilder.Entity<MesureEntite>(m =>
        {
            m.HasOne<PieceControlee>().WithMany(p => p.Mesures).HasForeignKey(x => x.PieceControleeId);
            m.HasOne(x => x.Cote).WithMany().HasForeignKey(x => x.CoteId).OnDelete(DeleteBehavior.Restrict);
            m.HasIndex(x => x.CoteId);
        });
    }
}
