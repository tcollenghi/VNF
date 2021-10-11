/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;
using VNF.Portal.ViewsModel;

namespace VNF.Portal.DataLayer
{
    public class DLOcorrenciasComentarios : Repository<OcorrenciasComentarios>
    {
        /// <summary>
        /// Retorna os comentários das ocorrências
        /// </summary>
        /// <param name="IdOcorrencia"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 22/05/2018 - CR00008351 - Ajuste para não consultar o AD para trazer dados do usuário</example>
        public List<OcorrenciasComentariosViewModel> GetComentarios(int IdOcorrencia)
        {
            //Marcio Spinosa - 22/05/2018 - CR00008351
            //List<OcorrenciasComentarios> lista = (from o in db.Ocorrencias
            var query = (from o in db.Ocorrencias
                                                  join i in db.OcorrenciasComentarios on o.IdOcorrencia equals i.IdOcorrencia
                                                  join u in db.TbUsuario on i.Usuario equals u.usucodusu
                                                  where o.IdOcorrencia == IdOcorrencia
                                                  select new
                                                  {
                                                      i.Anexo,
                                                      i.AnexoExtensao,
                                                      i.AnexoNome,
                                                      i.Comentario,
                                                      i.Data,
                                                      i.IdOcorrenciaComentario,
                                                      i.IdOcorrencia,
                                                      u.usunomusu,
                                                      u.usucodusu
                                                  }).OrderByDescending(x => x.Data).ToList();

            List<OcorrenciasComentariosViewModel> lista = query.Select(x => new OcorrenciasComentariosViewModel
                                            {
                                                Anexo = x.Anexo,
                                                AnexoExtensao = x.AnexoExtensao,
                                                AnexoNome = x.AnexoNome,
                                                Comentario = x.Comentario,
                                                Data = x.Data,
                                                IdOcorrenciaComentario = x.IdOcorrenciaComentario,
                                                IdOcorrencia = x.IdOcorrencia,
                                                idUsuario = x.usucodusu,
                                                Usuario = x.usunomusu
                                            }).OrderByDescending(x => x.Data).ToList();;
            return lista;
            //Marcio Spinosa - 22/05/2018 - CR00008351 - Fim
        }
    }
}
