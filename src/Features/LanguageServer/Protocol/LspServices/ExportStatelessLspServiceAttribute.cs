﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines an attribute to export an instance of <see cref="ILspService"/> that is re-used across
/// all server instances in the same mef container.  Services using this export attribute should not
/// store any kind of server specific state in them.
/// 
/// MEF will dispose of these services when the container is disposed of.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false), MetadataAttribute]
internal class ExportStatelessLspServiceAttribute : ExportAttribute
{
    /// <summary>
    /// The full type name of the service being exported.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The LSP server for which this service applies to.  If null, this service applies to any server
    /// with the matching contract name.
    /// </summary>
    public WellKnownLspServerKinds? ServerKind { get; }

    /// <summary>
    /// Services MEF exported as <see cref="ILspService"/> must by definition be stateless as they are
    /// shared amongst all LSP server instances through restarts.
    /// </summary>
    public bool IsStateless { get; } = true;

    private readonly Lazy<byte[]>? _lazyMethodHandlerDescriptorData;

    /// <summary>
    /// If this this service implements <see cref="IMethodHandler"/>, returns a blob of binary data
    /// that encodes an array of <see cref="MethodHandlerDescriptor"/>s; otherwise <see langword="null"/>.
    /// </summary>
    public byte[]? MethodHandlerDescriptorData => _lazyMethodHandlerDescriptorData?.Value;

    public ExportStatelessLspServiceAttribute(Type type, string contractName, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
        : base(contractName, typeof(ILspService))
    {
        Contract.ThrowIfFalse(type.GetInterfaces().Contains(typeof(ILspService)), $"{type.Name} does not inherit from {nameof(ILspService)}");
        Contract.ThrowIfNull(type.AssemblyQualifiedName);

        TypeName = type.AssemblyQualifiedName;
        ServerKind = serverKind;

        _lazyMethodHandlerDescriptorData = typeof(IMethodHandler).IsAssignableFrom(type)
            ? new Lazy<byte[]>(() => MefSerialization.Serialize(HandlerReflection.GetMethodHandlers(type)))
            : null;
    }
}
