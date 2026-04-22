using AiAssistant.AI;

namespace AiAssistant
{
    public class DeFine
    {
        public static MainWindow WorkingWin = null;
        public static string Version = "1.0.0.1 Alpha";
        public static void Init(MainWindow Win)
        {
            WorkingWin = Win;
            AICenter.Load();
            AICenter.Init();
        }
        public static string GetFullPath(string Path)
        {
            if (Path.Length > 0)
            {
                if (!Path.Trim().StartsWith(@"\"))
                {
                    Path = @"\" + Path;
                }
            }
            string GetShellPath = System.Windows.Forms.Application.StartupPath;
            if (GetShellPath.EndsWith(@"\"))
            {
                if (Path.StartsWith(@"\"))
                {
                    Path = Path.Substring(1);
                }
            }
            return GetShellPath + Path;
        }
    }
}
