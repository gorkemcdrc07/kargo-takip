using KargoTakip.Alan;
using Microsoft.EntityFrameworkCore;
using System;

namespace KargoTakip.Altyapi;

public class UygulamaBaglam : DbContext
{
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<Kurye> Kuryeler => Set<Kurye>();
    public DbSet<Gonderi> Gonderiler => Set<Gonderi>();
    public DbSet<GonderiOlayi> GonderiOlaylari => Set<GonderiOlayi>();

    public UygulamaBaglam(DbContextOptions<UygulamaBaglam> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Gonderi>().HasIndex(x => x.TakipKodu).IsUnique();


        b.Entity<Kullanici>().HasData(
            new Kullanici(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "admin@example.com",
                "Admin123!",
                "Admin"
            ),
            new Kullanici(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "kurye@example.com",
                "Kurye123!",
                "Kurye"
            )
        );
    }
}
