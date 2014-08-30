using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace VKPhotoDownloader
{
    public class DownloadData : INotifyPropertyChanged
    {
        private string _userID;
        public string UserID
        {
            get
            {
                return _userID;
            }
            set
            {
                string userId = value.Split('/').Last().Trim();
                if (!userId.ToCharArray().All(ch => ch > '0' && ch < '9'))
                {
                    AlbumName = null;
                    if (userId.StartsWith("id"))
                        _userID = userId.Remove(0, 2);
                    else if (userId.StartsWith("albums"))
                        _userID = userId.Remove(0, 6);
                    else if (userId.StartsWith("album"))
                    {
                        _userID = userId.Remove(0, 5).Split('_').First().Trim();
                        AlbumName = userId.Remove(0, 5).Split('_').Last().Trim();
                        switch (AlbumName)
                        {
                            case "0":
                                AlbumName = "-6";
                                break;
                            case "00":
                                AlbumName = "-7";
                                break;
                            case "000":
                                AlbumName = "-15";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                        _userID = (string)VKAPI.Instance.ExecuteApiCommand(@"utils.resolveScreenName", @"screen_name=" + userId);
                }
                else
                    _userID = userId;

                if (_userID == null)
                    throw new ArgumentException("Користувач з таким нікнеймом не існує.");
                OnPropertyChanged("UserID");
            }
        }

        private string _saveDir;
        public string SaveDir
        {
            get
            {
                return _saveDir;
            }
            set
            {
                if (System.IO.Directory.Exists(value.Trim()))
                    _saveDir = value.Trim();
                else
                {
                    _saveDir = null;
                    throw new ArgumentException("Такої папки не існує.");
                }
                OnPropertyChanged("SaveDir");
            }
        }

        public string AlbumName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class ImageData
    {
        public Thumbnails AlbumList { get; set; }
        public Thumbnails ImageList { get; set; }
    }

    public class Thumbnail : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string BigImage { get; set; }
        public byte[] Image { get; set; }

        private bool _checked;
        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                OnPropertyChanged("Checked");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class Thumbnails : List<Thumbnail> { }
}
