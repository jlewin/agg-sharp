using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

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

	}
}
