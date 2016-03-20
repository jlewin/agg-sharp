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

	// TODO: alternate names AggConfig, AggContext, AggPlatform
	// In all cases this is the compile time Agg config build by the host application
	public static class Configuration
	{
		// Takes a full typename string: i.e. "MyNamespace.MyType, MyAssembly"
		public static object LoadProviderFromAssembly(string typeString)
		{
			var type = Type.GetType(typeString);
			return (type == null) ? null : Activator.CreateInstance(type);
		}

		// Takes a full typename string: i.e. "MyNamespace.MyType, MyAssembly"
		public static T LoadProviderFromAssembly<T>(string typeString) where T : class
		{
			var type = Type.GetType(typeString);
			return (type == null) ? null : Activator.CreateInstance(type) as T;
		}

		public static ImageIOProvider ImageIO { get; }
		public static IFileDialogProvider FileDialogs { get; }
		public static IFrostedSerialPortFactory FrostedSerialPortFactory { get; }

		// TODO: This extra namespace for OSInformation is unnecessary. The one property that is on this item should be propagated to the Configuration class
		public static OsInformationProvider OsInformation { get; }

		static Configuration()
		{
			string typeString;

			// Initialize the OsInformation Provider
			typeString = "MatterControl.Agg.PlatformAbstract.OsInformationWindowsPlugin, agg_platform_win32, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			OsInformation = LoadProviderFromAssembly<OsInformationProvider>(typeString);
			if (OsInformation == null)
			{
				throw new Exception(string.Format("Unable to load the OSInfo provider"));
			}

			// Initialize the ImageIO Provider
			typeString = "MatterHackers.Agg.Image.ImageIOWindowsPlugin, agg_platform_win32";
			ImageIO = LoadProviderFromAssembly<ImageIOProvider>(typeString);
			if (ImageIO == null)
			{
				throw new Exception(string.Format("Unable to load the ImageIO provider"));
			}

			// Initialize the FileDialog Provider
			typeString = "MatterHackers.Agg.PlatformAbstract.FileDialogPlugin, agg_platform_win32, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			FileDialogs = LoadProviderFromAssembly<IFileDialogProvider>(typeString);
			if (FileDialogs == null)
			{
				throw new Exception(string.Format("Unable to load the File Dialog provider"));
			}
		}
	}
}
