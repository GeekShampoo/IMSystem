using IMSystem.Server.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IMSystem.Server.Domain.ValueObjects
{
    /// <summary>
    /// Email 值对象
    /// </summary>
    public class Email : ValueObject
    {
        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("邮箱地址不能为空。", nameof(email));
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(email))
            {
                throw new ArgumentException("无效的邮箱地址格式。", nameof(email));
            }

            return new Email(email.ToLowerInvariant().Trim()); // 统一转为小写并去除首尾空格
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(Email email) => email.Value;
    }

}