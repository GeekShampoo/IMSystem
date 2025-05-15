using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Signaling;
using System.Threading.Tasks;

namespace IMSystem.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles signaling interactions with the backend SignalingHub.
    /// </summary>
    public interface ISignalingService
    {
        /// <summary>
        /// Invites a user to a call.
        /// </summary>
        /// <param name="request">The call invitation request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> InviteUserAsync(CallInviteRequest request);

        /// <summary>
        /// Answers an incoming call.
        /// </summary>
        /// <param name="request">The call answer request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> AnswerCallAsync(CallAnswerRequest request);

        /// <summary>
        /// Rejects an incoming call.
        /// </summary>
        /// <param name="request">The call reject request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> RejectCallAsync(CallRejectRequest request);

        /// <summary>
        /// Hangs up an ongoing call.
        /// </summary>
        /// <param name="request">The call hangup request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> HangupCallAsync(CallHangupRequest request);

        /// <summary>
        /// Sends an SDP (Session Description Protocol) offer or answer.
        /// </summary>
        /// <param name="request">The SDP exchange request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> SendSdpAsync(SdpExchangeRequest request);

        /// <summary>
        /// Sends an ICE (Interactive Connectivity Establishment) candidate.
        /// </summary>
        /// <param name="request">The ICE candidate exchange request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the operation result.</returns>
        Task<Result> SendIceCandidateAsync(IceCandidateExchangeRequest request);
    }
}