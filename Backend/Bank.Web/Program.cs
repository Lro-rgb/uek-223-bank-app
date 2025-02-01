using System.Text;
using Bank.DbAccess;
using Bank.DbAccess.Data;
using Bank.DbAccess.Repositories;
using Bank.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Bank.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", corsPolicyBuilder =>
            {
                corsPolicyBuilder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
            
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("JwtSettings")
        );
            
        builder.Services.Configure<DatabaseSettings>(
            builder.Configuration.GetSection("DatabaseSettings")
        );
            
        builder.Services
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(jwtSettings?.PrivateKey ?? throw new InvalidOperationException("JwtSettings:PrivateKey is null"))
                        ),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddTransient<IDatabaseSeeder, DatabaseSeeder>();
        builder.Services.AddTransient<ILedgerRepository, LedgerRepository>();
        builder.Services.AddTransient<IUserRepository, UserRepository>();
        builder.Services.AddTransient<ILoginService, LoginService>();
        builder.Services.AddTransient<IBookingRepository, BookingRepository>();
            
        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "My API - V1",
                    Version = "v1"
                }
            );
                
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter your JWT token in this field",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            };

            c.AddSecurityRequirement(securityRequirement);
        });
            
            
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                builder.Configuration.GetSection("DatabaseSettings:ConnectionString").Value,
                new MySqlServerVersion(new Version(11, 6, 2)),
                sqlOptions => sqlOptions.MigrationsAssembly("Bank.Web")
            )
        );
            
        var app = builder.Build();
        app.UseCors("AllowAll");
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            options.RoutePrefix = string.Empty;
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            try
            {
                var databaseSeeder = services.GetRequiredService<IDatabaseSeeder>();     
                Console.WriteLine("Initializing database.");
                databaseSeeder.Initialize();
                Console.WriteLine("Seeding data.");
                databaseSeeder.Seed();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during startup: {ex.Message}");
            }
        }
            
        app.Run();
    }
}