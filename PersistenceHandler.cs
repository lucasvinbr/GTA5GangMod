using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;


namespace GTA
{
    /// <summary>
    /// This script controls all the saving and loading procedures called by the other scripts from this mod.
    /// </summary>
    public class PersistenceHandler
    {
        public static T LoadFromFile<T>(string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";
            if (File.Exists(filePath))
            {
                FileStream readStream = new FileStream(filePath, FileMode.Open);
                T loadedData = (T)serializer.Deserialize(readStream);
                readStream.Close();
                return loadedData;
            }
            else return default(T);
        }

        public static void SaveToFile<T>(T dataToSave, string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            
            if (!Directory.Exists(Application.StartupPath + "/gangModData/"))
            {
                Directory.CreateDirectory(Application.StartupPath + "/gangModData/");
            }

            string filePath = Application.StartupPath + "/gangModData/" + fileName + ".xml";

            StreamWriter writer = new StreamWriter(filePath);
            serializer.Serialize(writer, dataToSave);
            writer.Close();

            UI.ShowSubtitle("saved at: " + filePath);
        }
    }
}