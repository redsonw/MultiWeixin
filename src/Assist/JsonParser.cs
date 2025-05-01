using MultiWeixin.Models;
using Serilog;
using System.Net.Http;
using System.Text.Json;

namespace MultiWeixin.Assist;

/// <summary>
/// JSON解析器类，用于解析和序列化JSON数据。
/// </summary>
public partial class JsonParser(string url)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonSerializerOptions _serializeOptions =new()
    {
        WriteIndented = true, // 美化输出
        // PropertyNamingPolicy = JsonNamingPolicy.CamelCase // 使用小驼峰命名
    };

    private readonly string _url = url;

    /// <summary>
    /// 异步解析在线JSON文件。
    /// </summary>
    /// <returns>解析后的PatchDetails对象，如果解析失败则返回null。</returns>
    public async Task<PatchDetails?> ParseAsync()
    {
        try
        {
            // 从 URL 获取 JSON 字符串
            string jsonString = await FetchJsonAsync(_url);

            // 尝试反序列化 JSON 字符串
            var patchDetails = JsonSerializer.Deserialize<PatchDetails>(jsonString, _jsonOptions) ?? throw new JsonException("反序列化时返回了 null 对象。");

            Log.Information("特征字库文件解析完成。");

            return patchDetails;
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"网络连接错误: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Log.Error($"JSON 解析错误: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error($"解析微信版本信息时发生错误: {ex.Message}");
        }

        // 发生错误时返回 null
        return null;
    }

    /// <summary>
    /// 从指定的URL获取JSON字符串。
    /// </summary>
    /// <param name="jsonUrl">JSON文件的URL。</param>
    /// <returns>JSON字符串。</returns>
    /// <exception cref="ArgumentException">当URL为空或无效时抛出。</exception>
    /// <exception cref="HttpRequestException">当网络请求失败时抛出。</exception>
    /// <exception cref="TimeoutException">当请求超时时抛出。</exception>
    private static async Task<string> FetchJsonAsync(string jsonUrl)
    {
        if (string.IsNullOrWhiteSpace(jsonUrl))
            throw new ArgumentException("URL 不能为空或无效。", nameof(jsonUrl));

        using HttpClient client = new();

        try
        {
            HttpResponseMessage response = await client.GetAsync(jsonUrl);
            response.EnsureSuccessStatusCode(); // 检查响应是否成功

            Log.Information("特征文件获取成功。");

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            Log.Error($"请求失败: {jsonUrl}，错误信息: {ex.Message}");
            // 捕获网络请求异常并抛出
            throw new HttpRequestException($"请求失败: {jsonUrl}，错误信息: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {

            Log.Error($"请求超时: {jsonUrl}，错误信息: {ex.Message}");
            // 捕获请求超时等任务取消的异常
            throw new TimeoutException($"请求超时: {jsonUrl}，错误信息: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 序列化PatchDetails对象为JSON字符串。
    /// </summary>
    /// <param name="version">微信版本号。</param>
    /// <param name="offset">偏移量。</param>
    /// <param name="oldValue">旧值。</param>
    /// <param name="newValue">新值。</param>
    /// <returns>序列化后的JSON字符串。</returns>
    public string SerializeJson(string version, string offset, string oldValue, string newValue)
    {
        // 创建并填充 PatchDetails 对象
        var patchDetails = new PatchDetails
        {
            WeChat = new WeChat
            {
                Version = new Dictionary<string, VersionDetail>
                {
                    {
                        version, new VersionDetail(offset,oldValue,newValue)
                    }
                }
            }
        };

        // 序列化对象为 JSON 字符串
        return JsonSerializer.Serialize(patchDetails, _serializeOptions);
    }

}
