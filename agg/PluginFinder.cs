﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;

namespace MatterHackers.Agg
{
	public class PluginFinder<BaseClassToFind>
	{
		public List<BaseClassToFind> Plugins;

		public PluginFinder(string searchDirectory = null, IComparer<BaseClassToFind> sorter = null)
		{
#if __ANDROID__
			// Technique for loading directly form Android Assets (Requires you create and populate the Assets->StaticData->Plugins
			// folder with the actual plugins you want to load
			Plugins = LoadPluginsFromAssets();

#else
			string searchPath;
			if (searchDirectory == null)
			{
				searchPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			}
			else
			{
				searchPath = Path.GetFullPath(searchDirectory);
			}

			Plugins = FindAndAddPlugins(searchPath);
#endif

			if (sorter != null)
			{
				Plugins.Sort(sorter);
			}
		}

#if __ANDROID__
		private string[] pluginsInAssetsFolder = null;

		private byte[] LoadBytesFromStream(string assetsPath, Android.Content.Res.AssetManager assets)
		{
			byte[] bytes;
			using (var assetStream = assets.Open(assetsPath)){
				using (var memoryStream = new MemoryStream()){
					assetStream.CopyTo (memoryStream);
					bytes = memoryStream.ToArray();
				}
			}
			return bytes;
		}

		public List<BaseClassToFind> LoadPluginsFromAssets()
		{
			List<BaseClassToFind> factoryList = new List<BaseClassToFind>();

			var assets = Android.App.Application.Context.Assets;

			if(pluginsInAssetsFolder == null)
			{
				pluginsInAssetsFolder = assets.List("StaticData/Plugins");
			}

			List<Assembly> pluginAssemblies = new List<Assembly> ();
			string directory = Path.Combine("StaticData", "Plugins");

			// Iterate the Android Assets in the StaticData/Plugins directory
			foreach (string fileName in assets.List(directory))
			{
				if(Path.GetExtension(fileName) == ".dll")
				{
					try
					{
						string assemblyAssetPath = Path.Combine (directory, fileName);
						Byte[] bytes = LoadBytesFromStream(assemblyAssetPath, assets);

						Assembly assembly;
#if DEBUG
						// If symbols exist for the assembly, load both together to support debug breakpoints
						if(pluginsInAssetsFolder.Contains(fileName + ".mdb"))
						{
							byte[] symbolData = LoadBytesFromStream(assemblyAssetPath + ".mdb", assets);
							assembly = Assembly.Load(bytes, symbolData);
						}
						else
						{
							assembly = Assembly.Load(bytes);
						}
#else
						assembly = Assembly.Load(bytes);
#endif
						pluginAssemblies.Add(assembly);
					}
					// TODO: All of these exceptions need to be logged!
					catch (ReflectionTypeLoadException)
					{
					}
					catch (BadImageFormatException)
					{
					}
					catch (NotSupportedException)
					{
					}
				}
			}

			// Iterate plugin assemblies
			foreach (Assembly assembly in pluginAssemblies)
			{
				// Iterate each type
				foreach (Type type in assembly.GetTypes()) {
					if (type == null || !type.IsClass || !type.IsPublic) {
						continue;
					}

					// Add known/requested types to list
					if (type.BaseType == typeof(BaseClassToFind)) {
						factoryList.Add ((BaseClassToFind)Activator.CreateInstance (type));
					}
				}
			}

			return factoryList;
		}
#endif

		public List<BaseClassToFind> FindAndAddPlugins(string searchDirectory)
		{
			List<BaseClassToFind> factoryList = new List<BaseClassToFind>();
			if (Directory.Exists(searchDirectory))
			{
				//string[] files = Directory.GetFiles(searchDirectory, "*_HalFactory.dll");
				string[] dllFiles = Directory.GetFiles(searchDirectory, "*.dll");
				string[] exeFiles = Directory.GetFiles(searchDirectory, "*.exe");

				List<string> allFiles = new List<string>();
				allFiles.AddRange(dllFiles);
				allFiles.AddRange(exeFiles);
				string[] files = allFiles.ToArray();

				Type targetType = typeof(BaseClassToFind);

				foreach (string file in files)
				{
					try
					{
						Assembly assembly = Assembly.LoadFile(file);

						foreach (Type type in assembly.GetTypes())
						{
							if (type == null || !type.IsClass || !type.IsPublic)
							{
								continue;
							}

							string typeName = type.Name;

							if (targetType.IsInterface && targetType.IsAssignableFrom(type) ||
								type.BaseType == typeof(BaseClassToFind))
							{
								factoryList.Add((BaseClassToFind)Activator.CreateInstance(type));
							}
						}
					}
					catch (ReflectionTypeLoadException)
					{
					}
					catch (BadImageFormatException)
					{
					}
					catch (NotSupportedException)
					{
					}
				}
			}

			return factoryList;
		}
	}
}