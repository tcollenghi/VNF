using VNF.Integration.DataLayer.Model;
using System.Linq;
using System;

namespace VNF.Integration.DataLayer.DAL
{
    public class tbNFEDAL : Repository<tbNFE>
    {
        public tbNFE getByChaveAcesso(string chaveAcesso)
        {
            try
            {
                var ObjNfe = db.tbNFE.Where(t1 => t1.NFEID == chaveAcesso).SingleOrDefault();
                return ObjNfe;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}