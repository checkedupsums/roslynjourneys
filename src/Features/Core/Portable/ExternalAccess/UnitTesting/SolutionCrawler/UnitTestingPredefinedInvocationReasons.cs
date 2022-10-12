﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal static class UnitTestingPredefinedInvocationReasons
    {
        public const string SemanticChanged = nameof(SemanticChanged);
        public const string Reanalyze = nameof(Reanalyze);
        public const string ProjectConfigurationChanged = nameof(ProjectConfigurationChanged);

        public const string HighPriority = nameof(HighPriority);

#if false // Not used in unit testing crawling
        public const string DocumentAdded = nameof(DocumentAdded);
        public const string DocumentRemoved = nameof(DocumentRemoved);
        public const string DocumentOpened = nameof(DocumentOpened);
        public const string DocumentClosed = nameof(DocumentClosed);
        public const string ActiveDocumentSwitched = nameof(ActiveDocumentSwitched);
        public const string ProjectParseOptionsChanged = nameof(ProjectParseOptionsChanged);
        public const string SolutionRemoved = nameof(SolutionRemoved);
        public const string SyntaxChanged = nameof(SyntaxChanged);
#endif
    }
}
