using System;
using System.Configuration;

namespace DMSSocketReceiver.Utils
{
    class ConfigurationUtils
    {
        public static string readAppSetting(string key, string defaultValue = "")
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            string result = settings[key]!=null ? settings[key].Value: defaultValue;
            return result;
        }

        public static void saveAppSetting(string key, string value)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
    }

}
