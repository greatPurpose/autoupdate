using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIBootstrapper
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

        public static void Log(string str)
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
            }
            catch
            {
            }

        }
    }
}
