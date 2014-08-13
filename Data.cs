using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKPhotoDownloader
{
    public class DownloadData
    {
        public string UserID { get; set; }
        public string SaveDir { get; set; }
    }

    public class ImageData
    {
        public Thumbnails AlbumList { get; set; }
        public Thumbnails ImageList { get; set; }
    }
    
    public class Thumbnail
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string BigImage { get; set; }
        public byte[] Image { get; set; }
        public bool Checked { get; set; }
    }

    public class Thumbnails : List<Thumbnail> { }
}
