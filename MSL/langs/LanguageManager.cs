using HandyControl.Tools;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MSL.langs
{
    public class LanguageManager : INotifyPropertyChanged
    {
        private readonly ResourceManager _resourceManager;
        private static readonly Lazy<LanguageManager> _lazy = new Lazy<LanguageManager>(() => new LanguageManager());
        public static LanguageManager Instance => _lazy.Value;
        public event PropertyChangedEventHandler PropertyChanged;

        private LanguageManager()
        {
            _resourceManager = new ResourceManager(typeof(Lang));
        }

        public string this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                return _resourceManager.GetString(name);
            }
        }

        public void ChangeLanguage(CultureInfo cultureInfo)
        {
            if (cultureInfo.Name == "zh-CN")
            {
                CultureInfo _cultureInfo = new CultureInfo("");
                ConfigHelper.Instance.SetLang(_cultureInfo.Name);
                CultureInfo.CurrentCulture = _cultureInfo;
                CultureInfo.CurrentUICulture = _cultureInfo;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                return;
            }
            ConfigHelper.Instance.SetLang(cultureInfo.Name);
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

    }
}
