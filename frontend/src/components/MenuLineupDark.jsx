import React from 'react';
import { motion } from 'framer-motion';
import { LINEUP_BURGER_PRODUCTS, BRAND_CONFIG } from '../data/smashData';
import { Plus, Flame, Sparkles, Terminal } from 'lucide-react';

export default function MenuLineupDark() {
  return (
    <section id="lineup" className="relative w-full py-28 bg-[#0a0a0a] text-white px-6 border-t border-[#1a1a1a]">
      <div className="max-w-7xl mx-auto">
        {/* Section Header HUD */}
        <div className="flex flex-col md:flex-row md:items-end justify-between mb-16 pb-6 border-b border-[#222222]">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono-telemetry text-[#ff5500] mb-2">
              <span className="w-2 h-2 bg-[#ff5500]" />
              <span>SPECIFICATIONS // 01. THE LINE-UP</span>
            </div>
            <h2 className="font-condensed-heading text-4xl sm:text-5xl lg:text-6xl text-white uppercase tracking-tight">
              ENGINEERED SMASH BURGERS
            </h2>
          </div>
          <p className="mt-4 md:mt-0 max-w-md text-xs sm:text-sm text-[#777777] font-mono-telemetry leading-relaxed">
            Cada referencia es prensada al segundo 7 con sellado de alta costra y perfiles de sabor calibrados en laboratorio.
          </p>
        </div>

        {/* 4 Terminal Product Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {LINEUP_BURGER_PRODUCTS.map((burger) => (
            <motion.div
              key={burger.id}
              whileHover={{ y: -6 }}
              transition={{ duration: 0.2 }}
              className="bg-[#111111] border border-[#222222] hover:border-[#ff5500] p-5 flex flex-col justify-between group transition-all duration-300 relative glow-amber hover:shadow-[0_0_30px_rgba(255,85,0,0.25)]"
            >
              {/* Corner crosshairs */}
              <div className="absolute top-0 left-0 w-2 h-2 border-t-2 border-l-2 border-[#ff5500] opacity-0 group-hover:opacity-100 transition-opacity" />
              <div className="absolute top-0 right-0 w-2 h-2 border-t-2 border-r-2 border-[#ff5500] opacity-0 group-hover:opacity-100 transition-opacity" />
              <div className="absolute bottom-0 left-0 w-2 h-2 border-b-2 border-l-2 border-[#ff5500] opacity-0 group-hover:opacity-100 transition-opacity" />
              <div className="absolute bottom-0 right-0 w-2 h-2 border-b-2 border-r-2 border-[#ff5500] opacity-0 group-hover:opacity-100 transition-opacity" />

              <div>
                {/* Header Tag */}
                <div className="flex items-center justify-between text-[10px] font-mono-telemetry mb-4 pb-2 border-b border-[#1f1f1f]">
                  <span className="text-[#888888]">{burger.code}</span>
                  <span className="text-[#ffaa00] bg-[#1f1200] px-2 py-0.5 border border-[#ffaa00]/30 font-bold">
                    {burger.badge}
                  </span>
                </div>

                {/* Product Image with Amber Hover Glow */}
                <div className="relative aspect-square w-full overflow-hidden bg-[#0d0d0d] mb-5 border border-[#1f1f1f] group-hover:border-[#ff5500]/50 transition-colors">
                  <img
                    src={burger.image}
                    alt={burger.name}
                    className="w-full h-full object-cover filter brightness-90 contrast-110 group-hover:scale-108 group-hover:brightness-100 transition-all duration-500"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-[#0d0d0d] via-transparent to-transparent opacity-80" />
                </div>

                {/* Name & Specs */}
                <h3 className="font-condensed-heading text-xl text-white uppercase group-hover:text-[#ffaa00] transition-colors leading-tight mb-2">
                  {burger.name}
                </h3>

                <p className="text-xs text-[#888888] font-light leading-relaxed mb-4">
                  {burger.description}
                </p>

                {/* Specs Badges */}
                <div className="flex flex-wrap gap-1 mb-6">
                  {burger.specs.map((spec, i) => (
                    <span
                      key={i}
                      className="text-[9px] font-mono-telemetry bg-[#161616] text-[#aaaaaa] px-1.5 py-0.5 border border-[#262626]"
                    >
                      {spec}
                    </span>
                  ))}
                </div>
              </div>

              {/* Price & Action */}
              <div className="pt-4 border-t border-[#1f1f1f] flex items-center justify-between">
                <div>
                  <span className="block text-[9px] font-mono-telemetry text-[#666666]">
                    PRICE TAG
                  </span>
                  <span className="font-mono-telemetry text-lg font-bold text-white">
                    {burger.formattedPrice}
                  </span>
                </div>

                <a
                  href={`https://wa.me/${BRAND_CONFIG.whatsappNumber}?text=${encodeURIComponent(
                    `Hola Smash // The Sear. Quiero ordenar: ${burger.name} (${burger.formattedPrice}).`
                  )}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="btn-sear text-xs px-3 py-2 flex items-center gap-1.5"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>ADD TO ORDER</span>
                </a>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}
