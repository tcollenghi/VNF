using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLModoProcesso : Repository<TbModoProcesso>
    {
        public List<TbModoProcessoDetalhe> GetModoProcessoDetalhe()
        {
            return (from i in db.TbModoProcessoDetalhe
                    select i).OrderBy(x => x.mdp_processo).ToList();
        }

        public string Remove(int id)
        { 
            try
            {
                var cfop = (from c in db.TbModoProcessoCfop
                            where c.mpc_id_modo_processo == id
                            select c).ToList();
                foreach(var c in cfop)
                {
                    db.TbModoProcessoCfop.Remove(c);
                    db.SaveChanges();
                }

                var Contabil = (from c in db.TbModoProcessoCategoriaContabil
                                where c.mcc_id_modo_processo == id
                                select c).ToList();
                foreach(var c in Contabil)
                {
                    db.TbModoProcessoCategoriaContabil.Remove(c);
                    db.SaveChanges();
                }

                TbModoProcesso t = db.TbModoProcesso.Find(id);
                db.TbModoProcesso.Remove(t);
                db.SaveChanges();

                return "ok";
            }
            catch (Exception ex)
            {
                return "Não foi possível efetuar a sua solicitação.";
            }
        }
    }
}