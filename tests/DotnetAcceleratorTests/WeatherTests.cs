using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using DotnetAccelerator.Modules.WeatherModule.Domain.Models;
using DotnetAccelerator.Modules.WeatherModule.Domain.Services;
using DotnetAccelerator.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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