using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IMSystem.Client.Core.Services
{
    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IServiceProvider _serviceProvider;

        // 移除直接依赖 IAuthService，改用 IServiceProvider
        public ApiService(
            IHttpClientFactory httpClientFactory, 
            IServiceProvider serviceProvider,
            ILogger<ApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        // 通过服务提供者获取 IAuthService
        private IAuthService GetAuthService()
        {
            return _serviceProvider.GetRequiredService<IAuthService>();
        }

        public HttpClient GetClient()
        {
            var client = _httpClientFactory.CreateClient("IMSystem");
            
            // 通过方法调用获取 IAuthService
            var authService = GetAuthService();
            
            // 如果有令牌，添加认证头
            if (authService.IsAuthenticated() && !string.IsNullOrEmpty(authService.Token))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", authService.Token);
            }
            
            return client;
        }

        public async Task<TResponse> GetAsync<TResponse>(string endpoint)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions)!;
                }
                
                var errorResponse = await HandleApiErrorResponseAsync(response);
                throw new ApiException(errorResponse);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GET request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
        {
            try
            {
                var client = GetClient();
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions)!;
                }
                
                var errorResponse = await HandleApiErrorResponseAsync(response);
                throw new ApiException(errorResponse);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task PostAsync<TRequest>(string endpoint, TRequest request)
        {
            try
            {
                var client = GetClient();
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync(endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await HandleApiErrorResponseAsync(response);
                    throw new ApiException(errorResponse);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request)
        {
            try
            {
                var client = GetClient();
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PutAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions)!;
                }
                
                var errorResponse = await HandleApiErrorResponseAsync(response);
                throw new ApiException(errorResponse);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PUT request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task PutAsync<TRequest>(string endpoint, TRequest request)
        {
            try
            {
                var client = GetClient();
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PutAsync(endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await HandleApiErrorResponseAsync(response);
                    throw new ApiException(errorResponse);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PUT request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task DeleteAsync(string endpoint)
        {
            try
            {
                var client = GetClient();
                var response = await client.DeleteAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await HandleApiErrorResponseAsync(response);
                    throw new ApiException(errorResponse);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DELETE request to {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<ApiErrorResponse> HandleApiErrorResponseAsync(HttpResponseMessage response)
        {
            string responseContent = string.Empty;
            try
            {
                responseContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(responseContent, _jsonOptions);
                    if (errorResponse != null)
                    {
                        // 确保状态码存在，如果为0则使用HTTP响应的状态码
                        errorResponse.StatusCode = errorResponse.StatusCode == 0 ? (int)response.StatusCode : errorResponse.StatusCode;
                        // 确保标题存在，如果为空则使用HTTP响应的原因短语
                        errorResponse.Title = string.IsNullOrWhiteSpace(errorResponse.Title) ? (response.ReasonPhrase ?? "API Error") : errorResponse.Title;
                        
                        _logger.LogWarning("API Error: {StatusCode} {ErrorCode} - {Title} - {Detail}. Response: {ResponseContent}",
                            errorResponse.StatusCode, errorResponse.ErrorCode ?? "N/A", errorResponse.Title, errorResponse.Detail ?? "N/A", responseContent);
                        return errorResponse;
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize API error response. StatusCode: {StatusCode}, Reason: {ReasonPhrase}. Content: {Content}",
                    (int)response.StatusCode, response.ReasonPhrase, responseContent);
            }
            catch (Exception ex) // 更广泛的异常捕获
            {
                _logger.LogError(ex, "Unexpected error handling API error response. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Content: {Content}",
                    (int)response.StatusCode, response.ReasonPhrase, responseContent);
            }

            // 如果反序列化失败或内容为空，提供一个回退错误响应
            return new ApiErrorResponse(
                (int)response.StatusCode,
                response.ReasonPhrase ?? "API Request Failed",
                string.IsNullOrWhiteSpace(responseContent) ? 
                    "No additional error details provided by the server." : 
                    $"Raw error content: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}" 
            )
            {
                ErrorCode = "UnknownApiError", 
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// API操作异常
    /// </summary>
    public class ApiException : Exception
    {
        public ApiErrorResponse Error { get; }
        
        public ApiException(ApiErrorResponse error)
            : base(error.Detail ?? error.Title)
        {
            Error = error;
        }
    }
}