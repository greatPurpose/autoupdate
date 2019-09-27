using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HWND = System.IntPtr;

namespace ProclickEmployeeLib.Helpers
{
    public class ProcessesHelper
    {
        public static Process[] GetAllProccesses()
        {
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    Logger.Log(string.Format("Process: {0} ID: {1} Window title: {2}", process.ProcessName, process.Id, process.MainWindowTitle));
                }
            }

            return processlist;
        }

        /// <summary>
        /// Not reliable. I think the standard Processes does a better job
        /// </summary>
        /// <returns></returns>
        public static string[] GetOpenWindows()
        {
            var windows = OpenWindowGetter.GetOpenWindows();
            foreach (KeyValuePair<IntPtr, string> window in windows)
            {
                IntPtr handle = window.Key;
                string title = window.Value;

                Logger.Log(string.Format("{0}: {1}", handle, title));
            }

            return windows.Select(x => x.Value).ToArray();
        }

        /// <summary>
        /// Contains functionality to get all the open windows.
        /// </summary>
        private static class OpenWindowGetter
        {
            /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
            /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
            public static IDictionary<HWND, string> GetOpenWindows()
            {
                HWND shellWindow = GetShellWindow();
                Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

                EnumWindows(delegate (HWND hWnd, int lParam)
                {
                    if (hWnd == shellWindow) return true;
                    if (!IsWindowVisible(hWnd)) return true;

                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) return true;

                    StringBuilder builder = new StringBuilder(length);
                    GetWindowText(hWnd, builder, length + 1);

                    windows[hWnd] = builder.ToString();
                    return true;

                }, 0);

                return windows;
            }

            private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

            [DllImport("USER32.DLL")]
            private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

            [DllImport("USER32.DLL")]
            private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("USER32.DLL")]
            private static extern int GetWindowTextLength(HWND hWnd);

            [DllImport("USER32.DLL")]
            private static extern bool IsWindowVisible(HWND hWnd);

            [DllImport("USER32.DLL")]
            private static extern IntPtr GetShellWindow();
        }


        public static void Watch()
        {
            //if (RuningAsAdminChecker.IsProcessElevated)
            //{
            try
            {
                ManagementEventWatcher startWatch = new ManagementEventWatcher(
                  new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                startWatch.EventArrived += new EventArrivedEventHandler(StartWatch_EventArrived);
                startWatch.Start();
                Logger.Log("Start proccess watcher created");
            }
            catch (Exception e)
            {
                Logger.Log($"Exception of type {e.GetType()} when starting Process watcher, perhaps running without admin rights. message {e.Message}", true);
            }
            //}
            //else
            //{
            //    Logger.Log("process is not elevated, can not start process watcher", true);
            //}

            //ManagementEventWatcher stopWatch = new ManagementEventWatcher(
            //  new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            //stopWatch.EventArrived += new EventArrivedEventHandler(StopWatch_EventArrived);
            //stopWatch.Start();
            //Logger.Log("Stop proccess watcher created");
        }

        static void StopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //stopWatch.Stop();
            Logger.Log($"Process stopped: {e.NewEvent.Properties["ProcessName"].Value}");
        }

        static string[] ProccesesToKill = new string[] { "devtools", "fiddler" }; 

        static void StartWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //startWatch.Stop();
            //Logger.Log($"Process started: {e.NewEvent.Properties["ProcessName"].Value}");
            //TRY TO GET THE PROCESS AND ITS WINDOW TITLE

            var id = e.NewEvent.Properties["ProcessID"].Value;
            var parentId = e.NewEvent.Properties["ParentProcessID"].Value;

            try
            {
                Process proc = null;
                Process parentProc = null;

                proc = TryGetProcess(id);
                parentProc = TryGetProcess(parentId);

                if (proc?.MainWindowTitle.Trim() != "" || parentProc?.MainWindowTitle.Trim() != "")
                {
                    Logger.Log($"{proc?.MainWindowTitle} ({proc.Id}) : {parentProc?.MainWindowTitle} ({parentProc.Id})");
                    if(ProccesesToKill.Any( x =>  (proc?.MainWindowTitle + parentProc?.MainWindowTitle).ToLower().Contains(x)))
                    {
                        if (Login.IsClocking())
                        {
                            Logger.Log($"This process {proc?.MainWindowTitle + parentProc?.MainWindowTitle} needs to be killed!");
                            TryKillProcess(proc);
                        }
                    }
                }               
            }
            catch (Exception exc)
            {
            }
        }

        private static Process TryGetProcess(object id)
        {
            try
            {
                return Process.GetProcessById(int.Parse(id.ToString()));
            }
            catch
            {
                return null;
            }
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                process.Kill();
                Logger.Log("Process killed");
            }
            catch (Exception e)
            {
                Logger.Log("error killing process " + e.Message, true);
            }
        }
    }
}
