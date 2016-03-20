using MatterHackers.Agg.PlatformAbstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MatterHackers.Agg.PlatformAbstract
{
    public enum OSType { Unknown, Windows, Mac, X11, Other, Android };

    public static class Configuration
    {
        // Takes a full typename string: i.e. "MyNamespace.MyType, MyAssembly"
        public static object LoadProviderFromAssembly(string typeString)
        {
            var type = Type.GetType(typeString);
            return (type == null) ? null : Activator.CreateInstance(type);
        }

        public static ImageIOProvider ImageIO { get; private set; }
        public static OsInformationProvider OsInformation { get; private set; }
        public static FileDialogProvider FileDialogs { get; private set; }
		public static IFrostedSerialPortFactory FrostedSerialPortFactory { get; private set; }

		static Configuration()
        {
			string typeString;

			// Initialize the OsInformation Provider
			typeString = "MatterControl.Agg.PlatformAbstract.OsInformationWindowsPlugin, agg_platform_win32, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            OsInformation = LoadProviderFromAssembly(typeString) as OsInformationProvider;
            if (OsInformation == null)
            {
                throw new Exception(string.Format("Unable to load the OSInfo provider"));
            }

            // Initialize the ImageIO Provider
            typeString = "MatterHackers.Agg.Image.ImageIOWindowsPlugin, agg_platform_win32";
            ImageIO = LoadProviderFromAssembly(typeString) as ImageIOProvider;
            if (ImageIO == null)
            {
                throw new Exception(string.Format("Unable to load the ImageIO provider"));
            }

            // Initialize the FileDialog Provider
            typeString = "MatterHackers.Agg.PlatformAbstract.FileDialogPlugin, agg_platform_win32, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            FileDialogs = LoadProviderFromAssembly(typeString) as FileDialogProvider;
            if (FileDialogs == null)
            {
                throw new Exception(string.Format("Unable to load the File Dialog provider"));
            }

              
        }

    }

    //public class PluginFinder<BaseClassToFind>
    //{

    //    public List<BaseClassToFind> Plugins;

    //    // "MyAssembly.MyType, MyAssembly"
    //    public static object LoadTypeFromAssembly(string typeString)
    //    {
    //        return Activator.CreateInstance(Type.GetType(typeString));
    //    }

    //    public PluginFinder(string searchDirectory = null, IComparer<BaseClassToFind> sorter = null)
    //    {
    //        string searchPath;
    //        if (searchDirectory == null)
    //        {
    //            searchPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    //            //searchPath = Path.Combine(searchPath, "Plugins");
    //        }
    //        else
    //        {
    //            searchPath = Path.GetFullPath(searchDirectory);
    //        }

    //        Plugins = FindAndAddPlugins(searchPath);
    //        if (sorter != null)
    //        {
    //            Plugins.Sort(sorter);
    //        }
    //    }

    //    public List<BaseClassToFind> FindAndAddPlugins(string searchDirectory)
    //    {
    //        List<BaseClassToFind> factoryList = new List<BaseClassToFind>();
    //        if (Directory.Exists(searchDirectory))
    //        {
    //            //string[] files = Directory.GetFiles(searchDirectory, "*_HalFactory.dll");
    //            string[] dllFiles = Directory.GetFiles(searchDirectory, "*.dll");
    //            string[] exeFiles = Directory.GetFiles(searchDirectory, "*.exe");

    //            List<string> allFiles = new List<string>();
    //            allFiles.AddRange(dllFiles);
    //            allFiles.AddRange(exeFiles);
    //            string[] files = allFiles.ToArray();

    //            foreach (string file in files)
    //            {
    //                try
    //                {
    //                    Assembly assembly = Assembly.LoadFile(file);

    //                    foreach (Type type in assembly.GetTypes())
    //                    {
    //                        if (type == null || !type.IsClass || !type.IsPublic)
    //                        {
    //                            continue;
    //                        }

    //                        if (type.BaseType == typeof(BaseClassToFind))
    //                        {
    //                            factoryList.Add((BaseClassToFind)Activator.CreateInstance(type));
    //                        }
    //                    }
    //                }
    //                catch (ReflectionTypeLoadException)
    //                {
    //                }
    //                catch (BadImageFormatException)
    //                {
    //                }
    //                catch (NotSupportedException)
    //                {
    //                }
    //            }
    //        }

    //        return factoryList;
    //    }
    //}

}
