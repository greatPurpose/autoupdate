﻿
namespace ProclickEmployeeLib.Models
{ 
    public class NgrokModel
    {
        public Tunnel[] tunnels { get; set; }
        public string uri { get; set; }
    }

    public class Tunnel
    {
        public string name { get; set; }
        public string uri { get; set; }
        public string public_url { get; set; }
        public string proto { get; set; }
        public Config config { get; set; }
        public Metrics metrics { get; set; }
    }

    public class Config
    {
        public string addr { get; set; }
        public bool inspect { get; set; }
    }

    public class Metrics
    {
        public Conns conns { get; set; }
        public Http http { get; set; }
    }

    public class Conns
    {
        public int count { get; set; }
        public int gauge { get; set; }
        public int rate1 { get; set; }
        public int rate5 { get; set; }
        public int rate15 { get; set; }
        public int p50 { get; set; }
        public int p90 { get; set; }
        public int p95 { get; set; }
        public int p99 { get; set; }
    }

    public class Http
    {
        public int count { get; set; }
        public int rate1 { get; set; }
        public int rate5 { get; set; }
        public int rate15 { get; set; }
        public int p50 { get; set; }
        public int p90 { get; set; }
        public int p95 { get; set; }
        public int p99 { get; set; }
    }

}
