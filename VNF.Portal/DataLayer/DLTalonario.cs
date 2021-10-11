using VNF.Portal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MoreLinq;
using System.Text;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Reflection;
using System.Data.SqlClient;

namespace VNF.Portal.DataLayer
{
    public class DLTalonario : Repository<Talonario>
    {
        //public List<Classificacao> getClassificacao()
        //{
        //    return (from c in db.Classificacao select c).OrderBy(x => x.Descricao).ToList();
        //}

        public IEnumerable<Talonario> GetAll()
        {
            return (from p in db.Talonario
                    select p).OrderByDescending(x => x.DataEmissao).Take(200);
        }

        public Talonario GetByChave(string pChave)
        {
            return (from p in db.Talonario
                    where p.ChaveAcesso == pChave
                    select p).FirstOrDefault();
        }


        public DataTable LoadGrid(string pStrNumeroDocumento, string pStrSerie, string pStrCNPJEmitente, string pStrCNPJMetso, string pStrFinalizado, string pStrStatusIntegracao, string pStrDataDe, string pStrDataAte)
        {

            DataTable ddt = new DataTable();


            var query = db.vwTALONARIO.Where(x => 1 == 1);

            if (!string.IsNullOrWhiteSpace(pStrNumeroDocumento))
                query = query.Where(p => p.NumeroDocumento == pStrNumeroDocumento);


            if (!string.IsNullOrWhiteSpace(pStrSerie))
                query = query.Where(p => p.Serie == pStrSerie);


            if (!string.IsNullOrWhiteSpace(pStrCNPJEmitente))
                query = query.Where(p => p.CNPJEmitente == pStrCNPJEmitente);

            var retorno = query.OrderByDescending(x => x.DataEmissao).ToList();


            if (retorno.Count > 0)
            {
                ddt = LINQResultToDataTable(retorno);
            }

            return ddt;
        }



        public DataTable LINQResultToDataTable<T>(IEnumerable<T> Linqlist)
        {

            DataTable dt = new DataTable();

            PropertyInfo[] columns = null;

            if (Linqlist == null) return dt;

            foreach (T Record in Linqlist)
            {

                if (columns == null)
                {
                    columns = ((Type)Record.GetType()).GetProperties();
                    foreach (PropertyInfo GetProperty in columns)
                    {
                        Type colType = GetProperty.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                        == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dt.Columns.Add(new DataColumn(GetProperty.Name, colType));
                    }
                }

                DataRow dr = dt.NewRow();

      
                foreach (PropertyInfo pinfo in columns)
                {
                    dr[pinfo.Name.ToString()] = pinfo.GetValue(Record, null) == null ? DBNull.Value : pinfo.GetValue(Record, null);
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

    }
}