using Microsoft.Win32.TaskScheduler;
using System;
using System.Linq;
using System.Management;

namespace ProclickEmployeeLib.Helpers
{
    public class Scheduler
    {
        public static string TaskName = "ProClickApp";

        public static void SetLoginTrigger(string exePath)
        {
            using (var ts = new TaskService())
            {
                Logger.Log("SetLoginTrigger()");
                var td = ts.NewTask();
                td.RegistrationInfo.Author = "David Fried";
                td.RegistrationInfo.Description = "Runs the ProClick App";
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Settings.ExecutionTimeLimit = TimeSpan.FromDays(0);
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.RunOnlyIfNetworkAvailable = false;
                td.Triggers.AddNew(TaskTriggerType.Logon);

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem"))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    string userId = (string)collection.Cast<ManagementBaseObject>().First()["UserName"];
                    Logger.Log("wmi username " + userId);
               
                    td.Principal.UserId = userId;
                    td.Principal.LogonType = TaskLogonType.InteractiveToken;

                    td.Actions.Add(new ExecAction(exePath));
                    ts.RootFolder.RegisterTaskDefinition(TaskName, td, TaskCreation.Create, userId, null, TaskLogonType.InteractiveToken, null);
                }
            }
        }

        public static void DeleteTask()
        {
            using (var ts = new TaskService())
            {
                var task = ts.RootFolder.GetTasks().Where(a => a.Name.ToLower() == TaskName.ToLower()).FirstOrDefault();
                if (task != null)
                {
                    ts.RootFolder.DeleteTask(TaskName);
                }
            }
        }

        public static Task GetTask()
        {
            using (var ts = new TaskService())
            {
                var task = ts.RootFolder.GetTasks().Where(a => a.Name.ToLower() == TaskName.ToLower()).FirstOrDefault();
                return task;
            }
        }

        public static bool VerifyTask()
        {
            return GetTask() != null;
        }
    }
}
