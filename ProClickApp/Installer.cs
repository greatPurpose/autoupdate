using ProclickEmployeeLib.Helpers;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ProClickApp
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);
            //Add custom code here          
        }
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            //Add custom code here
        }
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            //Add custom code here
            Logger.Log("Installer override - move settings to add data folder");
            File.Delete(Path.Combine(Logger.BasePath, "Settings.json"));
            File.Move(Path.Combine(Path.GetDirectoryName(Context.Parameters["AssemblyPath"]), "Settings.json" ), Path.Combine(Logger.BasePath, "Settings.Json"));

            Logger.Log("Creating the scheduled task");

            Scheduler.SetLoginTrigger(Path.GetDirectoryName(Context.Parameters["AssemblyPath"]) + @"\ProClickApp.exe");
            
            Logger.Log("verifying task created...");
            if(Scheduler.VerifyTask())
            {
                Logger.Log("Task verified!");
            }
            else
            {
                Logger.Log("task not verified! throwing exception");
                throw new Exception("Could not created scheduled task!");
            }

            Logger.Log("Installer override - start exe file");
            Process.Start(Path.GetDirectoryName(Context.Parameters["AssemblyPath"]) + @"\ProClickApp.exe");
            Logger.Log("Installer override - start exe file complete");
        }

        public override void Uninstall(IDictionary savedState)
        {
            Logger.Log("uninstall overried - killing ngrok processes...");
            var ngrok = Process.GetProcessesByName("ngrok");
            ngrok.ToList().ForEach(x => x.Kill());
            ngrok = Process.GetProcessesByName("ngrok.exe");
            ngrok.ToList().ForEach(x => x.Kill());
            Logger.Log("uninstall overried - killing done\nDeleteing login sceduled task..");
            Scheduler.DeleteTask();
            Logger.Log("uninstall overried - DeleteTask verification, task exists: " + Scheduler.VerifyTask().ToString());
            base.Uninstall(savedState);
        }
    }
}
