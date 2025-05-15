using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.ValueObjects
{
    /// <summary>
    /// 代表一个经过验证的文件名值对象。
    /// </summary>
    public class FileName : ValueObject
    {
        /// <summary>
        /// 获取文件名字符串。
        /// </summary>
        public string Value { get; }

        private FileName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// 创建一个新的 <see cref="FileName"/> 实例。
        /// </summary>
        /// <param name="fileName">原始文件名字符串。</param>
        /// <returns>一个新的文件名实例。</returns>
        /// <exception cref="ArgumentException">当文件名为空、空白或包含非法字符时抛出。</exception>
        public static FileName Create(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("文件名不能为空。", nameof(fileName));
            }

            // 检查是否包含非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.Any(c => invalidChars.Contains(c)))
            {
                throw new ArgumentException($"文件名 '{fileName}' 包含非法字符。", nameof(fileName));
            }

            // 此处可以添加其他验证，例如长度限制、特定扩展名要求等

            return new FileName(fileName.Trim()); // 去除首尾空格并创建
        }

        /// <summary>
        /// 获取文件的扩展名（包括点 "."）。
        /// </summary>
        /// <returns>文件的扩展名；如果文件没有扩展名，则返回空字符串。</returns>
        public string GetExtension()
        {
            return Path.GetExtension(Value);
        }

        /// <summary>
        /// 获取不带扩展名的文件名。
        /// </summary>
        /// <returns>不带扩展名的文件名。</returns>
        public string GetNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(Value);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// 返回文件名的字符串表示形式。
        /// </summary>
        /// <returns>文件名字符串。</returns>
        public static implicit operator string(FileName fileName) => fileName.Value;
    }
}