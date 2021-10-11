using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLCFOP : Repository<TbModoProcessoCfop>
    {
        public List<TbModoProcessoCfop> GetByProcessoId(int Id)
        {
            return (from i in db.TbModoProcessoCfop
                    where i.mpc_id_modo_processo == Id
                    select i).ToList();
        }

        public List<TbCFOP> GetList()
        {
            return (from c in db.TbCFOP
                    select c).OrderBy(x => x.cfop_codigo).ToList();
        }
    }
}