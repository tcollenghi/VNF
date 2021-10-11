using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using VNF.Integration.DataLayer.Model;

namespace VNF.Integration.DataLayer
{
    public partial class VNFContext : DbContext
    {
        public VNFContext()
            : base("name=VNFContext")
        {
        }

        public DbSet<tbImportacaoNotaFiscal> tbImportacaoNotaFiscal { get; set; }
        public DbSet<tbImportacaoTipoDocumento> tbImportacaoTipoDocumento { get; set; }
        public DbSet<tbImportacaoItemNF> tbImportacaoItemNF { get; set; }
        public DbSet<tbNFE> tbNFE { get; set; }
        public DbSet<vwStatusIntegracao> vwStatusIntegracao { get; set; }
        public DbSet<TbLOGApplication> TbLOGApplication { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Properties<string>()
                .Configure(p => p.HasColumnType("varchar"));

            modelBuilder.Properties<string>()
                .Configure(p => p.HasMaxLength(255));

            modelBuilder.Configurations.Add(new vwStatusIntegracaoConfiguration());
        }
    }
}