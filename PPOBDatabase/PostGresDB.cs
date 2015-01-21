using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace PPOBDatabase
{
    
    public class PostGresDB:IDisposable
    {
        #region IDisposable Members
        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!this.disposed)
            {
                if(disposing)
                {
                    // Dispose managed resources.
                    disposeAll();
                }

                disposed = true;
            }
        }

        ~PostGresDB()
        {
            Dispose(false);
        }

        #endregion

        private void disposeAll() 
        {
            this.MyDataSet.Clear();
            this.MyDataSet.Dispose();
            this.MyConnection.Dispose();
        }

        public NpgsqlConnection MyConnection = new NpgsqlConnection();
        public System.Data.DataSet MyDataSet = new System.Data.DataSet();

		public void ConnectionString(string host, int port, string Database, string UserId, string Password)
        {            
			MyConnection.ConnectionString = "Server=" + host + ";Port="+port.ToString()+
			                                ";Database=" + Database + ";User id=" + UserId + 
			                                ";Password=" + Password + ";Timeout=15";
        }
        
        public bool ConnectionTest(out Exception Excep)
        {
            try
            {
                MyConnection.Open();
                MyConnection.Close();
                Excep = null;
                return true;
            }
            catch (Exception Ex)
            {
                Excep = Ex;
                return false;
            }
        }

        public System.Data.DataSet GetDataSet 
        {
            get { return MyDataSet; }
            set { MyDataSet = value; }
        }

        public void clearDataTable(string DataTableName)
        {
            if (isTableAliasExists(DataTableName))
            {
                this.MyDataSet.Tables[DataTableName].Clear();
            }
        }

        public object GetDataItem(string DataTableName, int RowNumber, int ColumnNumber) 
        {
            return this.MyDataSet.Tables[DataTableName].Rows[RowNumber][ColumnNumber];
        }

        public object GetDataItem(string DataTableName, int RowNumber, string ColumnName) 
        {
            return this.MyDataSet.Tables[DataTableName].Rows[RowNumber][ColumnName];
        }

        public System.Data.DataTable AddDataTable(string DataTableName) 
        {
            return MyDataSet.Tables.Add(DataTableName);
        }

        public bool isTableAliasExists(string DataTableName)
        {
            if (MyDataSet.Tables[DataTableName] == null)
                return false;
            else
                return true;
        }

        public void RemoveDataTable(string DataTableName, out  Exception Excep) 
        {
            try 
            {
                MyDataSet.Tables.Remove(DataTableName);
                Excep = null;
            }
            catch(Exception Ex) 
            {
                Excep = Ex;
            }
        }

        public void RemoveDataTable(System.Data.DataTable idDataTable, out Exception Excep) 
        {
            try 
            {
                MyDataSet.Tables.Remove(idDataTable);
                Excep = null;
            }
            catch(Exception Ex)
            {
                Excep = Ex;
            }
        }

        public void ClearDataTable() 
        {
            MyDataSet.Tables.Clear();
        }
        
        public int ExecNonQuerySql(string NonQuery, out Exception Excep)
        {
            NpgsqlCommand myCommand = new NpgsqlCommand(NonQuery, MyConnection);
            Int32 Hasil = 0;
            try
            {
                myCommand.Connection.Open();
            }
            catch (Exception Ex)
            {
                Excep = Ex;
                return 0;
            }
            try
            {
                Hasil = myCommand.ExecuteNonQuery();
            }
            catch (Exception Ex)
            {
                Excep = Ex;
                MyConnection.Close();
                return 0;
            }

            try
            {
                MyConnection.Close();
                Excep = null;
            }
            catch (Exception Ex)
            {
                Excep = Ex;
            }
            return Hasil;
        }

        public int ExecQuerySql(string Query, string DataTableName, out Exception Excep)
        {
            NpgsqlCommand SQLCommand = new NpgsqlCommand(Query, MyConnection);
            NpgsqlDataAdapter AdapterSQL = new NpgsqlDataAdapter();
            try
            {
                SQLCommand.CommandTimeout = 30;
                AdapterSQL.SelectCommand = SQLCommand;
                MyDataSet.Tables[DataTableName].Clear();
                AdapterSQL.Fill(MyDataSet.Tables[DataTableName]);
                Excep = null;
                return MyDataSet.Tables[DataTableName].Rows.Count;
            }
            catch (Exception Ex)
            {
                Excep = Ex;
            }
            return 0;
        }
        
    }
}
