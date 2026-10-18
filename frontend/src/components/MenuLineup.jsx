import React from 'react';
import { motion } from 'framer-motion';
import SectionHeader from './SectionHeader';
import { MENU_LINEUP, RESTAURANT_INFO } from '../data/restaurantData';
import { ArrowUpRight, Flame } from 'lucide-react';

export default function MenuLineup() {
  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.18,
      },
    },
  };

  const cardVariants = {
    hidden: { opacity: 0, y: 40 },
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.7,
        ease: [0.16, 1, 0.3, 1],
      },
    },
  };

  return (
    <section id="lineup" className="relative w-full py-24 sm:py-32 bg-[#3B0A14] text-[#F5EBDC] px-6">
      <div className="max-w-7xl mx-auto">
        <SectionHeader
          number="01"
          name="THE LINE-UP // PLATOS DE AUTOR"
          subtitle="Una cuidada selección de platos diseñados con rigor culinario, cortes selectos y salsas reducidas artesanalmente."
          light={false}
        />

        {/* Staggered Grid of 4 Cards */}
        <motion.div
          variants={containerVariants}
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true, margin: '-50px' }}
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 sm:gap-8"
        >
          {MENU_LINEUP.map((item) => (
            <motion.article
              key={item.id}
              variants={cardVariants}
              whileHover={{ y: -8 }}
              className="group relative flex flex-col bg-[#6D1A2A]/40 border border-[#F5EBDC]/20 hover:border-[#F5EBDC] rounded-sm overflow-hidden transition-all duration-300 shadow-xl"
            >
              {/* Image Container with subtle zoom & Burgundy Gradient Overlay */}
              <div className="relative aspect-[4/3] w-full overflow-hidden bg-[#3B0A14]">
                <img
                  src={item.image}
                  alt={item.name}
                  loading="lazy"
                  className="w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-108 filter brightness-90 group-hover:brightness-100"
                />
                {/* Gradient overlay from dark burgundy to transparent */}
                <div className="absolute inset-0 bg-gradient-to-t from-[#3B0A14] via-transparent to-transparent opacity-80" />

                {/* Badge Tag */}
                <div className="absolute top-3 left-3 bg-[#3B0A14]/90 border border-[#F5EBDC]/20 px-2.5 py-1 rounded-sm text-[10px] font-mono tracking-widest text-[#E3CFB0] uppercase">
                  {item.tag}
                </div>

                {/* Number index */}
                <div className="absolute top-3 right-3 text-[#E3CFB0]/40 font-mono text-xs font-bold">
                  {item.number}
                </div>
              </div>

              {/* Content Body */}
              <div className="p-5 flex-1 flex flex-col justify-between">
                <div>
                  <h3 className="font-display text-lg font-bold uppercase tracking-wider text-[#F5EBDC] group-hover:text-[#E3CFB0] transition-colors leading-snug">
                    {item.name}
                  </h3>

                  <p className="mt-3 text-xs text-[#E3CFB0]/80 leading-relaxed font-light line-clamp-3">
                    {item.description}
                  </p>

                  {/* Specs / Tags */}
                  <div className="mt-4 flex flex-wrap gap-1.5">
                    {item.specs.map((spec, i) => (
                      <span
                        key={i}
                        className="text-[10px] bg-[#3B0A14]/60 border border-[#F5EBDC]/10 text-[#E3CFB0] px-2 py-0.5 rounded-sm font-mono"
                      >
                        {spec}
                      </span>
                    ))}
                  </div>
                </div>

                {/* Price and Action */}
                <div className="mt-6 pt-4 border-t border-[#F5EBDC]/15 flex items-center justify-between">
                  <div>
                    <span className="block text-[9px] uppercase tracking-wider text-[#E3CFB0]/70 font-mono">
                      VALOR CARTA
                    </span>
                    <span className="font-mono text-base font-bold text-[#F5EBDC]">
                      {item.formattedPrice}
                    </span>
                  </div>

                  <a
                    href={`https://wa.me/${RESTAURANT_INFO.whatsappNumber}?text=${encodeURIComponent(
                      `Hola Saborator, deseo pedir el plato: ${item.name} (${item.formattedPrice}).`
                    )}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="p-2.5 rounded-sm bg-[#F5EBDC] text-[#3B0A14] hover:bg-[#3B0A14] hover:text-[#F5EBDC] hover:border hover:border-[#F5EBDC] transition-colors duration-200"
                    aria-label={`Pedir ${item.name}`}
                  >
                    <ArrowUpRight className="w-4 h-4" />
                  </a>
                </div>
              </div>
            </motion.article>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
