using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using ProclickEmployeeLib.Models;

namespace ProclickEmployeeLib.Helpers
{
    public class HttpListenerHelper
    {
        public static void CreateListener(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                Logger.Log("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // Create a listener.
            using (HttpListener listener = new HttpListener())
            {
                // Add the prefixes.
                foreach (string s in prefixes)
                {
                    listener.Prefixes.Add(s);
                }
                listener.Start();
                Logger.Log($"Listening on {prefixes[0]}...");

                while (listener.IsListening)
                {
                    // Note: The GetContext method blocks while waiting for a request. 
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;

                    if (request.HttpMethod == "OPTIONS")
                    {
                        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                        response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                        response.AddHeader("Access-Control-Max-Age", "1728000");
                    }
                    response.AppendHeader("Access-Control-Allow-Origin", "*");

                    var requestText = new StreamReader(context.Request.InputStream,
                       context.Request.ContentEncoding).ReadToEnd();

                    var cleanedRequestText = System.Web.HttpUtility.UrlDecode(requestText);

                    Logger.Log("Request = " + cleanedRequestText);

                    var requestBody = System.Web.HttpUtility.ParseQueryString(requestText);

                    string command = requestBody["command"] ?? "";
                
                    string responseString = "";

                    try
                    {
                        switch (command.ToLower())
                        {
                            case "screenshot":
                                try
                                {                                    
                                    var file = RecorderModel.RecordingsPath + "\\scsht" + new Random().Next() + ".jpeg";
                                    Logger.Log("taking screenshot to " + file);
                                    new ScreenshotHelper().CaptureMyScreen(file);
                                    Logger.Log("screenshot captured. uploading...");
                                    Utilities.Utilities.UploadFileToServer(file);                                    
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in screenshot, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            case "recordingstart":
                                try
                                {
                                    Utilities.Utilities.StartScreenRecorder();
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in recordingstart, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            case "recordingstop":
                                try
                                {
                                    string recordedFilePath = Utilities.Utilities.StopScreenRecorder();
                                    recordedFilePath = Utilities.Utilities.ConvertToMp4(recordedFilePath);
                                    Utilities.Utilities.UploadFileToServer(recordedFilePath);
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in recordingstop, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            case "ping":
                                break;
                            case "login":
                                try
                                {
                                    var login = new Login();
                                    Settings.GetUserId();
                                    if (login.LoggedIn)
                                    {
                                        foreach (var item in Login.Cookies)
                                        {
                                            item.Expires = DateTime.Now.AddYears(1);
                                        }

                                        //send the mac address to the server - the server will throw an error if it should be blocked
                                        var url = $"{Settings.ApiHost}/api/employeeService/verifyMacAddress?address={Utilities.Utilities.GetMAC()}&employeeId={Settings.EmployeeId}";
                                        try
                                        {
                                            HttpHelper.GetRequest(url, false);
                                            Logger.Log("MAC address verified");
                                            responseString = JsonConvert.SerializeObject(Login.Cookies);
                                        }
                                        catch
                                        {
                                            Logger.Log("mac address unverified! not returning the cookies");
                                            responseString = "unverified mac address";
                                        }
                                    }                            
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in login, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            case "getprocesses":
                                try
                                {
                                    var processes = ProcessesHelper.GetAllProccesses();
                                    Logger.Log("retrieving proceeses...");
                                    var res = new Models.ProcessesModel
                                    {
                                        Background = processes.Where(x => string.IsNullOrEmpty(x.MainWindowTitle)).Select(x => x.ProcessName),
                                        Windows = processes.Where(x => !string.IsNullOrEmpty(x.MainWindowTitle)).Select(x => x.MainWindowTitle)
                                    };
                                    Logger.Log("returning processes");
                                    responseString = JsonConvert.SerializeObject(res);
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in getprocesses, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            case "getlog":
                                try
                                {
                                    Logger.Log("building path");
                                    string path = requestBody["path"] ?? "";
                                    var basePath = Logger.BasePath;
                                    var filePath = "";
                                    if (path != "")
                                        filePath = Path.Combine(basePath, path);
                                    else
                                        filePath = Path.Combine(basePath, Logger.RelativePath);
                                    Logger.Log("path = " + filePath);
                                    var newFileName = Path.Combine(Path.GetDirectoryName(filePath),$"{new Random().Next()}-epl-{Settings.EmployeeId}-log-{Path.GetFileName(filePath)}");
                                    File.Move(filePath, newFileName);
                                    Utilities.Utilities.UploadFileToServer(newFileName);
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("exception in getlog, " + e.ToString(), true);
                                    responseString = e.Message;
                                }
                                break;
                            default:
                                responseString = "connection works!";
                                break;                                
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), true);
                        responseString = e.Message;
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    // You must close the output stream.
                    output.Close();
                }
            }
        }
    }
}
