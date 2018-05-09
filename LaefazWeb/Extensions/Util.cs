using LaefazWeb.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using TDMWeb.Exceptions;
//using TDMWeb.Enumerators;
//using TDMWeb.Exceptions;
using TDMWeb.Models;

namespace TDMWeb.Extensions
{
    public static class Util
    {

#if (DEBUG)
        public static string SqlConnectionString = @"Data Source=localhost;Database=TDMDb;Trusted_Connection=true;Persist Security Info=True";
#else
        public static string SqlConnectionString = ConfigurationManager.ConnectionStrings["BulkInsert"].ConnectionString;
#endif

        public static int quantidadeQuebraExcel = Convert.ToInt32(ConfigurationManager.AppSettings["QuantidadeQuebraExcel"].ToString());

        public static System.Data.DataTable LerExcel(string path)
        {
            System.Data.DataTable dt = new System.Data.DataTable();

            //Conexão com Excel
            string excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0 xml;HDR=YES;IMEX=1;'";

            using (OleDbConnection conExcel = new OleDbConnection(excelConnectionString))
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conExcel;

                conExcel.Open();

                System.Data.DataTable table = conExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                bool SheetTestData = table.AsEnumerable().Any(t => t.Field<string>("TABLE_NAME").ToUpper() == "MASSAS$");
                if (!SheetTestData)
                    throw new SheetFailedException("O arquivo do Excel deve conter apenas uma planilha com o nome MASSAS");

                cmd.CommandText = "select * from [MASSAS$]";

                OleDbDataAdapter adp = new OleDbDataAdapter(cmd);

                adp.Fill(dt);
            }

            return dt;
        }

        public static bool ValidaPlanilhaExcel(string path, int idDataPool)
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            //Conexão com Excel
            string excelConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0 xml;HDR=YES;IMEX=1;'";

            using (OleDbConnection conExcel = new OleDbConnection(excelConnectionString))
            {
                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = conExcel;

                conExcel.Open();

                System.Data.DataTable table = conExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

               cmd.CommandText = "select * from [CONFIG$D6:D11]";

                OleDbDataAdapter adp = new OleDbDataAdapter(cmd);
                adp.Fill(dt);
                            
            }
            bool retorno = true;
            string [] valores = new string[3];
            int count = 0;
            for(int i = 0; i < dt.Rows.Count; i++)
            {
                if (!dt.Rows[i].ItemArray[0].ToString().Equals(""))
                {
                    valores[count] = dt.Rows[i].ItemArray[0].ToString();
                    count++;
                }
                  
            }

            string sistema  = "";
            string script   = "";
            string condicao = "";

            for (int i = 0; i < valores.Length; i++)
            {
                if (i == 0)
                    sistema = valores[i];

                if (i == 1)
                    script = valores[i];

                if (i == 2)
                    condicao = valores[i];
            }

                     

            
            DbEntities db = new DbEntities();
            DataPool dp = db.DataPool.Where(x => x.Id == idDataPool).FirstOrDefault();
            
           

            if(!dp.AUT.Descricao.Equals(sistema, StringComparison.InvariantCultureIgnoreCase))
                retorno = false;
            
            if(!dp.Script_CondicaoScript.Script.Descricao.Equals(script, StringComparison.InvariantCultureIgnoreCase))
                retorno = false;

            if (dp.Script_CondicaoScript.CondicaoScript != null)
            {
                if(!dp.Script_CondicaoScript.CondicaoScript.Descricao.Equals(condicao, StringComparison.InvariantCultureIgnoreCase))
                {
                    retorno = false;
                }
            }else if (!condicao.Equals("") && !condicao.Equals("SELECIONE"))
            {
                retorno = false;
            }
                
            
            return retorno;
        }
    }
}