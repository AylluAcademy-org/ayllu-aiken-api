using Ayllu.Backend.Application.Interfaces;
using Ayllu.Backend.Domain.Configuration;
using Ayllu.Backend.Domain.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Ayllu.Backend.Infrastructure.Services
{
    public class CardanoCliService : ICardanoTransactionService
    {
        private readonly CardanoSettings _config;

        public CardanoCliService(IOptions<CardanoSettings> options)
        {
            _config = options.Value;
        }

        public async Task<(bool Success, string Message)> TransferTokenAsync(TransferTokenRequest request)
        {
            if (request.Amount <= 0)
                return (false, "❌ La cantidad de tokens debe ser mayor a cero.");

            // Validar configuración
            if (string.IsNullOrWhiteSpace(_config.Network))
                return (false, "❌ Configuración de red no definida.");

            string senderAddr = await File.ReadAllTextAsync(_config.GetFullPath(_config.WalletAddressPath!));
            string skeyPath = _config.GetFullPath(_config.SigningKeyPath!);
            string policyId = _config.PolicyId!;
            string tokenHex = _config.TokenHex!;

            // Formatear la red correctamente
            string networkParam = FormatNetworkParameter(_config.Network);
            string tokenUnit = $"{policyId}.{tokenHex}";

            // Use absolute paths and ensure directory exists
            string workingDir = Path.GetTempPath();
            string txRaw = Path.Combine(workingDir, $"tx-{Guid.NewGuid():N}.raw");
            string txSigned = Path.Combine(workingDir, $"tx-{Guid.NewGuid():N}.signed");
            string protocolParamsFile = Path.Combine(workingDir, $"protocol-{Guid.NewGuid():N}.json");

            try
            {
                // Obtener UTxOs necesarios
                var utxoResult = await GetUtxosForTransactionAsync(senderAddr, policyId, tokenHex, (ulong)request.Amount);
                if (!utxoResult.Success)
                    return (false, utxoResult.Message);

                var utxos = utxoResult.Utxos;
                ulong totalAda = (ulong)utxos.Sum(u => (long)u.AdaAmount);
                ulong totalTokens = (ulong)utxos.Sum(u => (long)u.TokenAmount);

                Console.WriteLine($"📊 UTxOs seleccionados: {utxos.Count}");
                Console.WriteLine($"💰 Total ADA: {totalAda} lovelace");
                Console.WriteLine($"🪙 Total tokens: {totalTokens}");

                // Obtener protocol parameters
                string protocolCmd = $"cardano-cli query protocol-parameters {networkParam} --out-file \"{protocolParamsFile}\"";
                Console.WriteLine($"🔧 Ejecutando comando de protocolo: {protocolCmd}");

                var protocolResult = await RunCommandAsync(protocolCmd);
                if (!protocolResult.Success)
                    return (false, $"❌ Error al obtener parámetros del protocolo:\n{protocolResult.Message}");

                // Construir inputs
                string txInputs = string.Join(" ", utxos.Select(u => $"--tx-in \"{u.UtxoId}\""));

                // Calcular valores iniciales
                ulong receiverOutput = 10_000_000; // 10 ADA para el receptor
                ulong tokensToSend = (ulong)request.Amount;
                ulong tokensRemaining = totalTokens - tokensToSend;

                // Usar el método build para calcular automáticamente el fee
                string buildCmd = string.Join(" ", new[]
                {
                    "cardano-cli",
                    "conway",
                    "transaction",
                    "build",
                    networkParam,
                    txInputs,
                    $"--tx-out \"{request.ReceiverAddress}+{receiverOutput}+{tokensToSend} {tokenUnit}\"",
                    $"--change-address \"{senderAddr.Trim()}\"",
                    $"--out-file \"{txRaw}\""
                });

                Console.WriteLine($"🔧 Ejecutando comando build: {buildCmd}");

                var buildResult = await RunCommandAsync(buildCmd);
                if (!buildResult.Success)
                {
                    Console.WriteLine($"❌ Error en build. Comando ejecutado: {buildCmd}");
                    return (false, $"❌ Error al construir transacción:\n{buildResult.Message}");
                }

                Console.WriteLine($"✅ Transacción construida: {txRaw}");

                // Verificar que el archivo se creó
                if (!File.Exists(txRaw))
                {
                    return (false, $"❌ El archivo de transacción no se creó: {txRaw}");
                }

                // Firmar
                string signCmd = string.Join(" ", new[]
                {
                    "cardano-cli",
                    "conway",
                    "transaction",
                    "sign",
                    $"--tx-body-file \"{txRaw}\"",
                    $"--signing-key-file \"{skeyPath}\"",
                    networkParam,
                    $"--out-file \"{txSigned}\""
                });

                Console.WriteLine($"🔧 Ejecutando comando sign: {signCmd}");

                var sign = await RunCommandAsync(signCmd);
                if (!sign.Success)
                    return (false, $"❌ Error al firmar:\n{sign.Message}");

                Console.WriteLine($"✅ Transacción firmada: {txSigned}");

                // Enviar
                string submitCmd = string.Join(" ", new[]
                {
                    "cardano-cli",
                    "conway",
                    "transaction",
                    "submit",
                    $"--tx-file \"{txSigned}\"",
                    networkParam
                });

                Console.WriteLine($"🔧 Ejecutando comando submit: {submitCmd}");

                var submit = await RunCommandAsync(submitCmd);
                if (!submit.Success)
                    return (false, $"❌ Error al enviar:\n{submit.Message}");

                Console.WriteLine($"✅ Transacción enviada exitosamente");
                return (true, "✅ Transacción enviada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción general: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return (false, $"❌ Error inesperado: {ex.Message}");
            }
            finally
            {
                TryDelete(txRaw);
                TryDelete(txSigned);
                TryDelete(protocolParamsFile);
                TryDelete("utxos.json");
            }
        }

        /// <summary>
        /// Formatea el parámetro de red según la configuración
        /// </summary>
        private string FormatNetworkParameter(string network)
        {
            // Si ya viene formateado, devolverlo tal como está
            if (network.StartsWith("--mainnet") || network.StartsWith("--testnet-magic"))
                return network;

            // Si es "mainnet", devolver --mainnet
            if (network.Equals("mainnet", StringComparison.OrdinalIgnoreCase))
                return "--mainnet";

            // Si es "testnet" o contiene "testnet", usar testnet-magic
            if (network.Contains("testnet", StringComparison.OrdinalIgnoreCase))
            {
                // Extraer el número mágico si existe
                var parts = network.Split(' ', '-', '_');
                foreach (var part in parts)
                {
                    if (int.TryParse(part, out int magic))
                        return $"--testnet-magic {magic}";
                }
                // Si no se encuentra número mágico, usar el predeterminado para preview
                return "--testnet-magic 2";
            }

            // Por defecto, asumir mainnet
            return "--mainnet";
        }

        /// <summary>
        /// Información de UTxO con tokens
        /// </summary>
        private class UtxoTokenInfo
        {
            public string UtxoId { get; set; } = "";
            public ulong AdaAmount { get; set; }
            public ulong TokenAmount { get; set; }
        }

        /// <summary>
        /// Resultado de la búsqueda de UTxOs
        /// </summary>
        private class UtxoSearchResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public List<UtxoTokenInfo> Utxos { get; set; } = new();
        }

        /// <summary>
        /// Obtiene los UTxOs necesarios para realizar la transacción
        /// </summary>
        private async Task<UtxoSearchResult> GetUtxosForTransactionAsync(string address, string policyId, string assetNameHex, ulong requiredTokens)
        {
            string networkParam = FormatNetworkParameter(_config.Network!);
            string utxoFile = Path.Combine(Path.GetTempPath(), $"utxos-{Guid.NewGuid():N}.json");

            string cmd = $"cardano-cli query utxo --address {address} {networkParam} --out-file \"{utxoFile}\"";
            Console.WriteLine($"🔧 Ejecutando comando UTxO: {cmd}");

            var result = await RunCommandAsync(cmd);

            if (!result.Success || !File.Exists(utxoFile))
            {
                return new UtxoSearchResult
                {
                    Success = false,
                    Message = $"❌ Error al obtener UTxOs: {result.Message}"
                };
            }

            try
            {
                string json = await File.ReadAllTextAsync(utxoFile);
                Console.WriteLine($"🔍 JSON UTxO obtenido: {json}");

                using var doc = JsonDocument.Parse(json);

                var tokenUtxos = new List<UtxoTokenInfo>();
                var adaUtxos = new List<UtxoTokenInfo>();

                Console.WriteLine($"🔍 Buscando tokens con PolicyId: {policyId}, AssetName: {assetNameHex}");

                foreach (var utxo in doc.RootElement.EnumerateObject())
                {
                    var utxoKey = utxo.Name;
                    var value = utxo.Value.GetProperty("value");

                    Console.WriteLine($"🔍 Analizando UTxO: {utxoKey}");

                    if (!value.TryGetProperty("lovelace", out var adaProp))
                    {
                        Console.WriteLine($"❌ UTxO {utxoKey} no tiene lovelace");
                        continue;
                    }

                    ulong adaAmount = adaProp.GetUInt64();
                    Console.WriteLine($"💰 UTxO {utxoKey} tiene {adaAmount} lovelace");

                    var utxoInfo = new UtxoTokenInfo
                    {
                        UtxoId = utxoKey,
                        AdaAmount = adaAmount,
                        TokenAmount = 0
                    };

                    // Buscar tokens en este UTxO
                    bool hasTargetTokens = false;
                    foreach (var assetGroup in value.EnumerateObject())
                    {
                        if (assetGroup.Name == "lovelace") continue;

                        Console.WriteLine($"🪙 UTxO {utxoKey} tiene asset group: {assetGroup.Name}");

                        if (assetGroup.Value.ValueKind == JsonValueKind.Object && assetGroup.Name == policyId)
                        {
                            Console.WriteLine($"✅ PolicyId coincide: {policyId}");

                            foreach (var token in assetGroup.Value.EnumerateObject())
                            {
                                Console.WriteLine($"🔍 Token encontrado: {token.Name} = {token.Value}");

                                if (token.Name == assetNameHex)
                                {
                                    ulong tokenAmount = token.Value.GetUInt64();
                                    Console.WriteLine($"✅ Token {assetNameHex} encontrado con cantidad: {tokenAmount}");

                                    utxoInfo.TokenAmount = tokenAmount;
                                    hasTargetTokens = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (hasTargetTokens)
                    {
                        tokenUtxos.Add(utxoInfo);
                        Console.WriteLine($"✅ UTxO con tokens agregado: {utxoKey}");
                    }
                    else if (adaAmount >= 1_000_000) // Solo considerar UTxOs con al menos 1 ADA
                    {
                        adaUtxos.Add(utxoInfo);
                        Console.WriteLine($"💰 UTxO con ADA agregado: {utxoKey}");
                    }
                }

                // Verificar si tenemos suficientes tokens
                ulong totalTokens = (ulong)tokenUtxos.Sum(u => (long)u.TokenAmount);
                if (totalTokens < requiredTokens)
                {
                    return new UtxoSearchResult
                    {
                        Success = false,
                        Message = $"❌ No hay suficientes tokens. Requeridos: {requiredTokens}, Disponibles: {totalTokens}"
                    };
                }

                // Seleccionar UTxOs con tokens necesarios
                var selectedUtxos = new List<UtxoTokenInfo>();
                ulong selectedTokens = 0;

                // Ordenar UTxOs con tokens por cantidad (descendente) para optimizar
                foreach (var utxo in tokenUtxos.OrderByDescending(u => u.TokenAmount))
                {
                    selectedUtxos.Add(utxo);
                    selectedTokens += utxo.TokenAmount;

                    if (selectedTokens >= requiredTokens)
                        break;
                }

                // Calcular ADA total disponible
                ulong totalAda = (ulong)selectedUtxos.Sum(u => (long)u.AdaAmount);
                ulong requiredAda = 10_000_000 + 1_000_000 + 500_000; // receptor (10 ADA) + cambio (1 ADA) + fee estimado

                Console.WriteLine($"💰 ADA disponible en UTxOs con tokens: {totalAda}");
                Console.WriteLine($"💰 ADA requerido estimado: {requiredAda}");

                // Si no hay suficiente ADA, agregar UTxOs adicionales
                if (totalAda < requiredAda)
                {
                    ulong additionalAdaNeeded = requiredAda - totalAda;
                    Console.WriteLine($"💰 ADA adicional necesario: {additionalAdaNeeded}");

                    foreach (var utxo in adaUtxos.OrderByDescending(u => u.AdaAmount))
                    {
                        selectedUtxos.Add(utxo);
                        totalAda += utxo.AdaAmount;

                        if (totalAda >= requiredAda)
                            break;
                    }
                }

                // Verificación final
                totalAda = (ulong)selectedUtxos.Sum(u => (long)u.AdaAmount);
                if (totalAda < requiredAda)
                {
                    return new UtxoSearchResult
                    {
                        Success = false,
                        Message = $"❌ No hay suficiente ADA. Requerido: {requiredAda}, Disponible: {totalAda}"
                    };
                }

                Console.WriteLine($"✅ UTxOs seleccionados: {selectedUtxos.Count}");
                Console.WriteLine($"✅ Total ADA: {totalAda}");
                Console.WriteLine($"✅ Total tokens: {(ulong)selectedUtxos.Sum(u => (long)u.TokenAmount)}");

                return new UtxoSearchResult
                {
                    Success = true,
                    Message = "UTxOs encontrados correctamente",
                    Utxos = selectedUtxos
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing UTxO JSON: {ex.Message}");
                return new UtxoSearchResult
                {
                    Success = false,
                    Message = $"❌ Error al procesar UTxOs: {ex.Message}"
                };
            }
            finally
            {
                TryDelete(utxoFile);
            }
        }

        private async Task<(bool Success, string Message)> RunCommandAsync(string cmd)
        {
            try
            {
                Console.WriteLine($"🔧 Ejecutando comando: {cmd}");

                var psi = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{cmd.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi)!;
                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Console.WriteLine($"📤 Exit code: {process.ExitCode}");
                if (!string.IsNullOrEmpty(stdout))
                    Console.WriteLine($"📤 Stdout: {stdout}");
                if (!string.IsNullOrEmpty(stderr))
                    Console.WriteLine($"📤 Stderr: {stderr}");

                return process.ExitCode == 0
                    ? (true, stdout.Trim())
                    : (false, stderr.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en RunCommandAsync: {ex.Message}");
                return (false, $"Excepción: {ex.Message}");
            }
        }

        private void TryDelete(string file)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }

        public async Task<(bool success, string message)> RegisterStudentAsync(RegisterStudentRequest request)
        {
            string? tempFolder = null;
            try
            {
                // 1. Validación básica
                if (string.IsNullOrWhiteSpace(request.TxHash) ||
                    string.IsNullOrWhiteSpace(request.StudentAddress) ||
                    string.IsNullOrWhiteSpace(request.StudentPKH))
                {
                    return (false, "❌ Datos de entrada incompletos.");
                }

                // 2. Cargar validador desde blueprint
                var blueprintService = new AikenBlueprintService();
                var validator = blueprintService.GetValidator("registration_validator.registration_validator.spend");

                if (validator == null)
                    return (false, "❌ No se encontró el validador en el blueprint.");

                // 3. Crear carpeta temporal
                tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempFolder);

                // 4. Guardar archivos temporales
                var scriptFile = Path.Combine(tempFolder, "validator.plutus");
                await AikenBlueprintService.WriteTextEnvelopeAsync(scriptFile, validator.CompiledCode);

                var datumPath = Path.Combine(tempFolder, "datum.json");
                var redeemerPath = Path.Combine(tempFolder, "redeemer.json");
                var txRaw = Path.Combine(tempFolder, "tx.raw");
                var txSigned = Path.Combine(tempFolder, "tx.signed");

                // 5. Crear datum y redeemer
                var datumData = new PlutusData
                {
                    Constructor = 0,
                    Fields = new List<object>
                        {
                            new PlutusBytes(request.StudentPKH)
                        }
                };

                await File.WriteAllTextAsync(datumPath, JsonSerializer.Serialize(datumData));

                // Crear PlutusData válido para redeemer
                var redeemerData = new PlutusData
                {
                    Constructor = 0,
                    Fields = new List<object>
                    {
                        new PlutusData
                            {
                                Constructor = 0, // Action.Register
                                Fields = new List<object>()
                            },
                            new PlutusBytes(request.StudentPKH),
                            new PlutusData
                            {
                                Constructor = 0,
                                Fields = new List<object>
                                {
                                    new PlutusBytes(request.TxHash),
                                    new PlutusInt(request.OutputIndex)
                                }
                            }
                        }
                };

                                    await File.WriteAllTextAsync(redeemerPath, JsonSerializer.Serialize(redeemerData));
                // 6. Construir la transacción
                string networkParam = FormatNetworkParameter(_config.Network!);
                string txIn = $"{request.TxHash}#{request.OutputIndex}";
                string txOut = $"{request.StudentAddress}+2000000";

                var buildArgs = new[]
                {
            "conway", "transaction", "build",
            networkParam,
            "--tx-in", txIn,
            "--tx-in-script-file", scriptFile,
            "--tx-in-datum-file", datumPath,
            "--tx-in-redeemer-file", redeemerPath,
            "--tx-out", txOut,
            "--change-address", request.StudentAddress,
            "--out-file", txRaw
        };

                var buildResult = await RunCliAsync("cardano-cli", buildArgs);
                if (!buildResult.success)
                    return (false, $"❌ Error al construir la transacción:\n{buildResult.message}");

                // 7. Firmar
                var skeyPath = _config.GetFullPath(_config.SigningKeyPath!);
                var signArgs = new[]
                {
            "transaction", "sign",
            "--tx-body-file", txRaw,
            "--signing-key-file", skeyPath,
            networkParam,
            "--out-file", txSigned
        };

                var signResult = await RunCliAsync("cardano-cli", signArgs);
                if (!signResult.success)
                    return (false, $"❌ Error al firmar la transacción:\n{signResult.message}");

                // 8. Enviar
                var submitArgs = new[]
                {
            "transaction", "submit",
            networkParam,
            "--tx-file", txSigned
        };

                var submitResult = await RunCliAsync("cardano-cli", submitArgs);
                if (!submitResult.success)
                    return (false, $"❌ Error al enviar la transacción:\n{submitResult.message}");

                return (true, "✅ Estudiante registrado y transacción enviada.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error general: {ex.Message}");
            }
            finally
            {
                if (tempFolder != null && Directory.Exists(tempFolder))
                {
                    try { Directory.Delete(tempFolder, true); } catch { }
                }
            }
        }





        private async Task<(bool success, string message)> RunCliAsync(string command, string[] args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = psi };
                process.Start();

                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                return process.ExitCode == 0
                    ? (true, stdout.Trim())
                    : (false, stderr.Trim());
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }



    }
}