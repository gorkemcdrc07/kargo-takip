import { type GirisYaniti, type Musteri, type Gonderi } from "../types";

const BASE = "";

function buildHeaders(token?: string) {
    const h: Record<string, string> = { "Content-Type": "application/json" };
    if (token) h.Authorization = `Bearer ${token}`;
    return h;
}


async function req<T>(path: string, init: RequestInit = {}, token?: string): Promise<T> {
    const res = await fetch(`${BASE}${path}`, {
        ...init,
        headers: { ...buildHeaders(token), ...(init.headers || {}) }
    });

    if (!res.ok) {
        let msg = `${res.status} ${res.statusText}`;


        const raw = await res.clone().text().catch(() => "");
        if (raw) {
            try {
                msg += " - " + JSON.stringify(JSON.parse(raw));
            } catch {
                msg += " - " + raw;
            }
        }

        throw new Error(msg);
    }

    return res.json() as Promise<T>;
}

export const api = {
    // kimlik
    login: (d: { eposta: string; parola: string }) =>
        req<GirisYaniti>("/api/kimlik/giris", { method: "POST", body: JSON.stringify(d) }),

    // müşteriler
    musteriler: (token: string) =>
        req<Musteri[]>("/api/musteriler", { method: "GET" }, token),

    musteriEkle: (d: { adSoyad: string; telefon?: string; adres?: string }, token: string) =>
        req<{ id: string; adSoyad: string }>(
            "/api/musteriler",
            { method: "POST", body: JSON.stringify(d) },
            token
        ),

    // gönderiler
    gonderiler: (token: string) =>
        req<Gonderi[]>("/api/gonderiler", { method: "GET" }, token),

    gonderiEkle: (d: { musteriId: string; cikisAdresi: string; varisAdresi: string }, token: string) =>
        req<{ id: string; takipKodu: string; Durum: string }>(
            "/api/gonderiler",
            { method: "POST", body: JSON.stringify(d) },
            token
        ),

    // durum geçişleri
    ata: (id: string, d: { kuryeId: string; not?: string }, token: string) =>
        req<{ id: string; kuryeId: string; Durum: string }>(
            `/api/gonderiler/${id}/atama`,
            { method: "POST", body: JSON.stringify(d) },
            token
        ),

    yolaCik: (id: string, d: { not?: string }, token: string) =>
        req<{ id: string; Durum: string }>(
            `/api/gonderiler/${id}/yola-cik`,
            { method: "POST", body: JSON.stringify(d) },
            token
        ),

    teslim: (id: string, d: { not?: string }, token: string) =>
        req<{ id: string; Durum: string }>(
            `/api/gonderiler/${id}/teslim`,
            { method: "POST", body: JSON.stringify(d) },
            token
        ),

    iptal: (id: string, d: { not?: string }, token: string) =>
        req<{ id: string; Durum: string }>(
            `/api/gonderiler/${id}/iptal`,
            { method: "POST", body: JSON.stringify(d) },
            token
        ),
};

// token yardımcıları
export function getToken() {
    return localStorage.getItem("token");
}
export function setToken(t: string | null) {
    if (t) localStorage.setItem("token", t);
    else localStorage.removeItem("token");
}
