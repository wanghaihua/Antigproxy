using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace AntigProxy
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool silentLaunch = false;
            bool forceRestart = false;
            bool printStatus = false;

            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (arg == null) continue;
                    string a = arg.Trim().ToLowerInvariant();
                    if (a == "--launch" || a == "-l")
                    {
                        silentLaunch = true;
                    }
                    else if (a == "--restart" || a == "-r")
                    {
                        forceRestart = true;
                    }
                    else if (a == "--status" || a == "-s")
                    {
                        printStatus = true;
                    }
                }
            }

            if (printStatus)
            {
                AppConfig cfg = ProxyService.LoadConfig();
                ProxyDetectionResult env = ProxyService.DetectEnvironment(cfg.ProxyHost, cfg.ProxyPort);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("ProxyAvailable=" + env.ProxyAvailable);
                sb.AppendLine("DetectedHost=" + env.DetectedHost);
                sb.AppendLine("DetectedPort=" + env.DetectedPort);
                sb.AppendLine("Source=" + env.Source);
                sb.AppendLine("SystemProxyEnabled=" + env.SystemProxyEnabled);
                sb.AppendLine("AntigravityRunning=" + env.AntigravityRunning);
                sb.AppendLine("AntigravityPath=" + cfg.AntigravityPath);
                string statusFile = Path.Combine(Path.GetDirectoryName(ProxyService.ConfigFilePath), "antigproxy.status.txt");
                File.WriteAllText(statusFile, sb.ToString(), new UTF8Encoding(false));
                return 0;
            }

            if (silentLaunch)
            {
                AppConfig cfg = ProxyService.LoadConfig();
                if (forceRestart)
                {
                    cfg.RestartRunningAntigravity = true;
                }
                LaunchResult res = ProxyService.LaunchAntigravity(cfg);
                try { ProxyService.SaveConfig(cfg); } catch { }
                if (!res.Success)
                {
                    MessageBox.Show(res.Message, "AntigProxy 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }
                return 0;
            }

            Application.Run(new MainForm());
            return 0;
        }
    }
}
