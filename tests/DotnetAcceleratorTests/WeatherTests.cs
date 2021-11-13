using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MyProjectGroup.Common.Messaging;
using MyProjectGroup.Common.Modules;
using MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Api;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule.Api;
using MyProjectGroup.DotnetAccelerator.Persistence;
using NSubstitute;
using Xunit;

namespace DotnetAcceleratorTests
{
    public class WeatherTests : IUseDbContext<DotnetAcceleratorContext>
    {
        public DotnetAcceleratorContext CreateContext() => ((IUseDbContext<DotnetAcceleratorContext>) this).GetDbContext();
        [Fact]
        public void CreateForecast_InvalidAirport_ThrowsDomainException()
        {
            var messageBus = Substitute.For<IMessageBus>();
            messageBus
                .Send(Arg.Any<AirportQuery>(), Arg.Any<CancellationToken>())
                .Returns(new[] {new Airport() {Id = "Test", Name = "Test Airport"}}.ToAsyncEnumerable());

            var sut = new WeatherService(CreateContext(), messageBus, NullLogger<WeatherService>.Instance);

            var forecast = new WeatherForecast()
            {
                AirportId = "Invalid",
                Summary = "Warm"
            };
            
            sut.Invoking(x => x.SaveForecast(forecast)).Should().ThrowAsync<DomainException>();
        }
        
        [Fact]
        public async Task CreateForecast_Valid_SavesToDatabase()
        {
            var messageBus = Substitute.For<IMessageBus>();
            messageBus
                .Send(Arg.Any<AirportQuery>(), Arg.Any<CancellationToken>())
                .Returns(new[] {new Airport() {Id = "Test", Name = "Test Airport"}}.ToAsyncEnumerable());

            var sut = new WeatherService(CreateContext(), messageBus, NullLogger<WeatherService>.Instance);

            var forecast = new WeatherForecast()
            {
                AirportId = "Invalid",
                Summary = "Warm"
            };
            
            var result = await sut.SaveForecast(forecast);
            result.Id.Should().NotBeEmpty("Id should have been set during save operation");
        }
    }
}