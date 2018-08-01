using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using MatterHackers.LocalizeAnalyzer;

namespace MatterHackers.LocalizeAnalyzer.Test
{
	[TestClass]
	public class UnitTest : CodeFixVerifier
	{
		//No diagnostics expected to show up
		[TestMethod]
		public void TestMethod1()
		{
			var test = @"";

			VerifyCSharpDiagnostic(test);
		}

		//Diagnostic and CodeFix both triggered and checked for
		[TestMethod]
		public void TestMethod2()
		{
			var test = @"
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Diagnostics;

	namespace ConsoleApplication1
	{
		using MatterHackers.Localizations;

		class TypeName
		{
			string test = ""With Trailing Colon:"".Localize();
		}
	}

	namespace MatterHackers.Localizations
	{
		// Dummy localize implementation
		public static class StringExtensions
		{
			public static string Localize(this string x) => x;
		}
	}
";
			var expected = new DiagnosticResult
			{
				Id = "ExtractTrailingColons",
				Message = "Trailing format characters",
				Severity = DiagnosticSeverity.Warning,
				Locations =
					new[] {
							new DiagnosticResultLocation("Test0.cs", 15, 18)
						}
			};

			VerifyCSharpDiagnostic(test, expected);

			VerifyCSharpFix(
				test,
				test.Replace(
					@":"".Localize()",
					@""".Localize() + "":"""));
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new TrailingFormatCharacterFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TrailingFormatCharacterAnalyzer();
		}
	}
}
