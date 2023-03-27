using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
                return CurrentlyUsedFile?.LanguageCode;
            }
        }

        public const string LOCALE_NOT_FOUND_TXT = "-MISSINGLOCALE-";

        public static string LocalesPath { get { return Application.StartupPath + "/gangModData/localization/"; } }

        /// <summary>
        /// the path to the locales folder, starting from the gangModData folder
        /// </summary>
        public const string LOCALES_SUBPATH = "localization/";

        public static LocalizationFile CurrentlyUsedFile;

        public static List<CultureInfo> AvailableLanguageCultures;

        /// <summary>
        /// sets up a starting locale file and prepares the available locales list.
        /// Should be run before anything else locale-related
        /// </summary>
        public static void Initialize()
        {
            var curCulture = CultureInfo.CurrentCulture;

            if (!string.IsNullOrEmpty(ModOptions.instance.preferredLanguage))
            {
                try
                {
                    curCulture = CultureInfo.GetCultureInfo(ModOptions.instance.preferredLanguage);
                }
                catch (Exception ex)
                {
                    Logger.WriteDedicatedErrorFile(ex);
                    ModOptions.instance.preferredLanguage = null;
                }
            }

            //fetch all available languages
            FetchAndStoreAvailableLanguages();

            // check if the local culture exists as one of the lang options;
            // if it does, use it. If not, use a default (en-US?)
            if (AvailableLanguageCultures.Contains(curCulture))
            {
                SetCurrentLangCulture(curCulture);
            }
            else
            {
                SetCurrentLangCulture(CultureInfo.GetCultureInfo("en-US"));
            }

            Logger.Log(CurrentlyUsedFile?.DebugDumpLocaleData(), 5);
            Logger.Log(GetTextByKey("test_locale", "locale test: does not work"), 1);
        }

        public static void FetchAndStoreAvailableLanguages()
        {
            AvailableLanguageCultures = new List<CultureInfo>();
            if (Directory.Exists(LocalesPath))
            {
                foreach (var filePath in Directory.EnumerateFiles(LocalesPath))
                {
                    if (filePath.EndsWith(".xml"))
                    {
                        var fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);
                        try
                        {
                            var fileCulture = CultureInfo.GetCultureInfo(fileNameNoExtension);
                            AvailableLanguageCultures.Add(fileCulture);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteDedicatedErrorFile(ex);
                        }
                    }
                }
            }
        }

        public static string GetTextByKey(string localeKey, string fallbackText = LOCALE_NOT_FOUND_TXT)
        {
            if(CurrentlyUsedFile == null)
            {
                return fallbackText;
            }

            return CurrentlyUsedFile.GetTextByKey(localeKey, fallbackText);
        }

        /// <summary>
        /// returns true on success
        /// </summary>
        /// <param name="targetCulture"></param>
        /// <returns></returns>
        public static bool SetCurrentLangCulture(CultureInfo targetCulture)
        {
            string fileSubPath = LOCALES_SUBPATH + targetCulture.Name;

            var fileData = PersistenceHandler.LoadFromFile<LocalizationFile>(fileSubPath);

            if(fileData != null)
            {
                CurrentlyUsedFile = fileData;
                return true;
            }
            else
            {
                Logger.WriteDedicatedErrorFile($"could not load locale culture file: {targetCulture.Name}");
                return false;
            }
        }

    }

    /// <summary>
    /// file containing texts for a language
    /// </summary>
    [System.Serializable]
    public class LocalizationFile
    {
        public string LanguageCode;
        public List<LocaleKey> Locales;

        public string GetTextByKey(string localeKey, string fallbackText = Localization.LOCALE_NOT_FOUND_TXT)
        {
            var entry = Locales.Find(l => l.Key == localeKey);

            // if it's a valid entry that we found, return its value
            if(entry.Key == localeKey)
            {
                return entry.Value;
            }

            Logger.Log($"locale file {LanguageCode}: key {localeKey} not found", 3);
            return fallbackText;
        }

        /// <summary>
        /// returns a (possibly very big) string containing all locale keys and values
        /// </summary>
        /// <returns></returns>
        public string DebugDumpLocaleData()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Locale file {LanguageCode}: dump locales start");

            foreach(var kvp in Locales)
            {
                sb.AppendLine($"key: {kvp.Key} - Value: {kvp.Value}");
            }

            sb.AppendLine($"Locale file {LanguageCode}: dump locales end");

            return sb.ToString();
        }

        public LocalizationFile() { }
    }

    /// <summary>
    /// a simple package for locale entries, containing a string key and a string value
    /// </summary>
    [Serializable]
    public struct LocaleKey
    {
        public string Key, Value;
    }
}
