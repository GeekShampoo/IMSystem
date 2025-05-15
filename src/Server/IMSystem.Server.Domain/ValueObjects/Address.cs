using System;
using System.Collections.Generic;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.ValueObjects
{
    /// <summary>
    /// 代表一个地理地址的值对象。
    /// </summary>
    public class Address : ValueObject
    {
        /// <summary>
        /// 街道地址。
        /// </summary>
        public string Street { get; }
        /// <summary>
        /// 城市名称。
        /// </summary>
        public string City { get; }
        /// <summary>
        /// 州或省份名称。
        /// </summary>
        public string StateOrProvince { get; }
        /// <summary>
        /// 国家名称。
        /// </summary>
        public string Country { get; }
        /// <summary>
        /// 邮政编码。
        /// </summary>
        public string ZipCode { get; }

        private Address(string street, string city, string stateOrProvince, string country, string zipCode)
        {
            Street = street;
            City = city;
            StateOrProvince = stateOrProvince;
            Country = country;
            ZipCode = zipCode;
        }

        /// <summary>
        /// 创建一个新的 <see cref="Address"/> 实例。
        /// </summary>
        /// <param name="street">街道地址。</param>
        /// <param name="city">城市。</param>
        /// <param name="stateOrProvince">州/省。</param>
        /// <param name="country">国家。</param>
        /// <param name="zipCode">邮政编码。</param>
        /// <returns>一个新的地址实例。</returns>
        /// <exception cref="ArgumentException">当任何参数为空或空白时抛出。</exception>
        public static Address Create(string street, string city, string stateOrProvince, string country, string zipCode)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("街道不能为空。", nameof(street));
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("城市不能为空。", nameof(city));
            if (string.IsNullOrWhiteSpace(stateOrProvince))
                throw new ArgumentException("州/省不能为空。", nameof(stateOrProvince));
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("国家不能为空。", nameof(country));
            if (string.IsNullOrWhiteSpace(zipCode))
                throw new ArgumentException("邮政编码不能为空。", nameof(zipCode));

            // 此处可以添加更复杂的验证逻辑，例如邮编格式、国家/地区有效性等

            return new Address(street, city, stateOrProvince, country, zipCode);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return StateOrProvince;
            yield return Country;
            yield return ZipCode;
        }

        public override string ToString()
        {
            return $"{Street}, {City}, {StateOrProvince}, {Country} {ZipCode}";
        }

        // 可以根据需要添加其他方法，例如格式化地址等
    }
}