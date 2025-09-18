using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KargoTakip.Altyapi;
using KargoTakip.Alan;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------- CONNECTION STRING (fallback) --------------------
var cs =
    builder.Configuration.GetConnectionString("Varsayilan") ??      
    builder.Configuration.GetConnectionString("Default") ?? 
    builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    builder.Configuration.GetConnectionString("Postgres") ??  
    builder.Configuration["POSTGRES_CONNECTION_STRING"];  

if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Connection string missing.");

// -------------------- SERVICES --------------------
builder.Services.AddDbContext<UygulamaBaglam>(o => o.UseNpgsql(cs));


builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:5175")
         .AllowAnyHeader()
         .AllowAnyMethod()));

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var cfg = builder.Configuration;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidAudience = cfg["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Swagger (+Bearer)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KargoTakip.Api", Version = "v1" });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token (yalnızca token'ı girin).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

var app = builder.Build();

// -------------------- MIDDLEWARE --------------------
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KargoTakip.Api v1"));

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// -------------------- ENDPOINTS --------------------

// Sağlık
app.MapGet("/api/saglik", () => Results.Ok("OK"));

// ---- KİMLİK (LOGIN ANONİM) ----
app.MapPost("/api/kimlik/giris", (GirisIstek istek, UygulamaBaglam db, IConfiguration cfg) =>
{
    var u = db.Kullanicilar.SingleOrDefault(x => x.Eposta == istek.Eposta);
    if (u is null) return Results.Unauthorized();

    bool parolaGecerli =
        (u.ParolaOzu?.StartsWith("$2") == true
            ? BCrypt.Net.BCrypt.Verify(istek.Parola, u.ParolaOzu) 
            : u.ParolaOzu == istek.Parola);  

    if (!parolaGecerli) return Results.Unauthorized();

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
    var token = new JwtSecurityToken(
        issuer: cfg["Jwt:Issuer"],
        audience: cfg["Jwt:Audience"],
        claims: new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(ClaimTypes.Role, u.Rol),
            new Claim(JwtRegisteredClaimNames.Email, u.Eposta)
        },
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return Results.Ok(new
    {
        accessToken = new JwtSecurityTokenHandler().WriteToken(token),
        kullanici = new { u.Id, u.Eposta, u.Rol, adSoyad = u.AdSoyad }
    });
})
.AllowAnonymous();

// ---- MÜŞTERİLER
app.MapPost("/api/musteriler", (YeniMusteriIstek istek, UygulamaBaglam db) =>
{
    var m = new Musteri { AdSoyad = istek.AdSoyad, Telefon = istek.Telefon, Adres = istek.Adres };
    db.Musteriler.Add(m);
    db.SaveChanges();
    return Results.Created($"/api/musteriler/{m.Id}", new { m.Id, m.AdSoyad });
}).RequireAuthorization();

app.MapGet("/api/musteriler", (UygulamaBaglam db) =>
{
    var liste = db.Musteriler
        .OrderBy(x => x.AdSoyad)
        .Select(x => new { x.Id, x.AdSoyad, x.Telefon })
        .ToList();
    return Results.Ok(liste);
}).RequireAuthorization();

// ---- GÖNDERİLER: oluştur ve listele
app.MapPost("/api/gonderiler", (YeniGonderiIstek istek, UygulamaBaglam db) =>
{
    if (!db.Musteriler.Any(m => m.Id == istek.MusteriId))
        return Results.BadRequest(new { hata = "Musteri bulunamadı" });

    var g = new Gonderi
    {
        MusteriId = istek.MusteriId,
        CikisAdresi = istek.CikisAdresi,
        VarisAdresi = istek.VarisAdresi
    };

    db.Gonderiler.Add(g);
    db.GonderiOlaylari.Add(new GonderiOlayi
    {
        GonderiId = g.Id,
        Onceki = null,
        Sonraki = GonderiDurumu.OLUSTURULDU,
        Not = "Oluşturuldu"
    });
    db.SaveChanges();

    return Results.Created($"/api/gonderiler/{g.Id}", new { g.Id, g.TakipKodu, Durum = g.Durum.ToString() });
}).RequireAuthorization();

app.MapGet("/api/gonderiler", (UygulamaBaglam db) =>
{
    var liste = db.Gonderiler
        .OrderByDescending(x => x.OlusturmaZamani)
        .Select(x => new
        {
            x.Id,
            x.TakipKodu,
            x.MusteriId,
            x.KuryeId,
            Durum = x.Durum.ToString(),
            x.CikisAdresi,
            x.VarisAdresi,
            x.OlusturmaZamani
        })
        .ToList();
    return Results.Ok(liste);
}).RequireAuthorization();

// ---- GÖNDERİ DURUM GEÇİŞLERİ
// Kurye ata: OLUSTURULDU > ATANDI
app.MapPost("/api/gonderiler/{id:guid}/atama", (Guid id, AtamaIstek istek, UygulamaBaglam db) =>
{
    var g = db.Gonderiler.SingleOrDefault(x => x.Id == id);
    if (g is null) return Results.NotFound();

    if (g.Durum != GonderiDurumu.OLUSTURULDU)
        return Results.BadRequest(new { hata = "Sadece 'OLUSTURULDU' durumundaki gönderi atanabilir." });

    var kuryeVarMi = db.Kullanicilar.Any(x => x.Id == istek.KuryeId && x.Rol == "Kurye");
    if (!kuryeVarMi) return Results.BadRequest(new { hata = "Geçersiz kurye." });

    var onceki = g.Durum;
    g.KuryeId = istek.KuryeId;
    g.Durum = GonderiDurumu.ATANDI;

    db.GonderiOlaylari.Add(new GonderiOlayi
    {
        GonderiId = g.Id,
        Onceki = onceki,
        Sonraki = g.Durum,
        Not = istek.Not ?? "Kurye atandı"
    });

    db.SaveChanges();
    return Results.Ok(new { g.Id, g.KuryeId, Durum = g.Durum.ToString() });
}).RequireAuthorization();

// Yola çık: ATANDI > YOLDA
app.MapPost("/api/gonderiler/{id:guid}/yola-cik", (Guid id, DurumNotIstek istek, UygulamaBaglam db) =>
{
    var g = db.Gonderiler.SingleOrDefault(x => x.Id == id);
    if (g is null) return Results.NotFound();

    if (g.Durum != GonderiDurumu.ATANDI)
        return Results.BadRequest(new { hata = "Sadece 'ATANDI' durumundaki gönderi yola çıkabilir." });

    var onceki = g.Durum;
    g.Durum = GonderiDurumu.YOLDA;

    db.GonderiOlaylari.Add(new GonderiOlayi
    {
        GonderiId = g.Id,
        Onceki = onceki,
        Sonraki = g.Durum,
        Not = istek.Not ?? "Yola çıkıldı"
    });

    db.SaveChanges();
    return Results.Ok(new { g.Id, Durum = g.Durum.ToString() });
}).RequireAuthorization();

// Teslim: YOLDA -> TESLIM_EDILDI
app.MapPost("/api/gonderiler/{id:guid}/teslim", (Guid id, DurumNotIstek istek, UygulamaBaglam db) =>
{
    var g = db.Gonderiler.SingleOrDefault(x => x.Id == id);
    if (g is null) return Results.NotFound();

    if (g.Durum != GonderiDurumu.YOLDA)
        return Results.BadRequest(new { hata = "Sadece 'YOLDA' durumundaki gönderi teslim edilebilir." });

    var onceki = g.Durum;
    g.Durum = GonderiDurumu.TESLIM_EDILDI;

    db.GonderiOlaylari.Add(new GonderiOlayi
    {
        GonderiId = g.Id,
        Onceki = onceki,
        Sonraki = g.Durum,
        Not = istek.Not ?? "Teslim edildi"
    });

    db.SaveChanges();
    return Results.Ok(new { g.Id, Durum = g.Durum.ToString() });
}).RequireAuthorization();

// İptal: OLUSTURULDU/ATANDI -> IPTAL_EDILDI
app.MapPost("/api/gonderiler/{id:guid}/iptal", (Guid id, DurumNotIstek istek, UygulamaBaglam db) =>
{
    var g = db.Gonderiler.SingleOrDefault(x => x.Id == id);
    if (g is null) return Results.NotFound();

    if (g.Durum != GonderiDurumu.OLUSTURULDU && g.Durum != GonderiDurumu.ATANDI)
        return Results.BadRequest(new { hata = "Sadece 'OLUSTURULDU' veya 'ATANDI' durumları iptal edilebilir." });

    var onceki = g.Durum;
    g.Durum = GonderiDurumu.IPTAL_EDILDI;

    db.GonderiOlaylari.Add(new GonderiOlayi
    {
        GonderiId = g.Id,
        Onceki = onceki,
        Sonraki = g.Durum,
        Not = istek.Not ?? "İptal edildi"
    });

    db.SaveChanges();
    return Results.Ok(new { g.Id, Durum = g.Durum.ToString() });
}).RequireAuthorization();

// ---- GÖNDERİ OLAY GEÇMİŞİ / DETAY
app.MapGet("/api/gonderiler/{id:guid}/olaylar", (Guid id, UygulamaBaglam db) =>
{
    var varMi = db.Gonderiler.Any(g => g.Id == id);
    if (!varMi) return Results.NotFound();

    var olaylar = db.GonderiOlaylari
        .Where(o => o.GonderiId == id)
        .OrderBy(o => o.Zaman)
        .Select(o => new
        {
            o.Zaman,
            Onceki = o.Onceki == null ? null : o.Onceki.ToString(),
            Sonraki = o.Sonraki.ToString(),
            o.Not
        })
        .ToList();

    return Results.Ok(olaylar);
}).RequireAuthorization();

app.MapGet("/api/gonderiler/{id:guid}", (Guid id, UygulamaBaglam db) =>
{
    var g = db.Gonderiler
        .Where(x => x.Id == id)
        .Select(x => new
        {
            x.Id,
            x.TakipKodu,
            x.MusteriId,
            x.KuryeId,
            Durum = x.Durum.ToString(),
            x.CikisAdresi,
            x.VarisAdresi,
            x.OlusturmaZamani,
            Olaylar = db.GonderiOlaylari
                .Where(o => o.GonderiId == x.Id)
                .OrderBy(o => o.Zaman)
                .Select(o => new
                {
                    o.Zaman,
                    Onceki = o.Onceki == null ? null : o.Onceki.ToString(),
                    Sonraki = o.Sonraki.ToString(),
                    o.Not
                }).ToList()
        })
        .SingleOrDefault();

    return g is null ? Results.NotFound() : Results.Ok(g);
}).RequireAuthorization();

// -------------------- MIGRATIONS --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UygulamaBaglam>();
    db.Database.Migrate();
}

app.Run();

// -------------------- DTO'lar --------------------
record GirisIstek(string Eposta, string Parola);
record YeniMusteriIstek(string AdSoyad, string? Telefon, string? Adres);
record YeniGonderiIstek(Guid MusteriId, string CikisAdresi, string VarisAdresi);
record AtamaIstek(Guid KuryeId, string? Not);
record DurumNotIstek(string? Not);
