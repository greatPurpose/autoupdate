using ProclickEmployeeLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ProclickEmployeeLib.Helpers
{
    class Login
    {
        public string Path { get; set; } = "/Account/Login";
        public string Username { get; set; }
        public string Password { get; set; }
        public static HttpClientHandler Handler { get; set; }
        public static IEnumerable<Cookie> Cookies { get; set; }
        public bool LoggedIn { get; set; } = false;
        private static bool AskingForCredentials { get; set; }

        public Login()
        {
            bool firstAttempt = true;

            while (AskingForCredentials)
            {
                //just run untill this is cleared up - dont ask twice for the creds, its silly
            }

            using(var form = new CredentialsForm())
            {
                int i = 0;
                while (!IsAuthenticated())
                {
                    AskingForCredentials = true;
                    i++;
                    Logger.Log($"getting credentials. attempt # {i}");

                    if (!firstAttempt && form.Username?.Length > 0)
                    {
                        form.SetText("Invalid Login Attempt, Please try again");
                        Logger.Log($"invalid login attempt # {i}");
                    }

                    form.ShowDialog();

                    firstAttempt = false;

                    Username = form.Username;
                    Password = form.Password;

                    Cookies = null;
                    var cookies = InitHandler();

                    using (HttpClient authClient = new HttpClient(Handler))
                    {
                        var uri = new Uri($"{Settings.ApiHost}{Path}");
                        authClient.BaseAddress = uri;
                        authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("test/html"));

                        var values = new Dictionary<string, string>
                        {
                            { "Email", Username },
                            { "Password", Password }
                        };

                        var content = new FormUrlEncodedContent(values);
                        HttpResponseMessage authenticationResponse = authClient.PostAsync("", content).Result;
                        Cookies = cookies.GetCookies(uri).Cast<Cookie>();               

                    }
                }
                AskingForCredentials = false;
            }
        }

        private bool IsAuthenticated()
        {
            if (Handler is null)
                return false;

            InitHandler();

            using (var authcheck = new HttpClient(Handler))
            {
                authcheck.BaseAddress = new Uri($"{Settings.ApiHost}/api/main/checkAuth");

                if (authcheck.PostAsync("", null).Result.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Log("not authenticated");
                    return false;
                }
                else
                {
                    Logger.Log("Logged authenticated");
                    LoggedIn = true;
                    return true;
                }
            }
        }

        public static CookieContainer InitHandler()
        {
            CookieContainer cookies = new CookieContainer();

            if(Cookies != null)
            {
                foreach (var cookie in Cookies)
                    cookies.Add(cookie);
            }

            Handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };

            return cookies;
        }

        public static bool IsClocking()
        {
            var res = HttpHelper.GetRequest($"{Settings.ApiHost}/api/EmployeeService/IsClocking?employeeId={Settings.EmployeeId}");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(res);
        }

    }
}
