using System;
using System.Data.SqlClient;
using System.Linq;

namespace VNF.Integration.DataLayer.DAL
{
    public class SpDeleteNFS
    {
        public VNFContext db = new VNFContext();

        /// <summary>
        /// Este metodo exclui os registro no VNF referente a NFS do portal de servico.
        /// </summary>
        /// <param name="IdNotaFiscal">Id da nota fiscal</param>
        /// <param name="ConfirmaConcluido">
        ///         Este parametro confirma S=sim a exclusao dos registros
        ///         com status integracao SAP concluido. se foi N=nao nao exclui os registro com status integracao 
        ///         SAP concluido.
        /// </param>
        /// <returns>
        ///         SEM_CHAVE_VNF = sem registro de integracao
        ///         EXCLUIDO_VNF = registros excluido do VNF
        ///         STATUS_SAP_CONCLUIDO = status SAP concluido *a integração SAP esta concluida precisa de confirmação pra excluir 
        /// </returns>
        public string DeleleNFS(int IdNotaFiscal, string ConfirmaConcluido)
        {
            try
            {
                SqlParameter param1 = new SqlParameter("@IdNotaFiscal", IdNotaFiscal);
                SqlParameter param2 = new SqlParameter("@ConfirmaConcluido", ConfirmaConcluido);
                var returnSP = db.Database.SqlQuery<string>("SP_DELETE_NFS_BY_PORTALSERVICO @IdNotaFiscal, @ConfirmaConcluido", param1, param2).Single();
                return returnSP;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}