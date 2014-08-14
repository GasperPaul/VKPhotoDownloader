using System.Collections.Generic;
using System.ComponentModel;

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
                _userID = value;
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
                _saveDir = value;
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
