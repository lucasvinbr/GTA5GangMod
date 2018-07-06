using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// This script controls all the saving and loading procedures called by the other scripts from this mod.
    /// </summary>
    public class PersistenceHandler
    {
        public static T LoadFromFile<T>(string fileName)
        {
            try
            {
                Logger.Log("attempting file load: " + fileName);
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";
				if (File.Exists(filePath)) {
					using (FileStream readStream = new FileStream(filePath, FileMode.Open)) {
						T loadedData = (T)serializer.Deserialize(readStream);
						readStream.Close();
						Logger.Log("loaded " + fileName + "!");
						return loadedData;
					}
				}
				else {
					Logger.Log("file " + fileName + " doesn't exist; loading a default setup");
					return default(T);
				}
            }
            catch (Exception e)
            {
                UI.Notify("an error occurred when trying to load xml file " + fileName + "! error: " + e.ToString());
				Logger.Log("loading file " + fileName + " failed! error: " + e.ToString());
				return default(T);
            }
        }

        public static void SaveToFile<T>(T dataToSave, string fileName, bool notifyMsg = true)
        {
            try
            {
                Logger.Log("attempting file save: " + fileName);
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                if (!Directory.Exists(Application.StartupPath + "/gangModData/"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "/gangModData/");
					Logger.Log("created directory to save file: " + fileName);
				}

                string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";

				using (StreamWriter writer = new StreamWriter(filePath)) {
					serializer.Serialize(writer, dataToSave);
					writer.Close();
				}
					
                if (notifyMsg)
                {
                    UI.ShowSubtitle("saved at: " + filePath);
                }

				Logger.Log("saved file successfully: " + fileName);
			}
			catch(Exception e)
            {
                UI.Notify("an error occurred while trying to save gang mod data! error: " + e.ToString());
				Logger.Log("failed to save file: " + fileName + "! Error: " + e.ToString());
			}
            
        }
    }
}