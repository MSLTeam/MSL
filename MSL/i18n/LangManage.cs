using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MSL.i18n
{
    public class LanguageManager : INotifyPropertyChanged
    {
        private readonly ResourceManager _resourceManager;
        private static readonly Lazy<LanguageManager> _lazy = new Lazy<LanguageManager>(() => new LanguageManager());
        public static LanguageManager Instance => _lazy.Value;
        public event PropertyChangedEventHandler PropertyChanged;

        private LanguageManager()
        {
            _resourceManager = new ResourceManager(typeof(Langs));
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
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
