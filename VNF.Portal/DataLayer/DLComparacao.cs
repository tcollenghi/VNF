using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLComparacao : Repository<com_comparacao>
    {
        public int QtdeItensNFE(string NFEID)
        {
            return db.com_comparacao.Where(x => x.com_nfe_id == NFEID).Count();
        }

        public DateTime GetDataComparacao(string NFEID)
        {

            var lista = (from c in db.com_comparacao
                         where c.com_nfe_id == NFEID
                         select c).ToList();

            DateTime data = new DateTime(); 
            if(lista.Count > 0)
                data = lista.Max(x => x.com_data_hora);

            return data;
        }
    }
}