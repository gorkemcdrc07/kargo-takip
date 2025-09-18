using System;

namespace KargoTakip.Alan;

public enum GonderiDurumu { OLUSTURULDU, ATANDI, YOLDA, TESLIM_EDILDI, IPTAL_EDILDI }

public record Kullanici(Guid Id, string Eposta, string ParolaOzu, string Rol);

public class Musteri
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AdSoyad { get; set; } = null!;
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
}

public class Kurye
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AdSoyad { get; set; } = null!;
    public string? Telefon { get; set; }
    public string? Plaka { get; set; }
}

public class Gonderi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TakipKodu { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpper();
    public Guid MusteriId { get; set; }
    public Guid? KuryeId { get; set; }
    public GonderiDurumu Durum { get; set; } = GonderiDurumu.OLUSTURULDU;
    public string CikisAdresi { get; set; } = null!;
    public string VarisAdresi { get; set; } = null!;
    public DateTime OlusturmaZamani { get; set; } = DateTime.UtcNow;
    public DateTime GuncellemeZamani { get; set; } = DateTime.UtcNow;
}

public class GonderiOlayi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GonderiId { get; set; }
    public GonderiDurumu? Onceki { get; set; }
    public GonderiDurumu Sonraki { get; set; }
    public DateTime Zaman { get; set; } = DateTime.UtcNow;
    public string? Not { get; set; }
}
