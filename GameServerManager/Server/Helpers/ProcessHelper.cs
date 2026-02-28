using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GameServerManager.Server.Helpers
{
    public static class ProcessHelper
    {
        public static Process? FindProcessByPort(int port)
        {
            var pi = new ProcessStartInfo("ss", $"-ltnup sport = :{port}")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(pi);

            if (proc is null)
                return null;

            while (!proc.StandardOutput.EndOfStream)
            {
                var line = proc.StandardOutput.ReadLine();

                if (line is null)
                    continue;

                var match = Regex.Match(line, @"pid=\d+");
                if (match.Success)
                {
                    return Process.GetProcessById(int.Parse(match.Value.Split('=')[1]));
                }
            }

            return null;
        }

        public static bool ExecuteBashCommand(string command)
        {
            var psi = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);

            return proc is not null;
        }

        public static bool SendToTmux(int serverPid, string command)
        {
            return ExecuteBashCommand($"tmux send-keys -t $(tmux list-panes -a -F \"\"\"#{{pane_id}},#{{pane_pid}}\"\"\" | grep $(ps -o ppid= -p {serverPid}) | cut -d, -f1) \\\"{command}\\\" Enter");
        }

        public static bool StartServerInTmux(string path, string file)
        {
            var psi = new ProcessStartInfo("tmux", $"new-session -d \"exec ./{file}\"")
            {
                CreateNoWindow = true,
                WorkingDirectory = path
            };

            using var proc = Process.Start(psi);

            return proc is not null;
        }
    }
}
