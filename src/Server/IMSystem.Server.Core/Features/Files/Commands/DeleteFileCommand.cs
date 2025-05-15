using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Files.Commands
{
    public class DeleteFileCommand : IRequest<Result>
    {
        public Guid FileMetadataId { get; set; }
        public Guid DeleterUserId { get; set; } // This will typically be the current authenticated user's ID

        public DeleteFileCommand(Guid fileMetadataId, Guid deleterUserId)
        {
            FileMetadataId = fileMetadataId;
            DeleterUserId = deleterUserId;
        }
    }
}