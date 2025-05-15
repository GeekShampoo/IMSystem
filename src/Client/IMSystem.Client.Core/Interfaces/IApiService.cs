using System;
using System.Net.Http;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// HTTP API 服务接口
    /// </summary>
    public interface IApiService
    {
        /// <summary>
        /// 获取带有认证信息的 HttpClient
        /// </summary>
        /// <returns>配置好的 HttpClient</returns>
        HttpClient GetClient();

        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="endpoint">API 端点</param>
        /// <returns>响应数据</returns>
        Task<TResponse> GetAsync<TResponse>(string endpoint);

        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="endpoint">API 端点</param>
        /// <param name="request">请求数据</param>
        /// <returns>响应数据</returns>
        Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request);

        /// <summary>
        /// 发送不需要响应体的 POST 请求
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="endpoint">API 端点</param>
        /// <param name="request">请求数据</param>
        Task PostAsync<TRequest>(string endpoint, TRequest request);

        /// <summary>
        /// 发送 PUT 请求
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="endpoint">API 端点</param>
        /// <param name="request">请求数据</param>
        /// <returns>响应数据</returns>
        Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request);

        /// <summary>
        /// 发送不需要响应体的 PUT 请求
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="endpoint">API 端点</param>
        /// <param name="request">请求数据</param>
        Task PutAsync<TRequest>(string endpoint, TRequest request);

        /// <summary>
        /// 发送 DELETE 请求
        /// </summary>
        /// <param name="endpoint">API 端点</param>
        Task DeleteAsync(string endpoint);

        /// <summary>
        /// 处理 API 错误响应
        /// </summary>
        /// <param name="response">HTTP 响应消息</param>
        /// <returns>解析后的 API 错误响应</returns>
        Task<ApiErrorResponse> HandleApiErrorResponseAsync(HttpResponseMessage response);
    }
}