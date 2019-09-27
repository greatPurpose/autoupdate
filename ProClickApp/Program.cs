using ProclickEmployeeLib.Helpers;
using ProclickEmployeeLib.Models;
using ProclickEmployeeLib.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProClickApp
{
    static class Program
    {
        //static readonly RegistryKey RegistryKeyApp =
        //    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        //static readonly string AppName = "ProclickApp";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.Log("App Starting on pid " + Process.GetCurrentProcess().Id);
            Logger.Log("Current version number = " + Settings.Version);

            //if (args.Length > 0 && args[0].ToLower() == "startupremove")
            //{
            //    try
            //    {
            //        Logger.Log("removing startup app from registry");
            //        RegistryKeyApp.DeleteValue(AppName, false);
            //        Logger.Log("Satrtup app removed");
            //    }
            //    catch (Exception e)
            //    {
            //        Logger.Log("error removing startup app - " + e.ToString());
            //    }

            //    return;
            //}

            //try
            //{
            //    Logger.Log("Setting as startup app in registry");
            //    RegistryKeyApp.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
            //    Logger.Log("registered as a startup app");
            //}
            //catch (Exception e)
            //{
            //    Logger.Log("Error registering startup app - " + e.ToString(), true);
            //}

            Logger.Log("terminating current instance if exists");
            var curPid = Process.GetCurrentProcess().Id;
            Process.GetProcessesByName("ProClickApp").ToList().ForEach(x =>
            {
                if (x.Id != curPid)
                {
                    x.Kill();
                    Logger.Log("killed process id: " + x.Id);
                }
            });


            //using (new SingleGlobalInstance(1000))
            //{
            Logger.Log("Starting the program");

            try
            {
                Task.Run(() =>
                {
                    Utilities.AutoUpdate();
                    if (System.Windows.Forms.Application.MessageLoop)
                    {
                        // WinForms app
                        System.Windows.Forms.Application.Exit();
                    }
                    else
                    {
                        // Console app
                        System.Environment.Exit(1);
                    }
                });

                Settings.Load();
                InterceptKeys.Watch();
                ProcessesHelper.Watch();
                Utilities.UploadFilesTimer();
                
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            new HttpTunnelHelper();

                            HttpListenerHelper
                                .CreateListener(new string[] { $"http://localhost:{Settings.HttpListenerPort}/index/" });
                            Logger.Log("Listener created successfully");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Exception in http listener or tunnel" + ex.Message, true);
                            Logger.Log(ex.ToString(), true);
                            if (i >= 5)
                            {
                                HandleException(ex);
                            }
                            else
                            {
                                Settings.HttpListenerPort += 1;
                            }
                        }
                    }
                });

            }
            catch (Exception ex)
            {
                Logger.Log("Exception in program start" + ex.Message, true);
                Logger.Log(ex.ToString(), true);
                HandleException(ex);
            }
            //}

            Application.Run();

        }

        internal static void HandleException(Exception ex)
        {
            string LF = Environment.NewLine + Environment.NewLine;
            string title = $"Oops... I got a crash at {DateTime.Now}";
            string infos = $"Please take a screenshot of this message\n\r\n\r" +
                           $"Message : {LF}{ex.Message}{LF}" +
                           $"Source : {LF}{ex.Source}{LF}" +
                           $"Stack : {LF}{ex.StackTrace}{LF}" +
                           $"InnerException : {ex.InnerException}";

            MessageBox.Show(infos, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

            Main(new string[] { });
        }

    }
}
