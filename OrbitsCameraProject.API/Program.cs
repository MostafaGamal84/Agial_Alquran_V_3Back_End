using Hangfire;
using Orbits.GeneralProject.BLL;
using Orbits.GeneralProject.BLL.Mapping;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.Setting.Authentication;
using Orbits.GeneralProject.DTO.Setting.Files;
using Orbits.GeneralProject.Repositroy.Base;
using OrbitsProject.API.BackgroundJobs;
using OrbitsProject.API.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Microsoft.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

#region Configuration
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName,
});

var env = builder.Environment.EnvironmentName;
builder.WebHost.UseIIS();
builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = false);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();
#endregion

// ----- Controllers
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// ----- Hangfire
builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// ----- Options (Config sections)
builder.Services.Configure<FileStorageSetting>(builder.Configuration.GetSection(AppsettingsEnum.FileStorageSetting.ToString()));
builder.Services.Configure<AuthSetting>(builder.Configuration.GetSection("AuthSetting"));

// ----- CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ----- HttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IAuditUserContext, HttpContextAuditUserContext>();

// ----- Swagger + Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Put **ONLY** your JWT Bearer token here.",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

// ----- BLL DI (scan)
foreach (var implementationType in typeof(BaseBLL).Assembly.GetTypes()
         .Where(t => t.IsClass && t.Name.EndsWith("BLL") && !t.IsAbstract))
{
    foreach (var interfaceType in implementationType.GetInterfaces())
    {
        builder.Services.AddScoped(interfaceType, implementationType);
    }
}

builder.Services.AddScoped<IStudentSubscriptionRenewalJob, StudentSubscriptionRenewalJob>();

// ----- Repository
builder.Services.AddScoped<IDbFactory, DbFactory>(s =>
    new DbFactory(
        new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")),
        s.GetRequiredService<IAuditUserContext>()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

// ----- AutoMapper
builder.Services.AddAutoMapper(
    typeof(EntityToDtoMappingProfile),
    typeof(DtoToEntityMappingProfile),
    typeof(DtoMappingProfile)
);

// ----- Cache
builder.Services.AddMemoryCache();

// ===== JWT Authentication =====
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // لا تعيد تسمية الـ claims داخليًا

var authSetting = builder.Configuration.GetSection("AuthSetting").Get<AuthSetting>();
if (authSetting == null || string.IsNullOrWhiteSpace(authSetting.Key))
    throw new InvalidOperationException("AuthSetting:Key is missing in configuration.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSetting.Key));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // اجعلها true بالإنتاج على HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,

            // مهم لتحديد ال Claims القياسية
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });

// ===== Build app =====
var app = builder.Build();

// ----- Swagger (يمكن جعلها env-based لو تحب)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();            // ✅ أضف دي

// ----- CORS
app.UseCors("CorsPolicy");

// ----- HTTPS + Static
app.UseHttpsRedirection();
app.UseStaticFiles();

// ----- Authn/Authz (الترتيب مهم)
app.UseAuthentication();
app.UseAuthorization();

// ----- Map controllers
app.MapControllers();
//app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IStudentSubscriptionRenewalJob>(
        "student-subscription-renewal-monthly",
        job => job.RenewSubscriptionsAsync(),
        Cron.Monthly(1, 0),
        BusinessDateTime.CairoTimeZone);
}


// ----- Run
app.Run();
