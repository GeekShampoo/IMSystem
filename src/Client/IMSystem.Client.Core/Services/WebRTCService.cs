using IMSystem.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Services
{
    public class WebRTCService : IWebRTCService
    {
        private readonly ILogger<WebRTCService> _logger;
        private PeerConnection? _peerConnection;
        private string? _currentCallId;
        private Transceiver? _audioTransceiver;
        private Transceiver? _videoTransceiver;
        private LocalAudioTrack? _localAudioTrack;
        private LocalVideoTrack? _localVideoTrack;
        private TaskCompletionSource<string>? _sdpTcs;

        public event Func<string, string, Task>? LocalSdpReadyAsync;
        public event Func<string, string, int, string, Task>? IceCandidateReadyAsync;
        public event Action<string, object>? RemoteStreamAdded;
        public event Action<string, object>? LocalStreamAdded;
        public event Action<string>? PeerConnectionClosed;
        public event Action<string, string>? WebRTCErrorOccurred;

        public WebRTCService(ILogger<WebRTCService> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync()
        {
            _logger.LogInformation("WebRTCService initializing...");
            // Microsoft.MixedReality.WebRTC 不需要显式的全局初始化
            return Task.CompletedTask;
        }

        public async Task CreatePeerConnectionAsync(string callId, bool isInitiator)
        {
            _logger.LogInformation("Creating PeerConnection for call {CallId}, Initiator: {IsInitiator}", callId, isInitiator);
            try
            {
                if (_peerConnection != null)
                {
                    _logger.LogWarning("Existing PeerConnection found for call {OldCallId}. Closing it before creating a new one for {NewCallId}.", _currentCallId, callId);
                    await ClosePeerConnectionAsync(_currentCallId ?? string.Empty);
                }

                _currentCallId = callId;
                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer> {
                        new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
                    }
                };
                
                _peerConnection = new PeerConnection();
                await _peerConnection.InitializeAsync(config);

                // 注册事件处理
                _peerConnection.IceCandidateReadytoSend += OnIceCandidateReadyToSendInternal;
                _peerConnection.LocalSdpReadytoSend += OnLocalSdpReadyToSendInternal;
                _peerConnection.Connected += OnPeerConnectedInternal;
                _peerConnection.AudioTrackAdded += OnAudioTrackAddedInternal;
                _peerConnection.VideoTrackAdded += OnVideoTrackAddedInternal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PeerConnection for call {CallId}", callId);
                OnWebRTCErrorOccurred(callId, $"创建 PeerConnection 错误: {ex.Message}");
                throw;
            }
        }

        public async Task StartLocalMediaAsync()
        {
            _logger.LogInformation("Starting local media...");
            if (_peerConnection == null)
            {
                _logger.LogError("Cannot start local media, PeerConnection is not initialized.");
                OnWebRTCErrorOccurred(_currentCallId ?? "N/A", "无法启动本地媒体，PeerConnection 未初始化。");
                throw new InvalidOperationException("PeerConnection 未初始化。请先调用 CreatePeerConnectionAsync。");
            }

            try
            {
                // 创建音频轨道
                try
                {
                    var audioSource = await DeviceAudioTrackSource.CreateAsync();
                    if (audioSource != null)
                    {
                        _localAudioTrack = LocalAudioTrack.CreateFromSource(audioSource, new LocalAudioTrackInitConfig());
                        _audioTransceiver = _peerConnection.AddTransceiver(MediaKind.Audio);
                        _audioTransceiver.LocalAudioTrack = _localAudioTrack;
                        _audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                        _logger.LogInformation("本地音频轨道已添加。");
                    }
                    else
                    {
                        _logger.LogWarning("未能创建音频源。");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建本地音频轨道错误。");
                    OnWebRTCErrorOccurred(_currentCallId ?? "N/A", $"创建本地音频错误: {ex.Message}");
                }

                // 创建视频轨道
                try
                {
                    var videoCaptureDevices = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
                    if (videoCaptureDevices.Any())
                    {
                        var selectedDevice = videoCaptureDevices.First();
                        
                        // 使用无参数的方法创建视频源，然后尝试获取默认设备
                        var videoSource = await DeviceVideoTrackSource.CreateAsync();
                        
                        if (videoSource != null)
                        {
                            _localVideoTrack = LocalVideoTrack.CreateFromSource(videoSource, new LocalVideoTrackInitConfig());
                            _videoTransceiver = _peerConnection.AddTransceiver(MediaKind.Video);
                            _videoTransceiver.LocalVideoTrack = _localVideoTrack;
                            _videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                            _logger.LogInformation("本地视频轨道已添加，使用默认设备");
                        }
                        else
                        {
                            _logger.LogWarning("未能创建视频源。");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("未找到视频捕获设备。");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建本地视频轨道错误。");
                    OnWebRTCErrorOccurred(_currentCallId ?? "N/A", $"创建本地视频错误: {ex.Message}");
                }

                // 触发本地流添加事件
                object streamObject = _localVideoTrack ?? (object?)_localAudioTrack ?? new object();
                if (_currentCallId != null)
                {
                    OnLocalStreamAdded(_currentCallId, streamObject);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动本地媒体错误，通话ID: {CallId}", _currentCallId);
                OnWebRTCErrorOccurred(_currentCallId ?? "N/A", $"启动本地媒体错误: {ex.Message}");
                await StopLocalMediaAsync(); // 清理部分启动的媒体
                throw;
            }
        }

        public async Task<string> CreateOfferAsync(string callId)
        {
            _logger.LogInformation("为通话 {CallId} 创建 Offer", callId);
            if (_peerConnection == null || _currentCallId != callId)
            {
                _logger.LogError("CreateOfferAsync: PeerConnection 未就绪或 callId 不匹配。当前: {CurrentCallId}, 请求: {RequestedCallId}", _currentCallId, callId);
                OnWebRTCErrorOccurred(callId, "PeerConnection 未就绪或 callId 不匹配。");
                throw new InvalidOperationException("PeerConnection 未就绪或 callId 不匹配。");
            }

            try
            {
                _sdpTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_peerConnection.CreateOffer()) // 这是同步的，会触发 LocalSdpReadytoSend 事件
                {
                    _logger.LogError("为通话 {CallId} 启动 CreateOffer 失败", callId);
                    _sdpTcs.TrySetException(new Exception("PeerConnection.CreateOffer() 返回 false。"));
                    OnWebRTCErrorOccurred(callId, "启动 CreateOffer 失败。");
                }

                var completedTask = await Task.WhenAny(_sdpTcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                if (completedTask != _sdpTcs.Task)
                {
                    _logger.LogError("为通话 {CallId} 创建 Offer 超时", callId);
                    OnWebRTCErrorOccurred(callId, "创建 Offer 超时。");
                    throw new TimeoutException("创建 Offer 超时。");
                }

                return await _sdpTcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为通话 {CallId} 创建 Offer 错误", callId);
                OnWebRTCErrorOccurred(callId, $"创建 Offer 错误: {ex.Message}");
                _sdpTcs?.TrySetCanceled();
                throw;
            }
        }

        public async Task<string> CreateAnswerAsync(string callId, string remoteOfferSdp)
        {
            _logger.LogInformation("为通话 {CallId} 创建 Answer", callId);
            if (_peerConnection == null || _currentCallId != callId)
            {
                _logger.LogError("CreateAnswerAsync: PeerConnection 未就绪或 callId 不匹配。当前: {CurrentCallId}, 请求: {RequestedCallId}", _currentCallId, callId);
                OnWebRTCErrorOccurred(callId, "PeerConnection 未就绪或 callId 不匹配。");
                throw new InvalidOperationException("PeerConnection 未就绪或 callId 不匹配。");
            }

            try
            {
                _sdpTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_peerConnection.CreateAnswer()) // 这是同步的，会触发 LocalSdpReadytoSend 事件
                {
                    _logger.LogError("为通话 {CallId} 启动 CreateAnswer 失败", callId);
                    _sdpTcs.TrySetException(new Exception("PeerConnection.CreateAnswer() 返回 false。"));
                    OnWebRTCErrorOccurred(callId, "启动 CreateAnswer 失败。");
                }

                var completedTask = await Task.WhenAny(_sdpTcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                if (completedTask != _sdpTcs.Task)
                {
                    _logger.LogError("为通话 {CallId} 创建 Answer 超时", callId);
                    OnWebRTCErrorOccurred(callId, "创建 Answer 超时。");
                    throw new TimeoutException("创建 Answer 超时。");
                }

                return await _sdpTcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为通话 {CallId} 创建 Answer 错误", callId);
                OnWebRTCErrorOccurred(callId, $"创建 Answer 错误: {ex.Message}");
                _sdpTcs?.TrySetCanceled();
                throw;
            }
        }

        public async Task SetRemoteDescriptionAsync(string callId, string sdpType, string sdp)
        {
            _logger.LogInformation("为通话 {CallId} 设置远程描述，类型: {SdpType}", callId, sdpType);
            if (_peerConnection == null || _currentCallId != callId)
            {
                _logger.LogError("SetRemoteDescriptionAsync: PeerConnection 未就绪或 callId 不匹配。当前: {CurrentCallId}, 请求: {RequestedCallId}", _currentCallId, callId);
                OnWebRTCErrorOccurred(callId, "PeerConnection 未就绪或 callId 不匹配。");
                throw new InvalidOperationException("PeerConnection 未就绪或 callId 不匹配。");
            }

            try
            {
                var message = new SdpMessage
                {
                    Type = sdpType.Equals("offer", StringComparison.OrdinalIgnoreCase) ? SdpMessageType.Offer : SdpMessageType.Answer,
                    Content = sdp
                };
                await _peerConnection.SetRemoteDescriptionAsync(message);
                _logger.LogInformation("成功为通话 {CallId} 设置远程描述", callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为通话 {CallId} 设置远程描述错误", callId);
                OnWebRTCErrorOccurred(callId, $"设置远程描述错误: {ex.Message}");
                throw;
            }
        }

        public Task AddIceCandidateAsync(string callId, string sdpMid, int sdpMLineIndex, string candidate)
        {
            _logger.LogInformation("为通话 {CallId} 添加 ICE Candidate，Mid: {SdpMid}, MLineIndex: {SdpMLineIndex}", callId, sdpMid, sdpMLineIndex);
            if (_peerConnection == null || _currentCallId != callId)
            {
                _logger.LogError("AddIceCandidateAsync: PeerConnection 未就绪或 callId 不匹配。当前: {CurrentCallId}, 请求: {RequestedCallId}", _currentCallId, callId);
                OnWebRTCErrorOccurred(callId, "PeerConnection 未就绪或 callId 不匹配。");
                throw new InvalidOperationException("PeerConnection 未就绪或 callId 不匹配。");
            }

            try
            {
                var iceCandidate = new IceCandidate
                {
                    SdpMid = sdpMid,
                    SdpMlineIndex = (ushort)sdpMLineIndex,
                    Content = candidate
                };
                _peerConnection.AddIceCandidate(iceCandidate);
                _logger.LogInformation("成功为通话 {CallId} 添加 ICE candidate", callId);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为通话 {CallId} 添加 ICE candidate 错误", callId);
                OnWebRTCErrorOccurred(callId, $"添加 ICE candidate 错误: {ex.Message}");
                throw;
            }
        }

        public Task StopLocalMediaAsync()
        {
            _logger.LogInformation("停止通话 {CallId} 的本地媒体...", _currentCallId);
            try
            {
                _localAudioTrack?.Dispose();
                _localAudioTrack = null;
                _logger.LogInformation("本地音频轨道已释放。");

                _localVideoTrack?.Dispose();
                _localVideoTrack = null;
                _logger.LogInformation("本地视频轨道已释放。");

                _audioTransceiver = null;
                _videoTransceiver = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止通话 {CallId} 的本地媒体错误", _currentCallId);
                OnWebRTCErrorOccurred(_currentCallId ?? "N/A", $"停止本地媒体错误: {ex.Message}");
                // 不要在这里抛出异常，因为这通常是清理步骤
            }
            return Task.CompletedTask;
        }

        public async Task ClosePeerConnectionAsync(string callId)
        {
            _logger.LogInformation("关闭通话 {CallId} 的 PeerConnection", callId);
            if (_peerConnection != null)
            {
                try
                {
                    // 取消注册事件
                    _peerConnection.IceCandidateReadytoSend -= OnIceCandidateReadyToSendInternal;
                    _peerConnection.LocalSdpReadytoSend -= OnLocalSdpReadyToSendInternal;
                    _peerConnection.Connected -= OnPeerConnectedInternal;
                    _peerConnection.AudioTrackAdded -= OnAudioTrackAddedInternal;
                    _peerConnection.VideoTrackAdded -= OnVideoTrackAddedInternal;

                    // 停止本地媒体
                    await StopLocalMediaAsync();

                    // 关闭连接
                    _peerConnection.Close();
                    _peerConnection = null;
                    _logger.LogInformation("通话 {CallId} 的 PeerConnection 已关闭", _currentCallId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "关闭通话 {CallId} 的 PeerConnection 错误", _currentCallId);
                    OnWebRTCErrorOccurred(_currentCallId ?? callId, $"关闭 PeerConnection 错误: {ex.Message}");
                    // 仍然清空以防止重用可能损坏的状态
                    _peerConnection = null; 
                }
                finally
                {
                    OnPeerConnectionClosed(_currentCallId ?? callId);
                    _currentCallId = null;
                    _sdpTcs?.TrySetCanceled(); // 取消任何待处理的 SDP 操作
                    _sdpTcs = null;
                }
            }
            else
            {
                _logger.LogWarning("尝试关闭通话 {CallId} 的 PeerConnection，但没有找到活动的 PeerConnection。", callId);
            }
        }

        private void OnLocalSdpReadyToSendInternal(SdpMessage message)
        {
            if (_currentCallId == null)
            {
                _logger.LogWarning("OnLocalSdpReadyToSendInternal: _currentCallId 为 null。忽略 SDP。");
                return;
            }
            _logger.LogInformation("通话 {CallId} 的本地 SDP 就绪: {SdpType}", _currentCallId, message.Type);
            _sdpTcs?.TrySetResult(message.Content);
            LocalSdpReadyAsync?.Invoke(_currentCallId, message.Content);
        }

        private void OnIceCandidateReadyToSendInternal(IceCandidate candidate)
        {
            if (_currentCallId == null)
            {
                _logger.LogWarning("OnIceCandidateReadyToSendInternal: _currentCallId 为 null。忽略 ICE candidate。");
                return;
            }
            _logger.LogInformation("通话 {CallId} 的 ICE candidate 就绪: {Candidate}", _currentCallId, candidate.Content);
            IceCandidateReadyAsync?.Invoke(_currentCallId, candidate.SdpMid, candidate.SdpMlineIndex, candidate.Content);
        }

        private void OnPeerConnectedInternal()
        {
            if (_currentCallId == null)
            {
                _logger.LogWarning("OnPeerConnectedInternal: _currentCallId 为 null。忽略连接事件。");
                return;
            }
            _logger.LogInformation("通话 {CallId} 的 PeerConnection 已连接", _currentCallId);
        }

        private void OnAudioTrackAddedInternal(RemoteAudioTrack track)
        {
            if (_currentCallId == null)
            {
                _logger.LogWarning("OnAudioTrackAddedInternal: _currentCallId 为 null。忽略远程音频轨道。");
                return;
            }
            _logger.LogInformation("通话 {CallId} 添加了远程音频轨道。轨道名称: {TrackName}", _currentCallId, track.Name);
            RemoteStreamAdded?.Invoke(_currentCallId, track);
        }

        private void OnVideoTrackAddedInternal(RemoteVideoTrack track)
        {
            if (_currentCallId == null)
            {
                _logger.LogWarning("OnVideoTrackAddedInternal: _currentCallId 为 null。忽略远程视频轨道。");
                return;
            }
            _logger.LogInformation("通话 {CallId} 添加了远程视频轨道。轨道名称: {TrackName}", _currentCallId, track.Name);
            RemoteStreamAdded?.Invoke(_currentCallId, track);
        }

        protected virtual void OnLocalStreamAdded(string callId, object streamObject)
        {
            LocalStreamAdded?.Invoke(callId, streamObject);
        }

        protected virtual void OnPeerConnectionClosed(string callId)
        {
            PeerConnectionClosed?.Invoke(callId);
        }

        protected virtual void OnWebRTCErrorOccurred(string callId, string errorMessage)
        {
            WebRTCErrorOccurred?.Invoke(callId, errorMessage);
        }
    }
}