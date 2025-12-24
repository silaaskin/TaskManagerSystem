using TaskManagerSystem.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Session ve Cache Hizmetleri
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Controllers + NewtonsoftJson Yapılandırması
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // Entity Framework'teki ilişkili tabloların birbirini sonsuz döngüye sokmasını engeller
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        // İsteğe bağlı: JSON içindeki null değerleri yazmamak istersen:
        // options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

// 3. MySQL Veritabanı Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 34))
    )
);

var app = builder.Build();

// 4. Middleware (Ara Yazılım) Sıralaması
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ÖNEMLİ: Session, Routing'den sonra ama Authorization'dan önce gelmelidir
app.UseSession();

app.UseAuthorization();

// 5. Route (Yönlendirme) Tanımı
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();