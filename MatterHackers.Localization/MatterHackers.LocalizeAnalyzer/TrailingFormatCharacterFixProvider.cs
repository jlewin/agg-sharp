using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatterHackers.LocalizeAnalyzer
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingFormatCharacterFixProvider)), Shared]
	public class TrailingFormatCharacterFixProvider : CodeFixProvider
	{
		private const string title = "Extract trailing formatter";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(TrailingFormatCharacterAnalyzer.DiagnosticId); }
		}

		public sealed override FixAllProvider GetFixAllProvider()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the type declaration identified by the diagnostic.
			var invocationExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
				CodeAction.Create(
					title: title,
					createChangedDocument: c => ExtractTrailingColon(context.Document, invocationExpression, c),
					equivalenceKey: title),
				diagnostic);
		}


		private async Task<Document> ExtractTrailingColon(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
		{
			SyntaxToken? identifier = TrailingFormatCharacterAnalyzer.GetMethodCallIdentifier(invocationExpression);

			if (identifier.Value.ValueText == "Localize"
				&& invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				if (memberAccess.Expression is LiteralExpressionSyntax literalExpression)
				{
					//Attach a syntax annotation to the class declaration
					var syntaxAnnotation = new SyntaxAnnotation();

					var sanitizedText = literalExpression.Token.ValueText.TrimEnd(':');

					var newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(sanitizedText));
					var literalWithAnnotation = newLiteral.WithAdditionalAnnotations(syntaxAnnotation);

					var root = await document.GetSyntaxRootAsync();

					var treeAfterReplace = root.ReplaceNode(literalExpression, literalWithAnnotation);

					// Use the annotation on our original node to find the new item
					var injectedItem = treeAfterReplace.DescendantNodes().First(n => n.HasAnnotation(syntaxAnnotation));

					var targetParent = injectedItem.Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

					var statementWithTrailingColon = SyntaxFactory.BinaryExpression(
						SyntaxKind.AddExpression,
						targetParent,
						SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(":")));

					var newResult = treeAfterReplace.ReplaceNode(targetParent, statementWithTrailingColon);

					return document.WithSyntaxRoot(newResult);
				}
			}

			return document;
		}
	}
}