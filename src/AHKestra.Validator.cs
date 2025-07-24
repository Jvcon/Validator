using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema; // <-- 使用 NJsonSchema 的命名空间

namespace AHKestra
{
    public static class Validator
    {
        // 使用 NJsonSchema 进行验证，这是一个异步过程
        public static async Task<ICollection<string>> Validate(string schemaPath, string manifestPath, bool ci)
        {
            var errors = new List<string>();

            // 1. 异步从文件加载 Schema
            // NJsonSchema 提供了便捷的异步方法来加载文件
            // 参考: [RicoSuter/NJsonSchema: JSON Schema reader, generator ... - GitHub](https://github.com/RicoSuter/NJsonSchema){target="_blank" class="gpt-web-url"}
            JsonSchema schema;
            try
            {
                schema = await JsonSchema.FromFileAsync(schemaPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading schema file '{schemaPath}': {ex.Message}");
                return errors;
            }
            
            // 2. 读取待验证的 JSON 文件内容
            string manifestContent;
            try
            {
                manifestContent = File.ReadAllText(manifestPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading manifest file '{manifestPath}': {ex.Message}");
                return errors;
            }

            // 3. 对 JSON 内容进行验证
            // .Validate() 方法会返回一个验证错误的集合
            // 参考: [Validating JSON with schema in .NET | by Nitesh Singhal | Medium](https://medium.com/@niteshsinghal85/validating-json-with-schema-in-net-7bdc02b0ef3c){target="_blank" class="gpt-web-url"}
            var validationErrors = schema.Validate(manifestContent);

            if (validationErrors.Count == 0)
            {
                return errors; // 返回空列表，表示验证通过
            }

            // 4. 格式化错误信息
            foreach (var error in validationErrors)
            {
                StringBuilder sb = new StringBuilder();
                var prefix = ci ? "    " : "";
                
                sb.AppendLine($"{prefix}{(ci ? "[*]" : "-")} Error: {error.Kind}");
                sb.AppendLine($"{prefix}  {(ci ? "[^]" : " ")} Message: {error.Path} - {error.Description}");
                sb.AppendLine($"{prefix}  {(ci ? "[^]" : " ")} Line: {error.LineNumber}:{error.LinePosition}");
                
                errors.Add(sb.ToString().TrimEnd());
            }

            return errors;
        }
    }
}
