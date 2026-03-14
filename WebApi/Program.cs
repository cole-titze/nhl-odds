using DatabaseAccess;
using DatabaseAccess.WebBookmakerOddsRepository;
using DatabaseAccess.WebGameOddsRepository;
using DatabaseAccess.WebTeamRepository;
using Microsoft.EntityFrameworkCore;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.JobService;
using WebApi.BusinessLogic.TeamGetter;

var builder = WebApplication.CreateBuilder(args);

string? _connectionString = Environment.GetEnvironmentVariable("NHL_DATABASE");
if (_connectionString == null)
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    _connectionString = config.GetConnectionString("NHL_DATABASE");
}
if (_connectionString == null)
    throw new Exception("Connection String Null");

// Add services to the container
builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHostedService<DailyDataCollectionService>();
builder.Services.AddScoped<ITeamGetter, TeamGetter>();
builder.Services.AddScoped<IGameOddsGetter, GameOddsGetter>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IWebBookmakerOddsRepository, WebBookmakerOddsRepository>();
builder.Services.AddScoped<IGameOddsRepository, GameOddsRepository>();
builder.Services.AddDbContext<GameDbContext>(x => x.UseSqlServer(_connectionString));
builder.Services.AddLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("http://10.0.0.19:8081");
                          builder.WithOrigins("http://192.168.1.19:8081");
                          builder.WithOrigins("http://localhost:8081");
                          builder.WithOrigins("http://localhost:5173");
                      });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(MyAllowSpecificOrigins);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
