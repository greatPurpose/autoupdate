using ProclickEmployeeLib.Models;
using System;
using System.Collections.Specialized;
using System.IO;

namespace ProclickEmployeeLib.Helpers
{
    public class Logger
    {
        public static string BasePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProclickService");
            }
        }

        public static string RelativePath
        {
            get
            {
                DateTime dt = DateTime.Now;
                return Path.Combine(dt.Year.ToString(), dt.Month.ToString(), dt.Day.ToString() + ".txt");
            }
        }

        public static void Log(string str, bool logToServer = false)
        {
            Console.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);

            try
            {
                var dir = BasePath;
                string log = string.Format("[{0}] {1}{2}", DateTime.Now, str, Environment.NewLine);
                DateTime dt = DateTime.Now;
                string logFilePath = Path.Combine(dir, dt.Year.ToString(), dt.Month.ToString());
                Directory.CreateDirectory(logFilePath);
                File.AppendAllText(Path.Combine(logFilePath, dt.Day.ToString() + ".txt"), log);

                if (logToServer)
                {
                    try
                    {
                        Log("saving log to api");                   
                        
                        var uri = $"{Settings.ApiHost}/api/employeeService/log?message={str}&employeeId={Settings.EmployeeId}";
                        //var parameters = new NameValueCollection { { "message", str }, { "employeeId", Settings.EmployeeId.ToString() } };
                        HttpHelper.PostRequest(uri);

                        Log("message saved to server");
                    }
                    catch (Exception e)
                    {
                        Log("Error saving to server. " + e.Message);
                    }
                }
            }
            catch
            {            
            }

        }
    }
}
