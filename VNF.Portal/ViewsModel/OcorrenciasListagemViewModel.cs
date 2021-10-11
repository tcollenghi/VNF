using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Portal.ViewsModel
{
    public class OcorrenciasListagemViewModel
    {
        public int IdOcorrencia { get; set; }
        public string NumeroDocumento { get; set; }
        public string Prioridade { get; set; } 
        public string Origem { get; set; } 
        public string Motivo { get; set; }
        public string CodigoFornecedor { get; set; }
        public string Comprador { get; set; }
        public string PO { get; set; }
        public string Item { get; set; }
        public DateTime? Data { get; set; }
        public string NFEID { get; set; }
        public DateTime? DataVer { get; set; }
        public int? CodLog { get; set; }
        public DateTime? DataRecebimento { get; set; }
        public DateTime? DataEsperada { get; set; }
        public string Status { get; set; }
        public DateTime? Vencimento { get; set; }
        public string Responsavel { get; set; }
        public string TipoDocumento { get; set; }
        public decimal? Valor { get; set; }
    }
}