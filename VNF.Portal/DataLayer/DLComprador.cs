using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLComprador : Repository<TbCOM>
    {
        public TbCOM GetByIDComprador(string Id)
        {
            return (from c in db.TbCOM
                    where c.CODCOM == Id
                    select c).FirstOrDefault();
        }

        public int GetCount(string Id)
        {
            return (from c in db.TbCOM
                    where c.CODCOM == Id
                    select c.CODCOM).Count();
        }
    }
}