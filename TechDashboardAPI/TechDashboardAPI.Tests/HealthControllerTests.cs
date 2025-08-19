using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechDashboardAPI.API.Controllers;
using Xunit;

namespace TechDashboardAPI.Tests
{
    public class HealthControllerTests
    {
        [Fact]
        public async Task Health_ReturnsWrappedApiResponse()
        {
            // Arrange
            var controller = new HealthController();

            // Act
            var result = await controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            // Validate ApiResponse structure via reflection
            var valueType = okResult.Value!.GetType();
            var successProp = valueType.GetProperty("Success");
            var messageProp = valueType.GetProperty("Message");
            var dataProp = valueType.GetProperty("Data");

            Assert.NotNull(successProp);
            Assert.NotNull(messageProp);
            Assert.NotNull(dataProp);

            var success = (bool)(successProp!.GetValue(okResult.Value) ?? false);
            Assert.True(success);

            var data = dataProp!.GetValue(okResult.Value);
            Assert.NotNull(data);

            var dataType = data!.GetType();
            var statusProp = dataType.GetProperty("status");
            Assert.NotNull(statusProp);
            var statusValue = statusProp!.GetValue(data)?.ToString();
            Assert.Equal("Healthy", statusValue);
        }
    }
}
