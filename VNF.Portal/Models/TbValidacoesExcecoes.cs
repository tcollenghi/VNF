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
    
    public partial class TbValidacoesExcecoes
    {
        public int id_validacao_excecao { get; set; }
        public int vex_id_validacao { get; set; }
        public string vex_cfop { get; set; }
        public string vex_deposito { get; set; }
        public string vex_cod_material { get; set; }
    
        public virtual TbValidacoes TbValidacoes { get; set; }
    }
}
