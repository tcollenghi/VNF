using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VNF.Integration.DataLayer.Model
{
    [Table("tbImportacaoNotaFiscal", Schema = "DBO")]
    public class tbImportacaoNotaFiscal
    {
        [Key]
        [DisplayName("IdImportacaoNotaFiscal")]
        [Column("IDIMPORTACAONOTAFISCAL")]
        public int IdImportacaoNotaFiscal { get; set; }

        [DisplayName("IdNotaFiscal")]
        [Column("IDNOTAFISCAL")]
        public int IdNotaFiscal { get; set; }

        [DisplayName("ChaveAcesso")]
        [Column("CHAVEACESSO")]
        public string ChaveAcesso { get; set; }

        [DisplayName("IdImportacaoTipoDocumento")]
        [Column("IDIMPORTACAOTIPODOCUMENTO")]
        public int IdImportacaoTipoDocumento { get; set; }
                
    }
}