// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.CodeFormatting
{
    internal interface IFormattingFilter
    {
        bool ShouldBeProcessed(Document document);
    }
}