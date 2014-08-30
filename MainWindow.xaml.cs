using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace VKPhotoDownloader
{
    public partial class MainWindow : MetroWindow
    {
        private VKAPI vkApi;
        private DownloadData data;
        private ImageData imgData;
        private TranslationManager tr;

        public MainWindow()
        {
            InitializeComponent();
            vkApi = VKAPI.Instance;
            data = new DownloadData()
            {
                UserID = "1"
            };
            imgData = new ImageData();
            tr = TranslationManager.Instance; 
            this.albumURL.DataContext = data;
            this.saveDir.DataContext = data;
            this.languageSelector.DataContext = tr;
            tr.CurrentLanguage = tr.Languages.First(a => a.TwoLetterISOLanguageName == tr.CurrentLanguage.TwoLetterISOLanguageName);
        }

        void Browser_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Uri.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#error"))
            {
                string errorMsg = vkApi.HandleOAuthError(e.Uri.AbsoluteUri);
                this.ShowMessageAsync(tr["Error"], errorMsg);
                Debug.WriteLine(@"OAuth error: " + errorMsg);
                System.Windows.Application.Current.Shutdown();
            }
            if (e.Uri.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#code"))
            {
                vkApi.Token = e.Uri.AbsoluteUri;
                //data.UserID = vkApi.CurrentUserID;
                this.tabControl.SelectedIndex = 1;
            }
        }

        private void btnShowAlbums_Click(object sender, RoutedEventArgs e)
        {
            this.albumThumbnailsHolder.Visibility = System.Windows.Visibility.Hidden;
            this.btnNext.Visibility = System.Windows.Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(data.UserID))
            {
                albumURL.Focus();
                return;
            }

            this.albumsProgress.IsActive = true;

            if (!string.IsNullOrWhiteSpace(data.AlbumName))
                ShowThumbnails();

            var albumDownloader = new BackgroundWorker();
            albumDownloader.WorkerReportsProgress = false;
            albumDownloader.DoWork += albumDownloader_DoWork;
            albumDownloader.RunWorkerCompleted += albumDownloader_RunWorkerCompleted;
            albumDownloader.RunWorkerAsync();
        }

        void albumDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (imgData.AlbumList != null && imgData.AlbumList.Count != 0)
            {
                this.albumThumbnails.ItemsSource = imgData.AlbumList;
            }
            else
            {
                this.ShowMessageAsync(@"", tr["NoAlbumsAvailale"]);
            }

            this.albumThumbnailsHolder.Visibility = System.Windows.Visibility.Visible;
            this.btnNext.Visibility = System.Windows.Visibility.Visible;
            this.albumsProgress.IsActive = false;
        }

        void albumDownloader_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                imgData.AlbumList = (Thumbnails)vkApi.ExecuteApiCommand(@"photos.getAlbums", @"owner_id=" + data.UserID + @"&album_ids=-6,-7,-15&need_covers=1");
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(tr["Error"], tr["NetworkError"]);
                Debug.WriteLine(@"Network Error: " + ex.Message);
            }
        }

        private void btnTo_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = tr["SelectDirDialogTitle"],
                    RootFolder = Environment.SpecialFolder.Desktop,
                    ShowNewFolderButton = true
                })
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    data.SaveDir = dlg.SelectedPath;
                }
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            var selection = (this.albumThumbnails.SelectedItem as Thumbnail);
            if (selection == null || selection.Name == null)
            {
                this.ShowMessageAsync(@"", tr["SelectAlbum"]);
                return;
            }

            data.AlbumName = selection.Name;
            ShowThumbnails();
        }

        private void ShowThumbnails()
        {
            tabControl.SelectedIndex++;

            this.thumbnailsHolder.Visibility = System.Windows.Visibility.Hidden;
            this.btnDownload.Visibility = System.Windows.Visibility.Collapsed;
            this.thumbnailsProgress.IsActive = true;

            var thumbnailsDownloader = new BackgroundWorker();
            thumbnailsDownloader.WorkerReportsProgress = false;
            thumbnailsDownloader.DoWork += thumbnailsDownloader_DoWork;
            thumbnailsDownloader.RunWorkerCompleted += thumbnailsDownloader_RunWorkerCompleted;
            thumbnailsDownloader.RunWorkerAsync();
        }

        void thumbnailsDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (imgData.ImageList != null && imgData.ImageList.Count != 0)
            {
                this.thumbnails.ItemsSource = imgData.ImageList;
            }
            else
            {
                this.ShowMessageAsync(@"", tr["NoPhotoAvailable"]);
                tabControl.SelectedIndex--;
            }

            this.thumbnailsHolder.Visibility = System.Windows.Visibility.Visible;
            this.btnDownload.Visibility = System.Windows.Visibility.Visible;
            this.thumbnailsProgress.IsActive = false;
        }

        void thumbnailsDownloader_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                imgData.ImageList = (Thumbnails)vkApi.ExecuteApiCommand(@"photos.get", @"owner_id=" + data.UserID + @"&album_id=" + data.AlbumName + @"&rev=1");
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(tr["Error"], tr["NetworkError"]);
                Debug.WriteLine(@"Network Error: " + ex.Message);
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex--;
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(data.SaveDir))
            {
                this.saveDir.Focus();
                return;
            }

            if (imgData.ImageList == null || imgData.ImageList.Count(photo => photo.Checked) == 0)
            {
                this.ShowMessageAsync(@"", tr["NoPhotosSelected"]);
                return;
            }

            progressBar.Value = 0;
            progressBar.Visibility = System.Windows.Visibility.Visible;

            var photoDownloader = new BackgroundWorker();
            photoDownloader.WorkerReportsProgress = true;
            photoDownloader.DoWork += photoDownloader_DoWork;
            photoDownloader.RunWorkerCompleted += photoDownloader_RunWorkerCompleted;
            photoDownloader.ProgressChanged += photoDownloader_ProgressChanged;
            photoDownloader.RunWorkerAsync();
        }

        void photoDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            this.ShowMessageAsync(tr["DownloadingComplete"], @"");
        }

        void photoDownloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value += e.ProgressPercentage;
        }

        void photoDownloader_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] buffer = new byte[4096];
            int step = 100 / imgData.ImageList.Count(photo => photo.Checked);
            foreach (var photo in imgData.ImageList)
            {
                if (photo.Checked)
                {
                    try
                    {
                        using (WebResponse response = WebRequest.Create(photo.BigImage).GetResponse())
                        using (Stream data = response.GetResponseStream())
                        using (FileStream file = File.Create(this.data.SaveDir + @"/" + photo.BigImage.Split('/').Last()))
                        {
                            int bytesRead;
                            do
                            {
                                bytesRead = data.Read(buffer, 0, buffer.Length);
                                file.Write(buffer, 0, bytesRead);
                            } while (bytesRead != 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.ShowMessageAsync(tr["Error"], tr["NetworkError"]);
                        Debug.WriteLine(@"Network Error: " + ex.Message);
                    }
                    (sender as BackgroundWorker).ReportProgress(step);
                }
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (imgData.ImageList != null && imgData.ImageList.Count != 0)
                foreach (var photo in imgData.ImageList)
                    photo.Checked = true;
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (imgData.ImageList != null && imgData.ImageList.Count != 0)
                foreach (var photo in imgData.ImageList)
                    photo.Checked = false;
        }

        private void thumbnails_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null)
                foreach (Thumbnail item in e.AddedItems)
                    item.Checked = !item.Checked;
        }

        private void MetroWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMessageAsync(@"", "VK Photo Downloader\n(c) Gasper, CC BY-NC 4.0");
        }
    }
}
