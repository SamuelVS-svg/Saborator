import React from 'react';
import { motion } from 'framer-motion';
import SectionHeader from './SectionHeader';
import { STORY_DATA } from '../data/restaurantData';
import { Quote } from 'lucide-react';

export default function StorySection() {
  return (
    <section className="relative w-full py-24 sm:py-32 bg-[#3B0A14] text-[#F5EBDC] px-6">
      <div className="max-w-7xl mx-auto">
        <SectionHeader
          number="06"
          name="LA HISTORIA // EL MANIFIESTO"
          subtitle="Nuestra devoción por redefinir el estándar culinario urbano en la Costa Caribe."
          light={false}
        />

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
          {/* Visual Grid Column (6 cols) */}
          <div className="lg:col-span-6 grid grid-cols-2 gap-4">
            <motion.div
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.8 }}
              className="relative aspect-[3/4] overflow-hidden rounded-sm border border-[#F5EBDC]/20 shadow-xl group"
            >
              <img
                src="/img/hamburguesa.jpg"
                alt="Cocina en acción"
                className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105 filter brightness-90"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#3B0A14] via-transparent to-transparent opacity-60" />
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 50 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.8, delay: 0.15 }}
              className="relative aspect-[3/4] overflow-hidden rounded-sm border border-[#F5EBDC]/20 shadow-xl mt-8 group"
            >
              <img
                src="/img/salchipapa.jpg"
                alt="Detalle culinario"
                className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105 filter brightness-90"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#3B0A14] via-transparent to-transparent opacity-60" />
            </motion.div>
          </div>

          {/* Narrative & Timeline Column (6 cols) */}
          <div className="lg:col-span-6 flex flex-col gap-8">
            {/* Quote Block */}
            <div className="p-6 bg-[#6D1A2A]/40 border-l-2 border-[#E3CFB0] rounded-r-sm">
              <Quote className="w-6 h-6 text-[#E3CFB0] mb-3 opacity-60" />
              <p className="font-display text-base sm:text-lg italic tracking-wide text-[#F5EBDC] leading-snug">
                {STORY_DATA.quote}
              </p>
              <span className="block mt-3 text-xs font-mono uppercase tracking-widest text-[#E3CFB0]">
                — {STORY_DATA.author}
              </span>
            </div>

            {/* Paragraphs */}
            <div className="space-y-4 text-sm sm:text-base text-[#E3CFB0]/80 font-light leading-relaxed">
              {STORY_DATA.paragraphs.map((p, i) => (
                <p key={i}>{p}</p>
              ))}
            </div>

            {/* Timeline Milestones */}
            <div className="pt-4 border-t border-[#F5EBDC]/15 space-y-4">
              {STORY_DATA.milestones.map((m, idx) => (
                <div key={idx} className="flex items-start gap-4">
                  <span className="font-mono text-xs font-bold text-[#E3CFB0] py-0.5 px-2 bg-[#6D1A2A] rounded-sm">
                    {m.year}
                  </span>
                  <div>
                    <h4 className="font-display text-xs uppercase font-bold text-[#F5EBDC]">
                      {m.title}
                    </h4>
                    <p className="text-xs text-[#E3CFB0]/70 font-light mt-0.5">
                      {m.desc}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
