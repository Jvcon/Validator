using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsonSchemaTool.Models; 

namespace AHKestra
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // 1. 简化参数校验：必须至少有 schema 和一个 manifest
            if (args.Length < 2)
            {
                PrintHelp();
                return 1;
            }
            var format = GetArgumentValue(args, "--format") ?? "text";
            var operationalArgs = args.Where(a => !a.StartsWith("--format")).ToArray();

            var schemaPath = operationalArgs[0];
            var manifestPaths = operationalArgs.Skip(1);

            var validator = new Validator();

            try
            {
                // 调用验证引擎，获取按文件分组的结果
                var validationResults = await validator.ValidateAsync(schemaPath, manifestPaths);
                var allFilesValid = validationResults.All(r => r.IsValid);

                if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    // 机器可读输出
                    var report = new ValidationReport(allFilesValid, validationResults);
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));
                }
                else
                {
                    // 人类可读输出
                    PrintHumanReadableOutput(validationResults);
                }
                
                return allFilesValid ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"A critical error occurred: {ex.Message}");
                Console.ResetColor();
                return 1; // 致命错误，返回失败
            }

            return allFilesValid ? 0 : 1;
        }

        private static void PrintHumanReadableOutput(List<FileValidationResult> results)
        {
            foreach (var result in results)
            {
                Console.WriteLine($"--- Validating: {Path.GetFileName(result.FilePath)} ---");

                if (result.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[SUCCESS] Document is valid.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAILURE] Found {result.Errors.Count} error(s):");
                    
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - Path: {error.Path} (Line: {error.LineNumber})");
                        Console.WriteLine($"    Kind: {error.ErrorType}");
                    }
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }


        private static string? GetArgumentValue(string[] args, string argName)
        {
        var index = Array.FindIndex(args, a => a.Equals(argName, StringComparison.OrdinalIgnoreCase));
        return (index != -1 && index + 1 < args.Length) ? args[index + 1] : null;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("JSON Schema Validator CLI");
            Console.WriteLine("-------------------------");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  validator.exe <schema_path> <manifest_path_1> [<manifest_path_2>...]");
            Console.WriteLine("\nArguments:");
            Console.WriteLine("  <schema_path>        Path to the JSON Schema file.");
            Console.WriteLine("  <manifest_path>      One or more paths to JSON manifest files to validate.");
        }
    }
}
