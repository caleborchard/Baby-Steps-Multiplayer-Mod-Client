using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyStepsMultiplayerClient.Debug
{
    public class BBSMMdBug
    {
        public static bool debugEnabled = false;
        private static string modLogPath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "bbsmm.log");
        public static void ClearFile() { File.Delete(modLogPath); File.Create(modLogPath); }
        public static void Log(string message) 
        { 
            if (debugEnabled)
            {
                MelonLogger.Msg("DEBUG:" + message);
                File.AppendAllLines(modLogPath, new string[] { message });
            }
        }
    }
}
