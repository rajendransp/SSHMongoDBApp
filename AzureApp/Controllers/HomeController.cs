﻿using AzureApp.Helper;
using MongoDB.Driver;
using Renci.SshNet;
using Syncfusion.Dashboard.Service.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;

namespace AzureWebApp.Controllers
{
    public class MongoConnection
    {
        public MongoServer MongoDBServer { get; set; }

        public string MongoLogs { get; set; }
    }

    public class MongoDBClientConnection
    {
        public MongoClient MongoDBClient { get; set; }
        public string MongoLogs { get; set; }
    }
    public class HomeController : Controller
    {
        [HttpPost]
        public JsonResult FormOne(Models.MongoDBConnectionDetails mongodbConnection)
        {
            try
            {
                MongoConnection mongoServer = GetMongoServer(mongodbConnection);
                return Json(string.Concat(string.Join(",",mongoServer.MongoDBServer.GetDatabaseNames().ToList()),"_________________", mongoServer.MongoLogs), JsonRequestBehavior.AllowGet);
            }
            catch(Exception e)
            {
                return Json(string.Concat(string.IsNullOrEmpty(e.Message) ? "":e.Message,"**********",e.InnerException==null?"":e.InnerException.Message, JsonRequestBehavior.AllowGet));
            }
        }
        
        [HttpPost]
        public string FileUpload(string filePath)
        {
            return FilePathHelper.WriteStreamToFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath.ToString())));
        }

        [HttpPost]
        public ActionResult UploadFiles()
        {
            if (Request.Files.Count > 0)
            {
                try
                {  
                    HttpFileCollectionBase files = Request.Files;
                    string fname = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                    for (int i = 0; i < files.Count; i++)
                    {
                        HttpPostedFileBase file = files[i];
                        string filename = Path.Combine(Server.MapPath("~/Files/certificates/"), fname);
                        string directoryPath = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        file.SaveAs(filename);
                    }
                    return Json(fname);
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase postedFile)
        {
            string path = Server.MapPath("~/Uploads/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (postedFile != null)
            {
                string fileName = Path.GetFileName(postedFile.FileName);
                postedFile.SaveAs(path + fileName);
                ViewBag.Message += string.Format("<b>{0}</b> uploaded.<br />", fileName);
            }

            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact(string message)
        {
            ViewBag.Message = message;

            return View();
        }
        private MongoConnection GetMongoServer(Models.MongoDBConnectionDetails connectionParameters)
        {
            MongoConnection connection = new MongoConnection();
            MongoDBClientConnection mongoDBClientConnection = PrepareMongoClient(connectionParameters);
            try
            {
                connection.MongoDBServer = mongoDBClientConnection.MongoDBClient.GetServer();
                connection.MongoLogs = mongoDBClientConnection.MongoLogs;
                //connection.MongoDBServer.Connect();
                return connection;
            }
            catch (Exception e)
            {
                throw new Exception(mongoDBClientConnection.MongoLogs,e);
            }
        }
    
        private MongoDBClientConnection PrepareMongoClient(Models.MongoDBConnectionDetails connectionParameters)
        {
            MongoDBClientConnection connection = new MongoDBClientConnection();
            string logs = string.Empty;
            try
            {
                connection.MongoDBClient = new MongoClient(GetMongoSettings(connectionParameters, out logs));
                connection.MongoLogs = logs;
                return connection;
            }
            catch(Exception ex)
            {
                throw new Exception(logs,ex);
            }
        }

        private MongoClientSettings GetMongoSettings(Models.MongoDBConnectionDetails connectionParameters,out string logs)
        {
            logs = string.Empty;
            MongoClientSettings settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(connectionParameters.HostName, connectionParameters.Port),
                RetryWrites = true
            };
            logs += "--- Basic MongoDB Settings has done. \n";

            string hostname = connectionParameters.SshHostName;
            logs += string.Format("--- Basic SSH Settings has done wih SSH host name {0} \n",hostname);
            int port = int.Parse(connectionParameters.SshPort);
            logs += string.Format("--- Basic SSH Settings has done wih SSH port name {0} \n", port);
            string username = connectionParameters.SshUserName;
            logs += string.Format("--- Basic SSH Settings has done wih SSH UserName {0} \n", username);
            string password = connectionParameters.SshPassword;
            logs += string.Format("--- Basic SSH Settings has done wih SSH Password {0} \n", password);
            logs += string.Format("--- Basic SSH Settings has done wih SSH Private key file {0} \n", connectionParameters.SshPrivateKey);
           string fullPath = Path.Combine(Server.MapPath("~/Files/certificates/"), connectionParameters.SshPrivateKey);
            logs += "--- Certificate file as readed as stream \n";
            try
            {

                ConnectionInfo conn = new ConnectionInfo(hostname, port, username, new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(fullPath, password)));
                logs += "--- SSH Connection setting has been prepared. \n";
                SshClient cli = new SshClient(conn);
                logs += "--- SSH Connection has been prepared. \n";
                cli.Connect();
                logs += "--- SSH Connection has been started. \n";
            }
            catch(Exception exception)
            {
                logs += "--- SSH Connection has been stopeed. \n";
                logs += string.Format("--- {0}\n",exception.Message);
                if(exception.InnerException != null)
                {
                    logs += string.Format("--- {0}\n", exception.InnerException.Message);
                }
            }


            logs += "--- SSH Connection has been finished. \n";











            //if (connectionParameters.IsSslEnabled &&
            //    !connectionParameters.AuthenticationMechanism.Equals(Models.MongoAuthentication.X509))
            //{
            //    settings.VerifySslCertificate = !connectionParameters.IsSelfSignedEnabled;
            //    connectionParameters.SshPrivateKeyData = FilePathHelper.ReadFile(Server, connectionParameters.SshPrivateKey);
            //    logs += "--- SSL Certificate data has been retrieved. \n";
            //    if (connectionParameters.SshPrivateKeyData != null)
            //    {
            //        logs += "--- SSL Certificate data not null. \n";
            //        logs += "----CertificateData. \n" + System.Text.Encoding.UTF8.GetString(connectionParameters.SshPrivateKeyData);
            //        var certificate = string.IsNullOrEmpty(connectionParameters.SshUserName) ? new X509Certificate2(connectionParameters.SshPrivateKeyData) : new X509Certificate2(connectionParameters.SshPrivateKeyData, connectionParameters.SshUserName);
            //        logs += "----CertificateAdded. \n";
            //        settings.SslSettings = new SslSettings()
            //        {
            //            ClientCertificates = new[] { certificate }
            //        };
            //        logs += certificate.Subject;
            //        logs += "--- Certificate has been added. \n";
            //    }
            //}
            //switch (connectionParameters.AuthenticationMechanism)
            //{
            //    case Models.MongoAuthentication.SCRAM:
            //        break;
            //    case Models.MongoAuthentication.X509:
            //        settings.VerifySslCertificate = !connectionParameters.IsSelfSignedEnabled;
            //        settings.UseSsl = true;
            //        connectionParameters.SshPrivateKeyData = FilePathHelper.ReadFile(Server, connectionParameters.SshPrivateKey);
            //        var certificate = string.IsNullOrEmpty(connectionParameters.SshUserName) ? new X509Certificate2(connectionParameters.SshPrivateKeyData) : new X509Certificate2(connectionParameters.SshPrivateKeyData, connectionParameters.SshUserName);
            //        settings.SslSettings = new SslSettings()
            //        {
            //            ClientCertificates = new[] { certificate }
            //        };
            //        settings.Credential = MongoCredential.CreateMongoX509Credential(certificate.Subject.ToString().Replace("S=", "ST=").Replace(", ", ","));
            //        logs += "--- switch called for x509 \n";
            //        break;
            //    default:
            //        break;
            //}
            //logs += "--- settings has been returned. \n";
            return settings;
        }
    }
}