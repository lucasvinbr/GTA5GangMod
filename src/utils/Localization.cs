using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// Static class for handling multiple languages
    /// </summary>
    public static class Localization
    {
        public static string CurLanguage
        {
            get
            {
                return CurrentlyUsedFile.LanguageCode;
            }
        }

        public static string LocalesPath { get { return Application.StartupPath + "/gangModData/localization/"; } }

        public static LocalizationFile CurrentlyUsedFile;

        public static string GetTextByKey(string localeKey)
        {
            return CurrentlyUsedFile.GetTextByKey(localeKey);
        }

        public static List<string> GetAvailableLanguageNames()
        {
            List<string> availableLanguages = new List<string>();

            foreach(var file in Directory.EnumerateFiles(LocalesPath))
            {
                if (file.EndsWith(".xml"))
                {
                    var fileNameNoExtension = file.Split('.')[0];
                    try
                    {
                        var fileCulture = CultureInfo.GetCultureInfo(fileNameNoExtension);
                        availableLanguages.Add(fileCulture.DisplayName);
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteDedicatedErrorFile(ex);
                    }
                }
            }

            return availableLanguages;
        }

        public static void Initialize()
        {
            var curLangName = CultureInfo.CurrentCulture.Name;
        }

    }

    /// <summary>
    /// file containing texts for a language
    /// </summary>
    [System.Serializable]
    public class LocalizationFile
    {
        public string LanguageCode;
        public string LanguageName;
        public List<KeyValuePair<string, string>> Locales;

        public string GetTextByKey(string localeKey)
        {
            var entry = Locales.Find(l => l.Key == localeKey);

            // if it's a valid entry that we found, return its value
            if(entry.Key == localeKey)
            {
                return entry.Value;
            }

            return "-LOCALE NOT FOUND-";
        }
    }
}
