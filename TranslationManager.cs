using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace VKPhotoDownloader
{
    class TranslationManager
    {
        private static TranslationManager _manager;
        public static TranslationManager Instance
        {
            get
            {
                return _manager ?? (_manager = new TranslationManager());
            }
        }

        public ITranslationProvider TranslationProvider { get; set; }

        public event EventHandler LanguageChanged;

        public CultureInfo CurrentLanguage
        {
            get
            {
                return Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value != Thread.CurrentThread.CurrentUICulture)
                {
                    Thread.CurrentThread.CurrentUICulture = value;
                    OnLanguageChanged();
                }
            }
        }

        public IEnumerable<CultureInfo> Languages
        {
            get
            {
                return TranslationProvider != null ? TranslationProvider.Languages : Enumerable.Empty<CultureInfo>();
            }
        }

        private void OnLanguageChanged()
        {
            if (LanguageChanged != null)
                LanguageChanged(this, EventArgs.Empty);
        }

        public object Translate(string key)
        {
            if (TranslationProvider != null)
            {
                object translation = TranslationProvider.Translate(key);
                if (translation != null)
                    return translation;
            }
            return string.Format("!{0}!", key);
        }

        public string this[string key]
        {
            get { return Translate(key).ToString(); }
        }
    }
}
