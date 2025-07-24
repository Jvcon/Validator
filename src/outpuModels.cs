// /src/OutputModels.cs

using System.Text.Json.Serialization;

namespace JsonSchemaTool.Models
{
    /// <summary>
    /// 代表单次验证操作的总体结果。
    /// </summary>
    public record ValidationReport(
        [property: JsonPropertyName("allFilesValid")] bool AllFilesValid,
        [property: JsonPropertyName("results")] List<FileValidationResult> Results
    );

    /// <summary>
    /// 代表单个文件的验证结果。
    /// </summary>
    public record FileValidationResult(
        [property: JsonPropertyName("filePath")] string FilePath,
        [property: JsonPropertyName("isValid")] bool IsValid,
        [property: JsonPropertyName("errors")] List<SimplifiedValidationError> Errors
    );

    /// <summary>
    /// 代表一个简化的、易于解析的验证错误。
    /// </summary>
    public record SimplifiedValidationError(
        [property: JsonPropertyName("errorType")] string ErrorType,
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("property")] string Property,
        [property: JsonPropertyName("lineNumber")] int LineNumber
    );
}
