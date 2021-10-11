using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLImportacaoNotaFiscal : Repository<tbImportacaoNotaFiscal>
    {
        public tbImportacaoNotaFiscal getImportacaoNotaByChaveAcesso(string chaveAcesso)
        {
            tbImportacaoNotaFiscal objImportacao;
            try
            {
                objImportacao = (from x in db.tbImportacaoNotaFiscal
                                     where x.ChaveAcesso == chaveAcesso
                                     select x).FirstOrDefault();
                return objImportacao;
            }
            catch (Exception)
            {

                return objImportacao = null;
            }
            
        }
    }
}