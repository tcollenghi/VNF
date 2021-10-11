/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
 */
using MetsoFramework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using VNF.Portal.Models;
using VNF.Portal.ViewsModel;

namespace VNF.Portal.DataLayer
{
    public class DLLogApplication : Repository<TbLOGApplication>
    {
        /// <summary>
        /// Retorna a nota fiscal através da chave de acesso
        /// </summary>
        /// <param name="NFEID"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para o VNF não consultar o AD para trazer dados do usuário</example>
        public List<TbLOGApplication> GetByNFEID(string NFEID)
        {
            //Marcio Spinosa - 28/05/2018 - CR00008351
            var lista = (from l in db.TbLOGApplication
                         join u in db.TbUsuario on l.log_user equals u.usucodusu 
                    where l.log_nfeid == NFEID
                    select new {
                                  l.id_log,
                                  l.log_type,
                                  u.usunomusu,
                                  l.log_date,
                                  l.log_title,
                                  l.log_description,
                                  l.log_nfeid,
                                  l.log_icon
                    }).OrderByDescending(x => x.log_date).ToList();


            List<TbLOGApplication> log = lista.Select(x => new TbLOGApplication
                         {
                               id_log = x.id_log,
                               log_type = x.log_type ,
                               log_user =  x.usunomusu,
                               log_date  = x.log_date,
                               log_title = x.log_title,
                               log_description = x.log_description,
                               log_nfeid = x.log_nfeid,
                               log_icon = x.log_icon
                         }).Distinct().OrderByDescending(x => x.log_date).ToList();

            return log;
            //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        }

        public List<ResumoViewModel> GetResumo(string NFEID)
        {
            List<ResumoViewModel> lista = new List<ResumoViewModel>();
            var tpDoc = (from tp in db.TbDOC_CAB
                         where tp.NFEID == NFEID
                         select tp.VNF_TIPO_DOCUMENTO).FirstOrDefault();
            if(tpDoc != null)
            {
                lista.Add(new ResumoViewModel() { Campo = "Tipo Documento", Valor = tpDoc });
            }
            

            var DocCabNFE = (from dc in db.TbDOC_CAB_NFE
                             where dc.NFEID == NFEID
                             select new
                             {
                                 dc.NF_IDE_NNF,
                                 dc.NF_IDE_DHEMI,
                                 dc.NF_EMIT_XNOME
                             }).FirstOrDefault();
            if(DocCabNFE != null)
            {
                lista.Add(new ResumoViewModel() { Campo = "Número NF", Valor = DocCabNFE.NF_IDE_NNF });
                lista.Add(new ResumoViewModel() { Campo = "Data Emissão", Valor = Convert.ToDateTime(DocCabNFE.NF_IDE_DHEMI).ToShortDateString() });
                lista.Add(new ResumoViewModel() { Campo = "Emitente", Valor = DocCabNFE.NF_EMIT_XNOME });
            }
           

            var ttbNFE = (from sit in db.TbNFE
                          where sit.NFEID == NFEID
                          select new
                          {
                              NFEREL = sit.NFEREL == "S" ? "Sim" : "Não",
                              sit.SITUACAO,
                              CONTINGENCIA = sit.CONTINGENCIA == "0" ? "Não" : "Sim"
                          }).FirstOrDefault();
            if(ttbNFE != null)
            {
                lista.Add(new ResumoViewModel() { Campo = "Relevante para validação", Valor = ttbNFE.NFEREL });
                lista.Add(new ResumoViewModel() { Campo = "Situação", Valor = ttbNFE.SITUACAO });
                lista.Add(new ResumoViewModel() { Campo = "Contingência", Valor = ttbNFE.CONTINGENCIA });
            }
            

            return lista.OrderBy(x => x.Campo).ToList();
        }

        /// <summary>
        /// Retorna os logs da nota fiscal através da chave de acesso
        /// </summary>
        /// <param name="NFEID"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para não consultar o AD para trazer dados do usuário</example>
        public string GetLogStringByNfeId(string NFEID)
        {
            List<TbLOGApplication> arrLog = GetByNFEID(NFEID);
            string strTabela = "";
            StringBuilder stbLog = new StringBuilder();
            if (arrLog.Count == 0)
            {
                stbLog.Append("<span>não existe informação</span>");
            }
            else
            {
                strTabela = @"<table class='table table-bordered tab-cust' width='100%'>
                                  <thead>
                                      <tr>
                                          <th width='15%'>Data</th>
                                          <th width='15%'>Usuário</th> 
                                          <th width='70%'>Detalhes</th> 
                                      </tr>
                                  </thead>
                                  <tbody>
                                      %LINHAS%
                                  </tbody>
                              </table><br />";

                foreach (TbLOGApplication oLog in arrLog)
                {
                    //Marcio Spinosa - 28/05/2018 - CR00008351
                    stbLog.Append(@"<tr>
                                        <td>" + Convert.ToDateTime(oLog.log_date).ToString("dd/MM/yyyy HH:mm") + @"</td> " +
                                        //<td>" + Uteis.GetUserNameBySamId(oLog.log_user) + @"</td>
                                        "<td>" + oLog.log_user + @"</td>
                                        <td>" + oLog.log_description + @"</td>
                                    </tr>");
                    //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
                }

                strTabela = strTabela.Replace("%LINHAS%", stbLog.ToString());
            }

            return strTabela;
        }

    }
}