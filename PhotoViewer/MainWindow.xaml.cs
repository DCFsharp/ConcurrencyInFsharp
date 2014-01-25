using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhotoViewerLib;

namespace PhotoViewer
{
    public partial class MainWindow : Window
    {
        private PhotoViewerLib.GetImages.Downloader downloader = new GetImages.Downloader(Environment.CurrentDirectory, GetImages.TypeDownload.FileSys);

        private FileSystemWatcher fileSystemWatcher;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
            fileSystemWatcher = new FileSystemWatcher(Environment.CurrentDirectory);
            fileSystemWatcher.Created += new FileSystemEventHandler(FileChanged);
            //    fileSystemWatcher.Changed += new FileSystemEventHandler(FileChanged);
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        void FileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                bool done = false;
                while (!done)
                {
                    try
                    {
                        var bmp = new BitmapImage(new Uri(e.FullPath));
                        imageList.Items.Add(bmp);
                        done = true;
                    }
                    catch { }
                }
            }));
        }

        private void CleanUp()
        {
            var files = Directory.GetFiles(Environment.CurrentDirectory, "*.jpg");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }


        private void Button_Click_Async(object sender, RoutedEventArgs e)
        {
            imageList.Items.Clear();
            System.Threading.ThreadPool.QueueUserWorkItem(o => downloader.DownloadAllAsync());
        }


        private void Button_Click_Agent(object sender, RoutedEventArgs e)
        {
            imageList.Items.Clear();
            System.Threading.ThreadPool.QueueUserWorkItem(o => downloader.DownloadAllAgent());
        }

        private void Button_Click_Sync(object sender, RoutedEventArgs e)
        {
            imageList.Items.Clear();
            System.Threading.ThreadPool.QueueUserWorkItem(o => downloader.DownloadAllSync());
        }
    }
}
