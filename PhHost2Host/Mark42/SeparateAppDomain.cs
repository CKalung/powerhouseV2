using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Mark42
{
    public static class SeparateAppDomain
    {
        #region Private methods

        private static int _counter = 0;
        private static Dictionary<string, Tuple<AppDomain, object>> _domains = new Dictionary<string, Tuple<AppDomain, object>>();

        #endregion

        #region Public methods

        /// <summary>
        /// Creates instance of <typeparamref name="TInstance"/> in separate AppDomain. 
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="folderPath">Path to the plugin folder. Will be used for shadow copying.</param>
        /// <param name="args">Optional constructor parameters.</param>
        /// <returns></returns>
        /// <remarks>If instance with such <paramref name="folderPath"/> already exists - returns existing instance.</remarks>
        public static TInstance CreateInstance<TInstance>(string folderPath, params object[] args)
        {
            if (_domains.ContainsKey(folderPath))
            {
                return (TInstance)_domains[folderPath].Item2;
            }

            string executableFolderPath = folderPath;
            string executableFolderName = Path.GetFileName(executableFolderPath);
            string id = string.Format("{0}_{1}", executableFolderName, ++_counter);
            string shadowCopyFolderName = string.Format("{0}_ShadowCopy", id);
            string shadowCopyFolderPath = CreateShadowCopyFolder(shadowCopyFolderName);

			Console.WriteLine (" FolderPath = " + executableFolderName);

            var setup = new AppDomainSetup
            {
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = executableFolderPath,
                CachePath = shadowCopyFolderPath
            };

            string domainName = string.Format("{0}_AppDomain", id);
            var domain = AppDomain.CreateDomain(domainName, AppDomain.CurrentDomain.Evidence, setup);
            var typeToCreate = typeof(TInstance);
            var instance = (TInstance)domain.CreateInstanceAndUnwrap(
                                                                    typeToCreate.Assembly.FullName,
                                                                    typeToCreate.FullName,
                                                                    false,
                                                                    System.Reflection.BindingFlags.CreateInstance,
                                                                    null,
                                                                    args,
                                                                    CultureInfo.InvariantCulture,
                                                                    null);
            _domains.Add(folderPath, new Tuple<AppDomain, object>(domain, instance));

            return instance;
        }

        public static AppDomain Extract(string filePath)
        {
            if (!_domains.ContainsKey(filePath)) return null;

            var domain = _domains[filePath].Item1;
            _domains.Remove(filePath);
            return domain;
        }

        public static void Delete(string filePath)
        {
            if (_domains.ContainsKey(filePath))
            {
                var domain = _domains[filePath].Item1;
                _domains.Remove(filePath);
                AppDomain.Unload(domain);
            }
        }

        #endregion

        #region Private methods

        private static string CreateShadowCopyFolder(string shadowCopyFolderName)
        {
            string shadowCopyPath = Path.Combine(Path.GetTempPath(), shadowCopyFolderName);
            if (!Directory.Exists(shadowCopyPath))
            {
                var di = Directory.CreateDirectory(shadowCopyPath);
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            return shadowCopyPath;
        }

        #endregion
    }
}
