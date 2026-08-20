using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// YARP reverse proxy — routes defined in appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT validation — gateway validates the token so downstream services don't have to
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Swagger UI for the gateway — provides links to each service's own Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "FreshMart API Gateway",
        Version     = "v1",
        Description = "Entry point for all FreshMart microservices.\n\n" +
                      "**Service Swagger UIs:**\n" +
                      "- Auth Service: http://localhost:5001/swagger\n" +
                      "- Product Service: http://localhost:5002/swagger\n" +
                      "- Order Service: http://localhost:5003/swagger\n" +
                      "- Payment Service: http://localhost:5004/swagger"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    c.RoutePrefix = "swagger";
});

app.UseMiddleware<ApiGateway.Middleware.CorrelationIdMiddleware>();
app.UseCors();
app.UseAuthentication();
app.MapReverseProxy();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }));

await app.RunAsync();
