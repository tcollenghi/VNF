using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Integration.DataLayer.Model;

namespace VNF.Integration.DataLayer.DAL
{
    public class vwStatusIntegracaoDAL:Repository<vwStatusIntegracao>
    {
        public vwStatusIntegracao getByChaveAcesso(string chaveAcesso)
        {
            try
            {
                var objStatusIntegracao = db.vwStatusIntegracao.Where(t1 => t1.NFEID == chaveAcesso).SingleOrDefault();
                return objStatusIntegracao;
            }
            catch (Exception ex)
            {
                throw ex;
            }            
        }
    }
}