using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.Tests;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using FluentAssertions;
using FluentValidation.Results;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.Tests.UseCases.User.Commands
{
    /// <summary>
    /// Tests for DeleteUserHandler.
    /// </summary>
    public class DeleteUserHandlerTests
    {
        /// <summary>
        /// Tests that user is deleted when all conditions are met.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldDeleteUser_WhenAllConditionsAreMet()
        {
            var room = DataFakers.ValidRoomBuilder.Build().Value;
            var adminUser = room.Users.First(u => u.IsAdmin);
            var userToDelete = room.Users.First(u => !u.IsAdmin && u.Id != adminUser.Id);
            var initialUserCount = room.Users.Count;
            var request = new DeleteUserRequest(adminUser.UserCode, userToDelete.Id);
            var mockRepository = new TestRoomRepository(room);
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            mockRepository.UpdatedRoom.Should().NotBeNull();
            mockRepository.UpdatedRoom!.Users.Should().HaveCount(initialUserCount - 1);
            mockRepository.UpdatedRoom.Users.Should().NotContain(u => u.Id == userToDelete.Id);
        }

        /// <summary>
        /// Tests that NotFound is returned when user with userCode is not found.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenUserWithUserCodeNotFound()
        {
            var request = new DeleteUserRequest("non_existent_code", 123UL);
            var mockRepository = new TestRoomRepository(null); 
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("not found"));
        }

        /// <summary>
        /// Tests that NotFound is returned when user with id is not found.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenUserWithIdNotFound()
        {
            var room = DataFakers.ValidRoomBuilder.Build().Value;
            var adminUser = room.Users.First(u => u.IsAdmin);
            var nonExistentUserId = 999UL;
            var request = new DeleteUserRequest(adminUser.UserCode, nonExistentUserId);
            var mockRepository = new TestRoomRepository(room);
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not exist"));
        }

        /// <summary>
        /// Tests that Forbidden is returned when user with userCode is not admin.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnForbidden_WhenUserWithUserCodeNotAdmin()
        {
            var room = DataFakers.ValidRoomBuilder.Build().Value;
            var nonAdminUser = room.Users.First(u => !u.IsAdmin);
            var userToDelete = room.Users.First(u => u.Id != nonAdminUser.Id);
            var request = new DeleteUserRequest(nonAdminUser.UserCode, userToDelete.Id);
            var mockRepository = new TestRoomRepository(room);
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("not an admin"));
        }

        /// <summary>
        /// Tests that BadRequest is returned when user tries to delete themselves.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenUserTriesToDeleteThemselves()
        {
            var room = DataFakers.ValidRoomBuilder.Build().Value;
            var adminUser = room.Users.First(u => u.IsAdmin);
            var request = new DeleteUserRequest(adminUser.UserCode, adminUser.Id);
            var mockRepository = new TestRoomRepository(room);
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("delete yourself"));
        }

        /// <summary>
        /// Tests that BadRequest is returned when room is already closed.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenRoomIsAlreadyClosed()
        {
            var room = CreateClosedRoom();
            var adminUser = room.Users.First(u => u.IsAdmin);
            var userToDelete = room.Users.First(u => !u.IsAdmin && u.Id != adminUser.Id);
            var request = new DeleteUserRequest(adminUser.UserCode, userToDelete.Id);
            var mockRepository = new TestRoomRepository(room);
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("already closed"));
        }

        /// <summary>
        /// Tests that NotFound is returned when target user is in a different room.
        /// </summary>
        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenTargetUserInDifferentRoom()
        {
            var room1 = DataFakers.ValidRoomBuilder.Build().Value;
            var room2 = DataFakers.ValidRoomBuilder.Build().Value;

            var adminFromRoom1 = room1.Users.First(u => u.IsAdmin);
            var userFromRoom2 = room2.Users.First(u => !u.IsAdmin);

            var request = new DeleteUserRequest(adminFromRoom1.UserCode, userFromRoom2.Id);
            var mockRepository = new TestRoomRepository(room1); 
            var handler = new DeleteUserHandler(mockRepository);

            var result = await handler.Handle(request, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not exist"));
        }

        private static RoomAggregate CreateClosedRoom()
        {
            var room = DataFakers.ValidRoomBuilder.Build().Value;
            var closedOnProperty = typeof(RoomAggregate).GetProperty("ClosedOn");
            closedOnProperty?.SetValue(room, DateTime.UtcNow.AddDays(-1));
            return room;
        }
    }

    /// <summary>
    /// Test room repository that implements only methods used by DeleteUserHandler.
    /// </summary>
    internal class TestRoomRepository : IRoomRepository
    {
        private readonly RoomAggregate? _roomToReturn;

        public TestRoomRepository(RoomAggregate? roomToReturn)
        {
            _roomToReturn = roomToReturn;
            UpdatedRoom = null;
        }

        public RoomAggregate? UpdatedRoom { get; private set; }

        public Task<RoomAggregate?> FindByUserCodeAsync(string userCode)
        {
            return Task.FromResult(_roomToReturn);
        }

        public Task<Result<RoomAggregate, ValidationResult>> UpdateAsync(RoomAggregate room)
        {
            UpdatedRoom = room;
            return Task.FromResult(Result.Success<RoomAggregate, ValidationResult>(room));
        }

        public Task<Result<RoomAggregate, ValidationResult>> UpdateAsync(RoomAggregate room, CancellationToken cancellationToken)
        {
            UpdatedRoom = room;
            return Task.FromResult(Result.Success<RoomAggregate, ValidationResult>(room));
        }

        // Not implemented - throw to catch accidental use
        public Task<RoomAggregate?> FindByIdAsync(ulong id) => throw new NotImplementedException();
        public Task<Result<RoomAggregate, ValidationResult>> CreateAsync(RoomAggregate room) => throw new NotImplementedException();
        public Task<Result<RoomAggregate, ValidationResult>> GetByUserCodeAsync(string userCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result<RoomAggregate, ValidationResult>> GetByRoomCodeAsync(string roomCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result<RoomAggregate, ValidationResult>> AddAsync(RoomAggregate room, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        Task<Result<RoomAggregate, ValidationResult>> IRoomRepository.AddAsync(RoomAggregate item, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        Task<Result> IRoomRepository.UpdateAsync(RoomAggregate roomToUpdate, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        Task<Result<RoomAggregate, ValidationResult>> IRoomRepository.GetByUserCodeAsync(string userCode, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        Task<Result<RoomAggregate, ValidationResult>> IRoomRepository.GetByRoomCodeAsync(string roomCode, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}