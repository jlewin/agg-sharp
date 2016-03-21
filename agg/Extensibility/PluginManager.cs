using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.Collections.Specialized;

namespace MatterHackers.Agg.Extensibility
{
	public class PluginManager
	{
		internal PluginManager()
		{
			var plugins = new List<IApplicationPlugin>();
			
			// Probing path
			string searchDirectory = Path.Combine(
								Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
								"Extensions");

			var pluginAssemblies = new List<string>(Directory.GetFiles(searchDirectory, "*.dll"));

			// HACK: Drop support for exe plugins - this very questionable -  why would anyone distribute a plugin as an 
			// executable and why would be want to support that scenario? It could be done - it's poor form and not appropriate
			pluginAssemblies.AddRange(Directory.GetFiles(searchDirectory, "*.exe"));

			Type pluginInterface = typeof(IApplicationPlugin);

			foreach (string assemblyPath in pluginAssemblies)
			{
				try
				{
					Assembly assembly = Assembly.LoadFile(assemblyPath);

					foreach (Type type in assembly.GetTypes().Where(t => t != null && t.IsClass && pluginInterface.IsAssignableFrom(t)))
					{
						if (!type.IsPublic)
						{
							// TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target?
							Trace.WriteLine("IApplicationPlugin exists but is not a Public Class: {0}", type.ToString());
							continue;
						}

						var instance = Activator.CreateInstance(type) as IApplicationPlugin;
						if (instance == null)
						{
							// TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target?
							Trace.WriteLine("Unable to create Plugin Instance: {0}", type.ToString());
							continue;
						}

						Console.WriteLine("Adding MC Plugin: " + instance.GetType().FullName);

						plugins.Add(instance);
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assemblyPath, ex.Message));
				}
			}

			this.Plugins = plugins;
		}

		public List<IApplicationPlugin> Plugins { get; }

		public IEnumerable<T> FromType<T>() where T : class, IApplicationPlugin
		{
			return Plugins.Where(p => p is T).Select(p => p as T);
		}

		public class MatterControlPluginItem
		{
			public string Name { get; set; }
			public string Url { get; set; }
			public string Version { get; set; }
			public DateTime ReleaseDate { get; set; }
		}

		private string dumpPath = @"C:\Data\Sources\MatterHackers\MatterControl\PluginRequestResults.json";

		public void GeneratePluginItems()
		{
			var source = new MatterControlPluginItem[]{
				new MatterControlPluginItem(){
					Name = "Test1",
					ReleaseDate = DateTime.Parse("12/1/2001"),
					Url = "http://something/1",
					Version = "1.2.3"
				},
				new MatterControlPluginItem(){
					Name = "Test2",
					ReleaseDate = DateTime.Parse("12/2/2001"),
					Url = "http://something/4",
					Version = "1.2.3"
				},
				new MatterControlPluginItem(){
					Name = "Test3",
					ReleaseDate = DateTime.Parse("12/3/2001"),
					Url = "http://something/2",
					Version = "1.0.0"
				}
			};

			File.WriteAllText(dumpPath, Newtonsoft.Json.JsonConvert.SerializeObject(source));
		}


		public void QueryPluginSource()
		{
			string sourceUrl = "http://someurl";

			WebClient client = new WebClient();

			// Build request keys
			NameValueCollection xxx = new NameValueCollection();
			xxx.Add("userToken", "xxx");

			// Perform request
			var results = client.UploadValues(sourceUrl, xxx);

			var pluginsText = File.ReadAllText(dumpPath);

			// Work with results
			var plugins = Newtonsoft.Json.JsonConvert.DeserializeObject<MatterControlPluginItem[]>(pluginsText);

			// Likely display results

			// or 

			// Compare the results looking for package upgrades

			// Display a notification that updates are available
		}
	}
}
