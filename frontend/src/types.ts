export type Kullanici = {
    id: string;
    eposta: string;
    rol: string;
    adSoyad?: string;  
};

export type GirisYaniti = {
    accessToken: string;
    kullanici: Kullanici;
};

export type Musteri = {
    id: string;
    adSoyad: string;
    telefon?: string;
    adres?: string;
};

export type Gonderi = {
    id: string;
    takipKodu: string;
    musteriId: string;
    kuryeId: string | null;
    durum: "OLUSTURULDU" | "ATANDI" | "YOLDA" | "TESLIM_EDILDI" | "IPTAL_EDILDI";
    cikisAdresi: string;
    varisAdresi: string;
    olusturmaZamani: string;
};
