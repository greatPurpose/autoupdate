using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProclickEmployeeLib.Models
{
    class FtpCredentials
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Path { get; set; } = "/";
    }
}
