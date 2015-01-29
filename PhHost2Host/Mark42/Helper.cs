using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mark42
{
    public static class Helper
    {
        #region File System

        /// <summary>
        /// Returns fully qualified file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetAbsolutePath(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (Path.IsPathRooted(fileName))
            {
                return fileName;
            }

            string baseDirectory = GetBaseDirectory();
            fileName = Path.Combine(baseDirectory, fileName);

            return fileName;
        }

        /// <summary>
        /// Returns the base directory of the current running assembly.
        /// </summary>
        /// <returns></returns>
        public static string GetBaseDirectory()
        {
            string baseDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            return baseDirectory;
        }

        #endregion
    }
}
