import React, { useState, lazy, Suspense } from 'react';
import { MotionConfig } from 'framer-motion';
import HudHeader from './components/HudHeader';
import ScrollytellingCanvas from './components/ScrollytellingCanvas';
import MenuLineupDark from './components/MenuLineupDark';
import SmashBuilder from './components/SmashBuilder';
import SearTelemetry from './components/SearTelemetry';
import HudFooter from './components/HudFooter';
// El modal de acceso pesa bastante y solo se usa al pulsar "entrar": se descarga bajo demanda.
const AuthModal = lazy(() => import('./components/AuthModal'));

function leerUsuario() {
  try {
    const stored = localStorage.getItem('saborator_user');
    return stored ? JSON.parse(stored) : null;
  } catch (e) {
    console.warn('Error al leer sesión local:', e);
    return null;
  }
}

export default function App() {
  const [authModalOpen, setAuthModalOpen] = useState(false);
  // Usuario autenticado persistido (se lee una sola vez al montar)
  const [currentUser, setCurrentUser] = useState(leerUsuario);
  const [authCargado, setAuthCargado] = useState(false);

  const handleLogout = () => {
    localStorage.removeItem('saborator_token');
    localStorage.removeItem('saborator_user');
    setCurrentUser(null);
  };

  const handleAuthSuccess = (user) => {
    setCurrentUser(user);
  };

  return (
    <MotionConfig reducedMotion="user">
    <div className="w-full min-h-screen bg-[#0a0a0a] text-white selection:bg-[#ff5500] selection:text-black relative">
      {/* HUD Telemetry Top Header con botón de Auth OTP */}
      <HudHeader 
        onOpenAuth={() => { setAuthCargado(true); setAuthModalOpen(true); }}
        currentUser={currentUser}
        onLogout={handleLogout}
      />

      <main>
        {/* 1. Hero & Scrollytelling de Preparación (Scroll-driven WebGL Three.js Animation + HUD) */}
        <ScrollytellingCanvas />

        {/* 2. Menú ("01. THE LINE-UP") */}
        <MenuLineupDark />

        {/* 3. Configurador Interactivo ("02. BUILD YOUR SMASH") */}
        <SmashBuilder />

        {/* 4. Especificaciones y Telemetría ("04. THE SEAR / THE CUT") */}
        <SearTelemetry />
      </main>

      {/* Terminal Footer */}
      <HudFooter />

      {/* Modal de Autenticación OTP (SMS / WhatsApp por Twilio + Invitado) */}
      {authCargado && (
        <Suspense fallback={null}>
          <AuthModal
            isOpen={authModalOpen}
            onClose={() => setAuthModalOpen(false)}
            onAuthSuccess={handleAuthSuccess}
          />
        </Suspense>
      )}
    </div>
    </MotionConfig>
  );
}
