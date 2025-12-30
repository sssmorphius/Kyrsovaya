using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PlayerService.Consumers;
using PlayerService.Models;
using System.Text;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ParticipantDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers for events from AuthService
    x.AddConsumer<TeamCreatedConsumer>();
    x.AddConsumer<TeamMemberChangedConsumer>();
    x.AddConsumer<TeamDeletedConsumer>();

    // Add consumers for events from TournamentService
    x.AddConsumer<TournamentStatusChangedConsumer>();
    x.AddConsumer<TournamentUpdatedConsumer>();

    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        // Configure receiving endpoints for AuthService events
        cfg.ReceiveEndpoint("team-created-player", e =>
        {
            e.ConfigureConsumer<TeamCreatedConsumer>(context);
            e.Bind("tournament.exchange", s =>
            {
                s.RoutingKey = "team.created";
                s.ExchangeType = "topic";
            });
        });

        cfg.ReceiveEndpoint("team-member-changed-player", e =>
        {
            e.ConfigureConsumer<TeamMemberChangedConsumer>(context);
            e.Bind("tournament.exchange", s =>
            {
                s.RoutingKey = "team.member.changed";
                s.ExchangeType = "topic";
            });
        });

        cfg.ReceiveEndpoint("team-deleted-player", e =>
        {
            e.ConfigureConsumer<TeamDeletedConsumer>(context);
            e.Bind("tournament.exchange", s =>
            {
                s.RoutingKey = "team.deleted";
                s.ExchangeType = "topic";
            });
        });

        // Configure receiving endpoints for TournamentService events
        cfg.ReceiveEndpoint("tournament-status-changed-player", e =>
        {
            e.ConfigureConsumer<TournamentStatusChangedConsumer>(context);
            e.Bind("tournament.exchange", s =>
            {
                s.RoutingKey = "tournament.status.changed";
                s.ExchangeType = "topic";
            });
        });

        cfg.ReceiveEndpoint("tournament-updated-player", e =>
        {
            e.ConfigureConsumer<TournamentUpdatedConsumer>(context);
            e.Bind("tournament.exchange", s =>
            {
                s.RoutingKey = "tournament.updated";
                s.ExchangeType = "topic";
            });
        });

        // Configure routing for events we publish
        cfg.Message<Shared.Contracts.Events.ApplicationSubmittedEvent>(x =>
            x.SetEntityName("tournament.exchange"));
        cfg.Message<Shared.Contracts.Events.ApplicationStatusChangedEvent>(x =>
            x.SetEntityName("tournament.exchange"));

        cfg.Publish<Shared.Contracts.Events.ApplicationSubmittedEvent>(x =>
            x.ExchangeType = "topic");
        cfg.Publish<Shared.Contracts.Events.ApplicationStatusChangedEvent>(x =>
            x.ExchangeType = "topic");

        cfg.Send<Shared.Contracts.Events.ApplicationSubmittedEvent>(x =>
            x.UseRoutingKeyFormatter(_ => "application.submitted"));
        cfg.Send<Shared.Contracts.Events.ApplicationStatusChangedEvent>(x =>
            x.UseRoutingKeyFormatter(_ => "application.status.changed"));
    });
});

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Add authorization
builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth Service API",
        Version = "v1"
    });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Player Service V1");
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created

app.Run();