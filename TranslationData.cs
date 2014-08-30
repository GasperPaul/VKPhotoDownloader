using System;
using System.ComponentModel;
using System.Windows;

namespace VKPhotoDownloader
{
    class TranslationData : IWeakEventListener, INotifyPropertyChanged, IDisposable
    {
        private string key;
        public object Translation
        {
            get
            {
                return TranslationManager.Instance.Translate(key);
            }
        }

        public TranslationData(string _key)
        {
            key = _key;
            LanguageChangedEventManager.AddListener(TranslationManager.Instance, this);
        }

        ~TranslationData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) 
                LanguageChangedEventManager.RemoveListener(TranslationManager.Instance, this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnLanguageChanged(object sender, EventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Translation"));
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(LanguageChangedEventManager))
            {
                OnLanguageChanged(sender, e);
                return true;
            }
            return false;
        }
    }

    class LanguageChangedEventManager : WeakEventManager
    {
        private static LanguageChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(LanguageChangedEventManager);
                var manager = (LanguageChangedEventManager)GetCurrentManager(managerType);
                if (manager == null)
                {
                    manager = new LanguageChangedEventManager();
                    SetCurrentManager(managerType, manager);
                }
                return manager;
            }
        }

        protected override void StartListening(object source)
        {
            (source as TranslationManager).LanguageChanged += OnLanguageChanged;
        }

        protected override void StopListening(object source)
        {
            (source as TranslationManager).LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            DeliverEvent(sender, e);
        }

        public static void AddListener(TranslationManager source, IWeakEventListener listener)
        {
            CurrentManager.ProtectedAddListener(source, listener);
        }

        public static void RemoveListener(TranslationManager source, IWeakEventListener listener)
        {
            CurrentManager.ProtectedRemoveListener(source, listener);
        }
    }
}
