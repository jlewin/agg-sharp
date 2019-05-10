using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MatterHackers.Agg.Tests
{
	[TestFixture]
	public class IVertexSourceTests
	{


		[Test]
		public void CharacterBoundsTest2()
		{
			var typeface = new StyledTypeFace(LiberationSansFont.Instance, 12);
			typeface.GetImageForCharacter(' ', 0, 0, Color.Blue);
		}


		[Test]
		public void VerticiesAreEqualTest()
		{
			int fontSize = 12;

			var typeface = new StyledTypeFace(LiberationSansFont.Instance, fontSize);

			string filename = $"{nameof(LiberationSansFont)}-{fontSize}.json";

			string testDataPath = TestContext.CurrentContext.ResolveProjectPath(4, "Tests", "TestData", filename);

			IVertexSource vertexSource = typeface.GetGlyphForCharacter('@');

			// Validates the .Vertices interface results matches classic vertex(x,y) results
			var vertices1 = vertexSource.Vertices().ToList();
			var vertices2 = new List<VertexData>();

			ShapePath.FlagsAndCommand curCommand;

			do
			{
				curCommand = vertexSource.vertex(out double x, out double y);
				vertices2.Add(new VertexData(curCommand, x, y));
			} while (curCommand != ShapePath.FlagsAndCommand.Stop);

			Assert.AreEqual(vertices1.Count(), vertices2.Count(), "Result counts should match");

			for (int i = 0; i < vertices1.Count(); i++)
			{
				var a = vertices1[i];
				var b = vertices2[i];

				Assert.AreEqual(a.command, b.command, "Command types should match");
				Assert.AreEqual(a.position, b.position, "Positions should match");
			}
		}

		[Test]
		public void CharacterBoundsTest()
		{
			// Validates character bounds computation from IVertexSource
			char[] sampleCharacters = "@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz{}[]| !\"#$%&?'()*+,-./0123456789".ToCharArray();

			int fontSize = 12;

			var typeface = new StyledTypeFace(LiberationSansFont.Instance, fontSize);

			string filename = $"{nameof(LiberationSansFont)}-{fontSize}.json";

			string testDataPath = TestContext.CurrentContext.ResolveProjectPath(4, "Tests", "TestData", filename);

			// Project sample string characters to dictionary with character bounds
			var characterBounds = sampleCharacters.ToDictionary(
				c => c,
				c => GetCharacterBounds(c, typeface));

			var jsonSettings = new JsonSerializerSettings()
			{
				Converters = new List<JsonConverter>() { new FlatRectangleDoubleConverter() }
			};

			// Update the control data
			if (false)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(testDataPath));

				File.WriteAllText(
					testDataPath,
					JsonConvert.SerializeObject(characterBounds,
					jsonSettings));
			}

			// Load sample data
			string json = File.ReadAllText(testDataPath);
			var controlData = JsonConvert.DeserializeObject<Dictionary<char, RectangleDouble>>(json, jsonSettings);

			// Validate each character against previously computed control data
			foreach (var kvp in characterBounds)
			{
				Assert.IsTrue(controlData.ContainsKey(kvp.Key), "Expected key not found: " + kvp.Key);

				RectangleDouble actual = kvp.Value;
				RectangleDouble expected = controlData[kvp.Key];

				Assert.AreEqual(expected.Left, actual.Left, 0.001, "Bounds Left differ");
				Assert.AreEqual(expected.Bottom, actual.Bottom, 0.001, "Bounds Bottom differ");
				Assert.AreEqual(expected.Right, actual.Right, 0.001, "Bounds Right differ");
				Assert.AreEqual(expected.Top, actual.Top, 0.001, "Bounds Top differ");

				Assert.AreEqual(expected, actual);
			}
		}

		// Behavior which relies on classic IVertexSource.vertex iteration
		private static RectangleDouble GetCharacterBounds(char character, StyledTypeFace typeface)
		{
			IVertexSource glyphForCharacter = typeface.GetGlyphForCharacter(character, 1);

			glyphForCharacter.rewind(0);

			ShapePath.FlagsAndCommand curCommand = glyphForCharacter.vertex(out double x, out double y);

			var bounds = new RectangleDouble(x, y, x, y);

			Debug.WriteLine("Init: " + bounds);

			// Reads up to the first Stop command - ignoring any further commands
			while (curCommand != ShapePath.FlagsAndCommand.Stop)
			{
				Debug.WriteLine("Expand: " + new Vector2(x, y) + curCommand);

				bounds.ExpandToInclude(x, y);
				curCommand = glyphForCharacter.vertex(out x, out y);
			}
			Debug.WriteLine("\r\n--------5555555555555555----------");




			var bounds2 = GetCharacterBounds2(character , typeface);

			Console.WriteLine("\r\n------------------");

			return bounds2;
		}

		private static RectangleDouble GetCharacterBounds2(char character, StyledTypeFace typeface)
		{
			var bounds = typeface.GetGlyphForCharacter(character).GetBounds();

			if (bounds == RectangleDouble.ZeroIntersection)
			{
				bounds = default(RectangleDouble);
			}

			return bounds;
		}

		private class FlatRectangleDoubleConverter : JsonConverter
		{
			public FlatRectangleDoubleConverter()
			{
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				if (value is RectangleDouble rect)
				{
					serializer.Serialize(writer, new[] { rect.Left, rect.Bottom, rect.Right, rect.Top });
				}
				else
				{
					JArray.FromObject(value).WriteTo(writer);
				}
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.StartArray)
				{
					JToken token = JToken.Load(reader);
					var rect = token.ToObject<double[]>();

					return new RectangleDouble(rect[0], rect[1], rect[2], rect[3]);
				}

				return existingValue;
			}

			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(RectangleDouble)
					|| objectType == typeof(double[]);
			}
		}
	}
}