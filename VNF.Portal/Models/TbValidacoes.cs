//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VNF.Portal.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TbValidacoes
    {
        public TbValidacoes()
        {
            this.TbValidacoesExcecoes = new HashSet<TbValidacoesExcecoes>();
        }
    
        public int id_validacao { get; set; }
        public string val_codigo { get; set; }
        public string val_titulo_usuario { get; set; }
        public string val_texto_reprovacao { get; set; }
        public bool val_notificar_compras { get; set; }
        public bool val_notificar_fornecedor { get; set; }
        public bool val_permitir_anulacao_compras { get; set; }
        public bool val_permitir_anulacao_fiscal { get; set; }
        public string val_texto_email_fornecedor { get; set; }
        public bool val_validar_nfe { get; set; }
        public bool val_validar_cte { get; set; }
        public bool val_validar_tal { get; set; }
        //public Nullable<bool> val_validar_tcom { get; set; }
        //public Nullable<bool> val_validar_nfs { get; set; }
        //public Nullable<bool> val_validar_fat { get; set; }
        public bool val_validar_tcom { get; set; }
        public bool val_validar_nfs { get; set; }
        public bool val_validar_fat { get; set; }

        public virtual ICollection<TbValidacoesExcecoes> TbValidacoesExcecoes { get; set; }
    }
}
