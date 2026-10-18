import React, { useState, useEffect } from 'react';
import HudHeader from './components/HudHeader';
import ScrollytellingCanvas from './components/ScrollytellingCanvas';
import MenuLineupDark from './components/MenuLineupDark';
import SmashBuilder from './components/SmashBuilder';
import SearTelemetry from './components/SearTelemetry';
import HudFooter from './components/HudFooter';
import AuthModal from './components/AuthModal';

export default function App() {
  const [authModalOpen, setAuthModalOpen] = useState(false);
  const [currentUser, setCurrentUser] = useState(null);

  // Cargar usuario autenticado persistido
  useEffect(() => {
    try {
      const stored = localStorage.getItem('saborator_user');
      if (stored) {
        setCurrentUser(JSON.parse(stored));
      }
    } catch (e) {
      console.warn('Error al leer sesión local:', e);
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('saborator_token');
    localStorage.removeItem('saborator_user');
    setCurrentUser(null);
  };

  const handleAuthSuccess = (user) => {
    setCurrentUser(user);
  };

  return (
    <div className="w-full min-h-screen bg-[#0a0a0a] text-white selection:bg-[#ff5500] selection:text-black relative">
      {/* HUD Telemetry Top Header con botón de Auth OTP */}
      <HudHeader 
        onOpenAuth={() => setAuthModalOpen(true)}
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
      <AuthModal 
        isOpen={authModalOpen}
        onClose={() => setAuthModalOpen(false)}
        onAuthSuccess={handleAuthSuccess}
      />
    </div>
  );
}
