import React from 'react';
import { BRAND_CONFIG } from '../data/smashData';
import { Flame, Activity, Terminal, Phone } from 'lucide-react';

export default function HudHeader({ onOpenAuth, currentUser, onLogout }) {
  return (
    <header className="fixed top-0 left-0 w-full z-50 bg-[#0a0a0a]/90 backdrop-blur-md border-b border-[#222222]">
      {/* Top telemetry ticker strip */}
      <div className="hidden sm:flex items-center justify-between px-6 py-1 bg-[#111111] border-b border-[#1f1f1f] text-[10px] font-mono-telemetry text-[#888888]">
        <div className="flex items-center gap-4">
          <span className="flex items-center gap-1.5 text-[#ff5500]">
            <span className="w-1.5 h-1.5 rounded-full bg-[#ff5500] animate-pulse" />
            LIVE TELEMETRY
          </span>
          <span>LOCATION: {BRAND_CONFIG.location}</span>
          <span className="hidden md:inline">FREQ: 60FPS WEBGL RENDER</span>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-[#ffaa00]">MAILLARD EQUATION: ACTIVE</span>
          <span>CORE TEMP: 230°C</span>
        </div>
      </div>

      {/* Main navigation row */}
      <div className="max-w-7xl mx-auto px-6 py-3 flex items-center justify-between">
        {/* Brand with Crosshairs */}
        <a href="#" className="flex items-center gap-3 group focus:outline-none">
          <div className="w-8 h-8 bg-[#161616] border border-[#ff5500]/60 flex items-center justify-center relative">
            <div className="absolute top-0 left-0 w-1.5 h-1.5 bg-[#ff5500]" />
            <div className="absolute bottom-0 right-0 w-1.5 h-1.5 bg-[#ff5500]" />
            <Flame className="w-4 h-4 text-[#ff5500]" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-condensed-heading text-lg sm:text-xl tracking-tight text-white leading-none">
                SMASH // THE SEAR
              </span>
              <span className="text-[9px] font-mono-telemetry bg-[#ff5500]/20 text-[#ffaa00] px-1.5 py-0.5 rounded-none border border-[#ff5500]/40">
                v2.6
              </span>
            </div>
            <p className="text-[9px] font-mono-telemetry text-[#666666] uppercase mt-0.5">
              ARTISANAL THERMAL LAB // BARRANQUILLA
            </p>
          </div>
        </a>

        {/* Links */}
        <nav className="hidden lg:flex items-center gap-6 font-mono-telemetry text-xs">
          <a href="#scrolly" className="text-[#888888] hover:text-[#ff5500] transition-colors">
            // 00. SCROLLYTELLING
          </a>
          <a href="#lineup" className="text-[#888888] hover:text-[#ff5500] transition-colors">
            // 01. THE LINE-UP
          </a>
          <a href="#builder" className="text-[#888888] hover:text-[#ff5500] transition-colors">
            // 02. BUILD YOUR SMASH
          </a>
          <a href="#specs" className="text-[#888888] hover:text-[#ff5500] transition-colors">
            // 03. SPECIFICATIONS
          </a>
        </nav>

        {/* Action Buttons */}
        <div className="flex items-center gap-3">
          {currentUser ? (
            <div className="flex items-center gap-2">
              <div className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 bg-[#161616] border border-[#ffaa00]/40 text-xs font-mono-telemetry text-[#ffaa00]">
                <span className="w-1.5 h-1.5 rounded-full bg-[#25D366] animate-pulse" />
                <span>{currentUser.phone || currentUser.name || 'COMENSAL'}</span>
              </div>
              <button
                type="button"
                onClick={onLogout}
                className="text-[11px] font-mono-telemetry text-[#888888] hover:text-[#ff5500] px-2 py-1 transition-colors"
                title="Cerrar sesión"
              >
                SALIR
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={onOpenAuth}
              className="btn-sear text-xs px-3.5 py-2 flex items-center gap-2 bg-[#ff5500]/15 border border-[#ff5500] text-white hover:bg-[#ff5500] hover:text-black transition-all cursor-pointer font-bold"
            >
              <Phone className="w-3.5 h-3.5 text-[#ffaa00]" />
              <span>INGRESAR // OTP</span>
            </button>
          )}

          <a
            href={`https://wa.me/${BRAND_CONFIG.whatsappNumber}?text=${encodeURIComponent('Hola Smash // The Sear, deseo ordenar una smash burger de alta precisión.')}`}
            target="_blank"
            rel="noopener noreferrer"
            className="btn-sear text-xs px-4 py-2 flex items-center gap-2"
          >
            <Terminal className="w-3.5 h-3.5" />
            <span>ORDER HUD</span>
          </a>
        </div>
      </div>
    </header>
  );
}
