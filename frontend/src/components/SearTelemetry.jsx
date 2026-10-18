import React from 'react';
import { motion } from 'framer-motion';
import { SEAR_TELEMETRY_STATS } from '../data/smashData';
import { Activity, Flame, ShieldAlert, Cpu } from 'lucide-react';

export default function SearTelemetry() {
  return (
    <section id="specs" className="relative w-full py-28 bg-[#0a0a0a] text-white px-6 border-t border-[#1f1f1f]">
      {/* Background embers video/image fallback with dark brutalist overlay */}
      <div className="absolute inset-0 z-0 overflow-hidden opacity-25">
        <img
          src="/img/hamburguesa.jpg"
          alt="Grill embers"
          className="w-full h-full object-cover filter grayscale contrast-200"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#0a0a0a] via-[#0a0a0a]/90 to-[#0a0a0a]" />
      </div>

      <div className="relative z-10 max-w-7xl mx-auto">
        {/* Section Header */}
        <div className="mb-16 pb-6 border-b border-[#222222] flex flex-col md:flex-row md:items-end justify-between">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono-telemetry text-[#ff5500] mb-2">
              <Cpu className="w-3.5 h-3.5 text-[#ff5500]" />
              <span>SPECIFICATIONS // 04. THE SEAR / THE CUT</span>
            </div>
            <h2 className="font-condensed-heading text-4xl sm:text-5xl lg:text-6xl text-white uppercase tracking-tight">
              THERMAL TELEMETRY BENCHMARK
            </h2>
          </div>
          <p className="text-xs font-mono-telemetry text-[#888888] max-w-sm mt-3 md:mt-0">
            Métricas cuantitativas que garantizan la costra Maillard sin sobrecocinar el núcleo jugoso.
          </p>
        </div>

        {/* 4 Quantitative Metrics Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {SEAR_TELEMETRY_STATS.map((stat, idx) => (
            <motion.div
              key={stat.id}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: idx * 0.12, duration: 0.6 }}
              className="bg-[#111111]/90 border border-[#262626] hover:border-[#ff5500] p-6 flex flex-col justify-between glow-amber relative group"
            >
              <div className="flex items-center justify-between text-[10px] font-mono-telemetry text-[#666666] mb-4 pb-2 border-b border-[#1f1f1f]">
                <span>SENSOR_NODE // 0{idx + 1}</span>
                <span className="text-[#ff5500] group-hover:animate-pulse">● LIVE</span>
              </div>

              <div>
                <div className="flex items-baseline gap-2 mb-1">
                  <span className="font-condensed-heading text-5xl sm:text-6xl text-white font-extrabold group-hover:text-[#ffaa00] transition-colors">
                    {stat.value}
                  </span>
                  <span className="font-mono-telemetry text-xs text-[#ff5500] font-bold">
                    {stat.unit}
                  </span>
                </div>

                <h3 className="text-xs font-mono-telemetry text-white font-bold uppercase tracking-wider mb-2">
                  {stat.label}
                </h3>

                <p className="text-[11px] text-[#777777] font-mono-telemetry leading-relaxed">
                  {stat.note}
                </p>
              </div>

              <div className="mt-6 pt-3 border-t border-[#1a1a1a] flex items-center justify-between text-[9px] font-mono-telemetry text-[#555555]">
                <span>TOLERANCE: ±0.2%</span>
                <span className="text-[#ffaa00]">OPTIMAL</span>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}
