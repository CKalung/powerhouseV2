using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using LOG_Handler;

namespace PPOBDatabase
{
    // Class Name   : DbCommon
    // Creator      : Iwan Supratman (deong)
    // Create Date  : June, 13 2013
    // Mail         : deong84@gmail.com
    // Globat Matra Aditama, PT.
    class DbCommon
    {
        private string dbhost = "";
        private int dbport = 0;
        private string dbname = "";
        private string dbuser = "";
        private string dbpasswd = "";
        private string dbSql = "";
        private PostGresDB dbposgres;
		private Exception dbex = null;

        public DbCommon(string _dbhost, int _dbport, string _dbname, string _dbuser, string _dbpasswd)
        {
            dbhost = _dbhost;
            dbport = _dbport;
            dbname = _dbname;
            dbuser = _dbuser;
            dbpasswd = _dbpasswd;    
        }

        public int ConnectDb()
        {
            try
            {
                dbposgres = new PostGresDB();
                dbex = new Exception();
                dbposgres.ConnectionString(dbhost, dbport, dbname, dbuser, dbpasswd);
                return 1;
            }
            catch (Exception ex) 
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.Message + "\r\n" + ex.StackTrace);
            }
            return 0;
        }

        public int DisconnectDb()
        {
            try
            {
                dbposgres.Dispose();
                return 1;
            }
            catch(Exception ex)
            {
                LogWriter.write(this, LogWriter.logCodeEnum.ERROR, ex.Message + "\r\n" + ex.StackTrace);
            }
            return 0;
        }

        public void SetQuery(string sql)
        {
            dbSql = sql;            
        }

        public int StartTrans()
        {
            return dbposgres.ExecNonQuerySql("BEGIN;", out dbex);
        }

        public int CommitTrans()
        {
            return dbposgres.ExecNonQuerySql("COMMIT;", out dbex);
        }

        public int RollBackTrans()
        {
            return dbposgres.ExecNonQuerySql("ROLLBACK;", out dbex);
        }

        public int ExecuteQuery()
        {
            return dbposgres.ExecNonQuerySql(dbSql, out dbex);
        }

        public void AddDataTbl(string dbtable)
        {
            dbposgres.AddDataTable(dbtable);
        }

        public void RemoveDataTbl(string dbtable)
        {
            dbposgres.RemoveDataTable(dbtable, out dbex);            
        }

        public DataTable GetResultData(string dbtable)
        {            
            //DataTable record = new DataTable(dbtable);
            //if(dbposgres.isTableAliasExists(dbtable))
            //{
                //for (int i = 0; i < dbposgres.MyDataSet.Tables[dbtable].Columns; i++)
                //{
                //record.Columns.Add("id", typeof(string));
                //}

                //for (int i = 0; i < dbposgres.MyDataSet.Tables[dbtable].Rows.Count; i++ )
                //{                    

                //}
            //}
            return dbposgres.MyDataSet.Tables[dbtable];
        }

        public int DbQuery(string dbtable)
        {
            return dbposgres.ExecQuerySql(dbSql, dbtable, out dbex);                    
        }

        //public int Get()
        //{
        //}
    }
}
