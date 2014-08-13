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

        public MainWindow()
        {
            InitializeComponent();
            vkApi = VKAPI.Instance;
            data = new DownloadData()
            {
                UserID = "id1"
            };
            imgData = new ImageData();
            this.albumURL.DataContext = data;
        }

        void Browser_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Uri.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#error"))
            {
                string errorMsg = vkApi.HandleOAuthError(e.Uri.AbsoluteUri);
                this.ShowMessageAsync(@"Error", errorMsg);
                Debug.WriteLine(@"OAuth error: " + errorMsg);
                System.Windows.Application.Current.Shutdown();
            }
            if (e.Uri.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#code"))
            {
                vkApi.Token = e.Uri.AbsoluteUri;
                this.tabControl.SelectedIndex = 1;
            }
            else
            {
                this.Height = 423;
                this.Width = 672;
            }
        }

        private void btnShowAlbums_Click(object sender, RoutedEventArgs e)
        {
            if (data.UserID.Trim().Equals(@""))
            {
                albumURL.Focus();
                return;
            }

            try
            {
                if (data.UserID.Trim().Contains("id"))
                    data.UserID = data.UserID.Trim().Remove(0, 2);
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(@"Помилка", @"Невірний формат поля ""Звідки"".");
                Debug.WriteLine("UserId error: " + ex.Message);
                return;
            }

            imgData.AlbumList = vkApi.ExecuteApiCommand(@"photos.getAlbums", @"owner_id=" + data.UserID + @"&album_ids=-6,-7,-15&need_covers=1");
            if (imgData.AlbumList != null)
            {
                this.albumThumbnails.ItemsSource = imgData.AlbumList;
                this.albumThumbnailsHolder.Visibility = System.Windows.Visibility.Visible;
            };
        }

        private void Button_To_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = @"Виберіть місце збереження фото",
                    RootFolder = Environment.SpecialFolder.Desktop,
                    ShowNewFolderButton = true
                })
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    data.SaveDir = dlg.SelectedPath;
                    this.saveDir.DataContext = data;
                };
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            var selection = (this.albumThumbnails.SelectedItem as Thumbnail);
            if (selection == null || selection.Name == null)
            {
                this.ShowMessageAsync(@"", @"Оберіть альбом для перегляду.");
                return;
            }

            try
            {
                imgData.ImageList = vkApi.ExecuteApiCommand(@"photos.get", @"owner_id=" + data.UserID + @"&album_id=" + selection.Name + @"&rev=1");
            }
            catch (Exception ex)
            {
                this.ShowMessageAsync(@"Помилка", @"Неможливо отримати дані з мережі.");
                Debug.WriteLine(@"Network Error: " + ex.Message);
            }

            if (imgData.ImageList != null)
            {
                this.thumbnails.ItemsSource = imgData.ImageList;
                tabControl.SelectedIndex++;
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex--;
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (data.SaveDir == null || !Directory.Exists(data.SaveDir.Trim()))
            {
                this.ShowMessageAsync(@"Помилка", @"Невірний формат поля ""Куди"".");
                this.saveDir.Focus();
                return;
            }

            progressBar.Value = 0;
            progressBar.Visibility = System.Windows.Visibility.Visible;

            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            this.ShowMessageAsync(@"Завантаження завершено!", @"");
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value += e.ProgressPercentage;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
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
                        this.ShowMessageAsync(@"Помилка", @"Неможливо отримати дані з мережі.");
                        Debug.WriteLine(@"Network Error: " + ex.Message);
                    }
                    (sender as BackgroundWorker).ReportProgress(step);
                }
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var photo in imgData.ImageList)
                photo.Checked = true;
            this.thumbnails.ItemsSource = null;
            this.thumbnails.ItemsSource = imgData.ImageList;
        }

        private void MetroWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
