using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace VKPhotoDownloader
{
    public class VKAPI
    {
        private static VKAPI _instance;
        public static VKAPI Instance
        {
            get { return _instance ?? (_instance = new VKAPI()); }
        }

        private string _token;
        public string Token
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries)[1].Split('&')[0]/*.Replace("code","access_token")*/;
            }
        }

        private string _currentUserID;
        [Obsolete]
        public string CurrentUserID
        {
            get
            {
                return _currentUserID ?? (_currentUserID = (string)ExecuteApiCommand("users.get", "v=5.24"));
            }
        }

        public object ExecuteApiCommand(string methodName, string param, Func<string, object> customParser = null)
        {
            string requestURL = "https://api.vk.com/method/" + methodName + ".xml?" + param + "&" + Token;
            using (WebResponse response = WebRequest.Create(requestURL).GetResponse())
            using (System.IO.Stream data = response.GetResponseStream())
            using (System.IO.StreamReader rdr = new System.IO.StreamReader(data))
                return ParseXml(methodName, rdr.ReadToEnd(), customParser);
        }

        private object ParseXml(string type, string XmlString, Func<string, object> customParser)
        {
            var imageList = new Thumbnails();
            var xDoc = System.Xml.Linq.XDocument.Parse(XmlString);

            if (xDoc.Root.Name.ToString() == "error")
            {
                var str = "API error:\n" + xDoc.Descendants("error")
                                               .Select(el => el.Element("error_msg").Value)
                                               .ToList<String>()[0];
                Debug.WriteLine(str);
                return null;
            }

            if (customParser != null)
                return customParser(XmlString);

            switch (type)
            {
                case ("photos.getAlbums"):
                    var albums = xDoc.Descendants("album").Select(a => new Thumbnail
                    {
                        Name = a.Element("aid").Value,
                        Title = a.Element("title").Value,
                        Image = ImageDataFromUri(a.Element("thumb_src").Value),
                        Checked = false
                    });
                    imageList.Clear();
                    foreach (var album in albums)
                        imageList.Add(album);
                    return imageList;

                case ("photos.get"):
                    Func<System.Xml.Linq.XElement, string> tmp = (xElement) =>
                    {
                        var bigImage = xElement.Element("src_xxxbig");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        bigImage = xElement.Element("src_xxbig");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        bigImage = xElement.Element("src_xbig");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        bigImage = xElement.Element("src_big");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        bigImage = xElement.Element("src");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        bigImage = xElement.Element("src_small");
                        if (bigImage != null && !string.IsNullOrWhiteSpace(bigImage.Value))
                            return bigImage.Value;
                        return null;
                    };

                    var photos = xDoc.Descendants("photo")
                                     .Select(a => new Thumbnail()
                                     {
                                         Name = a.Element("src_small").Value.Split('/').Last(),
                                         Title = tmp(a).Split('/').Last(),
                                         BigImage = tmp(a),
                                         Image = ImageDataFromUri(a.Element("src_small").Value),
                                         Checked = false
                                     });

                    imageList.Clear();
                    foreach (var photo in photos)
                        imageList.Add(photo);
                    return imageList;

                case "utils.resolveScreenName":
                    return xDoc.Element("response").HasElements ? xDoc.Element("response").Element("object_id").Value : null;

                case "users.get":
                    return xDoc.Element("response").Element("id").Value;

                default:
                    break;
            }
            return null;
        }

        private byte[] ImageDataFromUri(string uriString)
        {
            using (WebResponse response = WebRequest.Create(uriString).GetResponse())
            using (Stream data = response.GetResponseStream())
            using (var memoryStream = new MemoryStream())
            {
                System.Drawing.Image.FromStream(data).Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return memoryStream.ToArray();
            }
        }

        public string HandleOAuthError(string error)
        {
            string[] errorParts = error.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries)[1]
                .Split('&')[2]
                .Split('=')[1]
                .Split(new char[3] { '%', '2', '0' });
            string errorMsg = "";
            foreach (string s in errorParts)
                errorMsg += " " + s;
            errorMsg += '.';
            return errorMsg;
        }
    }
}
