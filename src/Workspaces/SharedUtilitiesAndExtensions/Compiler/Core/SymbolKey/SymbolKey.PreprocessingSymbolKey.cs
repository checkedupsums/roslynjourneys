﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private class PreprocessingSymbolKey : AbstractSymbolKey<IPreprocessingSymbol>
    {
        public static readonly PreprocessingSymbolKey Instance = new PreprocessingSymbolKey();

        public sealed override void Create(IPreprocessingSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.Name);
        }

        protected sealed override SymbolKeyResolution Resolve(SymbolKeyReader reader, IPreprocessingSymbol? contextualSymbol, out string? failureReason)
        {
            failureReason = null;
            var preprocessingName = reader.ReadRequiredString();
            var preprocessingSymbol = reader.Compilation.CreatePreprocessingSymbol(preprocessingName);
            return new SymbolKeyResolution(preprocessingSymbol);
        }
    }
}
