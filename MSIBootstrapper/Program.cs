using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MSIBootstrapper
{
    class Program
    {
        private static string Product_Key = "{78C42134-B9D9-4641-BDEE-ED1B1DEF78D0}";
        static void Main(string[] args)
        {
            Logger.Log("MSIBootstrapper running");

            var cmd = String.Format("/passive /x {0}", Product_Key);
            Logger.Log("cmd = msiexec " + cmd);

            Process p = Process.Start("msiexec", cmd);
            Console.WriteLine(cmd);
            if (p.WaitForExit(60 * 1000) == false)
            {
                MessageBox.Show("Installing update failed: timeout.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                Logger.Log("the processes is uninstalled.");

                cmd = String.Format("/passive /i \"{0}\" REINSTALLMODE=vomus", args[0]);
                Logger.Log("cmd = msiexec " + cmd);
                Console.WriteLine(cmd);
                Process p1 = Process.Start("msiexec", cmd);

                if (p1.WaitForExit(60 * 1000) == false)
                {
                    MessageBox.Show("Installing update failed: timeout.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else
                {
                    Logger.Log("the processes command exited ");
                    if (p.ExitCode != 0)
                    {
                        Logger.Log("exit code " + p.ExitCode);
                        Logger.Log("this is no good");

                        var errorMessage = p.ExitCode.ToString();
                        if (p.ExitCode == 1603)
                            errorMessage += ". This is commonly caused by not having admin rights, either run the application \"As administrator\" or login to an admin account";

                        MessageBox.Show(
                            "Installing update failed: error code " + errorMessage, "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        Logger.Log("stating " + args[1]);
                        Process p2 = Process.Start(args[1]);
                    }
                }
            }
        }
    }
}
