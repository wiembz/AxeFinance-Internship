using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;
using TechDashboardAPI.Infrastructure.Auth;
using TechDashboardAPI.Infrastructure.Data;
using TechDashboardAPI.Infrastructure.Services;
using Xunit;

namespace TechDashboardAPI.Tests
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IFakeAdService> _mockFakeAdService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<ITokenService> _mockTokenService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _mockConfiguration = new Mock<IConfiguration>();
            _mockFakeAdService = new Mock<IFakeAdService>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockTokenService = new Mock<ITokenService>();

            // Setup JWT configuration
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(x => x["SecretKey"]).Returns("TestSecretKeyThatIsLongEnoughForHS256Algorithm");
            mockJwtSection.Setup(x => x["ExpirationHours"]).Returns("24");
            _mockConfiguration.Setup(x => x.GetSection("JwtSettings")).Returns(mockJwtSection.Object);

            // Default token to a JWT-like string (three segments) for tests that expect a token
            _mockTokenService
                .Setup(s => s.GenerateJwtToken(It.IsAny<User>()))
                .Returns("header.payload.signature");

            _authService = new AuthService(
                _context,
                _mockConfiguration.Object,
                _mockFakeAdService.Object,
                _mockLogger.Object,
                _mockTokenService.Object);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsError_WhenUserExists()
        {
            // Arrange
            var existingUser = new User
            {
                Id = 1,
                Username = "existing",
                Email = "existing@example.com",
                PasswordHash = "hash",
                Role = UserRole.Contributor,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Username = "test",
                Email = "existing@example.com",
                Password = "Str0ng#Cred9"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        [Fact]
        public async Task RegisterAsync_CreatesFirstUserAsSuperAdmin_WhenNoUsersExist()
        {
            // Arrange
            _mockFakeAdService.Setup(x => x.ValidateUser(It.IsAny<string>())).Returns(true);

            var registerDto = new RegisterDto
            {
                Username = "admin",
                Email = "admin@example.com",
                // Strong password that avoids banned substrings
                Password = "Str0ng#Cred9"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal("SuperAdmin", result.Data.Role);
        }

        [Fact]
        public async Task LoginAsync_ReturnsToken_WhenCredentialsValid()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "test",
                Email = "test@example.com",
                // Use legacy SHA-256 hashing to simulate existing user; AuthService supports legacy verify+rehash
                PasswordHash = HashPassword("Str0ng#Cred9"),
                Role = UserRole.Contributor,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Str0ng#Cred9"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Contains(".", result.Data); // JWT contains dots
        }

        [Fact]
        public async Task LoginAsync_ReturnsError_WhenCredentialsInvalid()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid email or password", result.Message);
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
