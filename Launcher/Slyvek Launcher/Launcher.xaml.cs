using SlyvekLauncherCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Slyvek_Launcher
{
    public partial class MainWindow : Window
    {
        private int nbEntryToExtract = 0;
        private int nbEntryExtracted = 0;

        public MainWindow()
        {
            InitializeComponent();
            Core.OnRemoteVersionDownloaded += Core_OnRemoteVersionDownloaded;
            Core.OnRemoteVersionDownloading += Core_OnRemoteVersionDownloading;
            Core.OnRemoteVersionExtracting += Core_OnRemoteVersionExtracting;
            Core.OnRemoteVersionExtracted += Core_OnRemoteVersionExtracted;
            Core.OnExtractingStart += Core_OnExtractingStart;
            Core.Initialize();
            tbTitle.Text = "Dodge From Light Launcher - v" + Core.CurrentVersion.ToString();
            if (LauncherCore.CheckLauncherUpdate())
            {
                try
                {
                    Process.Start(Environment.CurrentDirectory + @"\Magic_DFL.exe");
                    Application.Current.Shutdown();
                    Environment.Exit(0);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    MessageBox.Show("You should reinstall the launcher");
                }
            }
            else
            {
                if (Core.CheckUpdate())
                {
                    Core.DownloadRemoteVersion();
                    pbDownload.Visibility = Visibility.Visible;
                    lbDownload.Visibility = Visibility.Visible;
                    PlayButton.Visibility = Visibility.Hidden;
                }
                else
                {
                    pbDownload.Visibility = Visibility.Hidden;
                    lbDownload.Visibility = Visibility.Hidden;
                    PlayButton.Visibility = Visibility.Visible;
                }
            }
        }

        #region Downloading and Extracting
        //  Core Downloading File
        private void Core_OnRemoteVersionDownloading(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            lbDownload.Text = "Downloading : " + Core.BytesToString(e.BytesReceived) + " / " + Core.BytesToString(e.TotalBytesToReceive) + " (" + e.ProgressPercentage + "%)";
            pbDownload.Value = e.ProgressPercentage;
        }
        // Core File Downloade
        private void Core_OnRemoteVersionDownloaded(object sender, SlyvekLauncherCore.Version e)
        {
            Core.ExtractRemoteVersion();
        }
        // Core Start Extraction
        private void Core_OnExtractingStart(object sender, ICollection<Ionic.Zip.ZipEntry> e)
        {
            nbEntryExtracted = 0;
            nbEntryToExtract = e.Count;
            Dispatcher.Invoke(() => { pbDownload.Maximum = nbEntryToExtract * 100; });
        }
        // Core File Extracting
        private void Core_OnRemoteVersionExtracting(object sender, Ionic.Zip.ExtractProgressEventArgs e)
        {
            if (e.EventType == Ionic.Zip.ZipProgressEventType.Extracting_EntryBytesWritten)
            {
                int percent = (int)((nbEntryExtracted * 100) + ((float)e.BytesTransferred / (float)e.TotalBytesToTransfer * 100f));
                Dispatcher.Invoke(() => { pbDownload.Value = percent; });
            }

            else if (e.EventType == Ionic.Zip.ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                string text = "Extracting : " + e.CurrentEntry.FileName + " (" + Core.BytesToString(e.CurrentEntry.UncompressedSize) + " bytes)"; ;
                Dispatcher.Invoke(() => { lbDownload.Text = text; });
                nbEntryExtracted++;
            }
        }
        // Core File Extracted
        private void Core_OnRemoteVersionExtracted(object sender, string e)
        {
            Dispatcher.Invoke(() =>
            {
                pbDownload.Visibility = Visibility.Hidden;
                lbDownload.Visibility = Visibility.Hidden;
                PlayButton.Visibility = Visibility.Visible;
                tbTitle.Text = "Dodge From Light Launcher - v" + Core.CurrentVersion.ToString();
            });
        }
        #endregion

        #region Menu
        private void MenuItem_CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (Core.CheckUpdate())
            {
                Core.DownloadRemoteVersion();
                pbDownload.Visibility = Visibility.Visible;
                lbDownload.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Hidden;
            }
            else
                MessageBox.Show("No Update Avaliable.");
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Core.StartDFL();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Menu_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch { }
        }

        private void Menu_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void MenuItem_ForceUpdate_Click(object sender, RoutedEventArgs e)
        {
            Core.DownloadRemoteVersion();
            pbDownload.Visibility = Visibility.Visible;
            lbDownload.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Hidden;
        }
    }
}