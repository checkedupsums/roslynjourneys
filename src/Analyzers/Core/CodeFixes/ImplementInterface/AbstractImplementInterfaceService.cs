﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService() : IImplementInterfaceService
{
    protected const string DisposingName = "disposing";

    protected abstract ISyntaxFormatting SyntaxFormatting { get; }
    protected abstract SyntaxGeneratorInternal SyntaxGeneratorInternal { get; }

    protected abstract string ToDisplayString(IMethodSymbol disposeImplMethod, SymbolDisplayFormat format);

    protected abstract bool CanImplementImplicitly { get; }
    protected abstract bool HasHiddenExplicitImplementation { get; }
    protected abstract bool TryInitializeState(Document document, SemanticModel model, SyntaxNode interfaceNode, CancellationToken cancellationToken, out SyntaxNode classOrStructDecl, out INamedTypeSymbol classOrStructType, out ImmutableArray<INamedTypeSymbol> interfaceTypes);
    protected abstract bool AllowDelegateAndEnumConstraints(ParseOptions options);

    protected abstract SyntaxNode AddCommentInsideIfStatement(SyntaxNode ifDisposingStatement, SyntaxTriviaList trivia);
    protected abstract SyntaxNode CreateFinalizer(SyntaxGenerator generator, INamedTypeSymbol classType, string disposeMethodDisplayString);

    public async Task<Document> ImplementInterfaceAsync(
        Document document, ImplementTypeOptions options, SyntaxNode node, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_ImplementInterface, cancellationToken))
        {
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var state = State.Generate(this, document, model, node, cancellationToken);
            if (state == null)
                return document;

            // While implementing just one default action, like in the case of pressing enter after interface name in VB,
            // choose to implement with the dispose pattern as that's the Dev12 behavior.
            var implementDisposePattern = ShouldImplementDisposePattern(model.Compilation, state, explicitly: false);
            var generator = new ImplementInterfaceGenerator(
                this, document, state, options, new() { OnlyRemaining = true, ImplementDisposePattern = implementDisposePattern });

            return await generator.ImplementInterfaceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IImplementInterfaceInfo?> AnalyzeAsync(Document document, SyntaxNode interfaceType, CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return State.Generate(this, document, model, interfaceType, cancellationToken);
    }

    protected TNode AddComment<TNode>(string comment, TNode node) where TNode : SyntaxNode
        => AddComments([comment], node);

    protected TNode AddComments<TNode>(string comment1, string comment2, TNode node) where TNode : SyntaxNode
        => AddComments([comment1, comment2], node);

    protected TNode AddComments<TNode>(string[] comments, TNode node) where TNode : SyntaxNode
        => node.WithPrependedLeadingTrivia(CreateCommentTrivia(comments));

    protected SyntaxTriviaList CreateCommentTrivia(
        params string[] comments)
    {
        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var trivia);

        foreach (var comment in comments)
        {
            trivia.Add(this.SyntaxGeneratorInternal.SingleLineComment(" " + comment));
            trivia.Add(this.SyntaxGeneratorInternal.ElasticCarriageReturnLineFeed);
        }

        return new SyntaxTriviaList(trivia);
    }

    public async Task<Document> ImplementInterfaceAsync(
        Document document,
        IImplementInterfaceInfo info,
        ImplementTypeOptions options,
        ImplementInterfaceConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var generator = new ImplementInterfaceGenerator(
            this, document, info, options, configuration);
        return await generator.ImplementInterfaceAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ISymbol> ExplicitlyImplementSingleInterfaceMemberAsync(
        Document document,
        IImplementInterfaceInfo info,
        ISymbol member,
        ImplementTypeOptions options,
        CancellationToken cancellationToken)
    {
        var configuration = new ImplementInterfaceConfiguration()
        {
            Abstractly = false,
            Explicitly = true,
            OnlyRemaining = false,
            ImplementDisposePattern = false,
            ThroughMember = null,
        };
        var generator = new ImplementInterfaceGenerator(
            this, document, info, options, configuration);
        var implementedMembers = await generator.GenerateExplicitlyImplementedMembersAsync(member, options.PropertyGenerationBehavior, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var singleImplemented = implementedMembers[0];
        Contract.ThrowIfNull(singleImplemented);

        // Since non-indexer properties are the only symbols that get their implementing accessor symbols returned,
        // we have to process the created symbols and reduce to the single property wherein the accessors are contained
        if (member is IPropertySymbol { IsIndexer: false })
        {
            IPropertySymbol? commonContainer = null;
            foreach (var implementedMember in implementedMembers)
            {
                if (implementedMember is IPropertySymbol implementedProperty)
                {
                    commonContainer ??= implementedProperty;
                    Contract.ThrowIfFalse(commonContainer == implementedProperty, "We should have a common property implemented");
                }
                else
                {
                    Contract.ThrowIfNull(implementedMember);
                    var containingProperty = implementedMember.ContainingSymbol as IPropertySymbol;
                    Contract.ThrowIfNull(containingProperty);
                    commonContainer ??= containingProperty;
                    Contract.ThrowIfFalse(commonContainer == containingProperty, "We should have a common property implemented");
                }
            }
            Contract.ThrowIfNull(commonContainer);
            singleImplemented = commonContainer;
        }
        else
        {
            Contract.ThrowIfFalse(implementedMembers.Length == 1, "We missed another case that may return multiple symbols");
        }

        return singleImplemented;
    }
}
