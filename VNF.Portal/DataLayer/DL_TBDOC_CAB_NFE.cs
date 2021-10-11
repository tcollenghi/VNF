using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DL_TBDOC_CAB_NFE :  Repository<TbDOC_CAB_NFE>
    {
        public TbDOC_CAB_NFE GetByChave(string pStrNFEID)
        {
            return  (from p in db.TbDOC_CAB_NFE
                    where p.NFEID == pStrNFEID
                    select p).FirstOrDefault();
        }
       
    }

}