﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents information on a file/folder create.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileCreate">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class FileCreate
{
    /// <summary>
    /// A <c>file://</c> URI for the location of the file/folder being created.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri Uri { get; set; }
}
