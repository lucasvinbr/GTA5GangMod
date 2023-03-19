using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// This script controls all the saving and loading procedures called by the other scripts from this mod.
    /// </summary>
    public class PersistenceHandler
    {
        public static T LoadFromFile<T>(string fileName)
        {

            Logger.Log("attempting file load: " + fileName, 2);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";
            if (File.Exists(filePath))
            {
                try
                {
                    using (FileStream readStream = new FileStream(filePath, FileMode.Open))
                    {
                        T loadedData = (T)serializer.Deserialize(readStream);
                        readStream.Close();
                        Logger.Log("loaded " + fileName + "!", 2);
                        return loadedData;
                    }
                }
                catch (Exception e)
                {
                    UI.Notify("an error occurred when trying to load xml file " + fileName + "! error: " + e.ToString());
                    Logger.Log("loading file " + fileName + " failed! error: " + e.ToString(), 1);
                    Logger.WriteDedicatedErrorFile("loading file " + fileName + " failed! error: " + e.ToString());
                    //backup the bad file! It's very sad to lose saved data, even if it's corrupted somehow
                    string bkpFilePath = Application.StartupPath + "/gangModData/" + fileName + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".xml";
                    File.Copy(filePath, bkpFilePath, true);
                    return default;
                }

            }
            else
            {
                Logger.Log("file " + fileName + " doesn't exist; loading a default setup", 2);
                return default;
            }

        }

        public static void SaveToFile<T>(T dataToSave, string fileName, bool notifyMsg = true)
        {
            try
            {
                Logger.Log("attempting file save: " + fileName, 2);
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                if (!Directory.Exists(Application.StartupPath + "/gangModData/"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "/gangModData/");
                    Logger.Log("created directory to save file: " + fileName, 2);
                }

                string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, dataToSave);
                    writer.Close();
                }

                if (notifyMsg)
                {
                    UI.ShowSubtitle("saved at: " + filePath);
                }

                Logger.Log("saved file successfully: " + fileName, 2);
            }
            catch (Exception e)
            {
                UI.Notify("an error occurred while trying to save gang mod data! error: " + e.ToString());
                Logger.Log("failed to save file: " + fileName + "! Error: " + e.ToString(), 1);
                Logger.WriteDedicatedErrorFile("failed to save file: " + fileName + "! Error: " + e.ToString());
            }

        }
    }
}