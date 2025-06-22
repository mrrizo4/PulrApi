using FakeItEasy;
using MediatR;
using Core.Application.Mediatr.Users.Commands.Login;

namespace XUnitTests.Users
{
    public class UserTests
    {
        private readonly IMediator _mediator;

        public UserTests()
        {
            _mediator = A.Fake<Mediator>();
        }

        [Fact]
        public void UserTests_LoginCommand_LoginResponse()
        {
            // Arrange
            var command = new LoginCommand()
            {
                IsEmail = true,
                Username = "user",
                Password = "pwd",
            };
            var handler = new LoginCommandHandler(null,null,null);

            // Act

            //Unit x = await handler.Handle(command, new System.Threading.CancellationToken());

            // Assert

            //Assert
            //_mediator.Verify(x => x.Publish(It.IsAny<CustomersChanged>()));
            
        }
    }
}