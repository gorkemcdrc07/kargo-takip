import { useEffect, useMemo, useState } from "react";
import { api, getToken, setToken as saveToken } from "../lib/api";
import type { Gonderi, Musteri } from "../types";
import { useNavigate } from "react-router-dom";

const DEFAULT_KURYE = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const LS_DISPLAY_NAME = "displayName";

function hataMesaji(e: unknown) {
    if (e instanceof Error && e.message) return e.message;
    try { return JSON.stringify(e); } catch { return String(e); }
}
function formatTarih(iso: string) { return new Date(iso).toLocaleString(); }
function statusClass(d: string) {
    switch (d) {
        case "OLUSTURULDU": return "status-olusturuldu";
        case "ATANDI": return "status-atandi";
        case "YOLDA": return "status-yolda";
        case "TESLIM_EDILDI": return "status-teslim";
        case "IPTAL_EDILDI": return "status-iptal";
        default: return "";
    }
}
function statusLabel(d: string) { return d.replace("_", " "); }

export default function Dashboard() {
    const [mesaj, setMesaj] = useState("");
    const [musteriler, setMusteriler] = useState<Musteri[]>([]);
    const [gonderiler, setGonderiler] = useState<Gonderi[]>([]);
    const [yeniMusteri, setYeniMusteri] = useState({ adSoyad: "", telefon: "", adres: "" });
    const [yeniGonderi, setYeniGonderi] = useState({ musteriId: "", cikisAdresi: "", varisAdresi: "" });
    const [kuryeId, setKuryeId] = useState(DEFAULT_KURYE);
    const displayName = typeof window !== "undefined" ? localStorage.getItem(LS_DISPLAY_NAME) : null;
    const token = getToken()!;
    const nav = useNavigate();

    const musteriMap = useMemo(() => {
        const m = new Map<string, Musteri>();
        for (const x of musteriler) m.set(x.id, x);
        return m;
    }, [musteriler]);

    async function yenile() {
        try {
            const [m, g] = await Promise.all([api.musteriler(token), api.gonderiler(token)]);
            setMusteriler(m);
            setGonderiler(g);
            if (!yeniGonderi.musteriId && m[0]) setYeniGonderi(s => ({ ...s, musteriId: m[0].id }));
        } catch (err) { setMesaj(`Veri çekilemedi: ${hataMesaji(err)}`); }
    }
    useEffect(() => { yenile(); }, []);

    function cikis() {
        saveToken(null);
        localStorage.removeItem(LS_DISPLAY_NAME);
        nav("/login", { replace: true });
    }

    return (
        <div>
            <header className="header">
                <div className="container header-bar">
                    <div className="brand">Kargo Takip</div>
                    <div className="header-actions">
                        {displayName && <span className="header-user">{displayName}</span>}
                        <button onClick={yenile} className="btn btn-header">Yenile</button>
                        <button onClick={cikis} className="btn btn-header btn-header-danger">Çıkış</button>
                    </div>
                </div>
            </header>

            <main className="page with-header">
                <div className="container panel">
                    {mesaj && <div className="msg" style={{ marginBottom: 12 }}>{mesaj}</div>}

                    {/* Yeni Müşteri ve Yeni Gönderi */}
                    <div className="grid-2">
                        <div className="card">
                            <h3 style={{ marginTop: 0 }}>Yeni Müşteri</h3>
                            <div className="small" style={{ marginBottom: 8 }}>Ad-soyad zorunlu, diğerleri opsiyonel.</div>
                            <div className="form">
                                <input className="input" placeholder="Ad Soyad"
                                    value={yeniMusteri.adSoyad}
                                    onChange={e => setYeniMusteri(s => ({ ...s, adSoyad: e.target.value }))} />
                                <input className="input" placeholder="Telefon"
                                    value={yeniMusteri.telefon}
                                    onChange={e => setYeniMusteri(s => ({ ...s, telefon: e.target.value }))} />
                                <input className="input" placeholder="Adres"
                                    value={yeniMusteri.adres}
                                    onChange={e => setYeniMusteri(s => ({ ...s, adres: e.target.value }))} />
                                <button className="btn btn-primary" onClick={async () => {
                                    if (!yeniMusteri.adSoyad.trim()) { setMesaj("Ad-soyad gerekli"); return; }
                                    try {
                                        await api.musteriEkle(yeniMusteri, token);
                                        setYeniMusteri({ adSoyad: "", telefon: "", adres: "" });
                                        await yenile();
                                        setMesaj("Müşteri eklendi.");
                                    } catch (err) { setMesaj(hataMesaji(err)); }
                                }}>Ekle</button>
                            </div>
                        </div>

                        <div className="card">
                            <h3 style={{ marginTop: 0 }}>Yeni Gönderi</h3>
                            <div className="form">
                                <select className="input" value={yeniGonderi.musteriId}
                                    onChange={e => setYeniGonderi(s => ({ ...s, musteriId: e.target.value }))}>
                                    {musteriler.map(m => <option key={m.id} value={m.id}>{m.adSoyad}</option>)}
                                </select>
                                <input className="input" placeholder="Çıkış Adresi"
                                    value={yeniGonderi.cikisAdresi}
                                    onChange={e => setYeniGonderi(s => ({ ...s, cikisAdresi: e.target.value }))} />
                                <input className="input" placeholder="Varış Adresi"
                                    value={yeniGonderi.varisAdresi}
                                    onChange={e => setYeniGonderi(s => ({ ...s, varisAdresi: e.target.value }))} />
                                <button className="btn btn-primary" onClick={async () => {
                                    if (!yeniGonderi.musteriId) { setMesaj("Müşteri seç"); return; }
                                    if (!yeniGonderi.cikisAdresi.trim() || !yeniGonderi.varisAdresi.trim()) { setMesaj("Adresleri doldur"); return; }
                                    try {
                                        await api.gonderiEkle(yeniGonderi, token);
                                        setYeniGonderi(s => ({ ...s, cikisAdresi: "", varisAdresi: "" }));
                                        await yenile();
                                        setMesaj("Gönderi oluşturuldu.");
                                    } catch (err) { setMesaj(hataMesaji(err)); }
                                }}>Oluştur</button>
                            </div>
                        </div>
                    </div>

                    {/* Kurye ID */}
                    <div className="card" style={{ marginTop: 16 }}>
                        <div className="small" style={{ marginBottom: 6 }}>Kurye ID (seed):</div>
                        <div style={{ display: "flex", gap: 8 }}>
                            <input className="input" value={kuryeId} onChange={e => setKuryeId(e.target.value)} />
                            <button className="btn btn-outline" onClick={() => setKuryeId(DEFAULT_KURYE)}>Varsayılanı getir</button>
                        </div>
                    </div>

                    {/* Liste */}
                    <div className="card" style={{ marginTop: 16 }}>
                        <h3 style={{ marginTop: 0 }}>Gönderiler</h3>
                        <div className="small" style={{ marginBottom: 8 }}>Duruma göre aksiyonlar sağda.</div>
                        <div style={{ overflowX: "auto" }}>
                            <table>
                                <thead>
                                    <tr>
                                        <th className="small">Takip</th>
                                        <th className="small">Müşteri</th>
                                        <th className="small">Durum</th>
                                        <th className="small">Adresler</th>
                                        <th className="small">Oluşturma</th>
                                        <th className="small">İşlem</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {gonderiler.map(g => {
                                        const musteri = musteriMap.get(g.musteriId)?.adSoyad ?? g.musteriId;
                                        return (
                                            <tr key={g.id} style={{ borderTop: "1px solid rgba(255,255,255,.1)" }}>
                                                <td><code>{g.takipKodu}</code></td>
                                                <td>{musteri}</td>
                                                <td><span className={`badge ${statusClass(g.durum)}`}>{statusLabel(g.durum)}</span></td>
                                                <td><div className="small">{g.cikisAdresi} → {g.varisAdresi}</div></td>
                                                <td className="small">{formatTarih(g.olusturmaZamani)}</td>
                                                <td><RowActions g={g} token={token} kuryeId={kuryeId} onDone={yenile} setMesaj={setMesaj} /></td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    </div>

                </div>
            </main>
        </div>
    );
}

function RowActions(p: {
    g: Gonderi; token: string; kuryeId: string;
    onDone: () => void; setMesaj: (m: string) => void;
}) {
    const { g, token, kuryeId, onDone, setMesaj } = p;
    async function handle<T>(fn: () => Promise<T>, ok: string) {
        try { await fn(); setMesaj(ok); onDone(); } catch (err) { setMesaj(hataMesaji(err)); }
    }
    if (g.durum === "OLUSTURULDU") {
        return (
            <div style={{ display: "flex", gap: 6 }}>
                <button className="btn btn-outline" onClick={() => handle(() => api.ata(g.id, { kuryeId, not: "Kurye atandı" }, token), "Atandı")}>Ata</button>
                <button className="btn btn-outline" onClick={() => handle(() => api.iptal(g.id, { not: "İptal" }, token), "İptal edildi")}>İptal</button>
            </div>
        );
    }
    if (g.durum === "ATANDI") {
        return (
            <div style={{ display: "flex", gap: 6 }}>
                <button className="btn btn-outline" onClick={() => handle(() => api.yolaCik(g.id, { not: "Yola çıkıldı" }, token), "Yolda")}>Yola çık</button>
                <button className="btn btn-outline" onClick={() => handle(() => api.iptal(g.id, { not: "İptal" }, token), "İptal edildi")}>İptal</button>
            </div>
        );
    }
    if (g.durum === "YOLDA") {
        return (
            <button className="btn btn-outline" onClick={() => handle(() => api.teslim(g.id, { not: "Teslim" }, token), "Teslim edildi")}>Teslim</button>
        );
    }
    return <span className="small">Tamamlandı</span>;
}
