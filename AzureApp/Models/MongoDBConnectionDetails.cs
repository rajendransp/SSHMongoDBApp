using AzureApp.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;

namespace AzureWebApp.Models
{
    public class FileModel
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase PostedFile { get; set; }
    }
    public class MongoDBConnectionDetails
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string SshHostName { get; set; }
        public string SshPort { get; set; }
        public string SshPrivateKey { get; set; }
        public string SshUserName { get; set; }
        public string SshPassword { get; set; }
        public byte[] SshPrivateKeyData
        {
            get;set;
        }
    }

    public enum MongoAuthentication
    {
        SCRAM,
        NONE,
        X509
    }
}