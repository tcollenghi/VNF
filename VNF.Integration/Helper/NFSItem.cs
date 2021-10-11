using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Integration.Helper
{
    public class NFSItem
    {
        public int IdNFSItem { get; set; }
        public int? IdNFS { get; set; }
        public decimal? ValorTotal { get; set; }
        public string Pedido { get; set; }
        public string ItemPedido { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string NCM { get; set; }
        public decimal? Quantidade { get; set; }
        public decimal? ValorUnitario { get; set; }
        public string CFOP { get; set; }
        public decimal? ICMS { get; set; }
        public decimal? IPI { get; set; }
        public decimal? FRETE { get; set; }
        public decimal? SEGURO { get; set; }
        public decimal? DESCONTO { get; set; }
        public decimal? OUTRASDESPESAS { get; set; }

        public virtual NFS NFS { get; set; }
    }
}