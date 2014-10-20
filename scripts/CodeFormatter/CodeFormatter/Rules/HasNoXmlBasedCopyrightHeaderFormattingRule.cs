using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CodeFormatter.Engine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeFormatter.Rules
{
    [ExportFormattingRule(3)]
    internal sealed class HasNoXmlBasedCopyrightHeaderFormattingRule : IFormattingRule
    {
        private const string RulerMarker = "//---";
        private const string StartMarker = "// <copyright ";
        private const string EndMarker = "// </copyright>";

        public async Task<Document> ProcessAsync(CancellationToken cancellationToken, Document document)
        {
            var syntaxNode = await document.GetSyntaxRootAsync(cancellationToken) as CSharpSyntaxNode;
            if (syntaxNode == null)
                return document;

            var filtereredList = GetFiltereredList(syntaxNode.GetLeadingTrivia());
            var newSyntaxNode = syntaxNode.WithLeadingTrivia(filtereredList);
            return document.WithSyntaxRoot(newSyntaxNode);
        }

        private static IEnumerable<SyntaxTrivia> GetFiltereredList(SyntaxTriviaList triviaList)
        {
            SyntaxTrivia start;
            SyntaxTrivia end;
            return TryGetStartAndEndOfXmlHeader(triviaList, out start, out end)
                    ? Filter(triviaList, start, end)
                    : triviaList;
        }

        private static IEnumerable<SyntaxTrivia> Filter(SyntaxTriviaList triviaList, SyntaxTrivia start, SyntaxTrivia end)
        {
            var inHeader = false;

            foreach (var trivia in triviaList)
            {
                if (trivia == start)
                    inHeader = true;
                else if (trivia == end)
                    inHeader = false;
                else if (!inHeader)
                    yield return trivia;
            }
        }

        private static bool TryGetStartAndEndOfXmlHeader(SyntaxTriviaList triviaList, out SyntaxTrivia start, out SyntaxTrivia end)
        {
            start = default(SyntaxTrivia);
            end = default(SyntaxTrivia);

            var hasStart = false;
            var hasEnd = false;

            foreach (var trivia in triviaList)
            {
                if (!hasStart && IsBeginningOfXmlHeader(trivia, out start))
                    hasStart = true;

                if (!hasEnd && IsEndOfXmlHeader(trivia, out end))
                    hasEnd = true;
            }

            return hasStart && hasEnd;
        }

        private static bool IsBeginningOfXmlHeader(SyntaxTrivia trivia, out SyntaxTrivia start)
        {
            var next = GetNextComment(trivia);

            var currentFullText = trivia.ToFullString();
            var nextFullText = next == null ? string.Empty : next.Value.ToFullString();

            start = trivia;
            return currentFullText.StartsWith(StartMarker) ||
                   currentFullText.StartsWith(RulerMarker) && nextFullText.StartsWith(StartMarker);
        }

        private static bool IsEndOfXmlHeader(SyntaxTrivia trivia, out SyntaxTrivia end)
        {
            var next = GetNextComment(trivia);

            var currentFullText = trivia.ToFullString();
            var nextFullText = next == null ? string.Empty : next.Value.ToFullString();

            end = nextFullText.StartsWith(RulerMarker)
                    ? next.Value
                    : trivia;
            return currentFullText.StartsWith(EndMarker);
        }

        private static SyntaxTrivia? GetNextComment(SyntaxTrivia currentTrivia)
        {
            var trivia = currentTrivia.Token.LeadingTrivia;
            return trivia.SkipWhile(t => t != currentTrivia)
                         .Skip(1)
                         .SkipWhile(t => t.CSharpKind() != SyntaxKind.SingleLineCommentTrivia)
                         .Select(t => (SyntaxTrivia?)t)
                         .FirstOrDefault();
        }
    }
}