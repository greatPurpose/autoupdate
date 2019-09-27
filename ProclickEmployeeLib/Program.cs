using ProclickEmployeeLib.Helpers;
using ProclickEmployeeLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProclickEmployeeLib
{
    class Program
    {        
        static void Main(string[] args)
        {
            Settings.Load();
            new HttpTunnelHelper();
            Task.Run(() =>
                HttpListenerHelper.CreateListener(new string[] { $"http://localhost:{Settings.HttpListenerPort}/index/" })
            );
            
            ProcessesHelper.Watch();
           
            InterceptKeys.Watch();
           
            Console.ReadLine();
        }

       
    }
}
