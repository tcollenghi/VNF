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
    
    public partial class OcorrenciasComentarios
    {
        public int IdOcorrenciaComentario { get; set; }
        public Nullable<int> IdOcorrencia { get; set; }
        public string Comentario { get; set; }
        public string Usuario { get; set; }
        public Nullable<System.DateTime> Data { get; set; }
        public byte[] Anexo { get; set; }
        public string AnexoNome { get; set; }
        public string AnexoExtensao { get; set; }
    
        public virtual Ocorrencias Ocorrencias { get; set; }
    }
}
