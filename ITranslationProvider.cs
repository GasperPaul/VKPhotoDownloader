using System.Collections.Generic;
using System.Globalization;

namespace VKPhotoDownloader
{
    interface ITranslationProvider
    {
        IEnumerable<CultureInfo> Languages { get; }
        object Translate(string key);
    }
}
