using System.Collections.Generic;
using System.Linq;

namespace IMSystem.Server.Domain.Common
{
    /// <summary>
    /// 值对象的抽象基类。
    /// 值对象是基于其属性值来定义其相等性的对象。它们应该是不可变的。
    /// </summary>
    public abstract class ValueObject
    {
        /// <summary>
        /// 获取用于比较此值对象相等性的原子组件。
        /// </summary>
        /// <returns>构成此值对象的属性的有序集合。</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x != null ? x.GetHashCode() : 0)
                .Aggregate((x, y) => x ^ y);
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (left is null ^ right is null) // 一个为null，另一个不为null
            {
                return false;
            }
            return left is null || left.Equals(right); // 两者都为null，或者left.Equals(right)
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
        {
            return !(left == right);
        }
    }
}