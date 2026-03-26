using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;
using Entities;
using System.Data.Odbc;



namespace Connect
{
    public class ExcelConnect
    {
        string filePath = string.Empty;
        string connectionString;
        public List<CustomWorksheet> availableSheets { get; set; }

        private OleDbConnection connection = null;

        public string QueryString { get; set; }

        private OdbcConnection connectionODBC = null;
        public ExcelConnect(string filename)
        {


            filePath = filename;
            // _workBook = new XLWorkbook(filePath);
            connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\";";

           //  connectionString = "Driver={Microsoft Excel Driver (*.xls, *.xlsx, *.xlsm, *.xlsb)};DriverId=790;Dbq=" + filename+";ReadOnly=1;";


        }


        public void ConnectODBC()
        {
            connectionODBC = new OdbcConnection(connectionString);

            


            connectionODBC.Open();
            DataTable dt = connectionODBC.GetSchema("Tables");
            if (dt != null)
            {
                availableSheets = new List<CustomWorksheet>();
                int ctrId = 1;
                foreach (DataRow dr in dt.Rows)
                {
                    CustomWorksheet customWorksheet = new CustomWorksheet();
                    customWorksheet.Id = ctrId;
                    customWorksheet.WorkSheetName = dr["TABLE_NAME"].ToString();

                   DataTable schemaTable = connectionODBC.GetSchema("Columns");

                    if (schemaTable != null)
                    {
                        int ctrId2 = 1;
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            // Column names are in the "COLUMN_NAME" column

                            CustomWorksheetColumn customWorksheetColumn = new CustomWorksheetColumn();
                            customWorksheetColumn.Id = ctrId2;
                            customWorksheetColumn.Name = row["COLUMN_NAME"].ToString();
                            customWorksheetColumn.MappedName = row["COLUMN_NAME"].ToString();

                            customWorksheet.WorkSheetColumns.Add(customWorksheetColumn);
                            customWorksheetColumn.Id = ctrId2;

                            ctrId2++;
                        }
                    }
                    ctrId++;
                    availableSheets.Add(customWorksheet);
                }
            }
            connectionODBC.Close();
        }


        public void Connect()
        {
            connection = new OleDbConnection(connectionString);


            connection.Open();
            DataTable dt = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            if (dt != null)
            {

                availableSheets = new List<CustomWorksheet>();
                int ctrId = 1;
                foreach (DataRow dr in dt.Rows)
                {
                    CustomWorksheet customWorksheet = new CustomWorksheet();
                    customWorksheet.Id = ctrId;
                    customWorksheet.WorkSheetName = dr["TABLE_NAME"].ToString();

                    DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, customWorksheet.WorkSheetName, null });

                    if (schemaTable != null)
                    {
                        int ctrId2 = 1;
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            // Column names are in the "COLUMN_NAME" column

                            CustomWorksheetColumn customWorksheetColumn = new CustomWorksheetColumn();
                            customWorksheetColumn.Id = ctrId2;   
                            customWorksheetColumn.Name = row["COLUMN_NAME"].ToString();
                            customWorksheetColumn.MappedName = row["COLUMN_NAME"].ToString();

                            customWorksheet.WorkSheetColumns.Add(customWorksheetColumn);
                            customWorksheetColumn.Id = ctrId2;

                            ctrId2++;
                        }
                    }
                    ctrId++;    
                    availableSheets.Add(customWorksheet);
                }
            }
            connection.Close();
        }

        public async Task<RequestResponse> ExecuteQuery(string query, CancellationToken token)
        {
            RequestResponse requestResponse = new RequestResponse();

            CustObj entity = new CustObj();
            entity.Objects = new List<CustomObject>();
            entity.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

            string text = string.Empty;


            try
            {
                await Task.Run(async () =>
                {
                    if (connection != null && connection.State == ConnectionState.Closed)
                    {
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        using (var oleDbDataAdapter = new OleDbDataAdapter(query, connection))
                        {
                            connection.Open();

                            System.Data.DataSet ds = new System.Data.DataSet();
                            oleDbDataAdapter.Fill(ds);

                            if (ds.Tables != null && ds.Tables.Count > 0)
                            {
                                int id = 1;
                                // fields
                                int columnCount = 0;
                                foreach (System.Data.DataColumn column in ds.Tables[0].Columns)
                                {
                                    entity.Fields.Add(column.ColumnName, column.DataType);
                                    columnCount++;
                                }

                                // rows
                                foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                                {
                                    var entityObject = new CustomObject();



                                    entityObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                                    for (int i = 0; i < columnCount; i++)
                                        entityObject.Object.Add(ds.Tables[0].Columns[i].ColumnName, row[i] == DBNull.Value ? null : row[i]);

                                    entityObject.PKId = id;
                                    id++;

                                    entity.Objects.Add(entityObject);
                                }

                            }
                        }
                        connection.Close();
                    }

                }, token);

                
                requestResponse.isSuccess = true;
                requestResponse.CustObj = entity;

                return requestResponse;
            }
            catch (Exception ex)
            {
                connection.Close();
                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;

                return requestResponse;
            }

        }



        public async Task<RequestResponse> ExecuteQueryODBC(string query, CancellationToken token)
        {
            RequestResponse requestResponse = new RequestResponse();

            CustObj entity = new CustObj();
            entity.Objects = new List<CustomObject>();
            entity.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

            string text = string.Empty;


            try
            {
                await Task.Run(async () =>
                {
                    if (connectionODBC != null && connectionODBC.State == ConnectionState.Closed)
                    {
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        using (var oleDbDataAdapter = new OdbcDataAdapter(query, connectionODBC))
                        {
                            connectionODBC.Open();

                            System.Data.DataSet ds = new System.Data.DataSet();
                            oleDbDataAdapter.Fill(ds);

                            if (ds.Tables != null && ds.Tables.Count > 0)
                            {
                                int id = 1;
                                // fields
                                int columnCount = 0;
                                foreach (System.Data.DataColumn column in ds.Tables[0].Columns)
                                {
                                    entity.Fields.Add(column.ColumnName, column.DataType);
                                    columnCount++;
                                }

                                // rows
                                foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                                {
                                    var entityObject = new CustomObject();



                                    entityObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                                    for (int i = 0; i < columnCount; i++)
                                        entityObject.Object.Add(ds.Tables[0].Columns[i].ColumnName, row[i] == DBNull.Value ? null : row[i]);

                                    entityObject.PKId = id;
                                    id++;

                                    entity.Objects.Add(entityObject);
                                }

                            }
                        }
                        connectionODBC.Close();
                    }

                }, token);


                requestResponse.isSuccess = true;
                requestResponse.CustObj = entity;

                return requestResponse;
            }
            catch (Exception ex)
            {
                connectionODBC.Close();
                requestResponse.isSuccess = false;
                requestResponse.ErrorMessage = ex.Message;

                return requestResponse;
            }

        }

        public async Task<CustObj> ExecuteQuery()
        {
            CustObj entity = new CustObj();
            entity.Objects = new List<CustomObject>();
            entity.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

            string text = string.Empty;


            try
            {
                if (connection != null && connection.State == ConnectionState.Closed)
                {

                    using (var oleDbDataAdapter = new OleDbDataAdapter(this.QueryString, connection))
                    {
                        connection.Open();

                        System.Data.DataSet ds = new System.Data.DataSet();
                        oleDbDataAdapter.Fill(ds);

                        if (ds.Tables != null && ds.Tables.Count > 0)
                        {
                            int id = 1;
                            // fields
                            int columnCount = 0;
                            foreach (System.Data.DataColumn column in ds.Tables[0].Columns)
                            {
                                entity.Fields.Add(column.ColumnName, column.DataType);
                                columnCount++;
                            }

                            // rows
                            foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                            {
                                var entityObject = new CustomObject();



                                entityObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                                for (int i = 0; i < columnCount; i++)
                                    entityObject.Object.Add(ds.Tables[0].Columns[i].ColumnName, row[i] == DBNull.Value ? null : row[i]);

                                entityObject.PKId = id;
                                id++;

                                entity.Objects.Add(entityObject);
                            }

                        }
                    }


                    connection.Close();
                }



                return entity;
            }
            catch (Exception ex)
            {
                connection.Close();
                throw new Exception(ex.Message);
            }

        }


        public async Task<CustObj> ExecuteQuery(string query)
        {
            CustObj entity = new CustObj();
            entity.Objects = new List<CustomObject>();
            entity.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

            string text = string.Empty;


            try
            {
                if (connection != null && connection.State == ConnectionState.Closed)
                {

                    using (var oleDbDataAdapter = new OleDbDataAdapter(query, connection))
                    {
                        connection.Open();

                        System.Data.DataSet ds = new System.Data.DataSet();
                        oleDbDataAdapter.Fill(ds);

                        if (ds.Tables != null && ds.Tables.Count > 0)
                        {
                            int id = 1;
                            // fields
                            int columnCount = 0;
                            foreach (System.Data.DataColumn column in ds.Tables[0].Columns)
                            {
                                entity.Fields.Add(column.ColumnName, column.DataType);
                                columnCount++;
                            }

                            // rows
                            foreach (System.Data.DataRow row in ds.Tables[0].Rows)
                            {
                                var entityObject = new CustomObject();



                                entityObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                                for (int i = 0; i < columnCount; i++)
                                    entityObject.Object.Add(ds.Tables[0].Columns[i].ColumnName, row[i] == DBNull.Value ? null : row[i]);

                                entityObject.PKId = id;
                                id++;

                                entity.Objects.Add(entityObject);
                            }

                        }
                    }


                    connection.Close();
                }



                return entity;
            }
            catch (Exception ex)
            {
                connection.Close();
                throw new Exception(ex.Message);
            }

        }

        public CustObj ExecuteQuery1(string query, string selectedModule)
        {

            List<ExcelReport> lstExcelReport = new List<ExcelReport>();
            int ctr = 0;
            string transactionName = string.Empty;
            try
            {
                if (connection != null && connection.State == ConnectionState.Closed)
                {

                    using (var command = new OleDbDataAdapter(query, connection))
                    {
                        connection.Open();
                        DataTable dt = new DataTable();
                        command.Fill(dt);



                        int columnCount = dt.Columns.Count;

                        for (int i = 1; i < columnCount; i++)
                        {
                            ctr = i;
                            ExcelReport excelReport = new ExcelReport();
                            transactionName = dt.Columns[i].ColumnName;
                            excelReport.TransactionName = transactionName;


                            excelReport.Module = transactionName.Split('_')[1];

                            if (excelReport.Module.ToLower() != selectedModule.ToLower())
                                continue;

                            if (transactionName.ToLower().Contains("_add"))
                                excelReport.Category = "Add";
                            else if (transactionName.ToLower().Contains("_save"))
                                excelReport.Category = "Save";
                            else
                                excelReport.Category = "Uncategorized";

                            excelReport.ColumnDates = new Dictionary<string, string>();

                            int rowCount = dt.Rows.Count;

                            for (int rowI = 0; rowI < rowCount; rowI++)
                            {
                                excelReport.ColumnDates.Add(dt.Rows[rowI][0].ToString(), dt.Rows[rowI][i].ToString());
                            }

                            lstExcelReport.Add(excelReport);
                        }
                    }
                }

                CustObj entity = new CustObj();
                entity.Objects = new List<CustomObject>();
                entity.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                if (lstExcelReport != null && lstExcelReport.Count > 0)
                {

                    int fieldCount = lstExcelReport[0].ColumnDates.Count;



                    //  add column with value (per row)
                    foreach (ExcelReport excelReport in lstExcelReport)
                    {

                        var entityObject = new CustomObject();

                        entityObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                        if (!entity.Fields.ContainsKey("Module"))
                        {
                            entity.Fields.Add("Module", "");
                        }
                        entityObject.Object.Add("Module", excelReport.Module);
                        if (!entity.Fields.ContainsKey("Category"))
                        {
                            entity.Fields.Add("Category", "");
                        }
                        entityObject.Object.Add("Category", excelReport.Category);

                        if (!entity.Fields.ContainsKey("Transaction Name"))
                        {
                            entity.Fields.Add("Transaction Name", "");
                        }
                        entityObject.Object.Add("Transaction Name", excelReport.TransactionName);



                        for (int i = 0; i < fieldCount; i++)
                        {
                            if (!entity.Fields.ContainsKey(excelReport.ColumnDates.Keys.ElementAt(i)))
                            {
                                entity.Fields.Add(excelReport.ColumnDates.Keys.ElementAt(i), "");
                            }
                            entityObject.Object.Add(excelReport.ColumnDates.Keys.ElementAt(i), string.IsNullOrEmpty(excelReport.ColumnDates.Values.ElementAt(i).ToString()) ? 0 : excelReport.ColumnDates.Values.ElementAt(i).ToString());

                        }


                        if (fieldCount > 1)
                        {
                            double value1 = Math.Round(string.IsNullOrEmpty(excelReport.ColumnDates.Values.ElementAt(0)) ? 0 : double.Parse(excelReport.ColumnDates.Values.ElementAt(0)),2);
                            double value2 = Math.Round(string.IsNullOrEmpty(excelReport.ColumnDates.Values.ElementAt(1)) ? 0 : double.Parse(excelReport.ColumnDates.Values.ElementAt(1)), 2);

                            

                            if (!entity.Fields.ContainsKey($"Improve / Degrade"))
                            {
                                entity.Fields.Add("Improve / Degrade", "");
                            }

                            double ResponseTime = Math.Round(value1 - value2,2);

                            double IDValue = (ResponseTime/ value1) * 100;

                            entityObject.Object.Add("Improve / Degrade", double.IsNaN(IDValue) ? 0 : Math.Round(IDValue,2));

                            if (!entity.Fields.ContainsKey($"Response Time Difference"))
                            {
                                entity.Fields.Add("Response Time Difference", "");
                            }

                            entityObject.Object.Add("Response Time Difference", Math.Round(ResponseTime,2));

                            if (!entity.Fields.ContainsKey($"Evaluation"))
                            {
                                entity.Fields.Add("Evaluation", "");
                            }

                            entityObject.Object.Add("Evaluation", "");

                        }
                        entity.Objects.Add(entityObject);

                    }

                }
                connection.Close();
                return entity;
            }
            catch (Exception ex)
            {
                connection.Close();
                throw new Exception(ex.Message);
            }

        }

        public List<ExcelModule> GetModules(string tablename)
        {
            List<ExcelModule> modules = new List<ExcelModule>();
            try
            {
                string query = $"SELECT * FROM [{tablename}]";


                if (connection != null && connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        using (var result = command.ExecuteReader())
                        {
                            int fieldCount = result.FieldCount;

                            int idCtr = 1;
                            while (result.Read())
                            {
                                for (int i = 1; i < fieldCount - 1; i++)
                                {


                                    ExcelModule module = new ExcelModule();
                                    string moduleName = result.GetName(i).ToString().Split('_')[1];
                                    module.Name = moduleName;
                                    if (modules.Select(x=>x.Name).Contains(moduleName))
                                        continue;
                                    module.Id = idCtr;
                                    modules.Add(module);
                                    idCtr++;
                                }
                                break;
                            }
                        }
                    }
                }

                connection.Close();

                return modules;
            }
            catch (Exception ex)
            {
                connection.Close();
                throw new Exception(ex.Message);
            }
        }


        public List<ExcelDataColumn> GetColumns(string tablename)
        {


            List<ExcelDataColumn> columns = new List<ExcelDataColumn>();
            try
            {
                string query = $"SELECT * FROM [{tablename}]";


                if (connection != null && connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    using (var command = new OleDbCommand(query, connection))
                    {
                        using (var result = command.ExecuteReader())
                        {
                            int fieldCount = result.FieldCount;
                            int idCtr = 1;

                            while (result.Read())
                            {
                                ExcelDataColumn column = new ExcelDataColumn();
                                column.Name = result[0].ToString();

                                column.Id = idCtr;
                                columns.Add(column);
                                idCtr++;
                            }
                        }
                    }
                }

                connection.Close();

                return columns;
            }
            catch (Exception ex)
            {
                connection.Close();
                throw new Exception(ex.Message);
            }
        }

    }
}
