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
		public static IStaticData StaticData { get; }
		public static PlatformConfig Config { get; }

		// TODO: This extra namespace for OSInformation is unnecessary. The one property that is on this item should be propagated to the Configuration class
		public static OsInformationProvider OsInformation { get; }

		static Configuration()
		{
			if(File.Exists("AggPlatform.json"))
			{
				// Use the config file from the file system if it exists
				Config = Newtonsoft.Json.JsonConvert.DeserializeObject<PlatformConfig>(File.ReadAllText("AggPlatform.json"));
			}
			else
			{
				// Use the default config embedded in the assembly if the file system override does not exist
				using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("MatterHackers.Agg.Extensibility.DefaultPlatformConfig.json")))
				{
					Config = Newtonsoft.Json.JsonConvert.DeserializeObject<PlatformConfig>(reader.ReadToEnd());
				}
			}

			// OsInformation Provider
			OsInformation = LoadProviderFromAssembly<OsInformationProvider>(Config.Providers.OsInformationProvider);
			if (OsInformation == null)
			{
				throw new Exception(string.Format("Unable to load the OsInformation provider"));
			}

			// ImageIO Provider
			ImageIO = LoadProviderFromAssembly<ImageIOProvider>(Config.Providers.ImageIOProvider);
			if (ImageIO == null)
			{
				throw new Exception(string.Format("Unable to load the ImageIO provider"));
			}

			// FileDialog Provider
			FileDialogs = LoadProviderFromAssembly<IFileDialogProvider>(Config.Providers.DialogProvider);
			if (FileDialogs == null)
			{
				throw new Exception(string.Format("Unable to load the File Dialog provider"));
			}

			// FileDialog Provider
			StaticData = LoadProviderFromAssembly<IStaticData>(Config.Providers.StaticDataProvider);
			if (StaticData == null)
			{
				throw new Exception(string.Format("Unable to load the StaticData provider"));
			}
		}

		public class PlatformConfig
		{
			public ProviderSettings Providers { get; set; }
			public SliceEngineSettings SliceEngine { get; set; } = new SliceEngineSettings();
		}

		public class ProviderSettings
		{
			public string OsInformationProvider { get; set; }
			public string DialogProvider { get; set; }
			public string ImageIOProvider { get; set; }
			public string StaticDataProvider { get; set; }
		}

		public class SliceEngineSettings
		{
			public bool RunInProcess { get; set; } = false;
		}
	}
}
