namespace IMSystem.Protocol.Common
{
    /// <summary>
    /// 表示操作的结果。
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 获取一个值，该值指示操作是否成功。
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 获取错误信息。如果操作成功，则为空。
        /// </summary>
        public Error? Error { get; }

        /// <summary>
        /// 获取一个值，该值指示操作是否失败。
        /// </summary>
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = isSuccess ? null : error ?? Common.Error.None;
        }

        /// <summary>
        /// 创建一个表示失败的结果。
        /// </summary>
        /// <param name="error">错误对象。</param>
        /// <returns>失败的结果对象。</returns>
        public static Result Failure(Error error)
        {
            return new Result(false, error);
        }

        /// <summary>
        /// 创建一个表示失败的结果。
        /// </summary>
        /// <param name="errorCode">错误代码。</param>
        /// <param name="errorMessage">错误消息。</param>
        /// <returns>失败的结果对象。</returns>
        public static Result Failure(string errorCode, string errorMessage)
        {
            return new Result(false, new Error(errorCode, errorMessage));
        }

        /// <summary>
        /// 创建一个表示成功的结果。
        /// </summary>
        /// <returns>成功的结果对象。</returns>
        public static Result Success()
        {
            return new Result(true, null);
        }

        /// <summary>
        /// 创建一个表示失败的泛型结果。
        /// </summary>
        /// <typeparam name="T">结果值的类型。</typeparam>
        /// <param name="error">错误对象。</param>
        /// <returns>失败的泛型结果对象。</returns>
        public static Result<T> Failure<T>(Error error)
        {
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// 表示带有值的操作结果。
    /// </summary>
    /// <typeparam name="TValue">结果值的类型。</typeparam>
    public class Result<TValue> : Result
    {
        private readonly TValue _value;

        /// <summary>
        /// 获取结果的值。如果操作失败，则可能为默认值。
        /// </summary>
        public TValue Value => IsSuccess ? _value : default!;

        protected Result(TValue value, bool isSuccess, Error? error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        /// <summary>
        /// 创建一个表示成功的带有值的结果。
        /// </summary>
        /// <param name="value">结果值。</param>
        /// <returns>成功的带有值的结果对象。</returns>
        public static Result<TValue> Success(TValue value)
        {
            return new Result<TValue>(value, true, null);
        }

        /// <summary>
        /// 创建一个表示失败的带有值的结果。
        /// </summary>
        /// <param name="error">错误对象。</param>
        /// <returns>失败的带有值的结果对象。</returns>
        public new static Result<TValue> Failure(Error error)
        {
            return new Result<TValue>(default!, false, error);
        }

        /// <summary>
        /// 创建一个表示失败的带有值的结果。
        /// </summary>
        /// <param name="errorCode">错误代码。</param>
        /// <param name="errorMessage">错误消息。</param>
        /// <returns>失败的带有值的结果对象。</returns>
        public new static Result<TValue> Failure(string errorCode, string errorMessage)
        {
            return new Result<TValue>(default!, false, new Error(errorCode, errorMessage));
        }
    }
}