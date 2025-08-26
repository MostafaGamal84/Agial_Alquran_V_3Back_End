
#region Configuration
using Orbits.GeneralProject.BLL;
using Orbits.GeneralProject.BLL.Mapping;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.Setting.Authentication;
using Orbits.GeneralProject.DTO.Setting.Files;
using Orbits.GeneralProject.Repositroy.Base;
using System.Data.SqlClient;


var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName,
});

var env = builder.Environment.EnvironmentName;
builder.WebHost.UseIIS();
builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = false);
builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
#endregion

builder.Services.AddControllers();
#region config sec
builder.Services.Configure<FileStorageSetting>(builder.Configuration.GetSection(AppsettingsEnum.FileStorageSetting.ToString()));

builder.Services.Configure<AuthSetting>(
    builder.Configuration.GetSection("AuthSetting"));
#endregion config sec
#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
#endregion

#region HttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
#endregion
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

#region BLL

foreach (var implementationType in typeof(BaseBLL).Assembly.GetTypes().Where(s => s.IsClass && s.Name.EndsWith("BLL") && !s.IsAbstract))
{
    foreach (var interfaceType in implementationType.GetInterfaces())
    {
        builder.Services.AddScoped(interfaceType, implementationType);
    }
}

#endregion BLL
//#region DexefStorageManager
//builder.Services.AddScoped<IDexefStorageManager, DexefStorageManager>();
//#endregion DexefStorageManager
//builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

#region Repository

builder.Services.AddScoped<IDbFactory, DbFactory>(s => new DbFactory(new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


#endregion Repository

#region Mapper

builder.Services.AddAutoMapper(typeof(EntityToDtoMappingProfile), typeof(DtoToEntityMappingProfile), typeof(DtoMappingProfile));

#endregion Mapper

#region Repository

builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

#endregion Repository
builder.Services.AddMemoryCache();
#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
#endregion
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(policyName: "CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();
//#region Localization 
//app.UseMiddleware<LocalizationMiddleware>();
//#endregion

app.MapControllers();

app.Run();