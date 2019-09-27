using Newtonsoft.Json;
using ProclickEmployeeLib.Helpers;
using System;
using System.IO;
using System.Net.Http;

namespace ProclickEmployeeLib.Models
{
    public class Settings
    {
        public static string ExecutionDir { get; set; } 
            = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).TrimStart("file:\\".ToCharArray());

        private static int _employeeId;
        [JsonProperty]
        public static int EmployeeId
        {
            get { if (_employeeId > 0) return _employeeId; else { GetUserId(); return EmployeeId; } }
            set { _employeeId = value; }
        }

        [JsonProperty]
        public static string NgrokWebInterface { get; set; } = "http://localhost:4040/api/tunnels";

        [JsonProperty]
        public static string ApiHost { get; set; }

        [JsonProperty]
        public static int HttpListenerPort { get; set; } = 7642;

        [JsonProperty]
        public static int Visibility { get; set; } = 0;

        [JsonProperty]
        public static string Version { get; set; }

        public static void GetUserId()
        {
            Logger.Log("GetUserId()");
            new Login();            
            Login.InitHandler();            
            using (var client = new HttpClient(Login.Handler))
            {
                client.BaseAddress = new Uri($"{ApiHost}/api/EmployeeService/GetUserId");
                EmployeeId = int.Parse(client.GetAsync("").Result.Content.ReadAsStringAsync().Result);                                    
                Save();
            }
        }

        public static void Load()
        {
            try
            {
                JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(Logger.BasePath, "settings.json")));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("LoadSettings() threw a file not found exception, saving and reloading");
                Save();
                Load();
            }
        }

        public static void Save()
        {
            File.WriteAllText(Path.Combine(Logger.BasePath, "settings.json"), JsonConvert.SerializeObject(new Settings()));            
        }
    }
}
