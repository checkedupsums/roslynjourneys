﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class PreprocessingSymbolReferenceFinder : AbstractReferenceFinder<IPreprocessingSymbol>
{
    protected override bool CanFind(IPreprocessingSymbol symbol) => true;

    protected sealed override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
        IPreprocessingSymbol symbol,
        FindReferencesDocumentState state,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        var tokens = await FindMatchingIdentifierTokensAsync(state, symbol.Name, cancellationToken).ConfigureAwait(false);

        var normalReferences = await FindPreprocessingReferencesInTokensAsync(
            symbol, state,
            tokens,
            cancellationToken).ConfigureAwait(false);

        return normalReferences;
    }

    protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
        IPreprocessingSymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<Document>.GetInstance(out var resultDocuments);

        // NOTE: We intentionally search for all documents in the entire solution. This is because
        //       the symbols are validly bound by their requested name, despite their current definition
        //       state. Therefore, the same symbol name could be shared across multiple projects and
        //       configured in the project configuration with the same shared identifier.

        var solution = project.Solution;
        var sourceDocuments = (IEnumerable<Document>?)documents
            ?? await GetAllSolutionDocumentsAsync(solution, cancellationToken).ConfigureAwait(false);

        foreach (var document in sourceDocuments)
        {
            var syntaxTreeIndex = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTreeIndex.ContainsDirective && syntaxTreeIndex.ProbablyContainsIdentifier(symbol.Name))
                resultDocuments.Add(document);
        }

        return resultDocuments.ToImmutable();
    }

    private static async ValueTask<IEnumerable<Document>> GetAllSolutionDocumentsAsync(Solution solution, CancellationToken cancellationToken)
    {
        var documents = Enumerable.Empty<Document>();

        foreach (var solutionProject in solution.Projects)
        {
            var projectDocuments = await solutionProject.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            documents = documents.Concat(projectDocuments);
        }

        return documents;
    }

    private static async ValueTask<ImmutableArray<FinderLocation>> FindPreprocessingReferencesInTokensAsync(
        IPreprocessingSymbol symbol,
        FindReferencesDocumentState state,
        ImmutableArray<SyntaxToken> tokens,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FinderLocation>.GetInstance(out var locations);
        foreach (var token in tokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matched = await PreprocessingSymbolsMatchAsync(symbol, state, token, cancellationToken)
                .ConfigureAwait(false);

            if (matched)
            {
                locations.Add(CreateFinderLocation(state, token, CandidateReason.None, cancellationToken));
            }
        }

        return locations.ToImmutable();
    }

    private static async ValueTask<bool> PreprocessingSymbolsMatchAsync(
        IPreprocessingSymbol searchSymbol, FindReferencesDocumentState state, SyntaxToken token, CancellationToken cancellationToken)
    {
        var symbol = state.SemanticModel.GetPreprocessingSymbolInfo(token.GetRequiredParent()).Symbol;
        return await SymbolFinder.OriginalSymbolsMatchAsync(state.Solution, searchSymbol, symbol, cancellationToken).ConfigureAwait(false);
    }
}
