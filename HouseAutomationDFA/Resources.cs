using System;
using System.Drawing;
using System.Resources;
using System.Globalization;

namespace HouseAutomationDFA.Properties
{
    internal static class Resources
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    resourceMan = new ResourceManager("HouseAutomationDFA.Properties.Resources", typeof(Resources).Assembly);
                }
                return resourceMan;
            }
        }

        internal static CultureInfo Culture
        {
            get => resourceCulture;
            set => resourceCulture = value;
        }

        // Image accessors used by MainForm.cs. They return null if resource is missing.
        public static Image LightsOn
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOn", resourceCulture); }
                catch { return null; }
            }
        }

        public static Image LightsOnCool
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOnCool", resourceCulture); }
                catch { return null; }
            }
        }

        public static Image LightsOnWarm
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOnWarm", resourceCulture); }
                catch { return null; }
            }
        }

        public static Image LightsOff
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOff", resourceCulture); }
                catch { return null; }
            }
        }

        public static Image LightsOffCool
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOffCool", resourceCulture); }
                catch { return null; }
            }
        }

        public static Image LightsOffWarm
        {
            get
            {
                try { return (Image)ResourceManager.GetObject("LightsOffWarm", resourceCulture); }
                catch { return null; }
            }
        }
    }
}