﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using NativeMethods = Microsoft.CodeAnalysis.Editor.Wpf.Utilities.NativeMethods;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private class ColorSchemeSettings
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IGlobalOptionService _optionService;

            public HasThemeBeenDefaultedIndexer HasThemeBeenDefaulted { get; }

            public ColorSchemeSettings(IServiceProvider serviceProvider, IGlobalOptionService globalOptionService)
            {
                _serviceProvider = serviceProvider;
                _optionService = globalOptionService;

                HasThemeBeenDefaulted = new HasThemeBeenDefaultedIndexer(globalOptionService);
            }

            public ImmutableDictionary<SchemeName, ColorScheme> GetColorSchemes()
            {
                return new[]
                {
                    SchemeName.Enhanced,
                    SchemeName.VisualStudio2017
                }.ToImmutableDictionary(name => name, name => GetColorScheme(name));
            }

            private ColorScheme GetColorScheme(SchemeName schemeName)
            {
                using var colorSchemeStream = GetColorSchemeXmlStream(schemeName);
                return ColorSchemeReader.ReadColorScheme(colorSchemeStream);
            }

            private Stream GetColorSchemeXmlStream(SchemeName schemeName)
            {
                var assembly = Assembly.GetExecutingAssembly();
                return assembly.GetManifestResourceStream($"Microsoft.VisualStudio.LanguageServices.ColorSchemes.{schemeName}.xml");
            }

            public void ApplyColorScheme(SchemeName schemeName, ImmutableArray<RegistryItem> registryItems)
            {
                using var registryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_Configuration, writable: true);

                foreach (var item in registryItems)
                {
                    using var itemKey = registryRoot.CreateSubKey(item.SectionName);
                    itemKey.SetValue(item.ValueName, item.ValueData);
                }

                _optionService.SetOptions(new SingleOptionSet(ColorSchemeOptions.AppliedColorScheme, schemeName));

                // Broadcast that system color settings have changed to force the ColorThemeService to reload colors.
                NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SYSCOLORCHANGE, wparam: IntPtr.Zero, lparam: IntPtr.Zero);
            }

            public SchemeName GetAppliedColorScheme()
            {
                var schemeName = _optionService.GetOption(ColorSchemeOptions.AppliedColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.AppliedColorScheme.DefaultValue;
            }

            public SchemeName GetConfiguredColorScheme()
            {
                var schemeName = _optionService.GetOption(ColorSchemeOptions.ColorScheme);
                return schemeName != SchemeName.None
                    ? schemeName
                    : ColorSchemeOptions.ColorScheme.DefaultValue;
            }

            public void MigrateToColorSchemeSetting(bool isThemeCustomized)
            {
                // Get the preview feature flag value.
                var useEnhancedColorsSetting = _optionService.GetOption(ColorSchemeOptions.LegacyUseEnhancedColors);

                // Return if we have already migrated.
                if (useEnhancedColorsSetting == ColorSchemeOptions.UseEnhancedColors.Migrated)
                {
                    return;
                }

                // Since we did not apply enhanced colors if the theme had been customized, default customized themes to classic colors.
                var colorScheme = (useEnhancedColorsSetting != ColorSchemeOptions.UseEnhancedColors.DoNotUse && !isThemeCustomized)
                    ? ColorSchemeOptions.Enhanced
                    : ColorSchemeOptions.VisualStudio2017;

                _optionService.SetOptions(new SingleOptionSet(ColorSchemeOptions.ColorScheme, colorScheme));
                _optionService.SetOptions(new SingleOptionSet(ColorSchemeOptions.LegacyUseEnhancedColors, ColorSchemeOptions.UseEnhancedColors.Migrated));
            }

            public Guid GetThemeId()
            {
                // Look up the value from the new roamed theme property first and
                // fallback to the original roamed theme property if that fails.
                var themeIdString = _optionService.GetOption(VisualStudioColorTheme.CurrentThemeNew)
                    ?? _optionService.GetOption(VisualStudioColorTheme.CurrentTheme);

                return Guid.TryParse(themeIdString, out var themeId) ? themeId : Guid.Empty;
            }

            private static class VisualStudioColorTheme
            {
                private const string CurrentThemeValueName = "Microsoft.VisualStudio.ColorTheme";
                private const string CurrentThemeValueNameNew = "Microsoft.VisualStudio.ColorThemeNew";

                public static readonly Option<string?> CurrentTheme = new Option<string?>(nameof(VisualStudioColorTheme),
                    nameof(CurrentTheme),
                    defaultValue: null,
                    storageLocations: new RoamingProfileStorageLocation(CurrentThemeValueName));

                public static readonly Option<string?> CurrentThemeNew = new Option<string?>(nameof(VisualStudioColorTheme),
                    nameof(CurrentThemeNew),
                    defaultValue: null,
                    storageLocations: new RoamingProfileStorageLocation(CurrentThemeValueNameNew));
            }

            public sealed class HasThemeBeenDefaultedIndexer
            {
                private static readonly ImmutableDictionary<Guid, Option<bool>> HasThemeBeenDefaultedOptions = new Dictionary<Guid, Option<bool>>
                {
                    [KnownColorThemes.Blue] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Blue),
                    [KnownColorThemes.Light] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Light),
                    [KnownColorThemes.Dark] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.Dark),
                    [KnownColorThemes.AdditionalContrast] = CreateHasThemeBeenDefaultedOption(KnownColorThemes.AdditionalContrast)
                }.ToImmutableDictionary();

                private static Option<bool> CreateHasThemeBeenDefaultedOption(Guid themeId)
                {
                    return new Option<bool>(nameof(ColorSchemeApplier), $"{nameof(HasThemeBeenDefaultedOptions)}{themeId}", defaultValue: false,
                        storageLocations: new RoamingProfileStorageLocation($@"Roslyn\ColorSchemeApplier\HasThemeBeenDefaulted\{themeId}"));
                }

                private readonly IGlobalOptionService _optionService;

                public HasThemeBeenDefaultedIndexer(IGlobalOptionService globalOptionService)
                {
                    _optionService = globalOptionService;
                }

                public bool this[Guid themeId]
                {
                    get => _optionService.GetOption(HasThemeBeenDefaultedOptions[themeId]);

                    set => _optionService.SetOptions(new SingleOptionSet(HasThemeBeenDefaultedOptions[themeId], value));
                }
            }

            private sealed class SingleOptionSet : OptionSet
            {
                private readonly OptionKey _optionKey;
                private readonly object? _value;

                public SingleOptionSet(IOption option, object? value)
                {
                    _optionKey = new OptionKey(option);
                    _value = value;
                }

                public override object? GetOption(OptionKey optionKey)
                {
                    if (optionKey != _optionKey)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    return _value;
                }

                public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
                {
                    throw new NotImplementedException();
                }

                internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
                {
                    return SpecializedCollections.SingletonEnumerable(_optionKey);
                }
            }
        }
    }
}
