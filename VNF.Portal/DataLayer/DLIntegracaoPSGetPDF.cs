using System.Linq;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLIntegracaoPSGetPDF :Repository<vwIntegracaoPSGetPdf>
    {
        public vwIntegracaoPSGetPdf getPdfPS(int idNotaFiscal)
        {
            
            return db.vwIntegracaoPSGetPdf.Where(t => t.ID == idNotaFiscal).SingleOrDefault();
        }
    }
}