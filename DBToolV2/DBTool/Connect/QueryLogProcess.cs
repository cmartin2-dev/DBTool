using DBTool.Commons;
using Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBTool.Connect
{
    public class QueryLogProcess
    {
        List<QueryLog> lstQueryLog = null;
        string jsonFile = string.Empty;

        string excelPath = @"C:\Users\cmartin2\Infor\Infor\Infor PLM for Fashion - DB Request\DBScriptExecuteRequest.json";
        public QueryLogProcess() { }

        public List<QueryLog> GetLog()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = @"QueryLog";
            string fileName = "QueryLog.json";

            jsonFile = $"{exeFolder}\\{filePath}\\{fileName}";



            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            if (!File.Exists(jsonFile))
                File.WriteAllText(jsonFile, "");

            string queryLogJsonFile = File.ReadAllText(jsonFile);
            lstQueryLog = JsonConvert.DeserializeObject<List<QueryLog>>(queryLogJsonFile);
            if (lstQueryLog == null)
                lstQueryLog = new List<QueryLog>();

            return lstQueryLog;
        }

        public void SaveLog()
        {
            try
            {
                string jsonStr = JsonConvert.SerializeObject(this.lstQueryLog);
                File.WriteAllText(jsonFile, jsonStr);

                DataTable dtTable = Utilities.JsonToDataTable(jsonStr);

                if (dtTable != null && dtTable.Rows.Count > 0)
                {
                    foreach (DataRow row in dtTable.Rows)
                    {
                        string script = row["Script"].ToString();
                        var aaa = Encoding.UTF8.GetBytes(script);
                        var bbb = Convert.ToBase64String(aaa);

                        row["Script"] = bbb;


                    }
                }

                if(File.Exists(excelPath))
                    File.Delete(excelPath);

                Utilities.SaveExportedFile(excelPath, jsonStr);

             //   Utilities.SaveExportedFileExcel(dtTable, excelPath);
                
            }
            catch (Exception ex)
            {
                DBTool.Controls.ThemedDialog.Show(ex.Message, "Error");
            }
        }
    }
}
