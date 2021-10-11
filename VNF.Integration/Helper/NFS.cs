using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Integration.Helper
{
    public class NFS
    {
        public int IdNFS { get; set; }
        public string NumeroDocumento { get; set; }
        public string Serie { get; set; }
        public string ChaveAcesso { get; set; }
        public string CNPJEmitente { get; set; }
        public string RazaoSocialEmitente { get; set; }
        public string CNPJMetso { get; set; }
        public string RazaoSocialMetso { get; set; }
        public DateTime? DataEmissao { get; set; }
        public DateTime? Vencimento { get; set; }
        public string Classificacao { get; set; }
        public string Tipo { get; set; }
        public string CodigoVerificacao { get; set; }
        public string LinkDocumento { get; set; }
        public string LinkAplicacao { get; set; }
        public byte[] Anexo { get; set; }
        public string AnexoNome { get; set; }
        public string AnexoExtensao { get; set; }
        public int? IdStatus { get; set; }
        public string Responsavel { get; set; }
        public string DataChegada { get; set; }
        public bool? Finalizado { get; set; }
        public decimal? ValorTotal { get; set; }
        public string Observacao { get; set; }
        public string TipoDocumento { get; set; }
        
        public virtual List<NFSItem> NFSItem { get; set; }
    }
}