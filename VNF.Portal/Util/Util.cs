using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Reflection;
using System.ComponentModel;
using System.Web.Mvc;
using MetsoFramework.Utils;
using VNF.Portal.DataLayer;


namespace VNF.Portal.Util
{
    public static class Util
    {
        public static object FillClass(DataTable dt, object Classe)
        { 
            if (dt.Rows.Count > 0)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    if(Classe.GetType().GetProperty(dc.ColumnName) != null)
                    {
                        if(!String.IsNullOrEmpty(dt.Rows[0][dc.ColumnName].ToString()))
                        {
                            string TipoDado = Classe.GetType().GetProperty(dc.ColumnName).PropertyType.FullName;
                            if (TipoDado.Contains("String")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, dt.Rows[0][dc.ColumnName].ToString(), null);
                            if (TipoDado.Contains("DateTime")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDateTime(dt.Rows[0][dc.ColumnName].ToString()), null);
                            if (TipoDado.Contains("Decimal")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDecimal(dt.Rows[0][dc.ColumnName].ToString()), null);
                            if (TipoDado.Contains("Double")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDouble(dt.Rows[0][dc.ColumnName].ToString()), null);
                            if (TipoDado.Contains("Int")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToInt32(dt.Rows[0][dc.ColumnName].ToString()), null);
                            if (TipoDado.Contains("Byte")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, System.Text.UTF8Encoding.UTF8.GetBytes(dt.Rows[0][dc.ColumnName].ToString()), null);
                            if (TipoDado.Contains("Bool")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToBoolean(dt.Rows[0][dc.ColumnName].ToString()), null);
                        } 
                    }
                }
            }
            return Classe;
        }

        public static object FillList(DataTable dt, object ClasseBase)
        {
            Type t = ClasseBase.GetType();
            List<object> Lista = new List<object>();
            if (dt.Rows.Count > 0)
            {
                for(int i =0; i < dt.Rows.Count; i++)
                {
                    var Classe = Activator.CreateInstance(t);
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (Classe.GetType().GetProperty(dc.ColumnName) != null)
                        {
                            if (!String.IsNullOrEmpty(dt.Rows[0][dc.ColumnName].ToString()))
                            {
                                string TipoDado = Classe.GetType().GetProperty(dc.ColumnName).PropertyType.FullName;
                                if (TipoDado.Contains("String")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, dt.Rows[i][dc.ColumnName].ToString(), null);
                                if (TipoDado.Contains("DateTime")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDateTime(dt.Rows[i][dc.ColumnName].ToString()), null);
                                if (TipoDado.Contains("Decimal")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDecimal(dt.Rows[i][dc.ColumnName].ToString()), null);
                                if (TipoDado.Contains("Double")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToDouble(dt.Rows[i][dc.ColumnName].ToString()), null);
                                if (TipoDado.Contains("Int")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToInt32(dt.Rows[i][dc.ColumnName].ToString()), null);
                                if (TipoDado.Contains("Byte")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, System.Text.UTF8Encoding.UTF8.GetBytes(dt.Rows[i][dc.ColumnName].ToString()), null);
                                if (TipoDado.Contains("Bool")) Classe.GetType().GetProperty(dc.ColumnName).SetValue(Classe, Convert.ToBoolean(dt.Rows[i][dc.ColumnName].ToString()), null);
                            }
                        }
                    }
                    Lista.Add(Classe);
                }
                
            }
            return Lista;
        }

        private static object GetClass(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }

        public static DataTable ListToDataTable<T>(IList<T> data)
        {
            DataTable table = new DataTable();

            //special handling for value types and string
            if (typeof(T).IsValueType || typeof(T).Equals(typeof(string)))
            {

                DataColumn dc = new DataColumn("Value");
                table.Columns.Add(dc);
                foreach (T item in data)
                {
                    DataRow dr = table.NewRow();
                    dr[0] = item;
                    table.Rows.Add(dr);
                }
            }
            else
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
                foreach (PropertyDescriptor prop in properties)
                {
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
                foreach (T item in data)
                {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties)
                    {
                        try
                        {
                            row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                        }
                        catch (Exception ex)
                        {
                            row[prop.Name] = DBNull.Value;
                        }
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        //Marcio Spinosa - 30/08/2018
        /// <summary>
        /// Método para retorno do nome do usuário após estar logado no sistema.
        /// </summary>
        /// <param name="pobjHtmlString"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 30/08/2018 - Ajuste efetuado para que a consulta ao usuário ocorra na BD</example>
        public static string getNameByLogonName(this HtmlHelper pobjHtmlString)
        {
            try
            {
                DLUsers objDLUsers = new DLUsers();
                string Name = objDLUsers.GetNameByLogonName(Uteis.LogonName());
                return Name;
            }
            catch (Exception ex)
            {
                return "Ocorreu um erro ao consultar os dados do usuário";
            }
        }
        //Marcio Spinosa - 30/08/2018 - Fim
    }
}