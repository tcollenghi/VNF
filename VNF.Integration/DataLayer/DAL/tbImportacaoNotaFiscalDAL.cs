using VNF.Integration.DataLayer.Model;
using System.Linq;
using System;

namespace VNF.Integration.DataLayer.DAL
{
    public class tbImportacaoNotaFiscalDAL : Repository<tbImportacaoNotaFiscal>
    {
        public tbImportacaoNotaFiscal getByIdNotaFiscal(int idNotaFiscal)
        {
            try
            {
                var objImportacaoNotaFiscal = db.tbImportacaoNotaFiscal.Where(t1 => t1.IdNotaFiscal == idNotaFiscal).SingleOrDefault();

                return objImportacaoNotaFiscal;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}