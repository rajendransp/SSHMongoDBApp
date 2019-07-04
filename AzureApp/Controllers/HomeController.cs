using AzureApp.Helper;
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
                CreateSSHTunnel(mongodbConnection,out string sSHLogs);
                MongoConnection mongoServer = GetMongoServer(mongodbConnection, out string mongoLogs);
                GetDatabases(mongoServer, out string databaseLogs);
                mongodbConnection.forwardedPortLocal.Stop();
                mongodbConnection.SshClient.Disconnect();
                return Json(string.Concat(sSHLogs, mongoLogs, databaseLogs), JsonRequestBehavior.AllowGet);
                //return Json(string.Concat(string.Join(",",mongoServer.MongoDBServer.GetDatabaseNames().ToList()),"_________________", mongoServer.MongoLogs), JsonRequestBehavior.AllowGet);
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

        private void CreateSSHTunnel(Models.MongoDBConnectionDetails connectionParameters,out string logs)
        {
            logs = string.Empty;
            try
            {
                string fullPath = Path.Combine(Server.MapPath("~/Files/certificates/"), connectionParameters.SshPrivateKey);
                ConnectionInfo conn = new ConnectionInfo(connectionParameters.SshHostName, int.Parse(connectionParameters.SshPort), connectionParameters.SshUserName, new PrivateKeyAuthenticationMethod(connectionParameters.SshUserName, new PrivateKeyFile(fullPath, connectionParameters.SshPassword)));
                connectionParameters.SshClient = new SshClient(conn);
                connectionParameters.SshClient.Connect();
                logs += string.Format("Connected with SSH Server \n");
                connectionParameters.forwardedPortLocal = new ForwardedPortLocal("127.0.0.1", connectionParameters.HostName, (uint)connectionParameters.Port);
                connectionParameters.SshClient.AddForwardedPort(connectionParameters.forwardedPortLocal);
                connectionParameters.forwardedPortLocal.Start();
                logs += string.Format("SSH Tunnel created \n");                
            }
            catch(Exception e)
            {
                logs += string.Format(e.Message);
            }      
        }

        private void GetDatabases(MongoConnection mongoConnection,out string logs)
        {
            logs = string.Empty;
            try
            {
                var databases = mongoConnection.MongoDBServer.GetDatabaseNames().ToList();
                logs += string.Format("\n Available databases in MongoDB Server:\n");
                foreach(string database in databases)
                    logs += string.Format(database + "\n");
            }
            catch(Exception e)
            {
                logs += string.Format(e.Message);
            }
            
        }
        private MongoConnection GetMongoServer(Models.MongoDBConnectionDetails connectionParameters,out string logs)
        {
            logs = string.Empty;
            MongoConnection connection = new MongoConnection();
            MongoDBClientConnection mongoDBClientConnection = PrepareMongoClient(connectionParameters);
            try
            {
                connection.MongoDBServer = mongoDBClientConnection.MongoDBClient.GetServer();                
                connection.MongoDBServer.Connect();
                logs += string.Format("Connected with MongoDB Server \n");                
            }
            catch (Exception e)
            {
                logs += string.Format(e.Message);
                
            }
            return connection;
        }
    
        private MongoDBClientConnection PrepareMongoClient(Models.MongoDBConnectionDetails connectionParameters)
        {
            MongoDBClientConnection connection = new MongoDBClientConnection();  
            connection.MongoDBClient = new MongoClient(GetMongoSettings(connectionParameters));               
            return connection;        
        }

        private MongoClientSettings GetMongoSettings(Models.MongoDBConnectionDetails connectionParameters)
        {           
            MongoClientSettings settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(connectionParameters.HostName, (int)connectionParameters.forwardedPortLocal.BoundPort),
                RetryWrites = true
            };        
            return settings;
        }
    }
}
