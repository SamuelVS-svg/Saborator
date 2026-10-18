import React, { useEffect, useState } from 'react';
import { motion, useScroll, useTransform } from 'framer-motion';
import { RESTAURANT_INFO } from '../data/restaurantData';
import { ChevronDown, Sparkles, Clock, Flame } from 'lucide-react';

export default function HeroSection() {
  const { scrollY } = useScroll();
  // Live dynamic counter that changes with scroll (e.g. grill temperature from 200°C to 285°C, or prep time)
  const [scrollMetric, setScrollMetric] = useState(200);

  useEffect(() => {
    const unsubscribe = scrollY.on('change', (latest) => {
      // Map scroll 0 -> 600px to temperature 200°C -> 285°C
      const temp = Math.min(285, Math.floor(200 + (latest / 600) * 85));
      setScrollMetric(temp);
    });
    return () => unsubscribe();
  }, [scrollY]);

  // Cinematic headline letter-by-letter reveal
  const headline = "ALTA COCINA & FUEGO";
  const subheadline = "BARRANQUILLA // EXPERIENCIA GASTRONÓMICA CONTEMPORÁNEA";

  const letterVariants = {
    hidden: { opacity: 0, y: 35 },
    visible: (i) => ({
      opacity: 1,
      y: 0,
      transition: {
        delay: i * 0.045,
        duration: 0.7,
        ease: [0.16, 1, 0.3, 1],
      },
    }),
  };

  return (
    <section className="relative w-full min-h-screen flex items-center justify-center overflow-hidden bg-[#3B0A14] text-[#F5EBDC]">
      {/* Background Video / Fallback Poster Placeholder */}
      <div className="absolute inset-0 z-0 overflow-hidden">
        {/* Placeholder Video: Replace with real /videos/hero.mp4 */}
        <video
          autoPlay
          loop
          muted
          playsInline
          poster="/img/hamburguesa.jpg"
          className="w-full h-full object-cover scale-105 filter brightness-75 contrast-110"
        >
          <source src={RESTAURANT_INFO.videoHeroPlaceholder} type="video/mp4" />
          {/* Fallback image is provided by poster attribute */}
        </video>

        {/* Mandatory Burgundy Gradient Overlay: Dark burgundy to transparent (never black) */}
        <div className="absolute inset-0 bg-gradient-to-t from-[#3B0A14] via-[#3B0A14]/75 to-transparent z-10" />
        <div className="absolute inset-0 bg-gradient-to-r from-[#3B0A14]/90 via-transparent to-[#3B0A14]/80 z-10" />
      </div>

      {/* Content Container */}
      <div className="relative z-20 max-w-6xl mx-auto px-6 pt-24 pb-16 text-center flex flex-col items-center">
        {/* Top Tagline Pill */}
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, ease: 'easeOut' }}
          className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full border border-[#F5EBDC]/20 bg-[#6D1A2A]/40 backdrop-blur-md mb-6"
        >
          <Sparkles className="w-3.5 h-3.5 text-[#E3CFB0]" />
          <span className="text-[11px] font-mono tracking-widest uppercase text-[#E3CFB0]">
            EST. 2024 // {RESTAURANT_INFO.city}
          </span>
        </motion.div>

        {/* Letter-by-letter headline reveal */}
        <h1 className="font-display text-4xl sm:text-6xl md:text-7xl lg:text-8xl font-extrabold uppercase tracking-widest text-[#F5EBDC] leading-none mb-6 flex flex-wrap justify-center overflow-hidden">
          {headline.split('').map((char, index) => (
            <motion.span
              key={index}
              custom={index}
              variants={letterVariants}
              initial="hidden"
              animate="visible"
              className="inline-block"
            >
              {char === ' ' ? '\u00A0' : char}
            </motion.span>
          ))}
        </h1>

        {/* Subtitle */}
        <motion.p
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.8, duration: 0.8 }}
          className="text-sm md:text-lg text-[#E3CFB0] tracking-widest uppercase max-w-2xl font-light mb-10 leading-relaxed"
        >
          {subheadline}
        </motion.p>

        {/* Call to Actions */}
        <motion.div
          initial={{ opacity: 0, y: 25 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 1, duration: 0.8 }}
          className="flex flex-col sm:flex-row items-center gap-4 w-full sm:w-auto"
        >
          <a
            href="#lineup"
            className="btn-primary w-full sm:w-auto px-8 py-4 rounded-sm text-xs text-center"
          >
            EXPLORAR THE LINE-UP
          </a>
          <a
            href="#builder"
            className="btn-outline w-full sm:w-auto px-8 py-4 rounded-sm text-xs text-center"
          >
            ARMAR PEDIDO PROPIO
          </a>
        </motion.div>

        {/* Live Scroll Counter Widget (e.g. Grill Temp / Prep speed) */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 1.2, duration: 1 }}
          className="mt-14 inline-flex items-center gap-6 px-6 py-3 rounded-sm bg-[#3B0A14]/80 border border-[#F5EBDC]/15 backdrop-blur-md"
        >
          <div className="flex items-center gap-2">
            <Flame className="w-4 h-4 text-[#E3CFB0] animate-pulse" />
            <div className="text-left">
              <span className="block text-[10px] uppercase tracking-widest text-[#E3CFB0]/70">
                Punto de Fuego Activo
              </span>
              <span className="font-mono font-bold text-base text-[#F5EBDC]">
                {scrollMetric}°C <span className="text-[10px] font-normal text-[#E3CFB0]">SEAL TEMP</span>
              </span>
            </div>
          </div>

          <div className="w-px h-8 bg-[#F5EBDC]/15" />

          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 text-[#E3CFB0]" />
            <div className="text-left">
              <span className="block text-[10px] uppercase tracking-widest text-[#E3CFB0]/70">
                Tiempo al Plato
              </span>
              <span className="font-mono font-bold text-base text-[#F5EBDC]">
                8-12 MIN
              </span>
            </div>
          </div>
        </motion.div>
      </div>

      {/* Animated Scroll Down Indicator */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 1.4, duration: 1 }}
        className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 flex flex-col items-center gap-2 pointer-events-none"
      >
        <span className="text-[10px] uppercase tracking-widest text-[#E3CFB0]/80 font-mono">
          SCROLL TO EXPLORE
        </span>
        <motion.div
          animate={{ y: [0, 8, 0] }}
          transition={{ repeat: Infinity, duration: 1.8, ease: 'easeInOut' }}
        >
          <ChevronDown className="w-5 h-5 text-[#E3CFB0]" />
        </motion.div>
      </motion.div>
    </section>
  );
}
