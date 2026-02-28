using GameServerManager.Server.Database;
using GameServerManager.Server.Hubs;
using GameServerManager.Server.Jobs;
using GameServerManager.Server.Middleware;
using GameServerManager.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DatabaseContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("GameServerManager")).EnableSensitiveDataLogging());

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>(true, "Bearer");
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Query["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddResponseCompression(opts => opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" }));
builder.Services.AddSignalR();
builder.Services.AddSingleton<ConsoleReaderService>();
builder.Services.AddHostedService<SystemResourcesBackgroundService>();

// Quartz Configuration

builder.Services.AddTransient<BackupJob>();

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    q.UseSimpleTypeLoader();
    q.UsePersistentStore(x =>
    {
        x.UseMicrosoftSQLite(builder.Configuration.GetConnectionString("GameServerManager"));
        x.UseProperties = true;
        x.UseJsonSerializer();
    });
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});

builder.Services.AddQuartzServer(options =>
{
    options.WaitForJobsToComplete = true;
});

builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));
builder.Services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true;
    options.Scheduling.OverWriteExistingData = true;
});

var app = builder.Build();

// Ensure database is created and seed test user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    dbContext.Database.EnsureCreated();

    // Create test user if no users exist
    if (!dbContext.Users.Any())
    {
        var testUser = new Users
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Password = Bcrypt.HashPassword("admin")
        };
        dbContext.Users.Add(testUser);
        dbContext.SaveChanges();
    }
}

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestCulture();

app.MapControllers();
app.MapHub<ConsoleHub>("/consolehub");
app.MapHub<DashboardHub>("/dashboardhub");
app.MapFallbackToFile("index.html");

app.Run();
