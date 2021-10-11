using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

namespace VNF.Portal.Models
{
    [Serializable]
    public class PortariaModel
    {
        public DataTable dtEquipamentos { get; set; }
        public DataTable dtFundicao { get; set; }
        public PastaModel pasta { get; set; }
    }

    [Serializable]
    public class PastaModel
    {
        public string IDPASTA { get; set; }
        public string DATLAN { get; set; }
        public string HORCHE { get; set; }
        public string HORENT { get; set; }
        public string PLACA { get; set; }
        public string NOMTRA { get; set; }
        public string NOMMOT { get; set; }
        public string SETOR { get; set; }
        public string QTD_NOTAS { get; set; }
        public List<NotaModel> NOTAS { get; set; }
    }

    [Serializable]
    public class NotaModel
    {
        public string PrioridadeAlta { get; set; }
        public string ChaveAcesso { get; set; }
        public string DOCe { get; set; }
        public string Fornecedor { get; set; }
        public string Status { get; set; }
    }

    [Serializable]
    public class model_J1B1N
    {
       
        public TbDOC_CAB_NFE TBDOC_CAB_NFE { get; set; }
        public TbDOC_ITEM_NFE TBDOC_ITEM_NFE { get; set; }
      
        public model_J1B1N()
        {
            TBDOC_CAB_NFE = new TbDOC_CAB_NFE();
            TBDOC_ITEM_NFE = new TbDOC_ITEM_NFE();
        }

        public model_J1B1N(string pStrNFEID)
        {
            TBDOC_CAB_NFE = new TbDOC_CAB_NFE();
            TBDOC_ITEM_NFE = new TbDOC_ITEM_NFE();
        }
    }
}