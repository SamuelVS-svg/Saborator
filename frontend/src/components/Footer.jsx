import React from 'react';
import { RESTAURANT_INFO } from '../data/restaurantData';
import { MapPin, Clock, Phone, Flame, MessageCircle } from 'lucide-react';

export default function Footer() {
  return (
    <>
      <footer className="relative w-full bg-[#3B0A14] text-[#F5EBDC] pt-20 pb-12 border-t border-[#F5EBDC]/15 px-6">
        <div className="max-w-7xl mx-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10 mb-16">
          {/* Col 1: Brand Info */}
          <div>
            <div className="flex items-center gap-3 mb-4">
              <div className="w-8 h-8 rounded-full bg-[#6D1A2A] border border-[#F5EBDC]/30 flex items-center justify-center">
                <Flame className="w-4 h-4 text-[#F5EBDC]" />
              </div>
              <span className="font-display font-bold text-lg text-[#F5EBDC] tracking-widest leading-none">
                {RESTAURANT_INFO.name}
              </span>
            </div>
            <p className="text-xs text-[#E3CFB0]/80 font-light leading-relaxed mb-4">
              {RESTAURANT_INFO.tagline}
            </p>
            <span className="text-[10px] font-mono tracking-widest text-[#E3CFB0]/60 uppercase block">
              BARRANQUILLA // COLOMBIA
            </span>
          </div>

          {/* Col 2: Ubicación & Contacto */}
          <div>
            <span className="text-xs font-mono font-bold uppercase tracking-widest text-[#E3CFB0] block mb-4">
              // UBICACIÓN & CONTACTO
            </span>
            <div className="space-y-3 text-xs text-[#E3CFB0]/80 font-light">
              <div className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-[#E3CFB0] shrink-0 mt-0.5" />
                <span>{RESTAURANT_INFO.address}</span>
              </div>
              <div className="flex items-center gap-2.5">
                <Phone className="w-4 h-4 text-[#E3CFB0] shrink-0" />
                <span>{RESTAURANT_INFO.phone}</span>
              </div>
            </div>
          </div>

          {/* Col 3: Horarios de Fuego */}
          <div>
            <span className="text-xs font-mono font-bold uppercase tracking-widest text-[#E3CFB0] block mb-4">
              // HORARIOS DE SERVICIO
            </span>
            <div className="flex items-start gap-2.5 text-xs text-[#E3CFB0]/80 font-light">
              <Clock className="w-4 h-4 text-[#E3CFB0] shrink-0 mt-0.5" />
              <div>
                <p className="font-medium text-[#F5EBDC]">{RESTAURANT_INFO.hours}</p>
                <p className="text-[11px] text-[#E3CFB0]/60 mt-1">Lunes y Martes cerrado por mantenimiento de brasas.</p>
              </div>
            </div>
          </div>

          {/* Col 4: Redes & Experiencia */}
          <div>
            <span className="text-xs font-mono font-bold uppercase tracking-widest text-[#E3CFB0] block mb-4">
              // COMUNIDAD
            </span>
            <p className="text-xs text-[#E3CFB0]/80 font-light mb-4">
              Síguenos en nuestras transmisiones gastronómicas y recetas del chef.
            </p>
            <div className="flex items-center gap-3">
              <a
                href="https://instagram.com"
                target="_blank"
                rel="noopener noreferrer"
                className="p-2.5 rounded-sm bg-[#6D1A2A]/50 border border-[#F5EBDC]/20 hover:border-[#F5EBDC] hover:bg-[#F5EBDC] hover:text-[#3B0A14] transition-colors"
                aria-label="Instagram"
              >
                <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                  <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z" />
                </svg>
              </a>
              <a
                href={`https://wa.me/${RESTAURANT_INFO.whatsappNumber}`}
                target="_blank"
                rel="noopener noreferrer"
                className="p-2.5 rounded-sm bg-[#6D1A2A]/50 border border-[#F5EBDC]/20 hover:border-[#F5EBDC] hover:bg-[#F5EBDC] hover:text-[#3B0A14] transition-colors"
                aria-label="WhatsApp"
              >
                <MessageCircle className="w-4 h-4" />
              </a>
            </div>
          </div>
        </div>

        {/* Bottom copyright line */}
        <div className="max-w-7xl mx-auto pt-8 border-t border-[#F5EBDC]/10 flex flex-col sm:flex-row items-center justify-between text-[11px] text-[#E3CFB0]/60 font-mono">
          <span>© {new Date().getFullYear()} {RESTAURANT_INFO.name}. TODOS LOS DERECHOS RESERVADOS.</span>
          <span className="mt-2 sm:mt-0">DISEÑO CINEMATOGRÁFICO DE ALTA PRECISIÓN</span>
        </div>
      </footer>

      {/* Floating WhatsApp Action Button */}
      <aside aria-label="Contacto directo por WhatsApp">
        <a
          href={`https://wa.me/${RESTAURANT_INFO.whatsappNumber}?text=${encodeURIComponent('Hola Saborator, quiero información y ordenar.')}`}
          target="_blank"
          rel="noopener noreferrer"
          className="fixed bottom-6 right-6 z-40 p-4 rounded-full bg-[#F5EBDC] text-[#3B0A14] shadow-2xl hover:scale-110 active:scale-95 transition-transform duration-200 border border-[#E3CFB0] flex items-center gap-2 group focus:outline-none focus:ring-2 focus:ring-[#F5EBDC]"
          aria-label="Chatear por WhatsApp"
        >
          <MessageCircle className="w-6 h-6 fill-current" />
          <span className="hidden sm:inline font-display font-bold text-xs uppercase tracking-wider pr-1">
            PEDIR AHORA
          </span>
        </a>
      </aside>
    </>
  );
}
