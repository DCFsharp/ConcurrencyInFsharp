﻿using System;
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
   public static readonly string ImageFolderDestination = @"C:\Demo\ConcurrecnyFSharp\DemoConcurrency\DemoConcurrency\Data\Images\ImagesProcessed";

        private PhotoViewerLib.GetImages.Downloader downloader = new GetImages.Downloader(ImageFolderDestination, GetImages.TypeDownload.FileSys);

        private FileSystemWatcher fileSystemWatcher;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
            fileSystemWatcher = new FileSystemWatcher(ImageFolderDestination);
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
                        if (imageList.Items.Count % 4 == 0)
                            myScrollViewer.ScrollToEnd();
                    }
                    catch { }
                }
            }));
        }

        private void CleanUp()
        {
            if (!Directory.Exists(ImageFolderDestination))
                Directory.CreateDirectory(ImageFolderDestination);

            var files = Directory.GetFiles(ImageFolderDestination, "*.jpg");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
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

        private void Button_Click_Clear(object sender, RoutedEventArgs e)
        {
            imageList.Items.Clear();
            CleanUp();          
        }
    }
}
