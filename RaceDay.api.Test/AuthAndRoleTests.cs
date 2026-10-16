using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthAndRoleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthAndRoleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real SQL Server DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Use InMemory database for tests
                services.AddDbContext<RaceDayDbContext>(options =>
                {
                    options.UseInMemoryDatabase("RaceDayTestDb");
                });
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    // ========== AUTHENTICATION TESTS ==========

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Molefe",
            Email = "thabo@test.com",
            Password = "Password123!",
            Role = "Organiser"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Lerato",
            LastName = "Dlamini",
            Email = "lerato@test.com",
            Password = "Password123!",
            Role = "Participant"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = CreateClient();

        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "duplicate@test.com",
            Password = "Password123!",
            Role = "Participant"
        };

        // First registration
        await client.PostAsJsonAsync("/api/auth/register", request);

        // Second registration with same email
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = CreateClient();

        // Register first
        var registerRequest = new RegisterRequest
        {
            FirstName = "Login",
            LastName = "Test",
            Email = "login@test.com",
            Password = "Password123!",
            Role = "Organiser"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Login
        var loginRequest = new LoginRequest
        {
            Email = "login@test.com",
            Password = "Password123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var client = CreateClient();

        var registerRequest = new RegisterRequest
        {
            FirstName = "Wrong",
            LastName = "Pass",
            Email = "wrongpass@test.com",
            Password = "Password123!",
            Role = "Participant"
        };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrongpass@test.com",
            Password = "WrongPassword!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ========== ROLE ENFORCEMENT TESTS ==========

    [Fact]
    public async Task Participant_Cannot_Create_Event_Returns403()
    {
        var client = CreateClient();

        // Register + Login as Participant
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Part",
            LastName = "icipant",
            Email = "participant@test.com",
            Password = "Password123!",
            Role = "Participant"
        });

        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "participant@test.com",
            Password = "Password123!"
        });

        // Try to create an event
        var eventRequest = new CreateEventRequest
        {
            Name = "Test Race",
            Description = "Should fail",
            EventDate = DateTime.Now.AddDays(30),
            Location = "Durban",
            DistanceKm = 10,
            EventType = "Fun Run"
        };

        var response = await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_Can_Create_Event_Returns201()
    {
        var client = CreateClient();

        // Register + Login as Organiser
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Org",
            LastName = "aniser",
            Email = "organiser@test.com",
            Password = "Password123!",
            Role = "Organiser"
        });

        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "organiser@test.com",
            Password = "Password123!"
        });

        var eventRequest = new CreateEventRequest
        {
            Name = "Durban Marathon",
            Description = "Annual marathon",
            EventDate = DateTime.Now.AddDays(60),
            Location = "Durban Beachfront",
            DistanceKm = 42.2m,
            EventType = "Marathon"
        };

        var response = await client.PostAsJsonAsync("/api/events", eventRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Access_Profile_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}