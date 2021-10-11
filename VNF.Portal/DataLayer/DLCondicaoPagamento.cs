using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLCondicaoPagamento : Repository<TbCON>
    {
        public void DeleteConPag(string id)
        {

            TbCON e = db.Set<TbCON>().Find(id);
            if (e != null)
            {
                db.Set<TbCON>().Remove(e);
            }
        }
    }
}
