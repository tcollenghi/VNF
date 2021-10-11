using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VNF.Integration.DataLayer.Model
{
    [Table("tbImportacaoTipoDocumento", Schema = "DBO")]
    public class tbImportacaoTipoDocumento
    {
        [Key]
        [DisplayName("IdImportacaoTipoDocumento")]
        [Column("IDIMPORTACAOTIPODOCUMENTO")]
        public int IdImportacaoTipoDocumento { get; set; }
        
        [DisplayName("DescTipoDocumento")]
        [Column("DESCTIPODOCUMENTO")]
        public string DescTipoDocumento { get; set; }
        
    }
}