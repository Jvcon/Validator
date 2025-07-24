using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.Validation; // <-- 确保引用了验证的命名空间
using JsonSchemaTool.Models; // 引用我们新的数据模型

namespace AHKestra
{
 /// <summary>
    /// 封装了核心的 JSON Schema 验证逻辑。
    /// 这个类不关心命令行或控制台，只负责验证。
    /// </summary>
    public class Validator
    {
        /// <summary>
        /// 根据一个 Schema 文件，验证多个 Manifest 文件。
        /// </summary>
        /// <param name="schemaPath">JSON Schema 文件的路径。</param>
        /// <param name="manifestPaths">待验证的 JSON Manifest 文件路径列表。</param>
        /// <returns>
        /// 一个字典，其键是 Manifest 文件名，值是该文件的验证错误列表。
        /// 如果某个文件验证通过，其对应的错误列表将为空。
        /// </returns>
        public async Task<List<FileValidationResult>> ValidateAsync(
            string schemaPath,
            IEnumerable<string> manifestPaths)
        {
            var results = new List<FileValidationResult>();

            // 1. 一次性加载 Schema，以供后续所有文件验证使用
            JsonSchema schema;
            try
            {
                schema = await JsonSchema.FromFileAsync(schemaPath);
            }
            catch (Exception ex)
            {
                // 如果 Schema 本身加载失败，这是一个致命错误，直接抛出
                throw new InvalidOperationException($"Failed to load schema from '{schemaPath}'.", ex);
            }

            // 2. 遍历并验证每一个 Manifest 文件
            foreach (var path in manifestPaths)
            {
                if (!File.Exists(path))
                {
                    var fileNotFound = new SimplifiedValidationError("FileError", path, "", 0);
                    results.Add(new FileValidationResult(path, false, new List<SimplifiedValidationError> { fileNotFound }));
                    continue;
                }

                var jsonContent = await File.ReadAllTextAsync(path);
                var validationErrors = schema.Validate(jsonContent);

                var simplifiedErrors = validationErrors
                    .Select(err => new SimplifiedValidationError(
                        ErrorType: err.Kind.ToString(),
                        Path: err.Path ?? "N/A",
                        Property: err.Property ?? "N/A",
                        LineNumber: err.LineNumber))
                    .ToList();

                results.Add(new FileValidationResult(path, simplifiedErrors.Count == 0, simplifiedErrors));
            }

            return results;
        }
    }
}
