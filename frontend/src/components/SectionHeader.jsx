import React from 'react';
import { motion } from 'framer-motion';

export default function SectionHeader({ number, name, subtitle, light = false }) {
  return (
    <div className="w-full mb-12 sm:mb-16">
      {/* Top Tag & Animated Horizontal Line */}
      <div className="flex items-center gap-4 mb-4">
        <span
          className={`font-mono text-xs tracking-widest font-bold uppercase ${
            light ? 'text-[#6D1A2A]' : 'text-[#E3CFB0]'
          }`}
        >
          // {number}. {name}
        </span>
        <motion.div
          initial={{ scaleX: 0, originX: 0 }}
          whileInView={{ scaleX: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
          className={`flex-1 h-px ${light ? 'bg-[#6D1A2A]/25' : 'bg-[#F5EBDC]/25'}`}
        />
      </div>

      {/* Main Masked Reveal Title */}
      <div className="overflow-hidden">
        <motion.h2
          initial={{ y: 50, opacity: 0 }}
          whileInView={{ y: 0, opacity: 1 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8, ease: [0.16, 1, 0.3, 1] }}
          className={`font-display text-3xl sm:text-4xl md:text-5xl font-bold uppercase tracking-wider ${
            light ? 'text-[#3B0A14]' : 'text-[#F5EBDC]'
          }`}
        >
          {name}
        </motion.h2>
      </div>

      {subtitle && (
        <motion.p
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ delay: 0.2, duration: 0.8 }}
          className={`mt-2 text-sm sm:text-base max-w-2xl font-light ${
            light ? 'text-[#6D1A2A]/80' : 'text-[#E3CFB0]/90'
          }`}
        >
          {subtitle}
        </motion.p>
      )}
    </div>
  );
}
