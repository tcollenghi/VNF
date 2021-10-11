using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Web;
using VNF.Integration.DataLayer.Model;

namespace VNF.Integration.DataLayer.Model
{
    [Table("vwStatusIntegracao", Schema = "DBO")]
    public class vwStatusIntegracao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [DisplayName("NFEID")]
        [Column("NFEID")]
        public string NFEID { get; set; }

        [DisplayName("STATUS_INTEGRACAO")]
        [Column("STATUS_INTEGRACAO")]
        public string STATUS_INTEGRACAO { get; set; }

        [DisplayName("DATA_INTEGRACAO")]
        [Column("DATA_INTEGRACAO")]
        public DateTime? DATA_INTEGRACAO { get; set; }        
    }
}

public class vwStatusIntegracaoConfiguration : EntityTypeConfiguration<vwStatusIntegracao>
{
    public vwStatusIntegracaoConfiguration()
    {
        ToTable("vwStatusIntegracao");
        HasKey(x => new { x.NFEID });
    }
}