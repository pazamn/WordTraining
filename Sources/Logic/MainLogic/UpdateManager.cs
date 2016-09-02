using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using WordTraining.Logic.Services;
using WordTraining.Windows;

namespace WordTraining.Logic.MainLogic
{
    public static class UpdateManager
    {
        #region Main update-checking things

        public const string VersionFilePath = "ftp://ftp.user1124416.atservers.net/Version.txt";
        public const string UpdateFilePath = "ftp://ftp.user1124416.atservers.net/Release.ZIP";
        public const string FtpUserName = "wordtrainer";
        public const string FtpPassword = "kEizUKA0";
        public const string TraceCategory = "Uploading to FTP";

        public static void CheckForUpdatesAndServiceArguments()
        {
            try
            {
                List<string> allArguments = Environment.GetCommandLineArgs().Select(a => a.ToLowerInvariant()).ToList();

                if (allArguments.Contains("/upload"))
                {
                    UploadVersionToFtp();
                    return;
                }

                if (allArguments.Contains("/update"))
                {
                    UpdateExecutableFile();
                    return;
                }

                if (allArguments.Contains("/deletetemp"))
                {
                    RestartFromMainFile();
                    return;
                }

                CheckForOtherInstances();
                CheckForUpdates();
            }
            catch (Exception e)
            {
                string message = "Failed to check updates of program. \r\n\r\n" + e.Message;
                MessageBox.Show(message, "Update failure", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion Main update-checking method things

        #region Auxiliary things

        private static void CheckForOtherInstances()
        {
            try
            {
                Process[] processesList = Process.GetProcesses();
                Process currentProcess = Process.GetCurrentProcess();

                bool otherProcessExists = processesList.Any(p => p.ProcessName == currentProcess.ProcessName && p.Id != currentProcess.Id);
                if (!otherProcessExists)
                {
                    return;
                }

                currentProcess.Kill();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to check other instances of this application. " + e.Message);
            }
        }

        private static void UploadVersionToFtp()
        {
            while (PreloaderWindow.CurrentWindow == null)
            {
                Thread.Sleep(100);
            }

            try
            {
                Debug.WriteLine("Uploading command started", TraceCategory);
                PreloaderWindow.ShowFirstStep();

                object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(DebuggableAttribute), false);
                DebuggableAttribute attribute = customAttributes.FirstOrDefault() as DebuggableAttribute;
                bool isDebugConfiguration = attribute != null && attribute.IsJITOptimizerDisabled && attribute.IsJITTrackingEnabled;
                Debug.WriteLine("Is current configuration debug: " + isDebugConfiguration, TraceCategory);
                if (isDebugConfiguration)
                {
                    //Do nothing, then kill current process
                    //Process.GetCurrentProcess().Kill();
                }

                string executionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(executionFolder) || !Directory.Exists(executionFolder))
                {
                    throw new Exception("Failed to get directory of current executing assembly.");
                }

                string releaseArchiveFileName = Path.GetFileName(UpdateFilePath);
                if (string.IsNullOrEmpty(releaseArchiveFileName))
                {
                    throw new Exception("Failed to get name of release archive file.");
                }

                string releaseArchiveFilePath = Path.Combine(executionFolder, releaseArchiveFileName);
                if (string.IsNullOrEmpty(releaseArchiveFilePath) || ! File.Exists(releaseArchiveFilePath))
                {
                    throw new Exception("Failed to get release archive file.");
                }

                WebClient client = new WebClient { Credentials = new NetworkCredential(FtpUserName, FtpPassword) };
                client.UploadFile(UpdateFilePath, releaseArchiveFilePath);

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                client.UploadString(VersionFilePath, currentVersion.ToString());
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to upload version to FTP. " + e.Message, "Uploading error", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void CheckForUpdates()
        {
            while (PreloaderWindow.CurrentWindow == null)
            {
                Thread.Sleep(100);
            }

            try
            {
                PreloaderWindow.ShowSecondStep();
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                WebClient client = new WebClient {Credentials = new NetworkCredential(FtpUserName, FtpPassword)};
                string serverVersionString = client.DownloadString(VersionFilePath);
                Version serverVersion = new Version(serverVersionString);
                if (serverVersion <= currentVersion)
                {
                    return;
                }

                string updateMessage = "There is new version of WordTraining (" + serverVersion + ") on server. Do you want to download the new version and update?";
                MessageBoxResult boxResult = PreloaderWindow.ShowDialog(updateMessage);
                if (boxResult != MessageBoxResult.Yes)
                {
                    return;
                }

                PreloaderWindow.ShowThirdStep();
                string tempFolder = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name + " updating " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                string tempRarFile = Path.Combine(tempFolder, "Release.ZIP");
                client.DownloadFile(UpdateFilePath, tempRarFile);
                if (!File.Exists(tempRarFile))
                {
                    throw new Exception("Failed to download updating archive from FTP server.");
                }

                string unzipFolder = FileSystemHelper.GetRootDirectoryPath();
                ZipFile.ExtractToDirectory(tempRarFile, unzipFolder);
                if (!Directory.Exists(unzipFolder) || !Directory.GetFiles(unzipFolder).Any())
                {
                    throw new Exception("Failed to get unzipped files.");
                }

                string exeFileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(exeFileName))
                {
                    throw new Exception("Failed to get directory of current executing assembly.");
                }

                string updatingExePath = Path.Combine(unzipFolder, "UpdatingFiles", exeFileName);
                if (string.IsNullOrEmpty(updatingExePath) || !File.Exists(updatingExePath))
                {
                    throw new Exception("Failed to find updating .EXE file.");
                }

                string commandLine = "/update /left=" + PreloaderWindow.GetLeftPosition() + " /top=" + PreloaderWindow.GetTopPosition();
                Process.Start(updatingExePath, commandLine);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to check for updates. " + e.Message);
            }
        }

        private static void UpdateExecutableFile()
        {
            while (PreloaderWindow.CurrentWindow == null)
            {
                Thread.Sleep(100);
            }

            try
            {
                PreloaderWindow.ShowFourthStep();
                Process[] processesList = Process.GetProcesses();
                Process currentProcess = Process.GetCurrentProcess();

                List<Process> otherProcess = processesList.Where(p => p.ProcessName == currentProcess.ProcessName && p.Id != currentProcess.Id).ToList();
                foreach (Process process in otherProcess.Where(p => !p.HasExited))
                {
                    process.Kill();
                }

                string currentExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string currentExeFileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(currentExecutingPath) || string.IsNullOrEmpty(currentExeFileName))
                {
                    throw new Exception("Failed to get path or name of current executing assembly.");
                }

                string mainAssemblyPath = Path.GetDirectoryName(currentExecutingPath);
                if (string.IsNullOrEmpty(mainAssemblyPath))
                {
                    throw new Exception("Failed to get path to main application.");
                }

                FileSystemHelper.CopyDirectoryRecursively(currentExecutingPath, mainAssemblyPath);
                string mainExeFilePath = Path.Combine(mainAssemblyPath, currentExeFileName);
                if (string.IsNullOrEmpty(mainExeFilePath) || !File.Exists(mainExeFilePath))
                {
                    throw new Exception("Failed to find temporary folder deleting .EXE file.");
                }

                string commandLine = "/deletetemp /left=" + PreloaderWindow.GetLeftPosition() + " /top=" + PreloaderWindow.GetTopPosition();
                Process.Start(mainExeFilePath, commandLine);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to update executable file. " + e.Message, "Updating error", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void RestartFromMainFile()
        {
            while (PreloaderWindow.CurrentWindow == null)
            {
                Thread.Sleep(100);
            }

            try
            {
                PreloaderWindow.ShowFifthStep();
                Process[] processesList = Process.GetProcesses();
                Process currentProcess = Process.GetCurrentProcess();

                List<Process> otherProcess = processesList.Where(p => p.ProcessName == currentProcess.ProcessName && p.Id != currentProcess.Id).ToList();
                foreach (Process process in otherProcess.Where(p => !p.HasExited))
                {
                    process.Kill();
                }

                string currentExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(currentExecutingPath))
                {
                    throw new Exception("Failed to get path or name of current executing assembly.");
                }

                string updatingFolderPath = Path.Combine(currentExecutingPath, "UpdatingFiles");
                if (Directory.Exists(updatingFolderPath))
                {
                    FileSystemHelper.DeleteDirectorySafely(updatingFolderPath);
                }

                string[] tempFolders = Directory.GetDirectories(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name + " updating *");
                foreach (string tempFolder in tempFolders)
                {
                    FileSystemHelper.DeleteDirectorySafely(tempFolder);
                }

                string commandLine = "/left=" + PreloaderWindow.GetLeftPosition() + " /top=" + PreloaderWindow.GetTopPosition();
                Process.Start(Assembly.GetExecutingAssembly().Location, commandLine);
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to restart from main file. " + e.Message, "Updating error", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion Auxiliary things
    }
}