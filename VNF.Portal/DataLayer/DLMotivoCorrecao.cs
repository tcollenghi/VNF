using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLMotivoCorrecao : Repository<MotivoCorrecao>
    {
        public string GetEmailComprador(string CodCom)
        {
            var comprador = (from t in db.TbCOM
                             where t.CODCOM == CodCom
                             select t).FirstOrDefault();
            return comprador.EMAIL;
        }

        public string GetEmailFornecedor(string CodFor)
        {
            var fornecedor = (from t in db.TbFOR
                              where t.CODFOR == CodFor
                              select t).FirstOrDefault();
            return fornecedor.EMAIL_NFE;
        }
    }
}