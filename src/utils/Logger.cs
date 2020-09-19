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
        /// <summary>
        /// logs the message to a file... 
        /// but only if the log level defined in the mod options is greater or equal to the message's level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logLevel">The smaller the level, the more relevant, with 1 being very important failure messages and 5 being max debug spam</param>
        public static void Log(object message, int logLevel)
        {
            if (ModOptions.instance == null) return;
            if (ModOptions.instance.loggerLevel >= logLevel)
            {
                File.AppendAllText("GangAndTurfMod.log", DateTime.Now + " : " + message + Environment.NewLine);
            }

        }

        /// <summary>
        /// overwrites the log file's content with a "cleared log" message
        /// </summary>
        public static void ClearLog()
        {
            File.WriteAllText("GangAndTurfMod.log", DateTime.Now + " : " + "Cleared log! (This happens when the mod is initialized)" + Environment.NewLine);
        }
    }
}
