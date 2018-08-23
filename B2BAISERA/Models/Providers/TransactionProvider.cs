using System;
using System.Collections.Generic;
using System.Linq;
//using B2BAISERA.Models.DataAccess;
//using B2BAISERA.Helper;
//using B2BAISERA.Logic;
using Microsoft.Practices.Unity;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using B2BAISERA.Models.EFServer;
using B2BAISERA.Helper;
using System.Data.EntityClient;
using System.Data;
using B2BAISERA.B2BAIWsDMZ;
using System.Globalization;

namespace B2BAISERA.Models.Providers
{
    public class TransactionProvider : DataAccessBase
    {
        public TransactionProvider()
            : base()
        {
        }

        public TransactionProvider(EProcEntities context)
            : base(context)
        {
        }

        #region MAIN
        //B2BAISERAEntities ctx = new B2BAISERAEntities(Repository.ConnectionStringEF);

        //public User GetUser(string userName, string password, string clientTag)
        //{
        //    var User = (from o in ctx.Users
        //                where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
        //                select o).FirstOrDefault();

        //    return User;
        //}

        public CUSTOM_USER GetUser(string userName, string password, string clientTag)
        {
            var user = (from o in entities.CUSTOM_USER
                        where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
                        select o).FirstOrDefault();

            return user;
        }

        public string GetLastTicketNo(string fileType)
        {
            var result = "";
            try
            {
                var query = (entities.CUSTOM_LOG
                    .Where(log => (log.Acknowledge == true) && (log.FileType == fileType))
                    .Select(log => new LogViewModel
                    {
                        ID = log.ID,
                        WebServiceName = log.WebServiceName,
                        MethodName = log.MethodName,
                        TicketNo = log.TicketNo,
                        Message = log.Message
                    })
                    ).OrderByDescending(log => log.ID).FirstOrDefault();

                result = query != null ? Convert.ToString(query.TicketNo) : "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion

        #region DOWNLOAD
        
        #region S02008
        public int InsertLogTransactionDownloadS02008(bool acknowledge, string ticketNo, string message, string transStatus, List<TransactionDataViewModel> listTransactionDataModel)
        {
            int result = 0;
            try
            {
                //insert into CUSTOM_DOWNLOAD_TRANSACTION
                CUSTOM_DOWNLOAD_TRANSACTION transaction = new CUSTOM_DOWNLOAD_TRANSACTION();
                transaction.Acknowledge = acknowledge;
                transaction.TicketNo = ticketNo;
                transaction.Message = message;
                EntityHelper.SetAuditForInsert(transaction, "SERA");
                entities.CUSTOM_DOWNLOAD_TRANSACTION.AddObject(transaction);

                for (int i = 0; i < listTransactionDataModel.Count; i++)
                {
                    //insert into CUSTOM_DOWNLOAD_TRANSACTIONDATA
                    CUSTOM_DOWNLOAD_TRANSACTIONDATA transactionData = new CUSTOM_DOWNLOAD_TRANSACTIONDATA();
                    transactionData.CUSTOM_DOWNLOAD_TRANSACTION = transaction;
                    transactionData.AIID = listTransactionDataModel[i].AIID;
                    transactionData.TransGUID = listTransactionDataModel[i].TransGUID;
                    transactionData.DocumentNumber = listTransactionDataModel[i].DocumentNumber;
                    transactionData.FileType = listTransactionDataModel[i].FileType;
                    transactionData.IPAddress = listTransactionDataModel[i].IPAddress;
                    transactionData.DestinationUser = listTransactionDataModel[i].DestinationUser;
                    transactionData.Key1 = listTransactionDataModel[i].Key1;
                    transactionData.Key2 = listTransactionDataModel[i].Key2;
                    transactionData.Key3 = listTransactionDataModel[i].Key3;
                    transactionData.DataLength = listTransactionDataModel[i].DataLength;
                    //transactionData.RowStatus = "";
                    EntityHelper.SetAuditForInsert(transactionData, "SERA");
                    entities.CUSTOM_DOWNLOAD_TRANSACTIONDATA.AddObject(transactionData);

                    for (int j = 0; j < listTransactionDataModel[i].Data.Length; j++)
                    {
                        //SPLITSTRING
                        S02008ViewModel modelSO2008 = SplitStringS02008(listTransactionDataModel[i].Data[j]);

                        if (modelSO2008 != null)
                        {
                            //insert into CUSTOM_S02008
                            CUSTOM_S02008 s02008 = new CUSTOM_S02008();
                            s02008.CUSTOM_DOWNLOAD_TRANSACTIONDATA = transactionData;
                            s02008.PONumber = modelSO2008.PONumber;
                            s02008.VersionPOSERA = modelSO2008.VersionPOSERA;
                            s02008.DataVersion = modelSO2008.DataVersion;
                            s02008.SalesOrderNo = modelSO2008.SalesOrderNo;
                            s02008.ChassisNumberByVendor = modelSO2008.ChassisNumberByVendor;
                            s02008.DateEntryCarrosserieAccessories = modelSO2008.DateEntryCarrosserieAccessories;
                            //start add by fhi 18.06.2014 : set owner
                            s02008.dibuatOleh = "system";
                            s02008.dibuatTanggal = DateTime.Now;
                            s02008.diubahOleh = "system";
                            s02008.diubahTanggal = DateTime.Now;
                            //end
                            entities.CUSTOM_S02008.AddObject(s02008);
                        }

                        //insert into CUSTOM_DOWNLOAD_TRANSACTIONDATADETAIL
                        CUSTOM_DOWNLOAD_TRANSACTIONDATADETAIL transactionDataDetail = new CUSTOM_DOWNLOAD_TRANSACTIONDATADETAIL();
                        transactionDataDetail.CUSTOM_DOWNLOAD_TRANSACTIONDATA = transactionData;
                        transactionDataDetail.Data = listTransactionDataModel[i].Data[j];
                        entities.CUSTOM_DOWNLOAD_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);
                    }
                }
                entities.SaveChanges();
                result = 1;
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            return result;
        }

        private S02008ViewModel SplitStringS02008(string stringHS)
        {
            S02008ViewModel model = new S02008ViewModel();
            try
            {
                string[] words = stringHS.Split('|');
                //if (words.Length == 3)
                //{
                for (int i = 1; i < words.Length; i++)
                {
                    if (!string.IsNullOrEmpty(words[i]))
                    {
                        decimal decimalValue;
                        var date = words[i].Trim();
                        switch (i)
                        {
                            case 1:
                                model.PONumber = words[i].Length > 50 ? words[i].Substring(0, 50).Trim() : words[i].Trim();
                                break;
                            case 2:
                                if (Decimal.TryParse(words[i], out decimalValue))
                                {
                                    model.VersionPOSERA = Convert.ToDecimal(words[i]);
                                }
                                break;
                            case 3:
                                if (Decimal.TryParse(words[i], out decimalValue))
                                {
                                    model.DataVersion = Convert.ToDecimal(words[i]);
                                }
                                break;
                            case 4:
                                model.SalesOrderNo = words[i].Length > 15 ? words[i].Substring(0, 15).Trim() : words[i].Trim();
                                break;
                            case 5:
                                model.ChassisNumberByVendor = words[i].Length > 18 ? words[i].Substring(0, 18).Trim() : words[i].Trim();
                                break;
                            case 6:
                                var lastDate = words[i].Remove(words[i].IndexOf('}')).Trim();
                                model.DateEntryCarrosserieAccessories = !string.IsNullOrEmpty(lastDate) ? Convert.ToDateTime(lastDate) : Convert.ToDateTime("19000101");
                                //if (lastDate.Length == 8)
                                //{
                                //    if (lastDate.Substring(0, 2) == "20")
                                //    {
                                //        var dateFormat = Convert.ToDateTime(lastDate);
                                //        var dateFormatString = dateFormat.ToString("yyyyMMdd");
                                //        model.DateEntryCarrosserieAccessories = dateFormat;
                                //    }
                                //}
                                break;
                            default:
                                break;
                        }
                    }
                }
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return model;
        }

        public int UpdateTransactionS02008(List<TransactionDataViewModel> listTransactionDataModel)
        {
            int result = 0;
            try
            {
                for (int i = 0; i < listTransactionDataModel.Count; i++)
                {
                    for (int j = 0; j < listTransactionDataModel[i].Data.Length; j++)
                    {
                        //SPLITSTRING
                        S02008ViewModel modelSO2008 = SplitStringS02008(listTransactionDataModel[i].Data[j]);

                        //UPDATE CUSTOMPO 
                        CUSTOMPO customPO = entities.CUSTOMPOes.SingleOrDefault(x => x.PONUMBER == modelSO2008.PONumber);
                        //customPO.TGLMASUKKAROSERI = modelSO2008.DateEntryCarrosserieAccessories;

                        //UPDATE CUSTOMGR
                        CUSTOMGR customGR = entities.CUSTOMGRs.SingleOrDefault(x => x.PONUMBER == modelSO2008.PONumber);
                        //customGR.NOCHASIS_INPUT = modelSO2008.ChassisNumberByVendor;

                        if (customPO != null && customGR != null)
                        {
                            //calling sp_UpdateS02008
                            result = UpdateS02008(modelSO2008);
                        }
                        //entities.SaveChanges();
                    }
                }
                //result = 1;
            }
            catch (Exception ex)
            {
                return result = 0;
            }
            return result;
        }

        public int UpdateS02008(S02008ViewModel model)
        {
            int result;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_UpdateS02008", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = model.PONumber;
                cmd.Parameters.Add("VERSIONPOSERA", DbType.Int32).Value = Convert.ToInt32(model.VersionPOSERA);
                cmd.Parameters.Add("DATAVERSION", DbType.Int32).Value = Convert.ToInt32(model.DataVersion);
                cmd.Parameters.Add("SALESORDERNO", DbType.String).Value = model.SalesOrderNo;
                cmd.Parameters.Add("NOCHASIS_INPUT", DbType.String).Value = model.ChassisNumberByVendor;
                cmd.Parameters.Add("TGLMASUKKAROSERI", DbType.DateTime).Value = model.DateEntryCarrosserieAccessories;
                OpenConnection();
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        #endregion

        #endregion

    }
}