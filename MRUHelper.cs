using Microsoft.VisualStudio.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebuggerListener
{
    internal class MRUHelper
    {
        private SettingsStore settingsStore;
        private List<string> recentProjectsPaths;

        public MRUHelper(string path)
        {
            ExternalSettingsManager externalSettingsManager = ExternalSettingsManager.CreateForApplication(path);
            settingsStore = externalSettingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            foreach (string name in settingsStore.GetPropertyNames(@"MRUItems\{a9c4a31f-f9cb-47a9-abc0-49ce82d0b3ac}\Items"))
            {
                string value = settingsStore.GetString(@"MRUItems\{a9c4a31f-f9cb-47a9-abc0-49ce82d0b3ac}\Items", name);
                Console.WriteLine("Property name: {0}, value: {1}", name, value.Split('|')[0]);
                recentProjectsPaths.Add(value.Split('|')[0]);
            }
        }
    }
}