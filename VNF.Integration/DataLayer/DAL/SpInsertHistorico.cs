using System;
using System.Data.SqlClient;
using System.Linq;

namespace VNF.Integration.DataLayer.DAL
{
    public class SpInsertHistorico
    {
        public VNFContext db = new VNFContext();

        public void InsereHistorico(
                                        DateTime Data,
                                        int idNotaFiscal,
                                        string UsuarioCodigo,
                                        string UsuarioNome,
                                        string NovoStatus,
                                        string Descricao
                                    )
        {
            try
            {
                SqlParameter param1 = new SqlParameter("@Data", Data);
                SqlParameter param2 = new SqlParameter("@idNotaFiscal", idNotaFiscal);
                SqlParameter param3 = new SqlParameter("@UsuarioCodigo", UsuarioCodigo);
                SqlParameter param4 = new SqlParameter("@UsuarioNome", UsuarioNome);
                SqlParameter param5 = new SqlParameter("@NovoStatus", NovoStatus);
                SqlParameter param6 = new SqlParameter("@Descricao", Descricao);

                db.Database.SqlQuery<string>("SP_INSERT_HISTORICO_PORTALSERVICO @Data, " +
                                                                                "@idNotaFiscal, " +
                                                                                "@UsuarioCodigo, " +
                                                                                "@UsuarioNome, " +
                                                                                "@NovoStatus, " +
                                                                                "@Descricao "
                                                                                , param1
                                                                                , param2
                                                                                , param3
                                                                                , param4
                                                                                , param5
                                                                                , param6).Single();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}