// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRule(Name = CopyrightHeaderRule.Name, Description = CopyrightHeaderRule.Description, Order = SyntaxRuleOrder.CopyrightHeaderRule)]
    internal sealed partial class CopyrightHeaderRule : SyntaxFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = FormattingDefaults.CopyrightRuleName;
        internal const string Description = "Insert the copyright header into every file";

        private abstract class CommonRule
        {
            /// <summary>
            /// This is the normalized copyright header that has no comment delimiters.
            /// </summary>
            private readonly ImmutableArray<string> _header;

            protected CommonRule(ImmutableArray<string> header)
            {
                _header = header;
            }

            internal SyntaxNode Process(SyntaxNode syntaxNode)
            {
                if (_header.IsDefaultOrEmpty)
                {
                    return syntaxNode;
                }

                var newHeader = new List<SyntaxTrivia>();
                foreach (var headerLine in _header)
                {
                    newHeader.Add(CreateLineComment(headerLine));
                    newHeader.Add(CreateNewLine());
                }
                newHeader.Add(CreateNewLine());

                var existingHeader = syntaxNode.GetLeadingTrivia();

                bool exactMatch = true;

                if (newHeader.Count == existingHeader.Count)
                {
                    for (int i = 0; i < newHeader.Count; i++)
                    {
                        if (newHeader[i].ToString() != existingHeader[i].ToString())
                        {
                            exactMatch = false;
                            break;
                        }
                    }
                }

                if (exactMatch)
                {
                    return syntaxNode;
                }

                return syntaxNode.WithLeadingTrivia(CreateTriviaList(newHeader));
            }

            private bool HasCopyrightHeader(SyntaxNode syntaxNode)
            {
                var existingHeader = GetExistingHeader(syntaxNode.GetLeadingTrivia());
                return SequenceStartsWith(_header, existingHeader);
            }

            private bool SequenceStartsWith(ImmutableArray<string> header, List<string> existingHeader)
            {
                // Only try if the existing header is at least as long as the new copyright header
                if (existingHeader.Count >= header.Count())
                {
                    return !header.Where((headerLine, i) => existingHeader[i] != headerLine).Any();
                }

                return false;
            }

            private List<string> GetExistingHeader(SyntaxTriviaList triviaList)
            {
                var i = 0;
                MovePastBlankLines(triviaList, ref i);

                var headerList = new List<string>();
                while (i < triviaList.Count && IsLineComment(triviaList[i]))
                {
                    headerList.Add(GetCommentText(triviaList[i].ToFullString()));
                    i++;
                    MoveToNextLineOrTrivia(triviaList, ref i);
                }

                return headerList;
            }

            /// <summary>
            /// Remove any copyright header that already exists.
            /// </summary>
            private SyntaxTriviaList RemoveExistingHeader(SyntaxTriviaList oldList)
            {
                var foundHeader = false;
                var i = 0;
                MovePastBlankLines(oldList, ref i);

                while (i < oldList.Count && IsLineComment(oldList[i]))
                {
                    if (oldList[i].ToFullString().IndexOf("copyright", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundHeader = true;
                    }
                    i++;
                }

                if (!foundHeader)
                {
                    return oldList;
                }

                MovePastBlankLines(oldList, ref i);
                return CreateTriviaList(oldList.Skip(i));
            }

            private void MovePastBlankLines(SyntaxTriviaList list, ref int index)
            {
                while (index < list.Count && (IsWhitespace(list[index]) || IsNewLine(list[index])))
                {
                    index++;
                }
            }

            private void MoveToNextLineOrTrivia(SyntaxTriviaList list, ref int index)
            {
                MovePastWhitespaces(list, ref index);

                if (index < list.Count && IsNewLine(list[index]))
                {
                    index++;
                }
            }

            private void MovePastWhitespaces(SyntaxTriviaList list, ref int index)
            {
                while (index < list.Count && IsWhitespace(list[index]))
                {
                    index++;
                }
            }

            protected abstract SyntaxTriviaList CreateTriviaList(IEnumerable<SyntaxTrivia> e);

            protected abstract bool IsLineComment(SyntaxTrivia trivia);

            protected abstract bool IsWhitespace(SyntaxTrivia trivia);
            protected abstract bool IsNewLine(SyntaxTrivia trivia);

            protected abstract SyntaxTrivia CreateLineComment(string commentText);

            protected abstract SyntaxTrivia CreateNewLine();
        }

        private readonly FormattingOptions _options;
        private ImmutableArray<string> _cachedHeader;
        private ImmutableArray<string> _cachedHeaderSource;

        public CopyrightHeaderRule(FormattingOptions options)
        {
            _options = options;
        }

        private ImmutableArray<string> GetHeader()
        {
            if (_cachedHeaderSource != _options.CopyrightHeader)
            {
                _cachedHeaderSource = _options.CopyrightHeader;
                _cachedHeader = _options.CopyrightHeader.Select(GetCommentText).ToImmutableArray();
            }

            return _cachedHeader;
        }

        private static string GetCommentText(string line)
        {
            if (line.StartsWith("'"))
            {
                return line.Substring(1).TrimStart();
            }

            if (line.StartsWith("//"))
            {
                return line.Substring(2).TrimStart();
            }

            return line;
        }

        public override bool SupportsLanguage(string languageName)
        {
            return languageName == LanguageNames.CSharp || languageName == LanguageNames.VisualBasic;
        }

        public override SyntaxNode ProcessCSharp(SyntaxNode syntaxNode)
        {
            return (new CSharpRule(GetHeader())).Process(syntaxNode);
        }

        public override SyntaxNode ProcessVisualBasic(SyntaxNode syntaxNode)
        {
            return (new VisualBasicRule(GetHeader())).Process(syntaxNode);
        }
    }
}