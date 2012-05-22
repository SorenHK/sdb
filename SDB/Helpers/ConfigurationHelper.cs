using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace SDB.Helpers
{
    class ConfigurationHelper
    {
        public static string Get(string nameSpace, string property)
        {
            return Get(nameSpace + '.' + property);
        }

        public static string Get(string property)
        {
            return ConfigurationManager.AppSettings[property];
        }

        public static void Set(string nameSpace, string property, string value)
        {
            Set(nameSpace + '.' + property, value);
        }

        public static void Set(string property, string value)
        {
            Set(property, value, ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
        }

        private static void Set(string property, string value, Configuration config)
        {
            if (config == null)
                return;

            var item = config.AppSettings.Settings[property];
            if (item == null)
                config.AppSettings.Settings.Add(new KeyValueConfigurationElement(property, value));
            else
                item.Value = value;

            config.Save();
        }
    }
}
