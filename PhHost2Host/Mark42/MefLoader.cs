using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mark42
{
    /// <summary>
    /// Discovers and instantiates all classes from specified folder that implements specific MEF contract.
    /// </summary>
    public class MefLoader : MarshalByRefObject, IDisposable
    {
        #region Private members

        private CompositionContainer _container;

        #endregion

        #region Constructor

        public MefLoader(string folderPath)
        {
            FolderPath = folderPath;
        }

        #endregion

        #region Properties

        public string FolderPath { get; private set; }

        #endregion

        #region Public methods

		string _searchPattern = "*.dll";

		public string SearchPattern{
			set{ _searchPattern = value; }
		}

        public List<TResult> Load<TResult>()
            where TResult : class
        {
            return Load<TResult, object>(null);
        }

        public List<TResult> Load<TResult, TArg1>(TArg1 arg1)
            where TResult : class
        {
            return Load<TResult, TArg1, object>(arg1, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2>(TArg1 arg1, TArg2 arg2)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, object>(arg1, arg2, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, object>(arg1, arg2, arg3, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, object>(arg1, arg2, arg3, arg4, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, object>(arg1, arg2, arg3, arg4, arg5, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, object>(arg1, arg2, arg3, arg4, arg5, arg6, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
           where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15)
          where TResult : class
        {
            return Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, object>(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, null);
        }

        public List<TResult> Load<TResult, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16)
            where TResult : class
        {
            List<TResult> instances = new List<TResult>();
            try
            {
                if (_container != null)
                {
                    (_container.Catalog as DirectoryCatalog).Refresh();
                    instances = _container.GetExportedValues<TResult>().ToList();
                }
                else
                {
					Console.WriteLine ("GEBUG 3");
					//DirectoryCatalog catalog = new DirectoryCatalog(FolderPath, _searchPattern);
					DirectoryCatalog catalog = new DirectoryCatalog(FolderPath);
//					foreach(string fil in catalog.LoadedFiles){
//						Console.WriteLine ("GEBUG 3 " + fil);
//					}
                    _container = new CompositionContainer(catalog);
					Console.WriteLine ("GEBUG 3 " + _container.Providers.Count);
                    if (arg1 != null) _container.ComposeExportedValue<TArg1>(arg1);
                    if (arg2 != null) _container.ComposeExportedValue<TArg2>(arg2);
                    if (arg3 != null) _container.ComposeExportedValue<TArg3>(arg3);
                    if (arg4 != null) _container.ComposeExportedValue<TArg4>(arg4);
                    if (arg5 != null) _container.ComposeExportedValue<TArg5>(arg5);
                    if (arg6 != null) _container.ComposeExportedValue<TArg6>(arg6);
                    if (arg7 != null) _container.ComposeExportedValue<TArg7>(arg7);
                    if (arg8 != null) _container.ComposeExportedValue<TArg8>(arg8);
                    if (arg9 != null) _container.ComposeExportedValue<TArg9>(arg9);
                    if (arg10 != null) _container.ComposeExportedValue<TArg10>(arg10);
                    if (arg11 != null) _container.ComposeExportedValue<TArg11>(arg11);
                    if (arg12 != null) _container.ComposeExportedValue<TArg12>(arg12);
                    if (arg13 != null) _container.ComposeExportedValue<TArg13>(arg13);
                    if (arg14 != null) _container.ComposeExportedValue<TArg14>(arg14);
                    if (arg15 != null) _container.ComposeExportedValue<TArg15>(arg15);
                    if (arg16 != null) _container.ComposeExportedValue<TArg16>(arg16);
					Console.WriteLine ("GEBUG 4");
                    instances = _container.GetExportedValues<TResult>().ToList();
					Console.WriteLine ("GEBUG 5");
                }

                return instances;
            }
            catch (ImportCardinalityMismatchException)//when no contract implementation
            {
				Console.WriteLine ("GEBUG 6");
                return instances;
            }
            catch (ReflectionTypeLoadException ex)//when wrong contract implementation
            {
				Console.WriteLine ("GEBUG 7 " + ex.Message);
                return instances;
            }
            catch (FileNotFoundException)//
            {
				Console.WriteLine ("GEBUG 8");
                return instances;
            }
            catch (Exception ex)
            {
                throw new FormatException("Load of MEF file failed.", ex);
            }
        }

        //public TResult Load<TResult, TArg1>(TArg1 arg1)
        //    where TResult : class
        //    where TArg1 : class
        //{
        //    try
        //    {
        //        if (_container != null)
        //        {
        //            return _container.GetExportedValue<TResult>();
        //        }

        //        Assembly assembly = Assembly.LoadFrom(FolderPath);

        //        AssemblyCatalog catalog = new AssemblyCatalog(assembly);
        //        _container = new CompositionContainer(catalog);
        //        if (arg1 != null) _container.ComposeExportedValue<TArg1>(arg1);
        //        TResult instance = _container.GetExportedValue<TResult>();

        //        return instance;
        //    }
        //    catch (ImportCardinalityMismatchException)//when no contract implementation
        //    {
        //        return default(TResult);
        //    }
        //    catch (ReflectionTypeLoadException)//when wrong contract implementation
        //    {
        //        return default(TResult);
        //    }
        //    catch (FileNotFoundException)//
        //    {
        //        return default(TResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new FormatException("Load of MEF file failed.", ex);
        //    }
        //}

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_container != null)
            {
                _container.Dispose();
            }
        }

        #endregion
    }
}
