import React, { useState, useEffect } from 'react';
import { RESTAURANT_INFO } from '../data/restaurantData';
import { Menu, X, Flame, Phone } from 'lucide-react';

export default function Navbar() {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 40);
    };
    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const navLinks = [
    { label: '01. The Line-up', href: '#lineup' },
    { label: '02. Constructor', href: '#builder' },
    { label: '03. Proceso', href: '#proceso' },
    { label: '04. Ingredientes', href: '#ingredientes' },
    { label: '05. Reservas', href: '#reserva' },
  ];

  return (
    <header
      className={`fixed top-0 left-0 w-full z-50 transition-all duration-300 ${
        scrolled
          ? 'bg-[#3B0A14]/90 backdrop-blur-md border-b border-[#F5EBDC]/10 py-3 shadow-lg'
          : 'bg-transparent py-5'
      }`}
    >
      <div className="max-w-7xl mx-auto px-6 flex items-center justify-between">
        {/* Brand / Logo */}
        <a href="#" className="flex items-center gap-3 group focus:outline-none focus:ring-2 focus:ring-[#F5EBDC]">
          <div className="w-9 h-9 rounded-full bg-[#6D1A2A] border border-[#F5EBDC]/30 flex items-center justify-center transition-transform duration-300 group-hover:scale-105">
            <Flame className="w-5 h-5 text-[#F5EBDC]" />
          </div>
          <div className="flex flex-col text-left">
            <span className="font-display font-bold text-lg md:text-xl text-[#F5EBDC] tracking-widest leading-none">
              {RESTAURANT_INFO.name}
            </span>
            <span className="text-[9px] text-[#E3CFB0] tracking-widest uppercase mt-0.5 font-medium">
              BAQ // ALTA COCINA
            </span>
          </div>
        </a>

        {/* Desktop Navigation */}
        <nav className="hidden md:flex items-center gap-8">
          {navLinks.map((link) => (
            <a
              key={link.label}
              href={link.href}
              className="text-xs uppercase tracking-widest text-[#F5EBDC]/80 hover:text-[#F5EBDC] transition-colors relative py-1 group font-medium"
            >
              {link.label}
              <span className="absolute bottom-0 left-0 w-0 h-[2px] bg-[#F5EBDC] transition-all duration-300 group-hover:w-full" />
            </a>
          ))}
        </nav>

        {/* Action Button */}
        <div className="hidden md:flex items-center gap-4">
          <a
            href={`https://wa.me/${RESTAURANT_INFO.whatsappNumber}`}
            target="_blank"
            rel="noopener noreferrer"
            className="btn-primary text-xs px-5 py-2.5 rounded-sm inline-flex items-center gap-2"
          >
            <Phone className="w-3.5 h-3.5" />
            <span>PEDIR AHORA</span>
          </a>
        </div>

        {/* Mobile menu trigger */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className="md:hidden p-2 text-[#F5EBDC] hover:text-[#E3CFB0] focus:outline-none focus:ring-2 focus:ring-[#F5EBDC]"
          aria-label="Abrir menú"
        >
          {menuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
        </button>
      </div>

      {/* Mobile Drawer */}
      {menuOpen && (
        <div className="md:hidden bg-[#3B0A14] border-b border-[#F5EBDC]/15 px-6 py-6 flex flex-col gap-4 animate-in fade-in slide-in-from-top duration-200">
          {navLinks.map((link) => (
            <a
              key={link.label}
              href={link.href}
              onClick={() => setMenuOpen(false)}
              className="text-sm uppercase tracking-wider text-[#F5EBDC] py-2 border-b border-[#F5EBDC]/10 font-medium"
            >
              {link.label}
            </a>
          ))}
          <a
            href={`https://wa.me/${RESTAURANT_INFO.whatsappNumber}`}
            target="_blank"
            rel="noopener noreferrer"
            onClick={() => setMenuOpen(false)}
            className="btn-primary text-xs px-5 py-3 rounded-sm text-center mt-2 flex items-center justify-center gap-2"
          >
            <Phone className="w-4 h-4" />
            <span>PEDIR POR WHATSAPP</span>
          </a>
        </div>
      )}
    </header>
  );
}
