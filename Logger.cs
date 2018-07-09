using System;
using System.IO;

//this one comes from https://github.com/crosire/scripthookvdotnet/wiki/Code-Snippets#logger-helper
//Thanks a lot!
namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// Static logger class that allows direct logging of anything to a text file
    /// </summary>
    public static class Logger
    {
        public static void Log(object message)
        {
			if (ModOptions.instance == null) return;
			if (ModOptions.instance.loggerEnabled) {
				File.AppendAllText("GangAndTurfMod-" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", DateTime.Now + " : " + message + Environment.NewLine);
			}
            
        }
    }
}
