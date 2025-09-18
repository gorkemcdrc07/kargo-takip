import { useState } from "react";
import styles from "./LoginPage.module.css";
import { api, setToken } from "../lib/api";
import { useNavigate } from "react-router-dom";

export default function LoginPage() {
    const [email, setEmail] = useState("admin@example.com");
    const [password, setPassword] = useState("Admin123!");
    const [message, setMessage] = useState("");
    const navigate = useNavigate();

    async function handleLogin(e: React.FormEvent) {
        e.preventDefault();
        setMessage("");
        try {
            const r = await api.login({ eposta: email, parola: password });
            setToken(r.accessToken);
            navigate("/app", { replace: true });
        } catch {
            setMessage("Giriş başarısız. Bilgileri kontrol edin.");
        }
    }

    return (
        <div className={styles.wrap}>
            <form className={styles.card} onSubmit={handleLogin}>
                <h1 className={styles.title}>Kargo Takip</h1>
                <p className={styles.subtitle}>Hesabınızla giriş yapın</p>

                <div className={styles.form}>
                    <div>
                        <label className={styles.label}>E-posta</label>
                        <input
                            type="email"
                            className={styles.input}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            autoComplete="email"
                            required
                        />
                    </div>

                    <div>
                        <label className={styles.label}>Parola</label>
                        <input
                            type="password"
                            className={styles.input}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            autoComplete="current-password"
                            required
                        />
                    </div>

                    <button className={styles.button} type="submit">
                        Giriş Yap
                    </button>
                </div>

                {message && <div className={styles.msg}>{message}</div>}
            </form>
        </div>
    );
}
