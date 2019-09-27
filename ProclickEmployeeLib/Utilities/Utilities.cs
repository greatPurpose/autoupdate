using FluentFTP;
using Newtonsoft.Json;
using ProclickEmployeeLib.AutoUpdater;
using ProclickEmployeeLib.Helpers;
using ProclickEmployeeLib.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Timers;

namespace ProclickEmployeeLib.Utilities
{
    public class Utilities
    {

        private static Timer _breakRecorderTimer;
        private static Timer _checkFilesInRecordingsDirTimer;


        /// <summary>
        /// Merges audio with video using ffmpeg
        /// </summary>
        /// <param name="audioPath"></param>
        /// <param name="videoPath"></param>
        /// <param name="outputPath"></param>
        public static void CombineAudioVideo(string audioPath, string videoPath, string outputPath)
        {
            string cmd = $"cd \"{Settings.ExecutionDir}\" & ffmpeg.exe -y -i \"{audioPath}\" -i \"{videoPath}\" -acodec copy -vcodec copy \"{outputPath}\"";
            Logger.Log(cmd);
            using (var p = new Process())
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = $"/K " + cmd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                p.StartInfo = psi;
                p.Start();

                int i = 0;
                while (!File.Exists(outputPath) && i < 200)
                {
                    i++;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// start the screen recorder
        /// </summary>
        public static void StartScreenRecorder()
        {
            Directory.CreateDirectory(RecorderModel.RecordingsPath);
            try
            {
                Logger.Log("starting video recorder");
                VideoRecorderHelper.StartRecording(RecorderModel.VideoFilename);
                Logger.Log("video recorder running");

                var timerInterval = 1 * 60 * 1000;
                _breakRecorderTimer = new Timer
                {
                    Interval = timerInterval
                };
                _breakRecorderTimer.Elapsed += BreakRecorderTimer_Elapsed;
                _breakRecorderTimer.Start();

                Logger.Log($"recording breaker timer started on {timerInterval} interval");
            }
            catch (Exception e)
            {
                Logger.Log("Error starting video recorder - " + e.Message, true);
            }
            try
            {
                AudioRecorderHelper.StartRecording(RecorderModel.AudioFilename);
            }
            catch (Exception e)
            {
                Logger.Log("Error starting audio recorder - " + e.Message, true);
            }
        }

        private static void BreakRecorderTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Log("Timer elapsed, breaking the recording and uploading");
            var file = StopScreenRecorder();
            System.Threading.Tasks.Task.Run(() =>
            {
                file = ConvertToMp4(file);
                Logger.Log($"file reset to {file} - uploading");
                UploadFileToServer(file);
            });

            Logger.Log("reseting filename, was " + RecorderModel.Filename);
            RecorderModel.ResetFilename();
            Logger.Log("filename reset to " + RecorderModel.Filename);

            StartScreenRecorder();
        }



        /// <summary>
        /// stops running screen recorder
        /// </summary>
        /// <returns>video file path </returns>
        public static string StopScreenRecorder()
        {
            Logger.Log("stopping video recorder");

            _breakRecorderTimer.Stop();
            _breakRecorderTimer.Dispose();

            var videoPath = VideoRecorderHelper.StopRecording();

            Logger.Log("video recorder stopped");

            try
            {
                Logger.Log("stopping audio recorder");
                var audioPath = AudioRecorderHelper.StopRecording();
                Logger.Log("audio recorder stopped");
                var finalPath = $"{RecorderModel.Filename}-final.avi";
                Logger.Log("combining...");
                CombineAudioVideo(audioPath, videoPath, finalPath);
                Logger.Log($"audio and video combined in {finalPath}");

                if (File.Exists(finalPath))
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Logger.Log("deleting video and audio");
                        while (IsFileLocked(audioPath))
                        {
                            System.Threading.Thread.Sleep(500);
                        }
                        File.Delete(audioPath);

                        while (IsFileLocked(videoPath))
                        {
                            System.Threading.Thread.Sleep(500);
                        }
                        File.Delete(videoPath);
                        Logger.Log("deleted");
                    });

                    return finalPath;
                }
                else
                {
                    throw new Exception("final combined video / audio does not exist");
                }

            }
            catch (Exception e)
            {
                Logger.Log("Exception: " + e.Message, true);
                Logger.Log("return video only", true);
                return videoPath;
            }
        }

        public static string ConvertToMp4(string aviPath)
        {
            string mp4Name = aviPath.Replace(".avi", ".mp4");
            using (var p = new Process())
            {
                string cmd = $"cd \"{Settings.ExecutionDir}\" & ffmpeg.exe -y -i \"{aviPath}\" -max_muxing_queue_size 9999 \"{mp4Name}\"";

                Logger.Log(cmd);
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "CMD.EXE",
                    Arguments = $"/K {cmd}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = Settings.Visibility == 0
                };
                p.StartInfo = psi;
                p.Start();
                int i = 0;

                while ((!File.Exists(mp4Name) || IsFileLocked(mp4Name)) && i < 200)
                {
                    i++;
                    System.Threading.Thread.Sleep(500);
                }

                if (File.Exists(mp4Name))
                {
                    Logger.Log("converted to mp4, closing converter processes");
                    p.Close();
                    Logger.Log("disposing converter process");
                    p.Dispose();
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            Logger.Log("deleting avi on seperate thread...");
                            File.Delete(aviPath);
                            Logger.Log("avi file deleted");
                        }
                        catch (Exception e)
                        {
                            Logger.Log("deleting avi file failed with " + e.Message);
                        }
                    });
                    Logger.Log("returning mp4");
                    return mp4Name;
                }

                Logger.Log("could not convert to MP4, return avi", true);

                return aviPath;
            }
        }

        public static string GetMAC()
        {
            return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();
        }

        public static void UploadFileToServer(string filePath)
        {
            //try
            //{
            //    Logger.Log("uploading file");
            //    string requestURL = $"{Settings.ApiHost}/api/EmployeeService/upload?employeeId={Settings.EmployeeId}";
            //    //string fileName = "";  
            //    using (WebClient wc = new WebClient())
            //    {
            //        byte[] bytes = wc.DownloadData(filePath); // You need to do this download if your file is on any other server otherwise you can convert that file directly to bytes  
            //        Dictionary<string, object> postParameters = new Dictionary<string, object>
            //        {
            //            {
            //                filePath,
            //                new FormUpload.FileParameter(bytes,    
            //                    Path.GetFileName(filePath), 
            //                    MimeMapping.GetMimeMapping(filePath))
            //            }
            //        };
            //        string userAgent = "employeeService";
            //        HttpWebResponse webResponse = FormUpload.MultipartFormPost(requestURL, userAgent, postParameters, "", "");
            //        // Process response  
            //        using(StreamReader responseReader = new StreamReader(webResponse.GetResponseStream()))
            //        {
            //            var returnResponseText = responseReader.ReadToEnd();
            //            webResponse.Close();
            //            Logger.Log("upload complete - deleting file ");
            //            File.Delete(filePath);
            //            Logger.Log("file deleted");
            //            return returnResponseText; 
            //        }
            //    }

            //}
            //catch (Exception exp)
            //{
            //    Logger.Log("exception in uploading file: " + exp.ToString());
            //    return exp.Message;
            //}

            try
            {
                Logger.Log($"Uploading file {filePath}...");
                FtpUpload(filePath);
                Logger.Log($"{filePath} succesfully uploaded. saving filename via api");
                HttpHelper.PostRequest($"{Settings.ApiHost}/api/EmployeeService/SaveRecording?filename={Path.GetFileName(filePath)}&employeeId={Settings.EmployeeId}");
                Logger.Log("deleting file");
                while (IsFileLocked(filePath))
                {
                    System.Threading.Thread.Sleep(500);
                }
                File.Delete(filePath);
                Logger.Log($"file {filePath} deleted");
            }
            catch (Exception e)
            {
                Logger.Log($"Error uploading {filePath}. " + e.ToString(), true);
            }
        }

        public static void FtpUpload(string file)
        {
            var creds = JsonConvert.DeserializeObject<FtpCredentials>(HttpHelper.GetRequest($"{Settings.ApiHost}/api/EmployeeService/ftp"));

            // create an FTP client
            using (FtpClient client = new FtpClient(creds.Host))
            {
                client.Credentials = new NetworkCredential(creds.Username, creds.Password);
                client.Port = 21;
                client.Connect();
                client.UploadFile(file, Path.GetFileName(file), FtpExists.Overwrite, true, FtpVerify.None);
                client.Disconnect();
            }
        }

        public static void UploadFilesTimer()
        {
            Logger.Log("Starting timer _checkFilesInRecordingsDirTimer");
            _checkFilesInRecordingsDirTimer = new Timer()
            {
                Interval = 60 * 60 * 1000
            };
            _checkFilesInRecordingsDirTimer.Elapsed += CheckFilesInRecordingsDirTimer_Elapsed;
            _checkFilesInRecordingsDirTimer.Start();
            Logger.Log("_checkFilesInRecordingsDirTimer started");
        }

        private static void CheckFilesInRecordingsDirTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Log("_checkFilesInRecordingsDirTimer elapsed");
            if (Directory.Exists(RecorderModel.RecordingsPath))
            {
                var files = Directory.GetFiles(RecorderModel.RecordingsPath);
                Logger.Log($"{files.Length} files in recordings dir");
                foreach (var item in files)
                {
                    try
                    {
                        if (!IsFileLocked(item))
                        {
                            if (item.ToLower().EndsWith("mp4"))
                                UploadFileToServer(item);
                            else
                            {
                                Logger.Log($"deleting file {item}");
                                File.Delete(item);
                            }
                        }
                        else
                        {
                            Logger.Log($"file {item} is locked");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"exception with CheckFilesInRecordingsDirTimer on file {item}, {ex.Message} ", true);
                    }
                }
            }
            else
            {
                Logger.Log("recordings path does not exist");
            }
        }

        public static void AutoUpdate()
        {
            while (true)
            {
                Logger.Log("AutoUpdate()");
                //wait, give me time to update the file....
                System.Threading.Thread.Sleep(10000);
                try
                {
                    AutoUpdaterDialog updater = new AutoUpdaterDialog(
                        "http://cms.alexanderfried.com/installer/",
                        "http://cms.alexanderfried.com/api/employeeService/CurrentVersion",
                        "appsetup.msi",
                        "ProclickApp",
                    VersionNumber.Parse(Settings.Version),
                    true);

                    if (updater.OnCheckUpload())
                    {
                        updater.ShowDialog();

                        if (updater.DoInstall)
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Exception in AutoUpdate()! " + e.Message);
                    Logger.Log(e.ToString());
                }

                System.Threading.Thread.Sleep(10 * 60 * 1000);//every 10min
            }
        }

        public static bool IsFileLocked(string filePath)
        {
            var file = new FileInfo(filePath);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
