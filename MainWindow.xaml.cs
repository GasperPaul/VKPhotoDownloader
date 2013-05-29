using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VKPhotoDownloader
{
    public partial class MainWindow : Window
    {
        private string token;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Integration.WindowsFormsHost browserHost = new System.Windows.Forms.Integration.WindowsFormsHost();
            System.Windows.Forms.WebBrowser Browser = new System.Windows.Forms.WebBrowser();
            grid.Children.Add(browserHost);
            Browser.ScriptErrorsSuppressed = true;
            browserHost.Child = Browser;
            Browser.Navigated += Browser_Navigated;
            Browser.Navigate(new Uri(@"https://oauth.vk.com/authorize?client_id=3675941&scope=photos,groups&redirect_uri=https://oauth.vk.com/blank.html&display=page&response_type=token"));
        }

        void Browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (e.Url.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#error"))
            {
                string[] tmp = e.Url.AbsoluteUri.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                string[] error = ((tmp[1].Split('&')[2]).Split('='))[1].Split(new char[3] { '%', '2', '0' });
                string errorMsg = "";
                foreach (string s in error)
                    errorMsg += " " + s;
                errorMsg += '.';
                System.Windows.MessageBox.Show(errorMsg, @"Error");
                Debug.WriteLine(@"OAuth error: " + errorMsg);
                System.Windows.Application.Current.Shutdown();
            }
            if (e.Url.AbsoluteUri.Contains("https://oauth.vk.com/blank.html#access"))
            {
                string[] tmp = e.Url.AbsoluteUri.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                this.token = (tmp[1].Split('&')[0]);
                (sender as System.Windows.Forms.WebBrowser).Parent.Dispose();
                this.SizeToContent = System.Windows.SizeToContent.Manual; this.Width = 395; this.Height = 120;
            }
            else
            {
                (sender as System.Windows.Forms.WebBrowser).Width = 607;
                (sender as System.Windows.Forms.WebBrowser).Height = (sender as System.Windows.Forms.WebBrowser).DocumentTitle.Contains(@"Разрешение доступа") ? 427 : 315;
            }
        }

        private string ExecuteApiCommand(string methodName, string param)
        {
            string requestURL = "https://api.vk.com/method/" + methodName + ".xml?" + param + "&" + token;
            WebRequest request = WebRequest.Create(requestURL);
            using (WebResponse response = request.GetResponse())
            using (System.IO.Stream data = response.GetResponseStream())
            using (System.IO.StreamReader rdr = new System.IO.StreamReader(data))
                return rdr.ReadToEnd();
        }

        private bool ParseXml(string type, string XmlString)
        {
            var xDoc = System.Xml.Linq.XDocument.Parse(XmlString);
            if (xDoc.Root.Name.ToString() == "error")
            {
                var str = "API error:\n" + xDoc.Descendants("error")
                                               .Select(el => el.Element("error_msg").Value)
                                               .ToList<String>()[0];
                Debug.WriteLine(str);
                System.Windows.MessageBox.Show(str, @"Error");
                return false;
            }
            switch (type)
            {
                case ("photos.get"):
                    var photos = xDoc.Descendants("photo")
                                     .Where(a => a.Element("src_xxbig") != null)
                                     .Select(a => a.Element("src_xxbig").Value);
                    int counter = 0;
                    foreach (var s in photos) counter++;
                    progressBar.Maximum = counter;
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                    foreach (var s in photos)
                    {
                        progressBar.Value++;

                        var names = s.Split('/');
                        var name = names[names.Length - 1];

                        WebRequest request = WebRequest.Create(s);
                        using (WebResponse response = request.GetResponse())
                        using (System.IO.Stream data = response.GetResponseStream())
                        using (FileStream file = File.Create(saveDir.Text + @"\" + name))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            do
                            {
                                bytesRead = data.Read(buffer, 0, buffer.Length);
                                file.Write(buffer, 0, bytesRead);
                            } while (bytesRead != 0);
                        }
                    }
                    progressBar.Visibility = System.Windows.Visibility.Hidden;
                    break;
                default: break;
            }
            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (albumURL.Text.Trim().Equals(@""))
            {
                albumURL.Focus();
                return;
            }
            string from = albumURL.Text;
            string[] fromData;
            try
            {
                string[] tmp = from.Split("album".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                fromData = tmp[tmp.Length - 1].Split('_');
                switch (fromData[1])
                {
                    case ("0"):
                        fromData[1] = "profile";
                        break;
                    case ("00"):
                        fromData[1] = "wall";
                        break;
                    case ("000"):
                        fromData[1] = "saved";
                        break;
                    default: break;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(@"Невірний формат поля ""Звідки"".", @"Помилка");
                return;
            }
            if (!Directory.Exists(saveDir.Text.Trim()))
            {
                System.Windows.MessageBox.Show(@"Невірний формат поля ""Куди"".", @"Помилка");
                return;
            }
            progressBar.IsIndeterminate = true;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            if (!ParseXml(@"photos.get", ExecuteApiCommand(@"photos.get", @"oid=" + fromData[0] + @"&aid=" + fromData[1] + @"&rev=1")))
            {
                progressBar.IsIndeterminate = false;
                progressBar.Visibility = System.Windows.Visibility.Hidden;
            };
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.RootFolder = Environment.SpecialFolder.Desktop;
                dlg.Description = @"Виберіть місце збереження фото";
                dlg.ShowNewFolderButton = true;
                ;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    saveDir.Text = dlg.SelectedPath;
                };
            }
        }
    }
}
