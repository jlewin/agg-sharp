using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.DebuggerVisualizers;
using MSClipperLib;

[assembly: DebuggerVisualizer(
	typeof(AggVisualizers.IntPointPathVisualizer),
	typeof(VisualizerObjectSource),
	Target = typeof(List<List<IntPoint>>),
	Description = "Agg Polygons Visualizer")]

namespace AggVisualizers
{
	using Polygons = List<List<IntPoint>>;
	using Polygon = List<IntPoint>;

	public static class VisualizerPolygonsHelper
	{
		public static void CreateGrid(TextWriter stream, int width, int height)
		{
			 stream.WriteLine(@"<svg overflow='visible'>
  <defs>
    <pattern id='smallGrid' width='1' height='1' patternUnits='userSpaceOnUse'>
      <path d='M 1 0 L 0 0 0 1' fill='none' stroke='#ccc' stroke-width='0.2' />
    </pattern>
    <pattern id='grid' width='10' height='10' patternUnits='userSpaceOnUse'>
      <rect width='10' height='10' fill='url(#smallGrid)' />
      <path d='M 10 0 L 0 0 0 10' fill='none' stroke='#ccc' stroke-width='0.4' />
    </pattern>
    <marker id='arrowS' markerWidth='15' markerHeight='15' refX='1.4' refY='3' orient='auto' markerUnits='strokeWidth' viewBox='0 0 20 20'>
      <path d='M0,3 L7,6 L7,0 z' />
    </marker>
    <marker id='arrowE' markerWidth='15' markerHeight='15' refX='5.6' refY='3' orient='auto' markerUnits='strokeWidth' viewBox='0 0 20 20'>
      <path d='M0,0 L0,6 L7,3 z' />
    </marker>
  </defs>
  <rect width='100%' height='100%' fill='url(#grid)' opacity='0.5' transform='translate(0, 0)' />");

				// Grid markers
				for (var i = 0; i <= width / 10; i++)
				{
					stream.WriteLine("  <text x='{0}' y='-1' style='fill: #bbb; font-size: 0.13em;'>{1}</text>", i * 10 - 1, i * 10);
				}

				for (var i = 1; i <= height / 10; i++)
				{
					stream.WriteLine("  <text x='-4.5' y='{0}' style='fill: #bbb; font-size: 0.13em;'>{1}</text>", i * 10, i * 10);
				}

				stream.WriteLine("</svg>");
		}


		public class ItemResult
		{
			public string SvgText { get; set; }
			public int Width { get; set; }
			public int Height { get; set; }
		}

		public static ItemResult CreatePolygonListSvg(this Polygons polygons)
		{
			double scaleDenominator = 150;
			IntRect bounds = Clipper.GetBounds(polygons);
			long temp = bounds.maxY;
			bounds.maxY = bounds.minY;
			bounds.minY = temp;

			var size = new IntPoint(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
			double scale = Math.Max(size.X, size.Y) / scaleDenominator;

			var scaledWidth = (int)Math.Abs(size.X / scale);
			scaledWidth += 10 - scaledWidth % 10;

			var scaledHeight = (int)Math.Abs(size.Y / scale);
			scaledHeight += 10 - scaledHeight % 10;

			var sb = new StringBuilder();

			var itemResult = new ItemResult()
			{
				Width = scaledWidth,
				Height = scaledHeight
			};

			using (var stream = new StringWriter(sb))
			{
				stream.WriteLine(@"<svg overflow='visible' style='opacity: 0.95'>
  <marker id='MidMarker' viewBox='0 0 5 5' refX='2.5' refY='2.5' markerUnits='strokeWidth' markerWidth='5' markerHeight='5' stroke='lightblue' stroke-width='.5' fill='none' orient='auto'>
    <path d='M 0 0 L 5 2.5 M 0 5 L 5 2.5'/>
  </marker>

  <g fill-rule='evenodd' style='fill: gray; stroke:#333; stroke-width:1'>");

				stream.Write("  <path marker-mid='url(#MidMarker)' d='");

				for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++)
				{
					Polygon polygon = polygons[polygonIndex];
					for (int intPointIndex = 0; intPointIndex < polygon.Count; intPointIndex++)
					{
						if (intPointIndex == 0)
						{
							stream.Write("M");
						}
						else
						{
							stream.Write("L");
						}
						stream.Write("{0},{1} ", (double)(polygon[intPointIndex].X - bounds.minX) / scale, (double)(polygon[intPointIndex].Y - bounds.maxY) / scale);
					}
					stream.Write("Z");
				}

				stream.WriteLine("'/>");
				stream.WriteLine("  </g>");

				for (int openPolygonIndex = 0; openPolygonIndex < polygons.Count; openPolygonIndex++)
				{
					Polygon openPolygon = polygons[openPolygonIndex];

					if (openPolygon.Count < 1)
					{
						continue;
					}

					stream.Write("  <polyline marker-mid='url(#MidMarker)' points='");

					for (int n = 0; n < openPolygon.Count; n++)
					{
						stream.Write("{0},{1} ", (double)(openPolygon[n].X - bounds.minX) / scale, (double)(openPolygon[n].Y - bounds.maxY) / scale);
					}
					stream.WriteLine("' style='fill: none; stroke:red; stroke-width:0.3' />");
				}

				stream.WriteLine("</svg>");

				itemResult.SvgText = sb.ToString();
			}

			return itemResult;
		}
	}
}
