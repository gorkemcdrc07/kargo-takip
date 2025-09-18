import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import Dashboard from "./pages/Menu";
import { getToken } from "./lib/api";
import type { JSX } from "react";

function ProtectedRoute({ children }: { children: JSX.Element }) {
    const token = getToken();
    return token ? children : <Navigate to="/login" replace />;
}

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Navigate to="/app" replace />} />
                <Route path="/login" element={<LoginPage />} />
                <Route
                    path="/app"
                    element={
                        <ProtectedRoute>
                            <Dashboard />
                        </ProtectedRoute>
                    }
                />
                <Route path="*" element={<Navigate to="/app" replace />} />
            </Routes>
        </BrowserRouter>
    );
}
