using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VNF.Integration.DataLayer.Model
{
    [Table("tbImportacaoItemNF", Schema = "DBO")]
    public class tbImportacaoItemNF
    {
        [Key]
        [DisplayName("IdImportacaoItemNF")]
        [Column("IDIMPORTACAOITEMNF")]
        public int IdImportacaoItemNF { get; set; }

        [DisplayName("IdImportacaoNotaFiscal")]
        [Column("IDIMPORTACAONOTAFISCAL")]
        public int IdImportacaoNotaFiscal { get; set; }

        [DisplayName("IdItemNotaFiscal")]
        [Column("IDITEMNOTAFISCAL")]
        public int IdItemNotaFiscal { get; set; }

        [DisplayName("ChaveAcesso")]
        [Column("CHAVEACESSO")]
        public string ChaveAcesso { get; set; }
        
    }
}