using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.Validation; // <-- 确保引用了验证的命名空间

namespace AHKestra
{
    public static class Validator
    {
        public static async Task<ICollection<string>> Validate(string schemaPath, string manifestPath, bool ci)
        {
            var errors = new List<string>();

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

            // 对 JSON 内容进行验证
            ICollection<ValidationError> validationErrors = schema.Validate(manifestContent);

            if (validationErrors.Count == 0)
            {
                return errors; // 返回空列表，表示验证通过
            }

            // --- 这是需要修改的核心部分 ---
            foreach (var error in validationErrors)
            {
                StringBuilder sb = new StringBuilder();
                var prefix = ci ? "    " : "";
                
                // 使用 error.Kind 来获取具体的错误类型，代替不存在的 error.Description
                // 使用 error.Path 来指明错误位置
                sb.AppendLine($"{prefix}{(ci ? "[*]" : "-")} Error: Validation failed at path '{error.Path}'.");
                
                // 使用结构化的信息构建更清晰的错误报告
                sb.AppendLine($"{prefix}  {(ci ? "[^]" : " ")} Kind: {error.Kind}");
                sb.AppendLine($"{prefix}  {(ci ? "[^]" : " ")} Property: '{error.Property}'");
                sb.AppendLine($"{prefix}  {(ci ? "[^]" : " ")} Location: Line {error.LineNumber}:{error.LinePosition}");
                
                errors.Add(sb.ToString().TrimEnd());
            }

            return errors;
        }
    }
}
