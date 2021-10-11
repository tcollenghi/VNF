/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLUsers : Repository<TbUsuario>
    {
        public string GetNameByLogonName(string LogonName)
        {
            var nome = (from u in db.TbUsuario
                        where u.usucodusu == LogonName
                        select u.usunomusu).FirstOrDefault();

            if (String.IsNullOrEmpty(nome))
            {
                nome = LogonName;
            }

            return nome;
        }
        //Marcio Spinosa - 28/05/2018 - CR00008351
        /// <summary>
        /// Retorna o nome do usuário através do e-mail
        /// </summary>
        /// <param name="pstrMail"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para não consultar o AD para trazer dados do usuário</example>
        public string GetNameByMail(string pstrMail)
        {
            var nome = (from u in db.TbUsuario
                        where u.usuEmail == pstrMail
                        select u.usucodusu).FirstOrDefault();

            return nome;
        }
        //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim

        public string GetEmailByName(string Name)
        {
            return (from u in db.TbUsuario
                    where u.usunomusu == Name
                    select u.usuEmail).FirstOrDefault();
        }

        public string GetEmailByLogonName(string LogonName)
        {
            var Email = (from u in db.TbUsuario
                         where u.usucodusu == LogonName
                         select u.usuEmail).FirstOrDefault();


            return Email;
        }

        //Marcio Spinosa - 23/05/2018 - CR00008351
        /// <summary>
        /// Retorna os dados do usuário pelo login 
        /// </summary>
        /// <param name="LogonName"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 23/05/2018 - CR00008351 - Ajuste para não consultar o AD para trazer dados do usuário</example>
        public string[] getDadosByLogon(string LogonName)
        {
            var qry = (from u in db.TbUsuario
                       where u.usucodusu == LogonName
                       || u.usunomusu == LogonName
                       select u).FirstOrDefault();

            string[] arrUserInfo = new string[3];
            if (qry != null)
            {
                arrUserInfo[0] = qry.usucodusu;
                arrUserInfo[1] = qry.usunomusu;
                arrUserInfo[2] = qry.usuEmail;
            }
            else
            {
                arrUserInfo[1] = "";
            }
            return arrUserInfo;

        }
        //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
    }
}