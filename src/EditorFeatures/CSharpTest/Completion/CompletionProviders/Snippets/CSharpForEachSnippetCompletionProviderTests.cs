﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    public class CSharpForEachSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => FeaturesResources.Insert_a_foreach_loop;

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForEachSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        foreach (var item in collection)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForEachSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"foreach (var item in collection)
{$$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InserForEachSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        var x = 5;
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        var x = 5;
        foreach (var item in collection)
        {$$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForEachSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            $$
        }
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            foreach (var item in collection)
            {$$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForEachSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        $$
    };
}";

            var expectedCodeAfterCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        foreach (var item in collection)
        {$$
        }
    };
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in collection)
    {$$
    }

    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
