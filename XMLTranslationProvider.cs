using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace VKPhotoDownloader
{
    class XMLTranslationProvider : ITranslationProvider
    {
        private IEnumerable<CultureInfo> languages;
        public IEnumerable<CultureInfo> Languages
        {
            get { return languages; }
        }

        private IDictionary<Tuple<string, string>, string> translations;
        public object Translate(string key)
        {
            return translations[Tuple.Create(key, TranslationManager.Instance.CurrentLanguage.TwoLetterISOLanguageName)] ?? translations[Tuple.Create(key, "en")];
        }

        public XMLTranslationProvider(string fileName)
        {
            var xDoc = XDocument.Parse(System.IO.File.OpenText(fileName).ReadToEnd());
            var texts = xDoc.Descendants("Text").SelectMany(e => 
                e.Descendants().Select(a => 
                    new KeyValuePair<Tuple<string, string>, string>(Tuple.Create(e.Attribute("id").Value, a.Name.ToString()), 
                        a.Attribute("value").Value)
                )
            );

            translations = new Dictionary<Tuple<string, string>, string>();
            foreach (var text in texts)
            {
                translations.Add(text);
            }

            languages = translations.Keys.Select(t => new CultureInfo(t.Item2)).Distinct();
        }
    }
}
