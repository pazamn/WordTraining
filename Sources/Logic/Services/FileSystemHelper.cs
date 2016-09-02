using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace WordTraining.Logic.Services
{
    public static class FileSystemHelper
    {
        public static string GetRootDirectoryPath()
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return string.Empty;
            }

            string directoryPath = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return string.Empty;
            }

            return directoryPath;
        }

        public static string GetAbsolutePath(string fileRelativePath, bool checkIfFileExists = true)
        {
            string directoryPath = GetRootDirectoryPath();
            if (string.IsNullOrEmpty(directoryPath))
            {
                return string.Empty;
            }

            string absolutePath = Path.Combine(directoryPath, fileRelativePath.Trim("\\/".ToCharArray())).Replace("/", "\\");
            if (string.IsNullOrEmpty(absolutePath))
            {
                return string.Empty;
            }

            if (checkIfFileExists && (!File.Exists(absolutePath) && !Directory.Exists(absolutePath)))
            {
                return string.Empty;
            }

            return absolutePath;
        }

        public static void CopyDirectoryRecursively(string sourcePath, string targetPath, bool overwriteFiles = true)
        {
            try
            {
                string[] directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
                string[] files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);

                foreach (string dirPath in directories)
                {
                    string targetDirectoryPath = dirPath.Replace(sourcePath, targetPath);
                    if (Directory.Exists(targetDirectoryPath))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(targetDirectoryPath);
                }
                
                foreach (string sourceFilePath in files)
                {
                    if (sourceFilePath.Contains("vshost"))
                    {
                        continue;
                    }

                    string targetFilePath = sourceFilePath.Replace(sourcePath, targetPath);
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                    }

                    File.Copy(sourceFilePath, targetFilePath, true);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to copy directory recursively. " + e.Message);
            }
        }

        public static void DeleteDirectorySafely(string path)
        {
            DateTime deletingStarted = DateTime.Now;
            while (true)
            {
                if (DateTime.Now - deletingStarted > TimeSpan.FromSeconds(10))
                {
                    return;
                }

                if (!Directory.Exists(path))
                {
                    return;
                }

                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}