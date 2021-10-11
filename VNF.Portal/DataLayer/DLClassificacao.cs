using VNF.Portal.Models; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VNF.Portal.DataLayer
{
    public class DLClassificacao : Repository<Classificacao>
    {
        public List<Classificacao> getClassificacao()
        {
            return (from c in db.Classificacao select c).OrderBy(x => x.Descricao).ToList();
        }
    }
}