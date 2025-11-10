using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Queries;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
        public class DeleteUserHandler(IRoomRepository roomRepository)
        : IRequestHandler<DeleteUserRequest, Result<RoomAggregate, ValidationResult>>
    {
        ///<inheritdoc/>
        public async Task<Result<RoomAggregate, ValidationResult>> Handle(DeleteUserRequest request,
            CancellationToken cancellationToken)
        {
            // 1. User with `userCode` not found
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new NotFoundError([
                        new ValidationFailure("UserCode", "User with the specified UserCode was not found.")
                    ]));
            }

            var room = roomResult.Value;

            // 2. User with `userCode` is not admin
            var currentUser = null as Epam.ItMarathon.ApiService.Domain.Entities.User.User;
            foreach (var user in room.Users)
            {
                if (user.UserCode == request.UserCode)
                {
                    currentUser = user;
                    break;
                }
            }

            if (currentUser == null || !currentUser.IsAdmin)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new ForbiddenError([
                        new ValidationFailure("UserCode", "Only the room administrator can delete users.")
                    ]));
            }

            // 3. User with `userCode` and `id` belong to different rooms
            var userToDelete = null as Epam.ItMarathon.ApiService.Domain.Entities.User.User;
            foreach (var user in room.Users)
            {
                if (user.Id == request.UserId)
                {
                    userToDelete = user;
                    break;
                }
            }

            if (userToDelete == null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new BadRequestError([
                        new ValidationFailure("UserId", "User with the specified Id does not belong to the room.")
                    ]));
            }

            // 4. User with `userCode` and `id` is the same user
            if (currentUser.Id != request.UserId)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new BadRequestError([
                        new ValidationFailure("UserId", "You can only delete your own account.")
                    ]));
            }

            // 5. Room is already closed
            if (room.ClosedOn is not null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new BadRequestError([
                        new ValidationFailure("Room", "The room is already closed. Cannot delete user.")
                    ]));
            }

            // Delete user
            var deleteResult = room.DeleteUser(request.UserId);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            // Update room
            var updateResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updateResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(
                    new BadRequestError([
                        new ValidationFailure(string.Empty, updateResult.Error)
                    ]));
            }

            // Return updated room
            var updatedRoomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            return updatedRoomResult;


        }
    }
}