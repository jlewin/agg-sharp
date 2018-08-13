using Microsoft.VisualStudio.DebuggerVisualizers;
using MSClipperLib;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

[assembly: DebuggerVisualizer(
	typeof(AggVisualizers.MSIntPointPathVisualizer),
	typeof(VisualizerObjectSource),
	Target = typeof(List<List<IntPoint>>),
	Description = "Agg Polygons Visualizer")]

namespace AggVisualizers
{
	using Polygons = List<List<IntPoint>>;

	public class MSIntPointPathVisualizer : DialogDebuggerVisualizer
	{
		protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
		{
			if (windowService == null)
			{
				throw new ArgumentNullException("windowService");
			}

			//Debugger.Launch();

			if (objectProvider == null)
			{
				throw new ArgumentNullException("objectProvider");
			}

			var activeObject = objectProvider.GetObject();

			var type = activeObject.GetType();

			if (activeObject is Polygons activePolygons)
			{
				WriteSvg(new[] { activePolygons.CreatePolygonListSvg() });
			}
			else if (activeObject is IEnumerable<Polygons> enumerablePolygons)
			{
				WriteSvg(enumerablePolygons.Select(polygons => polygons.CreatePolygonListSvg()));
			}
		}

		private static void WriteSvg(IEnumerable<VisualizerPolygonsHelper.ItemResult> results)
		{
			int maxWidth = results.Max(r => r.Width);
			int maxHeight = results.Max(r => r.Height);

			string svgFile = Path.ChangeExtension(Path.GetTempFileName(), ".svg");

			using (var fileStream = new StreamWriter(svgFile))
			using (var stream = new IndentedTextWriter(fileStream, "  "))
			{
				stream.WriteLine("<svg xmlns='http://www.w3.org/2000/svg' version='1.1'>");

				stream.Indent++;
				stream.WriteLine("<svg x='10' y='10' width='{0}' height='{1}' overflow='visible'>", maxWidth, maxHeight);
				
				VisualizerPolygonsHelper.CreateGrid(stream, maxWidth, maxHeight);

				stream.Indent++;
				foreach (var item in results)
				{
					stream.WriteLine(item.SvgText);
				}
				stream.Indent--;

				stream.WriteLine("</svg>");
				stream.Indent--;

				stream.WriteLine("</svg>");
			}

			Process.Start(svgFile);
		}
	}
}
