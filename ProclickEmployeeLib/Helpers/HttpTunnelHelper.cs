using Newtonsoft.Json;
using ProclickEmployeeLib.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ProclickEmployeeLib.Helpers
{
    public class HttpTunnelHelper
    {       
        private System.Timers.Timer timer;
        
        public HttpTunnelHelper()
        {
            Logger.Log("current dir: " + Settings.ExecutionDir);
            if(!File.Exists(Path.Combine(Settings.ExecutionDir, "ngrok.exe")))
            {
                throw new Exception("ngrok not where I am looking for it!");
            }
            NgrokInit();
        }

        private void NgrokInit()
        {
            timer = new System.Timers.Timer
            {
                Interval = 60 * 60 * 1000
            };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            KillAllNgrokProcess();

            var ngrokCommand = "cd \"" + Settings.ExecutionDir + "\" & " +
                $"ngrok.exe http localhost:{Settings.HttpListenerPort} " +
                $"-host-header=\"localhost:{Settings.HttpListenerPort}\"";

            Logger.Log("ngrok command - " + ngrokCommand);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k {ngrokCommand}"                
            };

            Process p = new Process
            {
                StartInfo = psi
            };

            if(Settings.Visibility == 0)
            {
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;                   
            }

            Logger.Log("starting ngrok process. visibility mode = " + Settings.Visibility);
            p.Start();
            Logger.Log("process running");

            Task.Run(() =>
            {
                try
                {
                    int i = 0;
                    bool ngrockRunning = false;
                    while (i < 25)
                    {
                        i++;
                        Thread.Sleep(500);
                        var json = HttpHelper.GetRequest(Settings.NgrokWebInterface);
                        if (json != null)
                        {
                            var obj = JsonConvert.DeserializeObject<NgrokModel>(json);
                            if (obj.tunnels.Length > 0)
                            {
                                ngrockRunning = true;
                                var publicUrl = obj.tunnels[obj.tunnels.Length - 1].public_url;
                                Logger.Log("ngrok url: " + publicUrl + "/index");
                                SaveUrl(publicUrl);
                                break;
                            }
                        }
                    }
                    if (!ngrockRunning)
                        Logger.Log("Error! ngrok is not starting !");
                }
                catch (Exception e)
                {
                    Logger.Log("exception in ngrok start " + e.Message, true);
                    Logger.Log(e.ToString(), true);
                }
            });

            Task.Run(() =>
            {
                try
                {                    
                    p.WaitForExit();
                    p.Dispose();
                }
                catch (Exception e )
                {
                    Logger.Log("Exception in ngrok wait for exit", true);
                    Logger.Log(e.ToString(), true);
                }
            });
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Log("OUTPUT: " + e.Data);
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Log("OUTPUT: " + e.Data);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            NgrokInit();
        }

        private void SaveUrl(string publicUrl)
        {          
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"{Settings.ApiHost}/api/EmployeeService/Ngrok?employeeId={Settings.EmployeeId}&Url={publicUrl}");
                var res = client.PostAsync("", null).Result;               
                Logger.Log("url posted");
            }
        }

        private bool IsPortAvailable()
        {
            int port = 4040; //<--- This is your value
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            Logger.Log($"port 4040 available: {isAvailable}");
            return isAvailable;
        }

        private void KillAllNgrokProcess()
        {
            var ngrok = Process.GetProcessesByName("ngrok");
            ngrok.ToList().ForEach(x => x.Kill());
            ngrok = Process.GetProcessesByName("ngrok.exe");
            ngrok.ToList().ForEach(x => x.Kill());
        }       
    }
}
