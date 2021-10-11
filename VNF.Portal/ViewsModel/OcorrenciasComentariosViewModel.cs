using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Portal.ViewsModel
{
    public class OcorrenciasComentariosViewModel
    {
        public byte[] Anexo { get; set; }
        public string AnexoExtensao {get;set;}
        public string Comentario {get;set;}
        public string AnexoNome { get; set; }
        public DateTime? Data {get;set;}
        public int IdOcorrenciaComentario {get;set;}
        public int? IdOcorrencia {get;set;}
        public string idUsuario {get;set;}
        public string Usuario { get; set; }
        
    }
}