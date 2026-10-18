import React from 'react';
import { motion } from 'framer-motion';
import SectionHeader from './SectionHeader';
import { STATS_DATA } from '../data/restaurantData';

export default function StatsSection() {
  const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
      opacity: 1,
      transition: {
        staggerChildren: 0.15,
      },
    },
  };

  const statVariants = {
    hidden: { opacity: 0, y: 30 },
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.8,
        ease: [0.16, 1, 0.3, 1],
      },
    },
  };

  return (
    <section className="relative w-full py-20 bg-[#3B0A14] text-[#F5EBDC] border-y border-[#F5EBDC]/15 px-6">
      <div className="max-w-7xl mx-auto">
        <div className="text-center max-w-xl mx-auto mb-14">
          <span className="font-mono text-xs uppercase tracking-widest text-[#E3CFB0]">
            // 03. MÉTRICAS DE RIGOR
          </span>
          <h2 className="font-display text-2xl sm:text-3xl font-bold uppercase tracking-wider text-[#F5EBDC] mt-2">
            PRECISIÓN MILIMÉTRICA EN CADA PLATO
          </h2>
        </div>

        {/* 4 Stats Grid */}
        <motion.div
          variants={containerVariants}
          initial="hidden"
          whileInView="visible"
          viewport={{ once: true, margin: '-50px' }}
          className="grid grid-cols-2 lg:grid-cols-4 gap-8 divide-y lg:divide-y-0 lg:divide-x divide-[#F5EBDC]/15"
        >
          {STATS_DATA.map((stat, i) => (
            <motion.div
              key={i}
              variants={statVariants}
              className={`flex flex-col items-center text-center p-4 ${i > 1 ? 'pt-8 lg:pt-4' : ''}`}
            >
              <span className="font-display text-4xl sm:text-5xl lg:text-6xl font-extrabold text-[#F5EBDC] tracking-tight mb-2">
                {stat.value}
              </span>
              <span className="font-mono text-xs sm:text-sm font-semibold tracking-wider text-[#E3CFB0] uppercase mb-1">
                {stat.label}
              </span>
              <span className="text-[11px] sm:text-xs text-[#E3CFB0]/70 font-light max-w-[200px]">
                {stat.detail}
              </span>
            </motion.div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
