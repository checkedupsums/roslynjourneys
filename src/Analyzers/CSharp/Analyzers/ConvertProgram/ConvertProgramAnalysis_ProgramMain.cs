﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram
{
    internal static partial class ConvertProgramAnalysis
    {
        public static bool IsApplication(Compilation compilation)
            => IsApplication(compilation.Options);

        public static bool IsApplication(CompilationOptions options)
            => options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;

        public static bool CanOfferUseProgramMain(
            CodeStyleOption2<bool> option,
            CompilationUnitSyntax root,
            Compilation compilation,
            bool forAnalyzer)
        {
            if (!HasGlobalStatement(root))
                return false;

            if (!CanOfferUseProgramMain(option, forAnalyzer))
                return false;

            // resiliency check for later on.  This shouldn't happen but we don't want to crash if we are in a weird
            // state where we have top level statements but no 'Program' type.
            var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
            if (programType == null)
                return false;

            if (programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).FirstOrDefault() is not IMethodSymbol)
                return false;

            return true;
        }

        private static bool HasGlobalStatement(CompilationUnitSyntax root)
        {
            foreach (var member in root.Members)
            {
                if (member.Kind() is SyntaxKind.GlobalStatement)
                    return true;
            }

            return false;
        }

        private static bool CanOfferUseProgramMain(CodeStyleOption2<bool> option, bool forAnalyzer)
        {
            var userPrefersProgramMain = option.Value == false;
            var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
            var forRefactoring = !forAnalyzer;

            // If the user likes Program.Main, then we offer to conver to Program.Main from the diagnostic analyzer.
            // If the user prefers Top-level-statements then we offer to use Program.Main from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersProgramMain == forAnalyzer || (forRefactoring && analyzerDisabled);
            return canOffer;
        }

        public static Location GetUseProgramMainDiagnosticLocation(CompilationUnitSyntax root, bool isHidden)
        {
            // if the diagnostic is hidden, show it anywhere from the top of the file through the end of the last global
            // statement.  That way the user can make the change anywhere in teh top level code.  Otherwise, just put
            // the diagnostic on the start of the first global statement.
            if (!isHidden)
                return root.Members.OfType<GlobalStatementSyntax>().First().GetFirstToken().GetLocation();

            // note: the legal start has to come after any #pragma directives.  We don't want this to be suppressed, but
            // then have the span of the diagnostic end up outside the suppression.
            var lastPragma = root.GetFirstToken().LeadingTrivia.LastOrDefault(t => t.Kind() is SyntaxKind.PragmaWarningDirectiveTrivia);
            var start = lastPragma == default ? 0 : lastPragma.FullSpan.End;

            return Location.Create(
                root.SyntaxTree,
                TextSpan.FromBounds(start, root.Members.OfType<GlobalStatementSyntax>().Last().FullSpan.End));
        }
    }
}
