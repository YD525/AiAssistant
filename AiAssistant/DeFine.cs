using AiAssistant.AI;

namespace AiAssistant
{
    public class DeFine
    {
        public static MainWindow WorkingWin = null;
        public static void Init(MainWindow Win)
        {
            WorkingWin = Win;
            AICenter.Load();
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
