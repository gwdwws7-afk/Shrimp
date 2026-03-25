using System;

namespace ThirdPersonController
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string zhCN;
        public string enUS;

        public string Get(LocalizationLanguage language)
        {
            switch (language)
            {
                case LocalizationLanguage.English:
                    return enUS;
                case LocalizationLanguage.SimplifiedChinese:
                default:
                    return zhCN;
            }
        }
    }
}
