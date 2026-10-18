import React, { useRef } from 'react';
import SectionHeader from './SectionHeader';
import { INGREDIENTS_GALLERY } from '../data/restaurantData';
import { ChevronLeft, ChevronRight, Sparkles } from 'lucide-react';

export default function IngredientsGallery() {
  const scrollRef = useRef(null);

  const scroll = (direction) => {
    if (scrollRef.current) {
      const { scrollLeft, clientWidth } = scrollRef.current;
      const scrollAmount = clientWidth * 0.75;
      scrollRef.current.scrollTo({
        left: direction === 'left' ? scrollLeft - scrollAmount : scrollLeft + scrollAmount,
        behavior: 'smooth',
      });
    }
  };

  return (
    <section id="ingredientes" className="relative w-full py-24 sm:py-32 bg-[#F5EBDC] text-[#3B0A14] px-6 overflow-hidden">
      <div className="max-w-7xl mx-auto">
        <div className="flex flex-col md:flex-row md:items-end justify-between mb-8">
          <SectionHeader
            number="05"
            name="GALERÍA DE INGREDIENTES // PUREZA & ORIGEN"
            subtitle="Desliza para descubrir los elementos que definen nuestro perfil de sabor inconfundible."
            light={true}
          />

          {/* Navigation Controls */}
          <div className="hidden md:flex items-center gap-3 mb-12">
            <button
              onClick={() => scroll('left')}
              className="p-3 rounded-full border border-[#3B0A14]/30 hover:bg-[#3B0A14] hover:text-[#F5EBDC] transition-colors"
              aria-label="Anterior"
            >
              <ChevronLeft className="w-5 h-5" />
            </button>
            <button
              onClick={() => scroll('right')}
              className="p-3 rounded-full border border-[#3B0A14]/30 hover:bg-[#3B0A14] hover:text-[#F5EBDC] transition-colors"
              aria-label="Siguiente"
            >
              <ChevronRight className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Horizontal Scroll Snap Track */}
        <div
          ref={scrollRef}
          className="flex gap-6 overflow-x-auto pb-8 pt-2 scrollbar-none snap-x snap-mandatory scroll-smooth"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {INGREDIENTS_GALLERY.map((item, idx) => (
            <div
              key={idx}
              className="min-w-[280px] sm:min-w-[340px] md:min-w-[380px] snap-center bg-white border border-[#3B0A14]/15 rounded-sm p-6 sm:p-8 flex flex-col justify-between hover:shadow-xl hover:-translate-y-1 transition-all duration-300 group"
            >
              <div>
                <div className="flex items-center justify-between mb-4">
                  <span className="text-[10px] font-mono tracking-widest uppercase bg-[#E3CFB0] text-[#3B0A14] px-2.5 py-1 rounded-sm font-semibold">
                    {item.accent}
                  </span>
                  <Sparkles className="w-4 h-4 text-[#6D1A2A] opacity-50 group-hover:opacity-100 transition-opacity" />
                </div>

                <h3 className="font-display text-xl sm:text-2xl font-bold uppercase tracking-wider text-[#3B0A14] mb-2 leading-tight">
                  {item.name}
                </h3>

                <span className="font-mono text-xs text-[#6D1A2A] block mb-4">
                  ORIGEN: {item.origin}
                </span>

                <p className="text-xs sm:text-sm text-[#3B0A14]/80 font-light leading-relaxed">
                  {item.desc}
                </p>
              </div>

              <div className="mt-8 pt-4 border-t border-[#3B0A14]/10 flex items-center justify-between">
                <span className="text-[10px] font-mono uppercase tracking-widest text-[#6D1A2A]">
                  0{idx + 1} / 0{INGREDIENTS_GALLERY.length}
                </span>
                <div className="w-1.5 h-1.5 rounded-full bg-[#3B0A14]" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
