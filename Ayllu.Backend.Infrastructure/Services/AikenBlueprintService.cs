using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ayllu.Backend.Infrastructure.Services
{
    public class AikenBlueprintService
    {
        private readonly string _blueprintPath;

        public AikenBlueprintService()
        {
            _blueprintPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "Blueprint", "plutus.json");
        }

        public BlueprintModel? Load()
        {
            if (!File.Exists(_blueprintPath))
                throw new FileNotFoundException($"No se encontró el archivo Plutus Blueprint en: {_blueprintPath}");

            var json = File.ReadAllText(_blueprintPath);
            return JsonSerializer.Deserialize<BlueprintModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
        }

        public Validator? GetValidator(string title)
        {
            var blueprint = Load();
            return blueprint?.Validators?.FirstOrDefault(v => v.Title == title);
        }

        public static async Task WriteTextEnvelopeAsync(string outputPath, string compiledCode)
        {
            var textEnvelope = new
            {
                type = "PlutusScriptV2",
                description = "Compiled Aiken Script",
                cborHex = compiledCode
            };

            var envelopeJson = JsonSerializer.Serialize(textEnvelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await File.WriteAllTextAsync(outputPath, envelopeJson);
        }

        public async Task<Dictionary<string, string>> GenerateValidatorAddressesAsync(string networkParam)
        {
            var validators = Load()?.Validators;
            if (validators == null || validators.Count == 0)
                throw new InvalidOperationException("No se encontraron validadores en el blueprint.");

            var result = new Dictionary<string, string>();
            var outputDir = Path.Combine(AppContext.BaseDirectory, "Scripts", "Addresses");
            Directory.CreateDirectory(outputDir);

            foreach (var validator in validators)
            {
                var plutusPath = Path.Combine(outputDir, $"{validator.Title}.plutus");

                var textEnvelope = new
                {
                    type = "PlutusScriptV2",
                    description = $"Validator {validator.Title}",
                    cborHex = validator.CompiledCode
                };

                var json = JsonSerializer.Serialize(textEnvelope, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(plutusPath, json);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cardano-cli",
                    Arguments = $"address build --payment-script-file \"{plutusPath}\" {networkParam}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                string address = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"Error generando dirección para {validator.Title}");

                result[validator.Title] = address.Trim();
            }

            return result;
        }


    }

    public class BlueprintModel
    {
        [JsonPropertyName("validators")]
        public List<Validator>? Validators { get; set; }
    }

    public class Validator
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("compiledCode")]
        public string CompiledCode { get; set; } = string.Empty;

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;
    }




}

