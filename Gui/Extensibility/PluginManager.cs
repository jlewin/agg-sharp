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
        private static List<IApplicationPlugin> plugins = new List<IApplicationPlugin>();

        public readonly static PluginManager Instance = new PluginManager();

        public List<IApplicationPlugin> Plugins
        {
            get
            {
                return plugins;
            }
        }

		public IEnumerable<T> FromType<T>() where T : class, IApplicationPlugin
		{
			return Plugins.Where(p => p is T).Select(p => p as T);
		}

        static PluginManager()
        {
            // Probing path
            var searchDirectory = Path.Combine(
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

                    foreach (Type type in assembly.GetTypes().Where(t => pluginInterface.IsAssignableFrom(t)))
                    {
                        if (type == null || !type.IsClass || !type.IsPublic)
                        {
                            // TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target
                            Trace.WriteLine("IApplicationPlugin exists but is not a Public Class: {0}", type.ToString());
                            continue;
                        }

                        var instance = Activator.CreateInstance(type) as IApplicationPlugin;
                        if (instance == null)
                        {
                            // TODO: We need to be able to log this in a usable way - consider MatterControl terminal as output target
                            Trace.WriteLine("Unable to create Plugin Instance: {0}", type.ToString());
                            continue;
                        }

                        plugins.Add(instance);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assemblyPath, ex.Message));
                }
                catch (BadImageFormatException ex)
                {
                    Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assemblyPath, ex.Message));
                }
                catch (NotSupportedException ex)
                {
                    Trace.WriteLine(string.Format("An unexpected exception occurred while loading plugins: {0}\r\n{1}", assemblyPath, ex.Message));
                }
            }
        }


    }
}
