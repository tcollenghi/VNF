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
    
    public partial class TbModoProcessoCategoriaContabil
    {
        public int id_modo_processo_categoria_contabil { get; set; }
        public int mcc_id_modo_processo { get; set; }
        public string mcc_categoria_contabil { get; set; }
    
        public virtual TbModoProcesso TbModoProcesso { get; set; }
    }
}
