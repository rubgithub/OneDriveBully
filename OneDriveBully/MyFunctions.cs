﻿using System;
using System.Windows.Forms;
using System.IO;
using System.Timers;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Data;
using OneDriveBully.Properties;
using System.Threading;

namespace OneDriveBully
{
    public class MyFunctions
    {
        //User Settings Variables
        public bool UserDefinedSettingsExist;
        private string rootPath;
        private string fileName = @"\OneDriveBully_SyncTempFile.txt";

        //Timer Variables
        private System.Timers.Timer MyTimer;
        private Int32 interval;
        private Int32 timeRemaining;

        public void initApp()
        {
            initTimer();
            checkUserSettings();
            if (UserDefinedSettingsExist)
            {                
                setTimerInterval(Properties.Settings.Default.TimerInterval);
            }
        }

        #region User Settings

        public void checkUserSettings()
        {
            //Check if user has updated the User Settings
            if (!Properties.Settings.Default.UserDefinedSettings)
            {
                // Show Settings Form
                SettingsForm _SettingsForm = new SettingsForm();
                _SettingsForm.ShowDialog();
            }

            // Check again
            Properties.Settings.Default.Reload();
            UserDefinedSettingsExist = Properties.Settings.Default.UserDefinedSettings;
            if (!UserDefinedSettingsExist)
            {
                WrongSettings();
            }
            else
            {
                //Check OneDrive root Path
                rootPath = @Properties.Settings.Default.OneDriveRootFolder + @"\";
                if (rootPath != null)
                {
                    if (!Directory.Exists(rootPath))
                    {
                        WrongSettings();
                    }
                }

                //Check Timer
                if(Properties.Settings.Default.TimerInterval <1)
                {
                    WrongSettings();
                }
            }
        }

        public void WrongSettings()
        {
            MessageBox.Show("Please update your settings.", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateIconText(2);
            stopTimer();
        }

        #endregion User Settings

        #region Timer Related Functions

        public void initTimer()
        {
            MyTimer = new System.Timers.Timer();
            MyTimer.AutoReset = true;
        }

        public void setTimerInterval(Int32 newInterval)
        {
            if (newInterval <= 0)
            {
                WrongSettings();
            }
            else
            {
                interval = newInterval * 60 * 1000;
                MyTimer.Interval = interval;

                stopTimer();
                startTimer();
            }
        }

        public void startTimer()
        {
            MyTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            MyTimer.Enabled = true;

            timeRemaining = interval;
            UpdateIconText(0);
        }

        public void stopTimer()
        {
            MyTimer.Enabled = false;
            MyTimer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            timeRemaining -= 1 * 60 * 1000;
            if (timeRemaining <= 0)
            {
                bullyNow();
                timeRemaining = interval;
            }
            UpdateIconText(0);
        }

        private void UpdateIconText(int ProgressStatus)
        {
            switch (ProgressStatus)
            {
                case 0:
                    ProcessIcon.ni.Icon = Resources.StandardIcon;
                    ProcessIcon.ni.Text = "OneDrive Bully" + " - Next Sync in: "
                    + (timeRemaining / 60 / 1000).ToString() + " minutes";
                    break;
                case 1:
                    ProcessIcon.ni.Icon = Resources.IconProgress;
                    ProcessIcon.ni.Text = "OneDrive Bullying in progress.";
                    break;
                case 2:
                    ProcessIcon.ni.Icon = Resources.IconError;
                    ProcessIcon.ni.Text = "Timer Stopped. Check Settings.";
                    break;
                default:
                    ProcessIcon.ni.Icon = Resources.StandardIcon;
                    ProcessIcon.ni.Text = "OneDrive Bully" + " - Next Sync in: "
                    + (timeRemaining / 60 / 1000).ToString() + " minutes";
                    break;
            }

        }

        #endregion Timer Related Functions

        #region Bully Function

        public void bullyNow()
        {
            UpdateIconText(1);
            checkUserSettings();

            if (File.Exists(rootPath + fileName))
            {
                File.Delete(rootPath + fileName);
            }

            File.Create(rootPath + fileName).Close();
            Thread.Sleep(5000);
            if (File.Exists(rootPath + fileName))
            {
                File.Delete(rootPath + fileName);
            }
                                 
            Thread.Sleep(5000);
            setTimerInterval(Properties.Settings.Default.TimerInterval);
        }

        #endregion Bully Function

        #region Windows Registry Related Functions 

        public void startOnWindowsStartup(bool register)
        {
            if (register)
            {
                RegistryKey add = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                add.SetValue(@"OneDriveBully", "\"" + Application.ExecutablePath.ToString() + "\"");
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(@"OneDriveBully");
                    }
                }
            }
        }

        #endregion Windows Registry Related Functions 

        #region Symbolic Links Related Functions

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            uint dwFlags
        );

        const uint SYMBLOC_LINK_FLAG_FILE = 0x0;
        const uint SYMBLOC_LINK_FLAG_DIRECTORY = 0x1;

        public bool createSymbolicLink(string DestDir, string TargetDir)
        {
            if (!IsElevated)
            {
                MessageBox.Show("You need to run the application as Administrator.",
                                    "Error creating Link", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Directory.Exists(DestDir))
            {
                if (!CreateSymbolicLink(DestDir, TargetDir, SYMBLOC_LINK_FLAG_DIRECTORY))
                {
                    MessageBox.Show("Unable to create symbolic link. " +
                                    "\n" + "(Error Code: " + Marshal.GetLastWin32Error() + ")",
                                    "Error creating Link", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }
            else
            {
                MessageBox.Show("Unable to create symbolic link." +
                "\n" + "Folder already exists.","Error creating Link",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return false;
            }
        }

        public bool deleteSymbolicLink(string DirToDelete)
        {
            if (Directory.Exists(DirToDelete))
            {
                Directory.Delete(DirToDelete);
                return true;
            }

            MessageBox.Show("Error: Unable to delete symbolic link." +
            "\n" + "Folder doesn't exist.", "Error deleting Link", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        static bool IsElevated
        {
            get
            {
                return WindowsIdentity.GetCurrent().Owner
                  .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }

        public DataTable getOneDriveForSymLinks()
        {
            DataTable SymLinksTable = new DataTable();
            SymLinksTable.Columns.Add("Folder Path", typeof(string));
            SymLinksTable.Columns.Add("OneDrive Path", typeof(string));
            SymLinksTable.Columns.Add("Folder Name", typeof(string));

            Properties.Settings.Default.Reload();
            string[] subDirs = Directory.GetDirectories(Properties.Settings.Default.OneDriveRootFolder);

            SymbolicLink sl = new SymbolicLink();

            foreach (string subDir in subDirs)
            {
                if (IsSymbolic(@subDir))
                {
                    string targetF = sl.GetSymLinkTarget(@subDir);
                    Console.WriteLine(subDir.ToString() + " - " + IsSymbolic(@subDir)
                                      + " ==> " + @targetF);
                    SymLinksTable.Rows.Add(@targetF, @subDir, getFolderName(subDir));
                }
            }
            return SymLinksTable;
        }

        private bool IsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        private string getFolderName(string path)
        {
            string OneDrivePath = Properties.Settings.Default.OneDriveRootFolder;    
            return path.Remove(0, OneDrivePath.Length + 1);
        }

        #endregion Symbolic Links Related Functions
    }
}