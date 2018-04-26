using DMSSocketReceiver.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DMSSocketReceiver.dmshandler.impl
{
    public class TestDMSHandler : IDMSHandler
    {
        private LogWriter writer;
        private string metadataFile;

        public TestDMSHandler(LogWriter writer)
        {
            this.metadataFile = ConfigurationUtils.readAppSetting("metadatafile", System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "fakedms.json"));
            ConfigurationUtils.saveAppSetting("metadatadir", this.metadataFile);
            this.writer = writer;
        }


        public DMSCapabilities DetermineCapabilities()
        {
            writer.WriteMessage("retrieving capabilities");
            return new DMSCapabilities(true, true);
        }

        public DMSDocument AttachDocument(string originKey, IDictionary<string, string> metadata)
        {
            IDictionary<string, string> previousDictionary = readDMSDictionary();

            writer.WriteMessage("attaching document");
            return SelectFileWindow.SelectDocument(previousDictionary);
        }

        public DMSDocument InsertDocument(string originKey, string filepath, IDictionary<string, string> metadata)
        {
            IDictionary<string, string> previousDictionary = readDMSDictionary(); writer.WriteMessage("inserting document");

            string guid = Guid.NewGuid().ToString();
            previousDictionary.Add(guid, filepath);
            writeDMSDictionary(previousDictionary);
            string documentName = Path.GetFileName(filepath);
            return new DMSDocument(guid, documentName);

        }

        public void ShowDocument(DMSDocument document)
        {
            IDictionary<string, string> previousDictionary =
                JsonConvert.DeserializeObject<IDictionary<string, string>>(File.ReadAllText(metadataFile));

            writer.WriteMessage(String.Format("showing document: {0}", document));

            string filename = previousDictionary[document.id];
            ProcessStartInfo psi = new ProcessStartInfo(filename);
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        public IDictionary<string, string> readDMSDictionary()
        {
            if (File.Exists(this.metadataFile))
                return
                    JsonConvert.DeserializeObject<IDictionary<string, string>>(File.ReadAllText(this.metadataFile));
            else
                return new Dictionary<string, string>();

        }

        public void writeDMSDictionary(IDictionary<string, string> previousDictionary)
        {
            File.WriteAllText(this.metadataFile, JsonConvert.SerializeObject(previousDictionary));
        }

    }
}
