﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PreferFrameworkType;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpPreferFrameworkTypeDiagnosticAnalyzer :
    PreferFrameworkTypeDiagnosticAnalyzerBase<
        SyntaxKind,
        ExpressionSyntax,
        TypeSyntax,
        IdentifierNameSyntax,
        PredefinedTypeSyntax>
{
    protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } =
        [SyntaxKind.PredefinedType, SyntaxKind.IdentifierName];

    ///<remarks>
    /// every predefined type keyword except <c>void</c> can be replaced by its framework type in code.
    ///</remarks>
    protected override bool IsPredefinedTypeReplaceableWithFrameworkType(PredefinedTypeSyntax node)
        => node.Keyword.Kind() != SyntaxKind.VoidKeyword;

    // Only offer to change nint->System.IntPtr when it would preserve semantics exactly.
    protected override bool IsIdentifierNameReplaceableWithFrameworkType(SemanticModel semanticModel, IdentifierNameSyntax node)
    {
        if (node.IsNint || node.IsNuint)
        {
            var languageVersion = semanticModel.SyntaxTree.Options.LanguageVersion();

            // In C# 11 we made it so that IntPtr and nint are identical, with no difference in semantics at all.
            if (languageVersion >= LanguageVersion.CSharp11)
                return true;

            // For C# 9 and 10, the types are only identical if the runtime unifies them.
            if (languageVersion >= LanguageVersion.CSharp9 && semanticModel.Compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr))
                return true;
        }

        return false;
    }

    protected override bool IsInMemberAccessOrCrefReferenceContext(ExpressionSyntax node)
        => node.IsDirectChildOfMemberAccessExpression() || node.InsideCrefReference();
}
