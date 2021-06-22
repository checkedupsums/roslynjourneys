﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

#if !CODE_STYLE
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            /// <summary>
            /// The index in <see cref="Testing.ProjectState.AnalyzerConfigFiles"/> of the generated
            /// <strong>.editorconfig</strong> file for <see cref="Options"/>, or <see langword="null"/> if no such
            /// file has been generated yet.
            /// </summary>
            private int? _analyzerConfigIndex;

            static Test()
            {
                // If we have outdated defaults from the host unit test application targeting an older .NET Framework, use more
                // reasonable TLS protocol version for outgoing connections.
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CS0618 // Type or member is obsolete
                if (ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
            }

            public Test()
            {
                MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

#if !CODE_STYLE
                    var options = solution.Options;
                    var (_, remainingOptions) = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(DefaultFileExt, EditorConfig, Options);
                    foreach (var (key, value) in remainingOptions)
                    {
                        options = options.WithChangedOption(key, value);
                    }

                    solution = solution.WithOptions(options);
#endif

                    return solution;
                });
            }

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.CSharp8"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

            /// <summary>
            /// Gets a collection of options to apply to <see cref="Solution.Options"/> for testing. Values may be added
            /// using a collection initializer.
            /// </summary>
            internal OptionsCollection Options { get; } = new OptionsCollection(LanguageNames.CSharp);

            public string? EditorConfig { get; set; }

            public Func<ImmutableArray<Diagnostic>, Diagnostic?>? DiagnosticSelector { get; set; }

            public Action<ImmutableArray<CodeAction>>? CodeActionsVerifier { get; set; }

            protected override async Task RunImplAsync(CancellationToken cancellationToken = default)
            {
                if (DiagnosticSelector is object)
                {
                    Assert.True(CodeFixTestBehaviors.HasFlag(Testing.CodeFixTestBehaviors.FixOne), $"'{nameof(DiagnosticSelector)}' can only be used with '{nameof(Testing.CodeFixTestBehaviors)}.{nameof(Testing.CodeFixTestBehaviors.FixOne)}'");
                }

                var (analyzerConfigSource, _) = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(DefaultFileExt, EditorConfig, Options);
                if (analyzerConfigSource is object)
                {
                    if (_analyzerConfigIndex is null)
                    {
                        _analyzerConfigIndex = TestState.AnalyzerConfigFiles.Count;
                        TestState.AnalyzerConfigFiles.Add(("/.editorconfig", analyzerConfigSource));
                    }
                    else
                    {
                        TestState.AnalyzerConfigFiles[_analyzerConfigIndex.Value] = ("/.editorconfig", analyzerConfigSource);
                    }
                }
                else if (_analyzerConfigIndex is { } index)
                {
                    _analyzerConfigIndex = null;
                    TestState.AnalyzerConfigFiles.RemoveAt(index);
                }

                await base.RunImplAsync(cancellationToken);
            }

#if !CODE_STYLE
            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
                => new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), project.Solution);
#endif

            protected override Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
            {
                return DiagnosticSelector?.Invoke(fixableDiagnostics)
                    ?? base.TrySelectDiagnosticToFix(fixableDiagnostics);
            }

            protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
            {
                CodeActionsVerifier?.Invoke(actions);
                return base.FilterCodeActions(actions);
            }
        }
    }
}
