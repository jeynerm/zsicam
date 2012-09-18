﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using zsi.Framework.Data.DataProvider.OleDb;
using zsi.Framework.Data;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Windows.Forms;
using zsi.Framework.Common;
namespace zsi.PhotoFingCapture.Models.DataControllers
{

    public class dcTimeInOutLog2 : zsi.Framework.Data.DataProvider.OleDb.MasterDataController<TimeInOutLog>
    {
        public SqlConnection SQLDBConn { get; set; }
        public override void InitDataController()
        {
            string _ConnectionString = zsi.PhotoFingCapture.Properties.Settings.Default.LiveSQLServerConnection;
            _ConnectionString = zsi.PhotoFingCapture.Util.DecryptStringData(_ConnectionString, "{u}.*.{u}", "{u}");
            _ConnectionString = zsi.PhotoFingCapture.Util.DecryptStringData(_ConnectionString, "{p}.*.{p}", "{p}");

            this.SQLDBConn = new SqlConnection(_ConnectionString);
            this.Procedures.Add(new Procedure("dbo.SelectTimeInOutLogs", SQLCommandType.Select));
            this.Procedures.Add(new Procedure("dbo.UpdateTimeInOutLog", SQLCommandType.Update));
        }


    } 

    public class dcTimeInOutLog : MasterDataController<TimeInOutLog>
    {
        private OleDbTransaction Trans { get; set; }
        public override void InitDataController()
        {
            string _ConnectionString = zsi.PhotoFingCapture.Properties.Settings.Default.AccessDBConnection;
            _ConnectionString = zsi.PhotoFingCapture.Util.DecryptStringData(_ConnectionString, "{p}.*.{p}", "{p}");
            this.DBConn = new OleDbConnection(_ConnectionString);
        }


    
        public void TimeInOut(Profile info, DateTime TimeValue,out DateTime TimeIn,out DateTime TimeOut )
        {
            try
            {
                string dtrDate = DateTime.Now.ToShortDateString();
                TimeOut = new DateTime(1, 1, 1);
                string sql = "Select top 1 * from TimeInOutLog where ProfileId='" + info.ProfileId + "' and DTRDate=#" + dtrDate + "# ORDER BY LogInOutId desc";               
                List<TimeInOutLog> list =    this.GetDataSource(sql);

                if (list.Count==0)
                {
                    TimeIn = TimeValue;
                    InsertTimeIn(info, TimeValue, dtrDate);
                    goto Finish;
                } 
                if (list[0].TimeOut != new DateTime(1, 1, 1))
                {
                    TimeIn = TimeValue;
                    InsertTimeIn(info,TimeValue,dtrDate);                    
                }
                else
                {
                    //update
                    TimeIn = list[0].TimeIn;
                    TimeOut = TimeValue;
                    int LogInOutId = list[0].LogInOutId;
                    this.OpenDB();
                    OleDbCommand _cmd = new OleDbCommand("update TimeInOutLog set TimeOut=? where ProfileId='" + info.ProfileId + "' and DTRDate=#" + dtrDate + "# and LogInOutId=" + LogInOutId , this.DBConn);
                    var _params = _cmd.Parameters;
                    SetParameterValue(_params, TimeValue, OleDbType.Date);
                    _cmd.ExecuteNonQuery();
                    _cmd.Dispose();
                    this.CloseDB();
                    
                }
            }           
            catch (Exception ex)
            {
                throw ex;
            }
        Finish:;            
        }
        private void InsertTimeIn(Profile info,DateTime TimeValue, string DtrDate)
        {

            //INSERT
            this.OpenDB();
            //System.Windows.Forms.MessageBox.Show("insert");
            OleDbCommand _cmd = new OleDbCommand("Insert into TimeInOutLog(ProfileId,ClientId,TimeIn,DTRDate) Values(?,?,?,?)", this.DBConn);
            var _params = _cmd.Parameters;
            SetParameterValue(_params, info.ProfileId, OleDbType.VarChar);
            SetParameterValue(_params, ClientSettings.ClientWorkStationInfo.ClientId, OleDbType.VarChar);
            SetParameterValue(_params, TimeValue, OleDbType.Date);
            SetParameterValue(_params, DtrDate, OleDbType.Date);
            _cmd.ExecuteNonQuery();
            _cmd.Dispose();
            this.CloseDB();
        }


        #region "Synchronize Update"
        
        private List<TimeInOutLog> GetNewDataFromServer(DateTime CreatedDate)
        {
            Procedure p = new Procedure("dbo.SelectTimeInOutLogs");
            p.Parameters.Add("p_CreatedDate", CreatedDate); 
            return this.GetDataSource(p);
        }

        private List<TimeInOutLog> GetUpdatedDataFromServer(DateTime UpdatedDate)
        {
            Procedure p = new Procedure("dbo.SelectTimeInOutLogs");
            p.Parameters.Add("p_UpdatedDate", UpdatedDate);      
            return this.GetDataSource(p);
        }

        private void UploadDataToServer(List<TimeInOutLog> list)
        {

            foreach (TimeInOutLog info in list)
            {
                dcTimeInOutLog2 dc = new dcTimeInOutLog2();
                dc.UpdateParameters.Add("p_ClientId", info.ClientId);
                dc.UpdateParameters.Add("p_LogInOutId", info.LogInOutId);
                dc.UpdateParameters.Add("p_ClientId", info.ClientId);
                dc.UpdateParameters.Add("p_ProfileId", info.ProfileId);
                dc.UpdateParameters.Add("p_ClientEmployeeId", info.ClientEmployeeId);
                dc.UpdateParameters.Add("p_DTRDate", info.DTRDate);
                dc.UpdateParameters.Add("p_TimeIn", info.TimeIn);
                dc.UpdateParameters.Add("p_TimeOut", info.TimeOut);
                dc.UpdateParameters.Add("p_LogTypeId", info.LogTypeId);
                dc.UpdateParameters.Add("p_LogRemarks", info.LogRemarks);
                dc.UpdateParameters.Add("p_UpdatedBy", info.UpdatedBy);
                dc.UpdateParameters.Add("p_UpdatedDate", info.UpdatedDate);
                dc.Update();
                dc = null;
            }

        }

        public void TimeInOutSync()
        {
            try
            {


                ConsoleApp.WriteLine(Application.ProductName, "Start uploading data to server.");

                DateTime _LastUpdate;
                this.DBConn.Open();
                OleDbCommand _cmd2 = new OleDbCommand("select * from updatelog", this.DBConn);
                OleDbDataReader _dr2 = _cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                Trans = this.DBConn.BeginTransaction();
                if (_dr2.HasRows == false)
                {
                    ConsoleApp.WriteLine(Application.ProductName, "Get new records from the live server");
                    dcTimeInOutLog2 dc = new dcTimeInOutLog2();
                     List<TimeInOutLog> list = dc.GetDataSource();
                     this.DownloadNewData(list);
                    UpdateLastUpdate();
                }
                else
                {
                    _dr2.Read();
                    _LastUpdate = Convert.ToDateTime(_dr2["TimeInOutLastUpdate"]);

                    ConsoleApp.WriteLine(Application.ProductName, "Get newest created and updated records from the live server");
                    List<TimeInOutLog> _NewList = this.GetNewDataFromServer(_LastUpdate);
                    List<TimeInOutLog> _UpdatedList = this.GetUpdatedDataFromServer(_LastUpdate);

                    this.DownloadNewData(_NewList);
                    this.DownloadUpdatedData(_UpdatedList);

                    if (_NewList.Count > 0 || _UpdatedList.Count > 0)
                    {
                        UpdateLastUpdate();
                    }

                }
                Trans.Commit();
                this.DBConn.Close();
                ConsoleApp.WriteLine(Application.ProductName, "Migrating DTR records has been done.");

            }
            catch (Exception ex)
            {
                try
                {
                    Trans.Rollback();
                }
                catch { }
                ConsoleApp.WriteLine(Application.ProductName, "[Error]," + ex.ToString());
                zsi.PhotoFingCapture.Util.LogError(ex.ToString());
            }
        }

        private void DownloadNewData(List<TimeInOutLog> list)
        {
            try
            {

                foreach (TimeInOutLog item in list)
                {
                    OleDbCommand _cmd2 = new OleDbCommand(
                    "Insert into TimeInOutLog(LogInOutId,ClientId,ProfileId,ClientEmployeeId,DTRDate,TimeIn,TimeOut,LogTypeId,LogRemarks,UpdatedBy,UpdatedDate) "
                    + "Values(?,?,?,?,?,?,?,?,?,?,?)"
                    , this.DBConn, Trans);

                    var _params = _cmd2.Parameters;
                    SetParameterValue(_params, item.LogInOutId, OleDbType.Integer);
                    SetParameterValue(_params, item.ClientId, OleDbType.Integer);
                    SetParameterValue(_params, item.ProfileId, OleDbType.VarChar);
                    SetParameterValue(_params, item.ClientEmployeeId, OleDbType.Integer);
                    SetParameterValue(_params, item.DTRDate, OleDbType.Date);
                    SetParameterValue(_params, item.TimeIn, OleDbType.Date);
                    SetParameterValue(_params, item.TimeOut, OleDbType.Date);
                    SetParameterValue(_params, item.LogTypeId, OleDbType.Integer);
                    SetParameterValue(_params, item.LogRemarks, OleDbType.VarChar);
                    SetParameterValue(_params, item.UpdatedBy, OleDbType.Integer);
                    SetParameterValue(_params, item.UpdatedDate, OleDbType.Date);
                    _cmd2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void DownloadUpdatedData(List<TimeInOutLog> list)
        {
            try
            {
                foreach (TimeInOutLog item in list)
                {
                    OleDbCommand _cmd2 = new OleDbCommand(
                    "Update TimeInOutLog set DTRDate=?,TimeIn=?,TimeOut=?,LogTypeId=?,LogRemarks=?,UpdatedBy=?,UpdatedDate=?"
                   + " where LogInOutId='" + item.LogInOutId + "'"
                    , this.DBConn, Trans);
                    var _params = _cmd2.Parameters;
                    SetParameterValue(_params, item.DTRDate, OleDbType.Date);
                    SetParameterValue(_params, item.TimeIn, OleDbType.Date);
                    SetParameterValue(_params, item.TimeOut, OleDbType.Date);
                    SetParameterValue(_params, item.LogTypeId, OleDbType.Integer);
                    SetParameterValue(_params, item.LogRemarks, OleDbType.VarChar);
                    SetParameterValue(_params, item.UpdatedBy, OleDbType.Integer);
                    SetParameterValue(_params, item.UpdatedDate, OleDbType.Date);

                    _cmd2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void UpdateLastUpdate()
        {

          
            OleDbCommand _cmd = new OleDbCommand("select * from updatelog", this.DBConn);
            this.DBConn.Open();
            OleDbDataReader _dr = _cmd.ExecuteReader();
            if (!_dr.HasRows)
            {
                _cmd = new OleDbCommand(
                "Insert Into UpdateLog(TimeInOutLastUpdate) values(?)", this.DBConn, Trans);
                _cmd.Parameters.AddWithValue("?", GetDbDate().ToString());
                _cmd.ExecuteNonQuery();
            }
            else
            {
                _cmd = new OleDbCommand(
                "Update UpdateLog set TimeInOutLastUpdate=?", this.DBConn, Trans);
                _cmd.Parameters.AddWithValue("?", GetDbDate().ToString());
                _cmd.ExecuteNonQuery();
            }
            this.DBConn.Close();
        }



        private DateTime GetDbDate()
        {
            try
            {   dcClientWorkStation2  dc = new dcClientWorkStation2();
                DateTime _resultDate = DateTime.Now;
                System.Data.SqlClient.SqlCommand _cmd = new System.Data.SqlClient.SqlCommand("dbo.SelectDBDate", dc.DBConn);
                _cmd.CommandType = CommandType.StoredProcedure;
                dc.DBConn.Open();
                SqlDataReader _dr = _cmd.ExecuteReader(CommandBehavior.CloseConnection);
                _dr.Read();
                _resultDate = (DateTime)_dr[0];
                dc.DBConn.Close();
                return _resultDate;
            }
            catch (Exception e) { throw e; }
        }

        #endregion


 

        private void OpenDB()
        {
            if (this.DBConn.State!=ConnectionState.Open){
                this.DBConn.Open();
            }
        }
        private void CloseDB()
        {
            if (this.DBConn.State != ConnectionState.Closed)
            {
                this.DBConn.Close();
            }
        }

        void SetParameterValue(OleDbParameterCollection Params, object value, OleDbType type)
        {
            if (value != null)
            {
                Params.Add("?", type).Value = value;
            }
            else
            {
                Params.AddWithValue("?", DBNull.Value);
            }

        }


    }
}