﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;


namespace Microsoft.DotNet.CodeFormatting.Rules
{
    internal abstract class SyntaxFormattingRule : ISyntaxFormattingRule
    {
        public abstract bool SupportsLanguage(string languageName);

        public SyntaxNode Process(SyntaxNode syntaxNode, string languageName)
        {
            switch (languageName)
            {
                case LanguageNames.CSharp:
                    return ProcessCSharp(syntaxNode);
                case LanguageNames.VisualBasic:
                    return ProcessVisualBasic(syntaxNode);
                default:
                    throw new NotSupportedException();
            }
        }

        public virtual SyntaxNode ProcessCSharp(SyntaxNode syntaxNode)
        {
            throw new NotSupportedException();
        }

        public virtual SyntaxNode ProcessVisualBasic(SyntaxNode syntaxNode)
        {
            throw new NotSupportedException();
        }
    }
}
