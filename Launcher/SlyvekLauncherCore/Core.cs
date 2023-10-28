using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace SlyvekLauncherCore
{
    public static class Core
    {
        #region Variables and Events
        // Public variables
        public static Version CurrentVersion = null;
        public static Version RemoteVersion = null;
        public static CoreClientState CurrentState = CoreClientState.None;

        // Public Events
        public static event EventHandler<Version> OnRemoteVersionDownloaded;
        public static event EventHandler<DownloadProgressChangedEventArgs> OnRemoteVersionDownloading;
        public static event EventHandler<ExtractProgressEventArgs> OnRemoteVersionExtracting;
        public static event EventHandler<string> OnRemoteVersionExtracted;
        public static event EventHandler<ICollection<ZipEntry>> OnExtractingStart;

        public static string LocalDataPath = Environment.CurrentDirectory + @"\Data";
        public static string LocalSlyvekPath = LocalDataPath + @"\DodgeFromLight";

        // Private variables
        private static string LocalVersionFile = Environment.CurrentDirectory + @"\version.txt";
        private static string LocalTempDataPath = Environment.CurrentDirectory + @"\temp.zip";

        private static string RemoteVersionFile = "http://vrdtmstudio.com/DFL/version.txt";
        private static string RemoteDataPath = "http://vrdtmstudio.com/DFL/";
        private static WebClient Client = new WebClient();
        #endregion

        #region initialization
        /// <summary>
        /// Initialize the Core
        /// </summary>
        public static void Initialize()
        {
            CurrentVersion = new Version(File.ReadAllText(LocalVersionFile));
            GetRemoteVersion();
            Client.DownloadProgressChanged += Client_DownloadProgressChanged;
            Client.DownloadFileCompleted += Client_DownloadFileCompleted;
        }

        /// <summary>
        /// Check if an Update is Avaliable (for Build Data)
        /// </summary>
        /// <returns>True if an update is avaliable</returns>
        public static bool CheckUpdate() => GetRemoteVersion().isHigher(CurrentVersion);

        /// <summary>
        /// Set Core.RemoteVersion as the Remote version avaliable on the Server
        /// </summary>
        /// <returns>Retourne the last Avaliable Version</returns>
        public static Version GetRemoteVersion()
        {
            try
            {
                string remoteVersionString = Client.DownloadString(RemoteVersionFile);
                RemoteVersion = new Version(remoteVersionString);
                return RemoteVersion;
            }
            catch
            {
                return CurrentVersion;
            }
        }
        #endregion

        #region Downloading
        /// <summary>
        /// Download the last data version
        /// </summary>
        public static void DownloadRemoteVersion()
        {
            CurrentState = CoreClientState.DownloadingRemoteData;
            Client.DownloadFileAsync(new Uri(RemoteDataPath + RemoteVersion.ToString() + ".zip"), LocalTempDataPath);
        }

        // Local Client Download Completed
        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            switch (CurrentState)
            {
                case CoreClientState.DownloadingRemoteData:
                    if (OnRemoteVersionDownloaded != null)
                        OnRemoteVersionDownloaded(sender, RemoteVersion);
                    break;
            }
            CurrentState = CoreClientState.None;
        }

        // Local Client Downloading
        private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            switch (CurrentState)
            {
                case CoreClientState.DownloadingRemoteData:
                    if (OnRemoteVersionDownloading != null)
                        OnRemoteVersionDownloading(sender, e);
                    break;
            }
        }
        #endregion

        #region Extracting
        /// <summary>
        /// Extract the downloaded Remote Version
        /// </summary>
        public static void ExtractRemoteVersion()
        {
            CurrentState = CoreClientState.ExtractingRemoteData;

            if (Directory.Exists(LocalSlyvekPath))
                Directory.Delete(LocalSlyvekPath, true);

            Thread t = new Thread(() =>
            {
                using (var zip = new ZipFile(LocalTempDataPath))
                {
                    zip.ExtractProgress += Zip_ExtractProgress;
                    if (OnExtractingStart != null)
                        OnExtractingStart(zip, zip.Entries);
                    zip.ExtractAll(LocalDataPath, ExtractExistingFileAction.OverwriteSilently);
                    zip.ExtractProgress -= Zip_ExtractProgress;
                }
                CurrentVersion = RemoteVersion;
                File.WriteAllText(LocalVersionFile, CurrentVersion.ToString());
                File.Delete(LocalTempDataPath);
                if (OnRemoteVersionExtracted != null)
                    OnRemoteVersionExtracted(null, "");
            });
            t.Start();
        }

        private static void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (OnRemoteVersionExtracting != null)
                OnRemoteVersionExtracting(sender, e);

            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractAll)
                CurrentState = CoreClientState.None;
        }
        #endregion

        #region Utils
        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return (Math.Sign(byteCount) * num).ToString("f2") + suf[place];
        }

        public static void StartDFL()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WorkingDirectory = LocalSlyvekPath;
            psi.FileName = "Dodge From Light.exe";
            Process.Start(psi);
        }
        #endregion
    }

    public enum CoreClientState
    {
        None = 0,
        DownloadingRemoteData = 1,
        ExtractingRemoteData = 2
    }
}