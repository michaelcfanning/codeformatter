// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.CodeFormatter.Analyzers
{ 
    internal static class Extensions
    {    
        public static bool OverlapsHiddenPosition(this SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.OverlapsHiddenPosition(node.Span, cancellationToken);
        }

        public static bool OverlapsHiddenPosition(this SyntaxNode node, TextSpan span, CancellationToken cancellationToken)
        {
            return node.SyntaxTree.OverlapsHiddenPosition(span, cancellationToken);
        }

        public static bool OverlapsHiddenPosition(this SyntaxNode declaration, SyntaxNode startNode, SyntaxNode endNode, CancellationToken cancellationToken)
        {
            var start = startNode.Span.End;
            var end = endNode.SpanStart;

            var textSpan = TextSpan.FromBounds(start, end);
            return declaration.OverlapsHiddenPosition(textSpan, cancellationToken);
        }

        /// <summary>
        /// Same as OverlapsHiddenPosition but doesn't throw on cancellation.  Instead, returns false
        /// in that case.
        /// </summary>
        public static bool TryOverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden,
            CancellationToken cancellationToken)
        {
            var startLineNumber = text.Lines.IndexOf(span.Start);
            var endLineNumber = text.Lines.IndexOf(span.End);

            // NOTE(cyrusn): It's safe to examine the start of a line because you can't have a line
            // with both a pp directive and code on it.  so, for example, if a node crosses a region
            // then it must be the case that the start of some line from the start of the node to
            // the end is hidden.  i.e.:
#if false
'           class C
'           {
'#line hidden
'           }
'#line default
#endif
            // The start of the line with the } on it is hidden, and thus the node overlaps a hidden
            // region.

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var linePosition = text.Lines[lineNumber].Start;
                var isHidden = isPositionHidden(linePosition, cancellationToken);
                if (isHidden)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool OverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden, CancellationToken cancellationToken)
        {
            var result = TryOverlapsHiddenPosition(text, span, isPositionHidden, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        public static bool OverlapsHiddenPosition(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken)
        {
            if (tree == null)
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);

            return text.OverlapsHiddenPosition(span, (position, cancellationToken2) =>
            {
                // implements the ASP.Net IsHidden rule
                var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
                return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
            },
                cancellationToken);
        }
        public static T WithPrependedLeadingTrivia<T>(
                   this T node,
                   params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0)
            {
                return node;
            }

            return node.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0)
            {
                return node;
            }

            return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            params SyntaxTrivia[] trivia)
        {
            if (trivia.Length == 0)
            {
                return token;
            }

            return token.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            SyntaxTriviaList trivia)
        {
            if (trivia.Count == 0)
            {
                return token;
            }

            return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }
    }
    internal partial class RemoveUnncessaryImportsHelper 
    {

        public static Document RemoveUnnecessaryImports(Document document, ISet<UsingDirectiveSyntax> unnecessaryImports, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            if (unnecessaryImports == null)
            {
                return document;
            }

            var oldRoot = (CompilationUnitSyntax)root;
            if (unnecessaryImports.Any(import => oldRoot.OverlapsHiddenPosition(cancellationToken)))
            {
                return document;
            }

            var newRoot = (CompilationUnitSyntax)new Rewriter(unnecessaryImports, cancellationToken).Visit(oldRoot);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return document.WithSyntaxRoot(FormatResult(document, newRoot, cancellationToken));
        }

        private static SyntaxNode FormatResult(Document document, CompilationUnitSyntax newRoot, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();
            AddFormattingSpans(newRoot, spans, cancellationToken);
            return Formatter.Format(newRoot, spans, document.Project.Solution.Workspace, document.Project.Solution.Workspace.Options, cancellationToken: cancellationToken);
        }

        private static void AddFormattingSpans(
            CompilationUnitSyntax compilationUnit,
            List<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spans.Add(TextSpan.FromBounds(0, GetEndPosition(compilationUnit, compilationUnit.Members)));

            foreach (var @namespace in compilationUnit.Members.OfType<NamespaceDeclarationSyntax>())
            {
                AddFormattingSpans(@namespace, spans, cancellationToken);
            }
        }

        private static void AddFormattingSpans(
            NamespaceDeclarationSyntax namespaceMember,
            List<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spans.Add(TextSpan.FromBounds(namespaceMember.SpanStart, GetEndPosition(namespaceMember, namespaceMember.Members)));

            foreach (var @namespace in namespaceMember.Members.OfType<NamespaceDeclarationSyntax>())
            {
                AddFormattingSpans(@namespace, spans, cancellationToken);
            }
        }

        private static int GetEndPosition(SyntaxNode container, SyntaxList<MemberDeclarationSyntax> list)
        {
            return list.Count > 0 ? list[0].SpanStart : container.Span.End;
        }
    }
}
