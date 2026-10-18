import React from 'react';
import { BRAND_CONFIG } from '../data/smashData';
import { Terminal, MapPin, Phone, MessageSquare, Flame } from 'lucide-react';

export default function HudFooter() {
  return (
    <>
      <footer className="relative w-full bg-[#0a0a0a] text-white pt-20 pb-12 border-t border-[#1f1f1f] px-6">
        <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10 mb-16">
          {/* Col 1 */}
          <div>
            <div className="flex items-center gap-2 mb-4">
              <div className="w-7 h-7 bg-[#ff5500] text-black flex items-center justify-center font-extrabold text-xs">
                //
              </div>
              <span className="font-condensed-heading text-xl text-white tracking-tight">
                {BRAND_CONFIG.name}
              </span>
            </div>
            <p className="text-xs font-mono-telemetry text-[#777777] leading-relaxed mb-4">
              {BRAND_CONFIG.tagline}
            </p>
            <span className="text-[10px] font-mono-telemetry text-[#ff5500] bg-[#1a0c05] px-2 py-0.5 border border-[#ff5500]/30 inline-block">
              {BRAND_CONFIG.telemetryStatus}
            </span>
          </div>

          {/* Col 2 */}
          <div>
            <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-4">
              // TERMINAL LOCATION
            </span>
            <div className="space-y-3 text-xs font-mono-telemetry text-[#888888]">
              <div className="flex items-start gap-2">
                <MapPin className="w-4 h-4 text-[#ff5500] shrink-0 mt-0.5" />
                <span>{BRAND_CONFIG.address}</span>
              </div>
              <div className="flex items-center gap-2">
                <Phone className="w-4 h-4 text-[#ff5500] shrink-0" />
                <span>{BRAND_CONFIG.phone}</span>
              </div>
            </div>
          </div>

          {/* Col 3 */}
          <div>
            <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-4">
              // OPERATING WINDOWS
            </span>
            <div className="text-xs font-mono-telemetry text-[#888888] space-y-1">
              <p className="text-white">MIÉRCOLES A DOMINGO</p>
              <p>17:00 — 23:30 COT</p>
              <p className="text-[10px] text-[#555555] mt-2">LUNES/MARTES: CALIBRACIÓN TÉRMICA</p>
            </div>
          </div>

          {/* Col 4 */}
          <div>
            <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-4">
              // TELEMETRY PROTOCOL
            </span>
            <p className="text-xs font-mono-telemetry text-[#777777] mb-4">
              Despachos en caliente con tiempo máximo de entrega inferior a 25 minutos.
            </p>
            <a
              href={`https://wa.me/${BRAND_CONFIG.whatsappNumber}?text=${encodeURIComponent('Hola Smash // The Sear, quiero consultar cobertura y menú en vivo.')}`}
              target="_blank"
              rel="noopener noreferrer"
              className="btn-sear-outline text-xs px-4 py-2 inline-flex items-center gap-2"
            >
              <MessageSquare className="w-3.5 h-3.5" />
              <span>WHATSAPP DISPATCH</span>
            </a>
          </div>
        </div>

        {/* Bottom strip */}
        <div className="max-w-7xl mx-auto pt-8 border-t border-[#1a1a1a] flex flex-col sm:flex-row items-center justify-between text-[10px] font-mono-telemetry text-[#555555]">
          <span>© {new Date().getFullYear()} {BRAND_CONFIG.name}. ALL TELEMETRY RIGHTS RESERVED.</span>
          <span className="text-[#ff5500] mt-2 sm:mt-0">WEBGL 3D SCROLLYTELLING ARCHITECTURE</span>
        </div>
      </footer>

      {/* Floating Action Button */}
      <a
        href={`https://wa.me/${BRAND_CONFIG.whatsappNumber}?text=${encodeURIComponent('Hola Smash // The Sear, deseo pedir ahora.')}`}
        target="_blank"
        rel="noopener noreferrer"
        className="fixed bottom-6 right-6 z-50 p-3.5 bg-[#ff5500] text-black shadow-[0_0_25px_rgba(255,85,0,0.6)] hover:bg-[#ffaa00] transition-colors flex items-center gap-2 group"
      >
        <Flame className="w-5 h-5 fill-current" />
        <span className="font-mono-telemetry font-bold text-xs uppercase pr-1 hidden sm:inline">
          ORDER SMASH
        </span>
      </a>
    </>
  );
}
