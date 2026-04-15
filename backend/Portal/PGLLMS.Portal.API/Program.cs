using PGLLMS.Admin.Infrastructure;
using PGLLMS.Portal.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Required by Identity registered in shared Infrastructure
builder.Services.AddDataProtection();

// Shared Infrastructure (DbContext + Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Portal Services
builder.Services.AddScoped<PortalFolderService>();
builder.Services.AddScoped<PortalCourseService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PGLLMS Portal API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PortalFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5174", "http://localhost:5175")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PGLLMS Portal API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("PortalFrontend");
app.MapControllers();

app.Run();
