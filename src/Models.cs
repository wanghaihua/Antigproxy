using System;

namespace AntigProxy
{
    public class AppConfig
    {
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string NoProxy { get; set; }
        public string AntigravityPath { get; set; }
        public bool AutoDetectProxy { get; set; }
        public bool PassChromiumProxyArgs { get; set; }
        public bool SetAllProxyEnv { get; set; }
        public bool RestartRunningAntigravity { get; set; }
        public bool CloseLauncherAfterLaunch { get; set; }

        public AppConfig()
        {
            ProxyHost = "127.0.0.1";
            ProxyPort = 7890;
            NoProxy = "localhost,127.0.0.1,::1";
            AntigravityPath = "";
            AutoDetectProxy = true;
            PassChromiumProxyArgs = true;
            SetAllProxyEnv = false;
            RestartRunningAntigravity = false;
            CloseLauncherAfterLaunch = false;
        }

        public string GetProxyUrl()
        {
            string host = string.IsNullOrWhiteSpace(ProxyHost) ? "127.0.0.1" : ProxyHost.Trim();
            int port = (ProxyPort > 0 && ProxyPort <= 65535) ? ProxyPort : 7890;
            return string.Format("http://{0}:{1}", host, port);
        }
    }

    public class ProxyDetectionResult
    {
        public bool ProxyAvailable { get; set; }
        public string DetectedHost { get; set; }
        public int DetectedPort { get; set; }
        public string Source { get; set; }
        public bool SystemProxyEnabled { get; set; }
        public bool AntigravityRunning { get; set; }
        public int AntigravityProcessCount { get; set; }
        public string DetectedAntigravityPath { get; set; }
    }

    public class ProxyTestResult
    {
        public bool TcpReachable { get; set; }
        public bool HttpSuccess { get; set; }
        public int StatusCode { get; set; }
        public long LatencyMs { get; set; }
        public string Message { get; set; }
    }

    public class LaunchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ProcessId { get; set; }
        public string ProxyUrlUsed { get; set; }
    }
}
