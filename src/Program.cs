using System.CommandLine;
using System.Text.Json; 
using AHKestra.Models; 

namespace AHKestra
{
    public class Program
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

        // 核心业务逻辑
        private static async Task RunValidationAsync(FileInfo schema, FileInfo[] manifests, string format)
        {
            var validator = new Validator();
            var manifestPaths = manifests.Select(m => m.FullName);
            
            try
            {
                var validationResults = await validator.ValidateAsync(schema.FullName, manifestPaths);
                var allFilesValid = validationResults.All(r => r.IsValid);

                if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    PrintJsonOutput(allFilesValid, validationResults);
                }
                else
                {
                    PrintHumanReadableOutput(validationResults);
                }
                
                Environment.ExitCode = allFilesValid ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"A critical error occurred: {ex.Message}");
                Console.ResetColor();
                Environment.ExitCode = 1;
            }
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
    }
}
