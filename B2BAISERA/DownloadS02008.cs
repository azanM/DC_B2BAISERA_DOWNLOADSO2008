using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using B2BAISERA.Log;
using B2BAISERA.Models.Providers;
using B2BAISERA.Properties;
using System.Net;
using System.Globalization;
using B2BAISERA.Models;
using System.Diagnostics;

namespace B2BAISERA
{
    public partial class DownloadS02008 : Form
    {
        private static string fileType = "S02008";
        private bool acknowledge;
        private string ticketNo = "";
        private string message = "";

        public DownloadS02008()
        {
            InitializeComponent();
        }

        private void DownloadS02008_Load(object sender, EventArgs e)
        {
            LogEvent logEvent = new LogEvent();
            //LoginAuthentication();
            //if (acknowledge == false || ticketNo == string.Empty)
            //{
            //    //close
            //}
            //else Download();

            //logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", "Download Finish.", fileType, "SERA");
            //Process.Start("taskkill.exe", "/f /im B2BAISERA_S02008.exe");

            try
            {
                LoginAuthentication();
                if (acknowledge == false || ticketNo == string.Empty)
                {
                    //close
                }
                else Download();

                logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", "Download Finish.", fileType, "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02008.exe");
            }
            catch (Exception ex)
            {
                //log file error
                //logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", ex.ToString(), "S02008", "SERA");
                logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", ex.Message, fileType, "SERA");
                //logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", ex.InnerException.Message, "S02008", "SERA");
                //logEvent.WriteDBLog("", "DownloadS02008_Load", false, "", ex.InnerException.StackTrace, "S02008", "SERA");


                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02008.exe");
            }
        }

        private void LoginAuthentication()
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            try
            {
                using (WsDev.B2BAIWebServiceDMZ wsB2B = new WsDev.B2BAIWebServiceDMZ())
                {
                    var User = transactionProvider.GetUser("SERA", "SERA", "B2BAITAG");
                    if (User != null)
                    {
                        var loginReq = new WsDev.LoginRequest();
                        loginReq.UserName = User.UserCode;
                        loginReq.Password = User.PassCode;
                        loginReq.ClientTag = User.ClientTag;

                        //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                        //WebProxy myProxy = new WebProxy();
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                        //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                        //wsB2B.Proxy = myProxy;

                        var wsResult = wsB2B.LoginAuthentication(loginReq);
                        acknowledge = wsResult.Acknowledge;
                        ticketNo = wsResult.TicketNo;
                        message = wsResult.Message;
                    }

                    LblResult.Text = "Service Result = ";
                    LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                    LblTicketNo.Text = "TicketNo : " + ticketNo;
                    LblMessage.Text = "Message :" + message;

                    //logevent login succeded
                    logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, message, fileType, "SERA");
                }
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");
            }
        }

        private void Download()
        {
            LogEvent logEvent = new LogEvent();
            try
            {
                using (WsDev.B2BAIWebServiceDMZ wsB2B = new WsDev.B2BAIWebServiceDMZ())
                {
                    //Sample Data Download Request
                    var downloadRequest = SampleDataDownloadRequestNew();

                    //hit web service
                    //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                    //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                    //WebProxy myProxy = new WebProxy();
                    //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                    //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                    //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                    //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                    //wsB2B.Proxy = myProxy;

                    var wsResult = wsB2B.DownloadDocument(downloadRequest);
                    acknowledge = wsResult.Acknowledge;
                    ticketNo = wsResult.TicketNo;
                    message = wsResult.Message;
                    //end hit web service

                    if (wsResult.transactionData == null)
                    {
                        downloadRequest = SampleDataDownloadRequestInProgress();
                        wsResult = wsB2B.DownloadDocument(downloadRequest);
                        acknowledge = wsResult.Acknowledge;
                        ticketNo = wsResult.TicketNo;
                        message = wsResult.Message;
                    }
                    List<B2BAISERA.Models.TransactionDataViewModel> listTransactionDataModel = new List<B2BAISERA.Models.TransactionDataViewModel>();
                    if (wsResult.transactionData != null)
                    {
                        WsDev.TransactionData[] transactionData = wsResult.transactionData;

                        for (int i = 0; i < transactionData.Length; i++)
                        {
                            B2BAISERA.Models.TransactionDataViewModel transactionDataModel = new B2BAISERA.Models.TransactionDataViewModel();
                            transactionDataModel.AIID = transactionData[i].ID;
                            transactionDataModel.TransGUID = transactionData[i].TransGUID;
                            transactionDataModel.DocumentNumber = transactionData[i].DocumentNumber;
                            transactionDataModel.FileType = transactionData[i].FileType;
                            transactionDataModel.IPAddress = transactionData[i].IPAddress;
                            transactionDataModel.DestinationUser = transactionData[i].DestinationUser;
                            transactionDataModel.Key1 = transactionData[i].Key1;
                            transactionDataModel.Key2 = transactionData[i].Key2;
                            transactionDataModel.Key3 = transactionData[i].Key3;
                            transactionDataModel.DataLength = transactionData[i].DataLength;
                            if (transactionData[i].Data.Count() > 0)
                            {
                                List<string> arrData1 = new List<string>();
                                for (int j = 0; j < transactionData[i].Data.Count(); j++)
                                {
                                    arrData1.Add(transactionData[i].Data[j]);
                                }
                                transactionDataModel.Data = arrData1.ToArray();
                            }
                            listTransactionDataModel.Add(transactionDataModel);
                        }
                    }
                    logEvent.WriteDBLog("B2BAIWebServiceDMZ", "DownloadDocumentS02008", acknowledge, ticketNo, message, fileType, "SERA");
                    LblResult.Text = "Service Result = ";
                    LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                    LblTicketNo.Text = "TicketNo : " + ticketNo;
                    LblMessage.Text = "Message :" + message;

                    TransactionProvider transactionProvider = new TransactionProvider();
                    int updateTransaction = 0;
                    int insertTransaction = 0;

                    if (listTransactionDataModel.Count > 0)
                    {
                        //2. UPDATE DB EPROC FROM listTransactionDataModel 
                        updateTransaction = transactionProvider.UpdateTransactionS02008(listTransactionDataModel);

                        //3. SUCCEDED OR FAILED, CALL METHOD UPDATEDOCUMENTSTATUS
                        if (updateTransaction == 1)
                        {
                            //4. INSERT INTO CUSTOM_TRANSACTION + CUSTOM_TRANSACTIONDATA + CUSTOM_TRANSACTIONDATADETAIL + CUSTOM_S02008 
                            insertTransaction = transactionProvider.InsertLogTransactionDownloadS02008(acknowledge, ticketNo, message, "Succeeded", listTransactionDataModel);
                            if (insertTransaction == 1)
                            {
                                var updateStatusRequest = SucceededDataUpdateStatusRequest(listTransactionDataModel);
                                var wsResultUpdateDocStatus = wsB2B.UpdateDocumentStatus(updateStatusRequest);
                                acknowledge = wsResultUpdateDocStatus.Acknowledge;
                                ticketNo = wsResultUpdateDocStatus.TicketNo;
                                message = wsResultUpdateDocStatus.Message;
                            }
                        }
                        else
                        {
                            //4. INSERT INTO CUSTOM_TRANSACTION + CUSTOM_TRANSACTIONDATA + CUSTOM_TRANSACTIONDATADETAIL + CUSTOM_S02008 
                            insertTransaction = transactionProvider.InsertLogTransactionDownloadS02008(acknowledge, ticketNo, message, "Failed", listTransactionDataModel);
                            if (insertTransaction == 1)
                            {
                                //var updateStatusRequest = FailedDataUpdateStatusRequest(listTransactionDataModel);
                                //var wsResultUpdateDocStatus = wsB2B.UpdateDocumentStatus(updateStatusRequest);
                                //acknowledge = wsResultUpdateDocStatus.Acknowledge;
                                //ticketNo = wsResultUpdateDocStatus.TicketNo;
                                //message = wsResultUpdateDocStatus.Message;
                                ////message += ", but Update Data Failed.";
                            }
                        }
                        logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UpdateDocumentStatusS02008", acknowledge, ticketNo, message, fileType, "SERA");

                        LblResult.Text = "Service Result = ";
                        LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                        LblTicketNo.Text = "TicketNo : " + ticketNo;
                        LblMessage.Text = "Message :" + message;
                    }
                }
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;

                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "DownloadDocumentS02008", acknowledge, ticketNo, message, fileType, "SERA");
            }
        }

        private WsDev.DownloadRequest SampleDataDownloadRequestNew()
        {
            WsDev.DownloadRequest downloadRequest = new WsDev.DownloadRequest();
            TransactionProvider transactionProvider = new TransactionProvider();
            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
            downloadRequest.TicketNo = lastTicketNo;
            downloadRequest.ClientTag = Resources.ClientTag;
            downloadRequest.FileType = fileType;
            downloadRequest.SourceUser = "AI";
            downloadRequest.Status = "New";
            downloadRequest.TransDateFrom = DateTime.Now.AddDays(-3);//Convert.ToDateTime("2013-10-29T00:00:00", new CultureInfo("id-ID"));
            downloadRequest.TransDateTo = DateTime.Now;//Convert.ToDateTime("2013-10-30T00:00:00", new CultureInfo("id-ID"));
            downloadRequest.Key1 = "";
            downloadRequest.Key2 = "";
            downloadRequest.Key3 = "";

            return downloadRequest;
        }

        private WsDev.DownloadRequest SampleDataDownloadRequestInProgress()
        {
            WsDev.DownloadRequest downloadRequest = new WsDev.DownloadRequest();
            TransactionProvider transactionProvider = new TransactionProvider();
            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
            downloadRequest.TicketNo = lastTicketNo;
            downloadRequest.ClientTag = Resources.ClientTag;
            downloadRequest.FileType = fileType;
            downloadRequest.SourceUser = "AI";
            downloadRequest.Status = "InProgress";
            downloadRequest.TransDateFrom = DateTime.Now.AddDays(-3);//Convert.ToDateTime("2013-10-29T00:00:00", new CultureInfo("id-ID"));
            downloadRequest.TransDateTo = DateTime.Now;//Convert.ToDateTime("2013-10-30T00:00:00", new CultureInfo("id-ID"));
            downloadRequest.Key1 = "";
            downloadRequest.Key2 = "";
            downloadRequest.Key3 = "";

            return downloadRequest;
        }

        private WsDev.UpdateStatusRequest SucceededDataUpdateStatusRequest(List<TransactionDataViewModel> listTransactionDataModel)
        {
            List<B2BAISERA.WsDev.TransactionDataID> listTransactionDataID = new List<WsDev.TransactionDataID>();
            for (int i = 0; i < listTransactionDataModel.Count; i++)
            {
                B2BAISERA.WsDev.TransactionDataID transactionDataID = new B2BAISERA.WsDev.TransactionDataID();
                transactionDataID.TransGUID = listTransactionDataModel[i].TransGUID;
                transactionDataID.DocumentNumber = listTransactionDataModel[i].DocumentNumber;
                transactionDataID.Key1 = listTransactionDataModel[i].Key1;
                transactionDataID.Key2 = listTransactionDataModel[i].Key2;
                transactionDataID.Key3 = listTransactionDataModel[i].Key3;
                transactionDataID.TransStatus = "Succeeded";
                transactionDataID.LogMessage = listTransactionDataModel[i].DocumentNumber + " SUKSES di-proses";
                listTransactionDataID.Add(transactionDataID);
            }

            WsDev.UpdateStatusRequest updateStatusRequest = new WsDev.UpdateStatusRequest();
            TransactionProvider transactionProvider = new TransactionProvider();
            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
            updateStatusRequest.TicketNo = lastTicketNo;
            updateStatusRequest.ClientTag = Resources.ClientTag;
            updateStatusRequest.transactionDataID = listTransactionDataID.ToArray();

            return updateStatusRequest;
        }

        private WsDev.UpdateStatusRequest FailedDataUpdateStatusRequest(List<TransactionDataViewModel> listTransactionDataModel)
        {
            List<B2BAISERA.WsDev.TransactionDataID> listTransactionDataID = new List<WsDev.TransactionDataID>();
            for (int i = 0; i < listTransactionDataModel.Count; i++)
            {
                B2BAISERA.WsDev.TransactionDataID transactionDataID = new B2BAISERA.WsDev.TransactionDataID();
                transactionDataID.TransGUID = listTransactionDataModel[i].TransGUID;
                transactionDataID.DocumentNumber = listTransactionDataModel[i].DocumentNumber;
                transactionDataID.Key1 = listTransactionDataModel[i].Key1;
                transactionDataID.Key2 = listTransactionDataModel[i].Key2;
                transactionDataID.Key3 = listTransactionDataModel[i].Key3;
                transactionDataID.TransStatus = "Failed";
                transactionDataID.LogMessage = listTransactionDataModel[i].DocumentNumber + " GAGAL di-proses";
                listTransactionDataID.Add(transactionDataID);
            }

            WsDev.UpdateStatusRequest updateStatusRequest = new WsDev.UpdateStatusRequest();
            TransactionProvider transactionProvider = new TransactionProvider();
            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
            updateStatusRequest.TicketNo = lastTicketNo;
            updateStatusRequest.ClientTag = Resources.ClientTag;
            updateStatusRequest.transactionDataID = listTransactionDataID.ToArray();

            return updateStatusRequest;
        }
    }
}
