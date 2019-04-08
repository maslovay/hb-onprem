using System;
using System.IO;

namespace HBLib.Utils
{
    public class OS
    {
        public static bool IsFileLocked(string fn)
        {
            FileStream stream = null;
            var file = new FileInfo(fn);
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        internal static bool PathExists(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }

        public static bool SafeDelete(string path, double timeout = 0.0)
        {
            // todo: make properly
            try
            {
                if (!PathExists(path)) return false;

                bool isDir;
                try
                {
                    var attr = File.GetAttributes(path);
                    isDir = attr.HasFlag(FileAttributes.Directory);
                }
                catch
                {
                    return false;
                }

                // get the file attributes for file or directory
                if (!isDir)
                {
                    var fn = path;
                    var dt0 = DateTime.Now;
                    var isLocked = true;
                    while (isLocked)
                    {
                        isLocked = IsFileLocked(fn);
                        if ((DateTime.Now - dt0).TotalSeconds > timeout) return false;
                    }

                    try
                    {
                        File.Delete(fn);
                        return true;
                    }
                    catch (IOException e)
                    {
                        return false;
                    }
                }

                try
                {
                    Directory.Delete(path);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string[] GetFiles(string dir, string pattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.GetFiles(dir, pattern, searchOption);
        }
    }
}