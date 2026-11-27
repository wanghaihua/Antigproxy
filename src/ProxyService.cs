using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace AntigProxy
{
    public static class ProxyService
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        // Common local HTTP/Mixed proxy ports used by Clash, Mihomo, v2rayN, Swell, FlClash, NekoRay, Sing-box, etc.
        private static readonly int[] CommonProxyPorts = new int[]
        {
            7890, 7897, 10809, 10808, 2080, 20171, 1080, 1087, 8080, 8888
        };

        public static string ConfigFilePath
        {
            get
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir))
                {
                    dir = AppDomain.CurrentDomain.BaseDirectory;
                }
                return Path.Combine(dir, "antigproxy.config.json");
            }
        }

        public static AppConfig LoadConfig()
        {
            AppConfig cfg = new AppConfig();
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string raw = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                    AppConfig loaded = Json.Deserialize<AppConfig>(raw);
                    if (loaded != null)
                    {
                        cfg = loaded;
                    }
                }
            }
            catch
            {
                // Ignore corrupt config and fall back to defaults
            }

            if (string.IsNullOrWhiteSpace(cfg.ProxyHost))
            {
                cfg.ProxyHost = "127.0.0.1";
            }
            if (cfg.ProxyPort <= 0 || cfg.ProxyPort > 65535)
            {
                cfg.ProxyPort = 7890;
            }
            if (string.IsNullOrWhiteSpace(cfg.NoProxy))
            {
                cfg.NoProxy = "localhost,127.0.0.1,::1";
            }

            if (string.IsNullOrWhiteSpace(cfg.AntigravityPath) || !File.Exists(cfg.AntigravityPath))
            {
                string found = FindAntigravityPath();
                if (!string.IsNullOrEmpty(found))
                {
                    cfg.AntigravityPath = found;
                }
            }

            return cfg;
        }

        public static void SaveConfig(AppConfig config)
        {
            string raw = FormatJson(Json.Serialize(config));
            File.WriteAllText(ConfigFilePath, raw, new UTF8Encoding(false));
        }

        public static string FindAntigravityPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string[] candidates = new string[]
            {
                Path.Combine(localAppData, @"Programs\Antigravity\Antigravity.exe"),
                Path.Combine(localAppData, @"Programs\antigravity\Antigravity.exe"),
                Path.Combine(programFiles, @"Antigravity\Antigravity.exe"),
                Path.Combine(programFilesX86, @"Antigravity\Antigravity.exe")
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            try
            {
                Process[] procs = Process.GetProcessesByName("Antigravity");
                foreach (Process p in procs)
                {
                    try
                    {
                        string exePath = (p.MainModule != null) ? p.MainModule.FileName : null;
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            return exePath;
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }

            return "";
        }

        public static bool IsPortOpen(string host, int port, int timeoutMs)
        {
            if (port <= 0 || port > 65535) return false;
            string targetHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(targetHost, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(timeoutMs, false);
                    if (!ok || !client.Connected)
                    {
                        return false;
                    }
                    client.EndConnect(ar);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static ProxyDetectionResult DetectEnvironment(string preferredHost, int preferredPort)
        {
            ProxyDetectionResult res = new ProxyDetectionResult();
            res.DetectedHost = string.IsNullOrWhiteSpace(preferredHost) ? "127.0.0.1" : preferredHost.Trim();
            res.DetectedPort = 0;
            res.Source = "未检测到本地代理";

            // 1. Check Antigravity running status
            try
            {
                Process[] procs = Process.GetProcessesByName("Antigravity");
                res.AntigravityProcessCount = procs.Length;
                res.AntigravityRunning = procs.Length > 0;
                foreach (Process p in procs)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(res.DetectedAntigravityPath) && p.MainModule != null && File.Exists(p.MainModule.FileName))
                        {
                            res.DetectedAntigravityPath = p.MainModule.FileName;
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(res.DetectedAntigravityPath))
            {
                res.DetectedAntigravityPath = FindAntigravityPath();
            }

            // 2. Check Windows System Proxy first (universal across Clash, v2rayN, Swell, FlClash, Mihomo, etc.)
            string sysHost = "";
            int sysPort = 0;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (key != null)
                    {
                        object enableObj = key.GetValue("ProxyEnable");
                        int enableVal = 0;
                        if (enableObj != null && int.TryParse(Convert.ToString(enableObj), out enableVal))
                        {
                            res.SystemProxyEnabled = (enableVal == 1);
                        }

                        object serverObj = key.GetValue("ProxyServer");
                        if (serverObj != null)
                        {
                            ParseHostAndPort(Convert.ToString(serverObj), out sysHost, out sysPort);
                        }
                    }
                }
            }
            catch { }

            if (res.SystemProxyEnabled && sysPort > 0 && IsPortOpen(sysHost, sysPort, 200))
            {
                res.ProxyAvailable = true;
                res.DetectedHost = sysHost;
                res.DetectedPort = sysPort;
                res.Source = "系统代理";
                return res;
            }

            // 3. Check user's configured host:port
            if (preferredPort > 0 && IsPortOpen(res.DetectedHost, preferredPort, 200))
            {
                res.ProxyAvailable = true;
                res.DetectedPort = preferredPort;
                res.Source = "本地端口监听中";
                return res;
            }

            // 4. Check inactive system proxy port if it is still listening locally
            if (sysPort > 0 && IsPortOpen(sysHost, sysPort, 200))
            {
                res.ProxyAvailable = true;
                res.DetectedHost = sysHost;
                res.DetectedPort = sysPort;
                res.Source = "本地代理端口";
                return res;
            }

            // 5. Probe common local proxy ports (Clash 7890/7897, v2rayN 10809/10808, Sing-box 2080, etc.)
            foreach (int port in CommonProxyPorts)
            {
                if (port == preferredPort || port == sysPort) continue;
                if (IsPortOpen("127.0.0.1", port, 120))
                {
                    res.ProxyAvailable = true;
                    res.DetectedHost = "127.0.0.1";
                    res.DetectedPort = port;
                    res.Source = "自动扫描端口";
                    return res;
                }
            }

            return res;
        }

        public static int KillRunningAntigravity()
        {
            int killed = 0;
            string[] names = new string[] { "Antigravity", "language_server_windows_x64" };
            foreach (string procName in names)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName(procName);
                    foreach (Process p in procs)
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                            killed++;
                        }
                        catch { }
                        finally { p.Dispose(); }
                    }
                }
                catch { }
            }
            return killed;
        }

        public static LaunchResult LaunchAntigravity(AppConfig config)
        {
            LaunchResult result = new LaunchResult();

            if (config == null)
            {
                result.Success = false;
                result.Message = "配置无效。";
                return result;
            }

            if (string.IsNullOrWhiteSpace(config.AntigravityPath) || !File.Exists(config.AntigravityPath))
            {
                string autoPath = FindAntigravityPath();
                if (!string.IsNullOrEmpty(autoPath))
                {
                    config.AntigravityPath = autoPath;
                }
                else
                {
                    result.Success = false;
                    result.Message = "未找到 Antigravity.exe，请手动选择程序路径。";
                    return result;
                }
            }

            // Auto-detect active local proxy if enabled and current port isn't open
            if (config.AutoDetectProxy && !IsPortOpen(config.ProxyHost, config.ProxyPort, 180))
            {
                ProxyDetectionResult detected = DetectEnvironment(config.ProxyHost, config.ProxyPort);
                if (detected.ProxyAvailable && detected.DetectedPort > 0)
                {
                    config.ProxyHost = detected.DetectedHost;
                    config.ProxyPort = detected.DetectedPort;
                }
            }

            if (config.RestartRunningAntigravity)
            {
                Process[] running = Process.GetProcessesByName("Antigravity");
                bool anyRunning = running.Length > 0;
                foreach (Process p in running) { p.Dispose(); }
                if (anyRunning)
                {
                    KillRunningAntigravity();
                    Thread.Sleep(650);
                }
            }

            string proxyUrl = config.GetProxyUrl();
            string noProxy = string.IsNullOrWhiteSpace(config.NoProxy)
                ? "localhost,127.0.0.1,::1"
                : config.NoProxy.Trim();

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = config.AntigravityPath;
                psi.WorkingDirectory = Path.GetDirectoryName(config.AntigravityPath);
                psi.UseShellExecute = false;

                psi.EnvironmentVariables["HTTP_PROXY"] = proxyUrl;
                psi.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
                psi.EnvironmentVariables["http_proxy"] = proxyUrl;
                psi.EnvironmentVariables["https_proxy"] = proxyUrl;
                psi.EnvironmentVariables["NO_PROXY"] = noProxy;
                psi.EnvironmentVariables["no_proxy"] = noProxy;
                psi.EnvironmentVariables["ELECTRON_NO_ATTACH_CONSOLE"] = "1";

                if (config.SetAllProxyEnv)
                {
                    psi.EnvironmentVariables["ALL_PROXY"] = proxyUrl;
                    psi.EnvironmentVariables["all_proxy"] = proxyUrl;
                }
                else
                {
                    if (psi.EnvironmentVariables.ContainsKey("ALL_PROXY"))
                        psi.EnvironmentVariables.Remove("ALL_PROXY");
                    if (psi.EnvironmentVariables.ContainsKey("all_proxy"))
                        psi.EnvironmentVariables.Remove("all_proxy");
                }

                if (config.PassChromiumProxyArgs)
                {
                    string bypassList = noProxy.Replace(",", ";");
                    psi.Arguments = string.Format("--proxy-server=\"{0}\" --proxy-bypass-list=\"{1}\"", proxyUrl, bypassList);
                }

                Process proc = Process.Start(psi);
                result.Success = true;
                result.ProcessId = (proc != null) ? proc.Id : 0;
                result.ProxyUrlUsed = proxyUrl;
                result.Message = string.Format("已免 TUN 启动 Antigravity（代理 {0}）", proxyUrl);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "启动失败：" + ex.Message;
                return result;
            }
        }

        public static async Task<ProxyTestResult> TestProxyConnectivityAsync(string host, int port)
        {
            return await Task.Run(delegate
            {
                ProxyTestResult res = new ProxyTestResult();
                string targetHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
                int targetPort = (port > 0 && port <= 65535) ? port : 7890;

                if (!IsPortOpen(targetHost, targetPort, 1800))
                {
                    res.TcpReachable = false;
                    res.Message = string.Format("端口 {0}:{1} 未监听，请确认代理客户端已启动", targetHost, targetPort);
                    return res;
                }

                res.TcpReachable = true;
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    string proxyUrl = string.Format("http://{0}:{1}", targetHost, targetPort);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.google.com/generate_204");
                    req.Method = "GET";
                    req.Proxy = new WebProxy(proxyUrl);
                    req.Timeout = 7000;
                    req.ReadWriteTimeout = 7000;
                    req.UserAgent = "AntigProxy/1.1";

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        sw.Stop();
                        res.StatusCode = (int)resp.StatusCode;
                        res.LatencyMs = sw.ElapsedMilliseconds;
                        res.HttpSuccess = (res.StatusCode == 204 || res.StatusCode == 200);
                        res.Message = string.Format("连通正常 · Google HTTP {0} · {1} ms", res.StatusCode, res.LatencyMs);
                    }
                }
                catch (WebException wex)
                {
                    sw.Stop();
                    res.LatencyMs = sw.ElapsedMilliseconds;
                    res.HttpSuccess = false;
                    HttpWebResponse errResp = wex.Response as HttpWebResponse;
                    if (errResp != null)
                    {
                        res.StatusCode = (int)errResp.StatusCode;
                        res.Message = string.Format("代理已连，但目标返回 HTTP {0} ({1} ms)", res.StatusCode, res.LatencyMs);
                    }
                    else
                    {
                        res.Message = "本地端口已开，但访问 Google 超时或节点异常";
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    res.HttpSuccess = false;
                    res.Message = "测试异常：" + ex.Message;
                }

                return res;
            });
        }

        public static bool CreateDesktopShortcut(out string message)
        {
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopDir, "Antigravity (免TUN代理).lnk");
                string launcherExe = Process.GetCurrentProcess().MainModule.FileName;

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    message = "系统缺少 WScript.Shell 组件，无法创建快捷方式";
                    return false;
                }

                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { shortcutPath }
                );

                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { launcherExe });
                shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { "--launch" });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(launcherExe) });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "免 TUN 注入本地代理并启动 Antigravity" });
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { launcherExe + ",0" });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);

                message = "已创建桌面快捷方式「Antigravity (免TUN代理)」，双击可静默直启";
                return true;
            }
            catch (Exception ex)
            {
                message = "创建快捷方式失败：" + ex.Message;
                return false;
            }
        }

        public static bool InstallPowerShellAgyCommand(out string message)
        {
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] profilePaths = new string[]
                {
                    Path.Combine(docs, @"PowerShell\Microsoft.PowerShell_profile.ps1"),
                    Path.Combine(docs, @"WindowsPowerShell\Microsoft.PowerShell_profile.ps1")
                };

                string launcherExe = Process.GetCurrentProcess().MainModule.FileName;
                string startMarker = "# >>> AntigProxy agy command >>>";
                string endMarker = "# <<< AntigProxy agy command <<<";

                StringBuilder block = new StringBuilder();
                block.AppendLine(startMarker);
                block.AppendLine("function agy {");
                block.AppendLine(string.Format("    Start-Process -FilePath '{0}' -ArgumentList '--launch'", launcherExe.Replace("'", "''")));
                block.AppendLine("}");
                block.AppendLine(endMarker);

                foreach (string profilePath in profilePaths)
                {
                    string dir = Path.GetDirectoryName(profilePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string existing = File.Exists(profilePath) ? File.ReadAllText(profilePath, Encoding.UTF8) : "";
                    string pattern = Regex.Escape(startMarker) + @"[\s\S]*?" + Regex.Escape(endMarker);
                    string updatedContent;
                    if (Regex.IsMatch(existing, pattern))
                    {
                        updatedContent = Regex.Replace(existing, pattern, block.ToString().TrimEnd());
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(existing) && !existing.EndsWith("\n"))
                        {
                            existing += Environment.NewLine;
                        }
                        updatedContent = existing + Environment.NewLine + block.ToString();
                    }

                    File.WriteAllText(profilePath, updatedContent, new UTF8Encoding(false));
                }

                message = "已写入终端 agy 命令，新开 PowerShell 输入 agy 即可免 TUN 启动";
                return true;
            }
            catch (Exception ex)
            {
                message = "写入终端命令失败：" + ex.Message;
                return false;
            }
        }

        private static void ParseHostAndPort(string proxyServer, out string host, out int port)
        {
            host = "127.0.0.1";
            port = 0;
            if (string.IsNullOrWhiteSpace(proxyServer)) return;

            // Handle "127.0.0.1:7890" or "http=127.0.0.1:7890;https=127.0.0.1:7890"
            string candidate = proxyServer.Trim();
            if (candidate.Contains("="))
            {
                string[] parts = candidate.Split(';');
                foreach (string part in parts)
                {
                    string p = part.Trim();
                    if (p.StartsWith("http=", StringComparison.OrdinalIgnoreCase))
                    {
                        candidate = p.Substring(5).Trim();
                        break;
                    }
                }
                if (candidate.Contains("="))
                {
                    int idx = candidate.IndexOf('=');
                    candidate = candidate.Substring(idx + 1).Split(';')[0].Trim();
                }
            }

            candidate = candidate.Replace("http://", "").Replace("https://", "");
            Match m = Regex.Match(candidate, @"^([^:\s]+):(\d{2,5})$");
            if (m.Success)
            {
                string h = m.Groups[1].Value.Trim();
                int p;
                if (int.TryParse(m.Groups[2].Value, out p) && p > 0 && p <= 65535)
                {
                    host = (h == "0.0.0.0") ? "127.0.0.1" : h;
                    port = p;
                }
            }
        }

        private static string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "{}";
            StringBuilder sb = new StringBuilder();
            bool inQuotes = false;
            int indent = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    sb.Append(ch);
                    continue;
                }

                if (!inQuotes)
                {
                    if (ch == '{' || ch == '[')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        sb.AppendLine();
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        sb.Append(ch);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
                    else if (ch == ':')
                    {
                        sb.Append(": ");
                    }
                    else if (!char.IsWhiteSpace(ch))
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
