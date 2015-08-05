// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.CodeFormatting.Rules
{
    internal class CSharpOnlyFormattingRule : IFormattingRule
    {
        protected CSharpOnlyFormattingRule()
        {
        }

        public bool SupportsLanguage(string languageName)
        {
            return languageName == LanguageNames.CSharp;
        }
    }
}
