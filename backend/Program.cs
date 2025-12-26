using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. 配置数据库 (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. 添加 Session 支持
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; 
});

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

// 3. 配置 CORS (允许前端访问)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. 添加控制器
builder.Services.AddControllers();

builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.Configure<AiTaggingOptions>(builder.Configuration.GetSection("AiTagging"));
builder.Services.AddSingleton<IAiVisionTagGenerator, OpenAiVisionTagGenerator>();
builder.Services.AddSingleton<AiTaggingBackgroundService>();
builder.Services.AddSingleton<IAiTaggingQueue>(sp => sp.GetRequiredService<AiTaggingBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<AiTaggingBackgroundService>());

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApiDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Version = "v1";
        document.Info.Title = "Image Gallery API";
        document.Info.Description = "基于 ASP.NET Core 9.0 的图片管理系统 API";
    };
});

var app = builder.Build();

InitializeDatabase(app);

// --- 管道配置 ---

app.UseHttpsRedirection();
app.UseStaticFiles();

// 顺序：CORS -> Session -> Authorization
app.UseCors("Frontend");
app.UseSession();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(); 
    app.UseSwaggerUi(); 
}

app.MapControllers();

app.Run();

static void InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    EnsurePhotoSchema(db);
}

static void EnsurePhotoSchema(AppDbContext db)
{
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""Tags"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Tags"" PRIMARY KEY AUTOINCREMENT,
        ""Name"" TEXT NOT NULL,
        ""Type"" INTEGER NOT NULL
    );");

    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Tags_Name_Type"" ON ""Tags"" (""Name"", ""Type"");");

    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""Photos"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Photos"" PRIMARY KEY AUTOINCREMENT,
        ""UserId"" INTEGER NOT NULL,
        ""FilePath"" TEXT NOT NULL,
        ""ThumbnailPath"" TEXT NOT NULL,
        ""Width"" INTEGER NOT NULL,
        ""Height"" INTEGER NOT NULL,
        ""TakenAt"" TEXT NULL,
        ""Location"" TEXT NULL,
        ""Description"" TEXT NULL,
        ""CreatedAt"" TEXT NOT NULL,
        CONSTRAINT ""FK_Photos_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
    );");

    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Photos_UserId"" ON ""Photos"" (""UserId"");");

    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""PhotoTags"" (
        ""PhotoId"" INTEGER NOT NULL,
        ""TagId"" INTEGER NOT NULL,
        CONSTRAINT ""PK_PhotoTags"" PRIMARY KEY (""PhotoId"", ""TagId""),
        CONSTRAINT ""FK_PhotoTags_Photos_PhotoId"" FOREIGN KEY (""PhotoId"") REFERENCES ""Photos"" (""Id"") ON DELETE CASCADE,
        CONSTRAINT ""FK_PhotoTags_Tags_TagId"" FOREIGN KEY (""TagId"") REFERENCES ""Tags"" (""Id"") ON DELETE CASCADE
    );");

    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_PhotoTags_TagId"" ON ""PhotoTags"" (""TagId"");");
}
