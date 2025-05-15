using System;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    public interface IWebRTCService
    {
        /// <summary>
        /// 用于任何必要的异步初始化。
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 创建或重置 PeerConnection。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        /// <param name="isInitiator">是否为发起方。</param>
        Task CreatePeerConnectionAsync(string callId, bool isInitiator);

        /// <summary>
        /// 创建 SDP Offer。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        /// <returns>SDP Offer 字符串。</returns>
        Task<string> CreateOfferAsync(string callId);

        /// <summary>
        /// 创建 SDP Answer。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        /// <param name="remoteOfferSdp">远端 SDP Offer。</param>
        /// <returns>SDP Answer 字符串。</returns>
        Task<string> CreateAnswerAsync(string callId, string remoteOfferSdp);

        /// <summary>
        /// 设置远端 SDP。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        /// <param name="sdpType">SDP 类型 ("offer" 或 "answer")。</param>
        /// <param name="sdp">SDP 字符串。</param>
        Task SetRemoteDescriptionAsync(string callId, string sdpType, string sdp);

        /// <summary>
        /// 添加远端 ICE Candidate。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        /// <param name="sdpMid">SDP Media ID。</param>
        /// <param name="sdpMLineIndex">SDP M-Line 索引。</param>
        /// <param name="candidate">ICE Candidate 字符串。</param>
        Task AddIceCandidateAsync(string callId, string sdpMid, int sdpMLineIndex, string candidate);

        /// <summary>
        /// 获取并启动本地摄像头和麦克风。
        /// </summary>
        Task StartLocalMediaAsync();

        /// <summary>
        /// 停止本地媒体流。
        /// </summary>
        Task StopLocalMediaAsync();

        /// <summary>
        /// 关闭并清理 PeerConnection。
        /// </summary>
        /// <param name="callId">呼叫ID。</param>
        Task ClosePeerConnectionAsync(string callId);

        /// <summary>
        /// 本地 SDP (Offer 或 Answer) 准备就绪时触发。
        /// 参数: callId, sdp
        /// </summary>
        event Func<string, string, Task> LocalSdpReadyAsync;

        /// <summary>
        /// 本地 ICE Candidate 准备就绪时触发。
        /// 参数: callId, sdpMid, sdpMLineIndex, candidate
        /// </summary>
        event Func<string, string, int, string, Task> IceCandidateReadyAsync;

        /// <summary>
        /// 远端媒体流添加时触发。
        /// 参数: callId, streamObject (类型取决于 WebRTC 库)
        /// </summary>
        event Action<string, object> RemoteStreamAdded;

        /// <summary>
        /// 本地媒体流添加时触发。
        /// 参数: callId, streamObject (类型取决于 WebRTC 库)
        /// </summary>
        event Action<string, object> LocalStreamAdded;

        /// <summary>
        /// PeerConnection 关闭时触发。
        /// 参数: callId
        /// </summary>
        event Action<string> PeerConnectionClosed;

        /// <summary>
        /// WebRTC 发生错误时触发。
        /// 参数: callId, errorMessage
        /// </summary>
        event Action<string, string> WebRTCErrorOccurred;
    }
}