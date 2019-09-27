using ProclickEmployeeLib.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ProclickEmployeeLib.AutoUpdater
{
    public partial class AutoUpdaterDialog : Form
    {
        #region ENUMERATIONS

        private enum UpdaterState
        {
            None,
            Checking,
            CheckingError,
            NoNewUpdates,
            DownloadingUpdate,
            DownloadingError,
            ReadyToInstall
        }
        #endregion

        #region PROPERTIES
        public bool DoInstall
        {
            get
            {
                return _doInstall;
            }
        }

        private UpdaterState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    _state = value;

                    switch (_state)
                    {                       
                        case UpdaterState.CheckingError:
                            EnterCheckingErrorState();
                            break;
                            
                        case UpdaterState.DownloadingError:
                            EnterDownloadingErrorState();
                            break;

                        case UpdaterState.NoNewUpdates:
                            EnterNoNewUpdatesState();
                            break;

                        case UpdaterState.ReadyToInstall:
                            EnterReadyToInstallState();
                            break;
                    }
                }
            }
        }

        #endregion

        #region PUBLIC METHODS

        public bool OnCheckUpload()
        {            
            DeleteTempFiles();
            this.State = UpdaterState.Checking;
            EnterCheckingState();
            if (this.State == UpdaterState.DownloadingUpdate)
                return true;

            return false;
        }

        #endregion

        #region PROTECTED METHODS

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnterDownloadingUpdateState();
        }

        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Generate User pathes
        /// </summary>
        private static string GenerateUserPath(string appName)
        {
            StringBuilder tempDirBuilder = new StringBuilder();
            tempDirBuilder.Append(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            tempDirBuilder.Append(Path.DirectorySeparatorChar);
            tempDirBuilder.Append(appName);
            return tempDirBuilder.ToString();
        }

        private static string GenerateUserPath(string appName, string purpose)
        {
            StringBuilder tempDirBuilder = new StringBuilder();
            tempDirBuilder.Append(GenerateUserPath(appName));
            tempDirBuilder.Append(Path.DirectorySeparatorChar);
            tempDirBuilder.Append(purpose);
            return tempDirBuilder.ToString();
        }

        /// <summary>
        /// Generate Temporary pathes
        /// </summary>
        public static string GenerateTemporaryPath(string appName, string purpose)
        {
            StringBuilder tempDirBuilder = new StringBuilder();
            tempDirBuilder.Append(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            tempDirBuilder.Append(Path.DirectorySeparatorChar);            
            tempDirBuilder.Append(appName);
            tempDirBuilder.Append(Path.DirectorySeparatorChar);
            tempDirBuilder.Append(purpose);
            tempDirBuilder.Append(Path.DirectorySeparatorChar);
            tempDirBuilder.Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            tempDirBuilder.Append("-");
            string ret = tempDirBuilder.ToString();
            int i = 1;

            while (Directory.Exists(ret + i.ToString()))
            {
                i++;
            }

            ret += i.ToString();

            return ret;
        }

        /// <summary>
        /// Updates are downloaded to temporary folders and installing an 
        /// update creates a copy of the MSI bootstrapper exe named in the form 
        /// AppSetup.msi.Temp.*.exe. Try to delete these files.
        /// </summary>
        private void DeleteTempFiles()
        {
            string[] files = Directory.GetFiles(Models.Settings.ExecutionDir,
                    "MSIBootstrapper.Temp.*.exe");
            foreach (string fileName in files)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception)
                {
                    // Not too bothered if this fails, will retry next time.
                }
            }

            // Find temporary folders created with Util.Files.GenerateTemporaryPath() in form "yyyyMMddHHmmss-n"
            string path = GenerateUserPath("ProClickApp Auto Updater", "MsiDownload");
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(GenerateUserPath("ProClickApp Auto Updater", "MsiDownload"), "??????????????-*");
                foreach (string directoryName in directories)
                {
                    try
                    {
                        Directory.Delete(directoryName, true);
                    }
                    catch (Exception)
                    {
                        // Same as above
                    }
                }

                // Delete auto updater directories if they are empty
                if (!Directory.EnumerateFileSystemEntries(path).Any())
                {
                    try
                    {
                        Directory.Delete(path);
                    }
                    catch (Exception) { }
                }
            }

            path = GenerateUserPath("ProClickApp Auto Updater");
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                try
                {
                    Directory.Delete(path);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Handles when the form enters the Checking For Updates
        /// state.
        /// </summary>
        private void EnterCheckingState()
        {
            Logger.Log("Checking for new updates. Current version is " + _existingVersion.ToString());

            lblStatus.Text = "Checking for new updates...";
            lblProgress.Visible = false;

            StartCheck();
        }

        private void StartCheck()
        {
            try
            {
                Logger.Log("Downloading updates catalogue(without authentication).");

                Stream readerStream = _webClient.OpenRead(new Uri(_versionCheckUri));
                StreamReader sr = new StreamReader(readerStream, Encoding.UTF8);
                OnOpenReadComplete(sr.ReadToEnd());
            }
            catch (Exception e)
            {
                _errorInfo = e.ToString();
                OnCheckingError();
            }
        }

        private void OnCheckingError()
        {
            this.State = UpdaterState.CheckingError;
            OnResult();
        }

        /// <summary>
        /// Handles when the form enters the Error Checking For Updates
        /// state.
        /// </summary>
        private void EnterCheckingErrorState()
        {
            Logger.Log(_errorInfo + " exception when connecting to update server.");
            lblStatus.Text = "Error checking for updates.\nPlease check your internet connection.";
            lblProgress.Visible = false;
        }

        /// <summary>
        /// Handles when the form enters the Downloading Update
        /// state.
        /// </summary>
        private void EnterDownloadingUpdateState()
        {
            lblStatus.Text = "Downloading update...";
            lblProgress.Visible = true;
            progressBar.Visible = true;
            progressBar.Value = 0;            

            try
            {
                _localPath = GenerateTemporaryPath("ProClickApp Auto Updater", "MsiDownload");
                Directory.CreateDirectory(_localPath);
                _localPath += Path.DirectorySeparatorChar;
                _localPath += _updateFilePath;

                string remotePath = _downloadUri + "/" + _updateFilePath;

                _webClient.DownloadFileAsync(new Uri(remotePath), _localPath);
            }
            catch (Exception e)
            {
                _errorInfo = e.ToString();
                this.State = UpdaterState.DownloadingError;
                OnResult();
            }
        }

        /// <summary>
        /// Handles when the form enters the Error Downloading Update
        /// state.
        /// </summary>
        private void EnterDownloadingErrorState()
        {
            lblStatus.Text = "Error while downloading update.";
            lblProgress.Visible = false;
            progressBar.Visible = false;
        }

        /// <summary>
        /// Handles when the form enters the Ready To Install
        /// state.
        /// </summary>
        private void EnterReadyToInstallState()
        {
            lblStatus.Text = "Ready to install update.";
            lblProgress.Visible = true;
            progressBar.Value = 100;
            progressBar.Visible = true;
        }

        /// <summary>
        /// Handles when the form enters the No New Updates
        /// state.
        /// </summary>
        private void EnterNoNewUpdatesState()
        {
            lblStatus.Text = "No new updates available.";
            lblProgress.Visible = false;
            progressBar.Value = 0;
            progressBar.Visible = false;
        }

        /// <summary>
        /// Handler for when the progress of downloading a file has changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            lblStatus.Text = string.Format("Downloading update... {0}KB / {1}KB",
                    e.BytesReceived / 1024,
                    e.TotalBytesToReceive / 1024);
        }

        /// <summary>
        /// Handler for when the update file has finished downloading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this.State = UpdaterState.ReadyToInstall;
            }
            else
            {
                _errorInfo = e.Error.ToString();
                this.State = UpdaterState.DownloadingError;
            }
            OnResult();
        }

        private void OnOpenReadComplete(string strlatestversion)
        {
           
                try
                {
                    Logger.Log("Updates catalogue downloaded, checking for new versions.");
    
                    strlatestversion = strlatestversion.Trim('\"');
                    _upgradeVersion = VersionNumber.Parse(strlatestversion);

                    if (_upgradeVersion.CompareTo(_existingVersion) <= 0)
                    {
                        //No new updates.
                        Logger.Log("No new updates found.");

                        if (_autoClose)
                        {
                            Close();
                        }
                        else
                        {
                            this.State = UpdaterState.NoNewUpdates;
                            OnResult();
                        }
                    }
                    else
                    {
                        Logger.Log("Downloading new version: " + strlatestversion);
                        this.State = UpdaterState.DownloadingUpdate;
                    }
                }
                catch (Exception ex)
                {
                    _errorInfo = ex.ToString();
                    Logger.Log("Exception while processing update catalogue: " + _errorInfo);
                    this.State = UpdaterState.CheckingError;
                    OnResult();
                }               
          
        }
        
        /// <summary>
        /// Handler for when the generic Action button is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResult()
        {
            switch (this.State)
            {
                case UpdaterState.CheckingError:
                    this.State = UpdaterState.Checking;
                    break;

                case UpdaterState.DownloadingError:
                    this.State = UpdaterState.DownloadingUpdate;
                    break;

                case UpdaterState.NoNewUpdates:
                    Close();
                    break;

                case UpdaterState.ReadyToInstall:
                    string exePath = Assembly.GetEntryAssembly().Location;
                    string commandLine = "\"" + _localPath + "\" \"" + exePath + "\"";
                    Logger.Log("Starting MsiBootstrapper (" + commandLine + ").");

                    // Make a copy of the MSI bootstrapper exe to run - in case
                    // the installer needs to modify the bootstrapper itself.
                    string tempExeName = String.Format(
                            "MSIBootstrapper.Temp.{0}.exe",
                            DateTime.Now.ToString("YYYYMMddTHHmmss"));
                    File.Copy(Path.Combine(Models.Settings.ExecutionDir,"MSIBootstrapper.exe"), tempExeName);

                    Process.Start(tempExeName, commandLine);
                    _doInstall = true;
                    Close();
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region CONSTRUCTION / DISPOSAL

        public AutoUpdaterDialog(
            string downloadUri,
            string vercheckingUri,
            string updateFilePath,
            string productName,
            VersionNumber existingVersion,
        bool autoClose)
        {
            InitializeComponent();

            _downloadUri = downloadUri;
            _versionCheckUri = vercheckingUri;
            _updateFilePath = updateFilePath;
            _productName = productName;
            _errorInfo = "";
            _existingVersion = existingVersion;
            _upgradeVersion = null;
            _localPath = "";
            _doInstall = false;
            _autoClose = autoClose;

            _webClient = new WebClient();
            _webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);
            _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(
                    webClient_DownloadProgressChanged);
            _state = UpdaterState.None;           

        }

        #endregion


        #region FIELDS

        private UpdaterState _state;
        private WebClient _webClient;
        private string _downloadUri;
        private string _versionCheckUri;
        private string _updateFilePath;
        private string _productName;
        private string _errorInfo;
        private string _localPath;

        private VersionNumber _existingVersion;
        private VersionNumber _upgradeVersion;

        private bool _doInstall;
        private bool _autoClose;

        #endregion

    }
}
