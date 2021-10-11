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
using VNF.Business;
using MetsoFramework.Utils;
using VNF.Portal.Util.ExtensionsMethods;

namespace VNF.Portal.DataLayer
{
    public class DLOcorrencias : Repository<Ocorrencias>
    {
        /// <summary>
        /// Retorna os parametros da ocorrência
        /// </summary>
        /// <param name="RecebedorCNPJ"></param>
        /// <param name="DataEnvioDe"></param>
        /// <param name="DataEnvioAte"></param>
        /// <param name="VencimentoDe"></param>
        /// <param name="VencimentoAte"></param>
        /// <param name="FornecedorCNPJ"></param>
        /// <param name="NumeroDocumento"></param>
        /// <param name="Status"></param>
        /// <param name="pQtdRegistros"></param>
        /// <param name="pExceedQtd"></param>
        /// <param name="Exporta"></param>
        /// <param name="Responsavel"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 22/05/2018 - CR00008351 - Ajuste não consultar o AD para trazer dados do usuário</example>
        public List<OcorrenciasListagemViewModel> GetByParams(string RecebedorCNPJ, string DataEnvioDe, string DataEnvioAte, string VencimentoDe, string VencimentoAte, string FornecedorCNPJ, string NumeroDocumento, string Status,
                                                                int pQtdRegistros, ref Boolean pExceedQtd,
                                                                bool Exporta = false, string Responsavel = "")
        {
            DateTime? EnvioDe = null;
            DateTime? EnvioAte = null;
            DateTime? VencDe = null;
            DateTime? VencAte = null;

            if (!String.IsNullOrEmpty(DataEnvioDe)) EnvioDe = Convert.ToDateTime(DataEnvioDe + " 00:00:00");
            if (!String.IsNullOrEmpty(DataEnvioAte)) EnvioAte = Convert.ToDateTime(DataEnvioAte + " 23:59:00");
            if (!String.IsNullOrEmpty(VencimentoDe)) VencDe = Convert.ToDateTime(VencimentoDe + " 00:00:00");
            if (!String.IsNullOrEmpty(VencimentoAte)) VencAte = Convert.ToDateTime(VencimentoAte + " 23:59:00");

            var query = db.Ocorrencias
                // join m in db.MotivoCorrecao on o.IdMotivoCorrecao equals m.IdMotivoCorrecao 
                            .Join(db.MotivoCorrecao, ocor => ocor.IdMotivoCorrecao, motivo => motivo.IdMotivoCorrecao, (ocor, motivo) => new { ocor, motivo })
                // join n in db.TbDOC_CAB_NFE on o.NFEID equals n.NFEID
                            .Join(db.TbDOC_CAB_NFE, omotv => omotv.ocor.NFEID, cnfe => cnfe.NFEID, (ocnfe, cnfe) => new { ocnfe, cnfe })
                // join h in db.TbDOC_CAB on o.NFEID equals h.NFEID
                            .Join(db.TbDOC_CAB, ocnfe => ocnfe.cnfe.NFEID, cab => cab.NFEID, (ocab, cab) => new { ocab, cab })
                //Marcio Spinosa - 28/05/2018 - CR00008351
                            .Join(db.TbUsuario, ousu => ousu.ocab.ocnfe.ocor.Responsavel, usuario => usuario.usucodusu, (ousu, usuario) => new { ousu, usuario })//Marcio Spinosa - 28/05/2018 - CR00008351
                // from tv in db.TbVER.Where(x => x.NFEID == o.NFEID).DefaultIfEmpty()
                            .GroupJoin(db.TbVER, ocab => ocab.ousu.cab.NFEID, ver => ver.NFEID, (over, ver) => new { over, ver = ver.DefaultIfEmpty() })
                            .SelectMany(vo => vo.ver.DefaultIfEmpty(), (iover, ver) => new { iover.over, ver });

            // ((EnvioDe == null || o.DataRecebimento >= EnvioDe) && (EnvioAte == null || o.DataRecebimento <= EnvioAte))
            if (EnvioDe != null && EnvioAte != null)
                query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataRecebimento >= EnvioDe && o.over.ousu.ocab.ocnfe.ocor.DataRecebimento <= EnvioAte);
            else
            {
                if (EnvioDe != null) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataRecebimento >= EnvioDe);
                if (EnvioAte != null) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataRecebimento <= EnvioAte);
            }

            // ((VencDe == null || o.DataEsperada >= VencDe) && (VencAte == null || o.DataEsperada <= VencAte))
            if (VencDe != null && VencAte != null)
                query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataEsperada >= VencDe && o.over.ousu.ocab.ocnfe.ocor.DataEsperada <= VencAte);
            else
            {
                if (VencDe != null) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataEsperada >= VencDe);
                if (VencAte != null) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.DataEsperada <= VencAte);
            }

            // (NumeroDocumento == "" || n.NF_IDE_NNF == NumeroDocumento)
            if (!String.IsNullOrWhiteSpace(NumeroDocumento)) query = query.Where(o => o.over.ousu.ocab.cnfe.NF_IDE_NNF == NumeroDocumento);

            // (FornecedorCNPJ == "" || n.NF_EMIT_CNPJ == FornecedorCNPJ)
            if (!String.IsNullOrWhiteSpace(FornecedorCNPJ)) query = query.Where(o => o.over.ousu.ocab.cnfe.NF_EMIT_CNPJ == FornecedorCNPJ);

            // (RecebedorCNPJ == "" || n.NF_DEST_CNPJ == FornecedorCNPJ)
            if (!String.IsNullOrWhiteSpace(RecebedorCNPJ)) query = query.Where(o => o.over.ousu.ocab.cnfe.NF_DEST_CNPJ == FornecedorCNPJ);

            // (Status == "" || o.Status == Status)
            if (!String.IsNullOrWhiteSpace(Status)) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.Status == Status);

            // (String.IsNullOrEmpty(Responsavel) || o.Responsavel == Responsavel)
            if (!String.IsNullOrWhiteSpace(Responsavel)) query = query.Where(o => o.over.ousu.ocab.ocnfe.ocor.Responsavel == Responsavel);


            List<OcorrenciasListagemViewModel> lista = query.Select(x => new OcorrenciasListagemViewModel
                                                        {
                                                            IdOcorrencia = x.over.ousu.ocab.ocnfe.ocor.IdOcorrencia,
                                                            NumeroDocumento = x.over.ousu.ocab.cnfe.NF_IDE_NNF,
                                                            Prioridade = "Alta",
                                                            Origem = "Fiscal",
                                                            Motivo = x.over.ousu.ocab.ocnfe.motivo.Titulo,
                                                            CodigoFornecedor = x.over.ousu.ocab.cnfe.NF_EMIT_XNOME,
                                                            //Comprador = x.over.ocab.ocnfe.ocor.Responsavel,
                                                            Comprador = x.over.usuario.usunomusu,
                                                            Data = x.over.ousu.ocab.ocnfe.ocor.DataRecebimento,
                                                            NFEID = x.over.ousu.ocab.ocnfe.ocor.NFEID,
                                                            DataVer = (x.ver != null) ? (DateTime?)x.ver.DATVER : null,
                                                            CodLog = 0,
                                                            Status = x.over.ousu.ocab.ocnfe.ocor.Status,
                                                            TipoDocumento = x.over.ousu.cab.VNF_TIPO_DOCUMENTO,
                                                            Valor = x.over.ousu.ocab.cnfe.NF_ICMSTOT_VNF
                                                        }).Distinct().OrderByDescending(x => x.Data).Take(pQtdRegistros + 1).ToList();

            pExceedQtd = lista.Count > pQtdRegistros;
            lista = lista.Take(pQtdRegistros).ToList();

            //Funcionalidade comentada devido a não utilização, caso seja necessário retornar avaliar outra maneira de obter a infomação da data
            //pois a logica abaixo possui problemas de performance
            //////////if (Exporta)
            //////////{
            //////////    BLNotaFiscal bl = new BLNotaFiscal();
            //////////    foreach (var i in lista)
            //////////    {
            //////////        var obj = bl.GetByID(i.NFEID, false);
            //////////        var venc = obj.DUPLICATAS.OrderBy(x => x.NF_COBR_DUP_DVENC).FirstOrDefault();
            //////////        if (venc != null)
            //////////        {
            //////////            i.Vencimento = venc.NF_COBR_DUP_DVENC;
            //////////        }
            //////////    }
            //////////}

            //Alterando nome de login para nome do comprador
            //foreach (var o in lista)
            //{
            //    o.Comprador = Uteis.GetUserInfoBySamId(o.Comprador)[0];
            //}    
            //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
            return lista;
        }

        public IEnumerable<OcorrenciasListagemViewModel> GetListagem(IEnumerable<string> NFEDIs) {
            var chunkSize = 100;
            foreach (var chunk in NFEDIs.Chunk(chunkSize)) {
                var query = (from o in db.Ocorrencias
                             join m in db.MotivoCorrecao on o.IdMotivoCorrecao equals m.IdMotivoCorrecao
                             join n in db.TbDOC_CAB_NFE on o.NFEID equals n.NFEID

                             where chunk.Contains(o.NFEID)

                             from t in db.TbLOG.Where(x => x.NFEID == o.NFEID).DefaultIfEmpty()
                             from tv in db.TbVER.Where(x => x.NFEID == o.NFEID).DefaultIfEmpty()

                             select new OcorrenciasListagemViewModel {
                                 IdOcorrencia = o.IdOcorrencia,
                                 NumeroDocumento = n.NF_IDE_NNF,
                                 Prioridade = "Alta",
                                 Origem = "Fiscal",
                                 Motivo = m.Descricao,
                                 CodigoFornecedor = t.CODFOR,
                                 Comprador = o.Responsavel,
                                 Data = o.DataRecebimento,
                                 NFEID = o.NFEID,
                                 DataVer = tv.DATVER,
                                 CodLog = 0
                             }).Distinct();

                foreach (var item in query) {
                    yield return item;
                }
            }
        }

        public OcorrenciasListagemViewModel GetByNFEID(int IdOcorrencia)
        {
            return (from o in db.Ocorrencias
                    join m in db.MotivoCorrecao on o.IdMotivoCorrecao equals m.IdMotivoCorrecao
                    where o.IdOcorrencia == IdOcorrencia
                    select new OcorrenciasListagemViewModel
                    {
                        IdOcorrencia = o.IdOcorrencia,
                        Motivo = m.Titulo,
                        DataEsperada = o.DataEsperada,
                        DataRecebimento  = o.DataRecebimento,
                        Status = o.Status,
                        Responsavel = o.Responsavel
                    }).FirstOrDefault();
        }

        public List<TbDOC_CAB_ANEXOS> GetOcorrenciaAnexos(string NFEID)
        {
            return (from a in db.TbDOC_CAB_ANEXOS
                    where a.NFEID == NFEID
                    select a).ToList();
        }

        public string GetCompradorResponsavel(string NFEID)
        {
            string Retorno = "";
            var aux = (from s in db.TbDOC_CAB_SAP
                       where s.NFEID == NFEID
                       select s.SAP_PURCHASING_GROUP).FirstOrDefault();

            if(!String.IsNullOrEmpty(aux))
            {
                var Comprador = (from c in db.TbCOM
                                 where c.CODCOM == aux
                                 select c.EMAIL).FirstOrDefault();
                if(Comprador != null)
                {
                    return Comprador;
                }
            }

            return Retorno;
        }

        //public string GetAcessoFinalizaOcorrencia(string LogonName)
        //{
        //    var aux = (from a in db.TbAcesso
        //               where a.acecodusu == LogonName
        //               && a.acecodtel == "FINO"
        //               select a).FirstOrDefault();
        //    if(aux != null)
        //    {
        //        return "S";
        //    }

        //    return "N";
        //}
    }   
}
