#nullable enable
namespace XamlColorSchemeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ColorSchemeGenerator
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        public bool verbose = false;
        public bool forceFileWrite = false;

        public void GenerateColorSchemeFiles(string generatorParametersFile, string templateFile, string? outputPath = null)
        {
            var parameters = GetParametersFromFile(generatorParametersFile);

            outputPath ??= Path.GetDirectoryName(Path.GetFullPath(templateFile));

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new Exception("OutputPath could not be determined.");
            }

            if (this.verbose)
            {
                Console.WriteLine($"parametersFile: {generatorParametersFile}");
                Console.WriteLine($"templateFile:   {templateFile}");
                Console.WriteLine($"outputPath:     {outputPath}");
            }

            Directory.CreateDirectory(outputPath);

            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            var colorSchemesWithoutVariantName = parameters.ColorSchemes
                .Where(x => string.IsNullOrEmpty(x.ForColorSchemeVariant) || x.ForColorSchemeVariant == "None")
                .ToList();
            var colorSchemesWithVariantName = parameters.ColorSchemes
                .Where(x => string.IsNullOrEmpty(x.ForColorSchemeVariant) == false && x.ForColorSchemeVariant != "None")
                .ToList();

            foreach (var baseColorScheme in parameters.BaseColorSchemes)
            {
                if (colorSchemesWithoutVariantName.Count == 0
                    && colorSchemesWithVariantName.Count == 0)
                {
                    var themeName = baseColorScheme.Name;
                    var colorSchemeName = string.Empty;
                    var alternativeColorSchemeName = string.Empty;
                    var themeDisplayName = baseColorScheme.Name;

                    this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, false, baseColorScheme.Values, parameters.DefaultValues);
                }

                foreach (var colorScheme in colorSchemesWithoutVariantName)
                {
                    if (string.IsNullOrEmpty(colorScheme.ForBaseColor) == false
                        && colorScheme.ForBaseColor != baseColorScheme.Name)
                    {
                        continue;
                    }

                    var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}";
                    var colorSchemeName = colorScheme.Name;
                    var alternativeColorSchemeName = colorScheme.Name;
                    var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                    this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.IsHighContrast, colorScheme.Values, baseColorScheme.Values, parameters.DefaultValues);
                }

                foreach (var colorSchemeVariant in parameters.AdditionalColorSchemeVariants)
                {
                    foreach (var colorScheme in colorSchemesWithoutVariantName.Concat(colorSchemesWithVariantName))
                    {
                        if (string.IsNullOrEmpty(colorScheme.ForBaseColor) == false
                            && colorScheme.ForBaseColor != baseColorScheme.Name)
                        {
                            continue;
                        }

                        if (colorScheme.ForColorSchemeVariant == "None")
                        {
                            continue;
                        }

                        var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var colorSchemeName = $"{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var alternativeColorSchemeName = colorScheme.Name;
                        var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                        this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.IsHighContrast, colorScheme.Values, colorSchemeVariant.Values, baseColorScheme.Values, parameters.DefaultValues);
                    }
                }
            }
        }

        public static ThemeGenerator.ThemeGeneratorParameters GetParametersFromFile(string inputFile)
        {
            return ThemeGenerator.Current.GetParametersFromString(ReadAllTextShared(inputFile));
        }

        private static string ReadAllTextShared(string file)
        {
            Stream? stream = null;
            try
            {
                stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

                using (var textReader = new StreamReader(stream, Encoding.UTF8))
                {
                    stream = null;
                    return textReader.ReadToEnd();
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public void GenerateColorSchemeFile(string templateDirectory, string templateContent, string themeName, string themeDisplayName, string baseColorScheme, string colorScheme, string alternativeColorScheme, bool isHighContrast, params Dictionary<string, string>[] valueSources)
        {
            if (isHighContrast)
            {
                themeDisplayName += " HighContrast";
            }

            var themeTempFileContent = ThemeGenerator.Current.GenerateColorSchemeFileContent(templateContent, themeName, themeDisplayName, baseColorScheme, colorScheme, alternativeColorScheme, isHighContrast, valueSources);

            var themeFilename = $"{themeName}";

            if (isHighContrast)
            {
                themeFilename += ".HighContrast";
            }

            var themeFile = Path.Combine(templateDirectory, $"{themeFilename}.xaml");

            Trace.WriteLine($"Checking \"{themeFile}\"...");

            var fileHasToBeWritten = File.Exists(themeFile) == false
                                     || ReadAllTextShared(themeFile) != themeTempFileContent
                                     || this.forceFileWrite;

            if (this.verbose)
            {
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine($"templateDirectory:      {templateDirectory}");
                Console.WriteLine($"themeName:              {themeName}");
                Console.WriteLine($"themeDisplayName:       {themeDisplayName}");
                Console.WriteLine($"baseColorScheme:        {baseColorScheme}");
                Console.WriteLine($"colorScheme:            {colorScheme}");
                Console.WriteLine($"alternativeColorScheme: {alternativeColorScheme}");
                Console.WriteLine($"isHighContrast:         {isHighContrast}");
                Console.WriteLine($"themeFilename:          {themeFilename}");
                Console.WriteLine($"themeFile:              {themeFile}");
                Console.WriteLine($"fileHasToBeWritten:     {fileHasToBeWritten}");
            }

            if (fileHasToBeWritten)
            {
                using (var sw = new StreamWriter(themeFile, false, Encoding.UTF8, BufferSize))
                {
                    sw.Write(themeTempFileContent);
                }

                if (this.verbose)
                {
                    Console.WriteLine($"  --> Resource Dictionary saved to \"{themeFile}\".");
                }
                else
                {
                    Trace.WriteLine($"Resource Dictionary saved to \"{themeFile}\".");
                }
            }
            else
            {
                if (this.verbose)
                {
                    Console.WriteLine("  --> New Resource Dictionary did not differ from existing file. No new file written.");
                }
                else
                {
                    Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
                }
            }
        }
    }
}