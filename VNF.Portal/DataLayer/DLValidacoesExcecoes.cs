using MvcSiteMapProvider.Linq;
using System.Collections.Generic;
using System.Linq;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLValidacoesExcecoes : Repository<TbValidacoesExcecoes>
    {
        public List<TbValidacoesExcecoes> GetByIDValidacaoes(int id)
        {
            return db.TbValidacoesExcecoes.Where(c => c.vex_id_validacao == id).ToList();
        }
    }
}
