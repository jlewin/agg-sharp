using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MatterHackers.LocalizeAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TrailingFormatCharacterAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "ExtractTrailingColons";
		public const string ComplicatedUseID = "ComplicatedUseID";

		private static readonly string Title = "Extract trailing formatter";
		private static readonly string MessageFormat = "Trailing format characters";
		private static readonly string Description = "Extract and concatenate trailing format characters";
		private const string Category = "Localization";

		private static DiagnosticDescriptor MoveColonRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

		private static DiagnosticDescriptor ComplicatedLocalizeUse = new DiagnosticDescriptor(ComplicatedUseID, "Requires review", "Something {0} - {1}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "Complicated Localize use");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new[] { MoveColonRule, ComplicatedLocalizeUse });

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext analysisContext)
		{
			var invocation = (InvocationExpressionSyntax)analysisContext.Node;

			var identifier = GetMethodCallIdentifier(invocation);
			if (identifier == null)
			{
				return;
			}

			if (identifier.Value.ValueText == "Localize")
			{
				if (invocation.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault()?.Modifiers.Any(SyntaxKind.StaticKeyword) == true)
				{
					//var textInvokedOn = literalExpression.Token.ValueText;
					analysisContext.ReportDiagnostic(
						Diagnostic.Create(MoveColonRule, invocation.GetLocation()));
				}

				// Validate that discovered Localize call is on the type we expect and is an extension method
				var memberSymbol = analysisContext.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
				var typeInfo = memberSymbol?.ReducedFrom?.ToString() ?? "";

				if (memberSymbol?.MethodKind == MethodKind.ReducedExtension // IsExtensionMethod
					&& typeInfo.StartsWith("MatterHackers.Localizations.StringExtensions.Localize")
					&& invocation.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					if (memberAccess.Expression is LiteralExpressionSyntax literalExpression)
					{
						if (literalExpression.Token.ValueText.Trim().EndsWith(":"))
						{
							//var textInvokedOn = literalExpression.Token.ValueText;
							analysisContext.ReportDiagnostic(
								Diagnostic.Create(MoveColonRule, invocation.GetLocation()));
						}
					}
					else
					{
						analysisContext.ReportDiagnostic(
							Diagnostic.Create(ComplicatedLocalizeUse, invocation.GetLocation()));
					}
				}
			}
		}

		internal static SyntaxToken? GetMethodCallIdentifier(InvocationExpressionSyntax invocation)
		{
			var directMethodCall = invocation.Expression as IdentifierNameSyntax;
			if (directMethodCall != null)
			{
				return directMethodCall.Identifier;
			}

			var memberAccessCall = invocation.Expression as MemberAccessExpressionSyntax;
			if (memberAccessCall != null)
			{
				return memberAccessCall.Name.Identifier;
			}

			return null;
		}
	}
}
