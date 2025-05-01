using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiWeixin.Models
{
    /// <summary>
    /// 二进制替换操作配置项
    /// </summary>
    public class Options
    {
        /// <summary>
        /// 目标文件路径
        /// </summary>
        public string? FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 指定的内存地址
        /// <para>0x 开头的十六进制基址</para>
        /// </summary>
        public string? OffsetByte { get; set; } = string.Empty;

        /// <summary>
        /// 原始字节
        /// </summary>
        public string? OldValue {  get; set; } = string.Empty;

        /// <summary>
        /// 欲替换的字节
        /// </summary>
        public string? NewValue {  get; set; } = string.Empty;

        /// <summary>
        /// 要搜索的字节模式（null 表示通配符）
        /// <para>示例：[0x01, null, 0xFF] 表示匹配 0x01??FF 模式</para>
        /// </summary>
        public IEnumerable<byte?> SearchByte { get; set; } = [];

        /// <summary>
        /// 要替换的字节模式（null 表示保持原样）
        /// <para>长度必须与 SearchByte 保持一致</para>
        /// </summary>
        public IEnumerable<byte?> ReplaceByte { get; set; } = [];
    }
}
