import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { BUILDER_CUSTOM_SPECS, BRAND_CONFIG } from '../data/smashData';
import { Check, Flame, Send, Layers, RefreshCw } from 'lucide-react';

export default function SmashBuilder() {
  const [patty, setPatty] = useState(BUILDER_CUSTOM_SPECS.patties[1]); // Default Double
  const [bun, setBun] = useState(BUILDER_CUSTOM_SPECS.buns[0]);
  const [cheese, setCheese] = useState(BUILDER_CUSTOM_SPECS.cheeses[0]);
  const [toppings, setToppings] = useState([BUILDER_CUSTOM_SPECS.toppings[0], BUILDER_CUSTOM_SPECS.toppings[1]]);
  const [sauce, setSauce] = useState(BUILDER_CUSTOM_SPECS.sauces[0]);
  const [beverage, setBeverage] = useState(BUILDER_CUSTOM_SPECS.beverages[0]);

  // Toggle topping
  const toggleTopping = (item) => {
    if (toppings.some((t) => t.id === item.id)) {
      setToppings(toppings.filter((t) => t.id !== item.id));
    } else {
      setToppings([...toppings, item]);
    }
  };

  // Calculate live total price
  const totalPrice =
    patty.price +
    bun.price +
    cheese.price +
    toppings.reduce((acc, t) => acc + t.price, 0) +
    sauce.price +
    (beverage ? beverage.price : 0);

  // Format currency
  const formatMoney = (val) => `$${val.toLocaleString('es-CO')} COP`;

  // Dispatch order to WhatsApp
  const handleOrder = () => {
    const toppingsStr = toppings.length > 0 ? toppings.map((t) => t.name).join(', ') : 'Sin toppings adicionales';
    const msg = `⚡ *ORDEN PERSONALIZADA // SMASH THE SEAR*
🥩 *Carne:* ${patty.name} ($${patty.price.toLocaleString('es-CO')})
🥖 *Pan:* ${bun.name} ($${bun.price.toLocaleString('es-CO')})
🧀 *Queso:* ${cheese.name} ($${cheese.price.toLocaleString('es-CO')})
🥓 *Toppings:* ${toppingsStr}
🥫 *Salsa:* ${sauce.name} ($${sauce.price.toLocaleString('es-CO')})
🥤 *Bebida:* ${beverage ? beverage.name : 'Ninguna'}

💰 *TOTAL FINAL:* ${formatMoney(totalPrice)}
📍 Entrega en: Barranquilla Core`;

    window.open(`https://wa.me/${BRAND_CONFIG.whatsappNumber}?text=${encodeURIComponent(msg)}`, '_blank');
  };

  return (
    <section id="builder" className="relative w-full py-28 bg-[#0d0d0d] text-white px-6 border-t border-[#1f1f1f] grid-pattern">
      <div className="max-w-7xl mx-auto">
        {/* Section Header */}
        <div className="mb-14 pb-6 border-b border-[#222222] flex flex-col md:flex-row md:items-end justify-between">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono-telemetry text-[#ff5500] mb-2">
              <span className="w-2 h-2 bg-[#ff5500]" />
              <span>CUSTOMIZER // 02. BUILD YOUR SMASH</span>
            </div>
            <h2 className="font-condensed-heading text-4xl sm:text-5xl text-white uppercase tracking-tight">
              INTERACTIVE BURGER LAB
            </h2>
          </div>
          <p className="text-xs font-mono-telemetry text-[#777777] mt-2 md:mt-0">
            TOTAL CALIBRATION // 100% PERSONALIZADA
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Options Matrix (8 cols) */}
          <div className="lg:col-span-8 flex flex-col gap-8">
            {/* 1. Patties */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [01] SELECCIÓN DE SMASH PATTIES (102G / DISCO)
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_CUSTOM_SPECS.patties.map((p) => {
                  const isSelected = patty.id === p.id;
                  return (
                    <button
                      key={p.id}
                      onClick={() => setPatty(p)}
                      className={`text-left p-4 border transition-all text-xs font-mono-telemetry flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span className="font-bold text-white uppercase">{p.name}</span>
                        {isSelected && <Check className="w-4 h-4 text-[#ff5500]" />}
                      </div>
                      <span className="text-[#ffaa00]">+{formatMoney(p.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 2. Bun */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [02] BASE Y PAN TOSTADO
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_CUSTOM_SPECS.buns.map((b) => {
                  const isSelected = bun.id === b.id;
                  return (
                    <button
                      key={b.id}
                      onClick={() => setBun(b)}
                      className={`text-left p-4 border transition-all text-xs font-mono-telemetry flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span className="font-bold text-white uppercase">{b.name}</span>
                        {isSelected && <Check className="w-4 h-4 text-[#ff5500]" />}
                      </div>
                      <span className="text-[#ffaa00]">+{formatMoney(b.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 3. Cheese */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [03] LÁMINAS DE QUESO FUNDIDO
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_CUSTOM_SPECS.cheeses.map((c) => {
                  const isSelected = cheese.id === c.id;
                  return (
                    <button
                      key={c.id}
                      onClick={() => setCheese(c)}
                      className={`text-left p-4 border transition-all text-xs font-mono-telemetry flex items-center justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <span className="font-bold text-white uppercase">{c.name}</span>
                      <span className="text-[#ffaa00]">+{formatMoney(c.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 4. Toppings */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [04] TOPPINGS & CRUNCH MULTI-SELECT
              </span>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {BUILDER_CUSTOM_SPECS.toppings.map((t) => {
                  const isSelected = toppings.some((item) => item.id === t.id);
                  return (
                    <button
                      key={t.id}
                      onClick={() => toggleTopping(t)}
                      className={`text-left p-3 border transition-all text-xs font-mono-telemetry flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span className="font-semibold text-white">{t.name}</span>
                        {isSelected && <Check className="w-3.5 h-3.5 text-[#ff5500]" />}
                      </div>
                      <span className="text-[#ffaa00]">+{formatMoney(t.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 5. Sauces */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [05] SALSA DE LABORATORIO
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_CUSTOM_SPECS.sauces.map((s) => {
                  const isSelected = sauce.id === s.id;
                  return (
                    <button
                      key={s.id}
                      onClick={() => setSauce(s)}
                      className={`text-left p-4 border transition-all text-xs font-mono-telemetry flex items-center justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <span className="font-bold text-white uppercase">{s.name}</span>
                      <span className="text-[#ffaa00]">+{formatMoney(s.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* 6. Drinks */}
            <div>
              <span className="text-xs font-mono-telemetry text-[#ffaa00] uppercase block mb-3">
                [06] BEBIDA CRIOGÉNICA
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_CUSTOM_SPECS.beverages.map((b) => {
                  const isSelected = beverage?.id === b.id;
                  return (
                    <button
                      key={b.id}
                      onClick={() => setBeverage(isSelected ? null : b)}
                      className={`text-left p-3 border transition-all text-xs font-mono-telemetry flex items-center justify-between ${
                        isSelected
                          ? 'border-[#ff5500] bg-[#1a0c05] text-white ring-1 ring-[#ff5500]'
                          : 'border-[#222222] bg-[#141414] text-[#888888] hover:border-[#444444]'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <img src={b.image} alt={b.name} className="w-6 h-6 object-contain" />
                        <span className="text-white font-medium">{b.name}</span>
                      </div>
                      <span className="text-[#ffaa00]">+{formatMoney(b.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Live HUD Visual Preview + Price Trigger (4 cols sticky) */}
          <div className="lg:col-span-4 lg:sticky lg:top-24">
            <div className="bg-[#111111] border border-[#ff5500]/50 p-6 glow-amber relative">
              <div className="flex items-center justify-between pb-4 border-b border-[#222222] mb-6">
                <div className="flex items-center gap-2">
                  <Flame className="w-4 h-4 text-[#ff5500] animate-pulse" />
                  <span className="text-xs font-mono-telemetry uppercase text-white font-bold">
                    VISUAL HUD PREVIEW
                  </span>
                </div>
                <span className="text-[10px] font-mono-telemetry text-[#ffaa00] bg-[#1f1000] px-2 py-0.5 border border-[#ffaa00]/30">
                  LIVE VAPOR
                </span>
              </div>

              {/* Graphic Layered Visual representation of the burger */}
              <div className="relative h-44 bg-[#0a0a0a] border border-[#1f1f1f] flex flex-col items-center justify-center overflow-hidden mb-6">
                {/* Background steam glow */}
                <div className="absolute w-32 h-32 rounded-full bg-[#ff5500]/10 blur-xl pointer-events-none" />

                {/* Burger stack layers */}
                <div className="flex flex-col items-center gap-1 z-10">
                  {/* Top Bun */}
                  <div className="w-24 h-6 bg-[#d9822b] rounded-t-full border border-[#f0a04b] flex items-center justify-center text-[8px] font-mono-telemetry text-black font-extrabold shadow-sm">
                    TOP BUN
                  </div>
                  {/* Sauce */}
                  <div className="w-20 h-1.5 bg-[#ff5500] rounded-full" />
                  {/* Cheese */}
                  <div className="w-26 h-2 bg-[#ffaa00] rounded-sm transform rotate-1" />
                  {/* Patty 1 */}
                  <div className="w-28 h-4 bg-[#2b1008] rounded-sm border border-[#ff5500]/40 flex items-center justify-center text-[8px] font-mono-telemetry text-white">
                    {patty.name.split('(')[0]}
                  </div>
                  {/* Bottom Bun */}
                  <div className="w-24 h-4 bg-[#b5651d] rounded-b-md border border-[#8a4a10] flex items-center justify-center text-[8px] font-mono-telemetry text-black font-extrabold">
                    BOTTOM
                  </div>
                </div>

                <div className="absolute bottom-1 right-2 text-[8px] font-mono-telemetry text-[#555555]">
                  RENDER: LAYER_STACK // 60FPS
                </div>
              </div>

              {/* Specs Summary */}
              <div className="space-y-2 text-xs font-mono-telemetry text-[#888888] pb-6 border-b border-[#222222]">
                <div className="flex justify-between">
                  <span>CARNE:</span>
                  <span className="text-white text-right">{patty.name}</span>
                </div>
                <div className="flex justify-between">
                  <span>PAN:</span>
                  <span className="text-white text-right">{bun.name}</span>
                </div>
                <div className="flex justify-between">
                  <span>QUESO:</span>
                  <span className="text-white text-right">{cheese.name}</span>
                </div>
                <div className="flex justify-between">
                  <span>TOPPINGS:</span>
                  <span className="text-white text-right truncate max-w-[150px]">
                    {toppings.length > 0 ? toppings.map((t) => t.name).join(', ') : 'Ninguno'}
                  </span>
                </div>
              </div>

              {/* Live Animated Price */}
              <div className="py-5">
                <span className="block text-[10px] font-mono-telemetry text-[#666666] uppercase">
                  TOTAL CALCULATED PRICE
                </span>
                <AnimatePresence mode="wait">
                  <motion.div
                    key={totalPrice}
                    initial={{ opacity: 0, y: -6 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: 6 }}
                    className="font-mono-telemetry text-3xl font-extrabold text-[#ff5500] tracking-tight mt-1"
                  >
                    {formatMoney(totalPrice)}
                  </motion.div>
                </AnimatePresence>
                <span className="text-[10px] font-mono-telemetry text-[#777777] block mt-1">
                  Incluye empaque térmico al vacío
                </span>
              </div>

              {/* WhatsApp Launch Action */}
              <button
                type="button"
                onClick={handleOrder}
                className="w-full btn-sear py-4 text-xs flex items-center justify-center gap-2"
              >
                <Send className="w-4 h-4" />
                <span>DESPACHAR A COCINA // WHATSAPP</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
