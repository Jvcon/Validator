using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AHKestra
{
    public class Program
    {
        // Main方法需要是异步的，以支持 NJsonSchema 的异步加载
        public static async Task<int> Main(string[] args)
        {
            bool ci = String.Format("{0}", Environment.GetEnvironmentVariable("CI")).ToLower() == "true";
            bool allValid = true;

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: validator.exe <schema> <manifest> [<manifest>...]");
                return 1;
            }

            // 解析参数的逻辑保持不变
            IList<string> manifests = args.ToList<String>();
            String schemaPath = manifests.First();
            manifests.RemoveAt(0);
            String combinedArgs = String.Join("", manifests);
            if(combinedArgs.Contains("*") || combinedArgs.Contains("?")) {
                try {
                    // 这段路径处理逻辑可以保持
                    var path = new Uri(Path.Combine(Directory.GetCurrentDirectory(), combinedArgs)).LocalPath;
                    var drive = Path.GetPathRoot(path);
                    var pattern = path.Replace(drive, "");
                    manifests = Directory.GetFiles(drive, pattern).ToList<String>();
                } catch (System.ArgumentException ex) {
                    Console.WriteLine("Invalid path provided! ({0})", ex.Message);
                    return 1;
                }
            }

            // 对每个 manifest 文件进行验证
            foreach(var manifestPath in manifests) {
                // 调用新的静态异步验证方法
                var errors = await AHKestra.Validator.Validate(schemaPath, manifestPath, ci);

                var prefix = ci ? "      " : "";
                if (errors.Count == 0)
                {
                    Console.WriteLine($"{prefix}[+] {Path.GetFileName(manifestPath)} validates against the schema!");
                }
                else
                {
                    Console.WriteLine($"{prefix}[-] {Path.GetFileName(manifestPath)} has {errors.Count} Error{(errors.Count > 1 ? "s" : "")}!");
                    allValid = false;
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error);
                    }
                }
            }

            return allValid ? 0 : 1;
        }
    }
}
