using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLCodigoContabilidade : Repository<TbModoProcessoCategoriaContabil>
    {
        public List<TbModoProcessoCategoriaContabil> GetByProcessoId(int Id)
        {
            return (from i in db.TbModoProcessoCategoriaContabil
                    where i.mcc_id_modo_processo == Id
                    select i).ToList();
        } 
    }
}