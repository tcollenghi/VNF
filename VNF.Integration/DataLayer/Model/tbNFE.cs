using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VNF.Integration.DataLayer.Model
{
    [Table("TbNFE", Schema = "DBO")]
    public class tbNFE
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [DisplayName("NFEID")]
        [Column("NFEID")]
        public string NFEID { get; set; }

        [DisplayName("NFEVAL")]
        [Column("NFEVAL")]
        public string NFEVAL { get; set; }

        [DisplayName("NFECAN")]
        [Column("NFECAN")]
        public string NFECAN { get; set; }

        [DisplayName("ID_LISTA")]
        [Column("ID_LISTA")]
        public decimal ID_LISTA { get; set; }

        [DisplayName("DATVAL")]
        [Column("DATVAL")]
        public DateTime DATVAL { get; set; }

        [DisplayName("NFEREL")]
        [Column("NFEREL")]
        public string NFEREL { get; set; }

        [DisplayName("USUCAN")]
        [Column("USUCAN")]
        public string USUCAN { get; set; }

        [DisplayName("JUNAUT")]
        [Column("JUNAUT")]
        public string JUNAUT { get; set; }

        [DisplayName("SITUACAO")]
        [Column("SITUACAO")]
        public string SITUACAO { get; set; }

        [DisplayName("REPROCESSAR")]
        [Column("REPROCESSAR")]
        public string REPROCESSAR { get; set; }

        [DisplayName("CNPJ_METSO")]
        [Column("CNPJ_METSO")]
        public string CNPJ_METSO { get; set; }

        [DisplayName("CONTINGENCIA")]
        [Column("CONTINGENCIA")]
        public string CONTINGENCIA { get; set; }
    }
}