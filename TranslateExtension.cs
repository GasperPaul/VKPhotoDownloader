using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace VKPhotoDownloader
{
    class TranslateExtension : MarkupExtension
    {
        [ConstructorArgument("_key")]
        public string Key { get; set; }

        public TranslateExtension(string _key)
        {
            Key = _key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding("Translation") { Source = new TranslationData(Key) }.ProvideValue(serviceProvider);
        }
    }
}
