using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine;
using System.Text.Json; 
using AHKestra.Models; 

namespace AHKestra
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
             public static async Task<int> Main(string[] args)
        {
            // 1. 定义我们需要的参数和选项
            var schemaArgument = new Argument<FileInfo>(
                name: "schema",
                description: "Path to the JSON Schema file.")
            {
                // 确保文件存在
                Arity = ArgumentArity.ExactlyOne
            };
            schemaArgument.ExistingOnly();

            var manifestsArgument = new Argument<FileInfo[]>(
                name: "manifests",
                description: "One or more paths to JSON manifest files to validate.")
            {
                // 允许多个文件
                Arity = ArgumentArity.OneOrMore
            };
            manifestsArgument.ExistingOnly();

            var formatOption = new Option<string>(
                name: "--format",
                // 为选项提供默认值和描述
                getDefaultValue: () => "text",
                description: "The output format ('text' or 'json').");
            formatOption.AddAlias("-f"); 

            var rootCommand = new RootCommand("A command-line tool to validate JSON files against a JSON Schema, for the AHKestra project.")
            {
                schemaArgument,
                manifestsArgument,
                formatOption
            };

            // 3. 设置处理程序，将解析后的参数直接、安全地传递给我们的业务逻辑
            rootCommand.SetHandler(async (schema, manifests, format) =>
            {
                // 这里的 schema, manifests, format 都是已经由 System.CommandLine 
                // 解析并验证过的强类型对象，不再需要手动处理 string[] args。
                await RunValidationAsync(schema, manifests, format);
            }, schemaArgument, manifestsArgument, formatOption);

            // 4. 执行命令，库会自动处理解析、错误报告和帮助文本显示
            return await rootCommand.InvokeAsync(args);
        }

        private static void PrintJsonOutput(bool allValid, List<FileValidationResult> results)
        {
            var report = new ValidationReport(allValid, results);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));
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
            if (index != -1 && index + 1 < args.Length)
            {
                // 确保我们不把下一个参数误认为是值
                if (!args[index + 1].StartsWith("--"))
                {
                    return args[index + 1];
                }
            }
            return null;
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
