import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import SectionHeader from './SectionHeader';
import { BUILDER_OPTIONS, RESTAURANT_INFO } from '../data/restaurantData';
import { Check, Send, ShoppingBag } from 'lucide-react';

export default function OrderBuilder() {
  const [selectedBase, setSelectedBase] = useState(BUILDER_OPTIONS.bases[0]);
  const [selectedBread, setSelectedBread] = useState(BUILDER_OPTIONS.breads[0]);
  const [selectedCheese, setSelectedCheese] = useState(BUILDER_OPTIONS.cheeses[0]);
  const [selectedExtras, setSelectedExtras] = useState([BUILDER_OPTIONS.extras[0]]);
  const [selectedDrink, setSelectedDrink] = useState(BUILDER_OPTIONS.drinks[0]);

  // Toggle extra
  const toggleExtra = (extra) => {
    if (selectedExtras.some((e) => e.id === extra.id)) {
      setSelectedExtras(selectedExtras.filter((e) => e.id !== extra.id));
    } else {
      setSelectedExtras([...selectedExtras, extra]);
    }
  };

  // Calculate live total
  const total =
    selectedBase.price +
    selectedBread.price +
    selectedCheese.price +
    selectedExtras.reduce((acc, curr) => acc + curr.price, 0) +
    (selectedDrink ? selectedDrink.price : 0);

  // Format currency
  const formatMoney = (amount) => {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  // Prepare WhatsApp message
  const handleWhatsAppOrder = () => {
    const extrasList =
      selectedExtras.length > 0
        ? selectedExtras.map((e) => e.name).join(', ')
        : 'Sin extras adicionales';

    const message = `👋 ¡Hola Saborator! Deseo realizar el siguiente pedido personalizado desde el sitio web:

🥩 *Base:* ${selectedBase.name} ($${selectedBase.price.toLocaleString('es-CO')})
🥖 *Pan:* ${selectedBread.name} ($${selectedBread.price.toLocaleString('es-CO')})
🧀 *Queso:* ${selectedCheese.name} ($${selectedCheese.price.toLocaleString('es-CO')})
🥓 *Extras:* ${extrasList}
🥤 *Bebida:* ${selectedDrink ? selectedDrink.name : 'Sin bebida'}

💰 *TOTAL ESTIMADO:* ${formatMoney(total)}

📍 Ciudad: Barranquilla
¿Cuál es el tiempo de entrega y método de pago disponible?`;

    const url = `https://wa.me/${RESTAURANT_INFO.whatsappNumber}?text=${encodeURIComponent(
      message
    )}`;
    window.open(url, '_blank');
  };

  return (
    <section id="builder" className="relative w-full py-24 sm:py-32 bg-[#F5EBDC] text-[#3B0A14] px-6">
      <div className="max-w-7xl mx-auto">
        <SectionHeader
          number="02"
          name="CONSTRUCTOR DE PEDIDO // CUSTOM CRAFT"
          subtitle="Diseña tu plato exactamente como lo imaginas. Cada componente es preparado con técnicas de alta cocina."
          light={true}
        />

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Options Column (8 cols) */}
          <div className="lg:col-span-8 flex flex-col gap-8">
            {/* Step 1: Base */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <span className="font-mono text-xs font-bold text-[#6D1A2A]">PASO 01 //</span>
                <h3 className="font-display text-sm font-bold uppercase tracking-wider text-[#3B0A14]">
                  SELECCIONA TU PROTEÍNA BASE
                </h3>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_OPTIONS.bases.map((base) => {
                  const isSelected = selectedBase.id === base.id;
                  return (
                    <button
                      key={base.id}
                      onClick={() => setSelectedBase(base)}
                      type="button"
                      className={`text-left p-4 rounded-sm border transition-all text-sm flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#3B0A14] bg-[#E3CFB0] shadow-md ring-2 ring-[#3B0A14]'
                          : 'border-[#3B0A14]/20 bg-white/60 hover:bg-white'
                      }`}
                    >
                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <span className="font-bold font-display text-xs uppercase">{base.name}</span>
                          {isSelected && <Check className="w-4 h-4 text-[#3B0A14]" />}
                        </div>
                        <p className="text-[11px] text-[#6D1A2A]/80 font-light">{base.description}</p>
                      </div>
                      <span className="font-mono font-semibold text-xs mt-3 text-[#3B0A14]">
                        +{formatMoney(base.price)}
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Step 2: Bread */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <span className="font-mono text-xs font-bold text-[#6D1A2A]">PASO 02 //</span>
                <h3 className="font-display text-sm font-bold uppercase tracking-wider text-[#3B0A14]">
                  PAN O BASE ARTESANAL
                </h3>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_OPTIONS.breads.map((bread) => {
                  const isSelected = selectedBread.id === bread.id;
                  return (
                    <button
                      key={bread.id}
                      onClick={() => setSelectedBread(bread)}
                      type="button"
                      className={`text-left p-4 rounded-sm border transition-all text-sm flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#3B0A14] bg-[#E3CFB0] shadow-md ring-2 ring-[#3B0A14]'
                          : 'border-[#3B0A14]/20 bg-white/60 hover:bg-white'
                      }`}
                    >
                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <span className="font-bold font-display text-xs uppercase">{bread.name}</span>
                          {isSelected && <Check className="w-4 h-4 text-[#3B0A14]" />}
                        </div>
                        <p className="text-[11px] text-[#6D1A2A]/80 font-light">{bread.description}</p>
                      </div>
                      <span className="font-mono font-semibold text-xs mt-3 text-[#3B0A14]">
                        +{formatMoney(bread.price)}
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Step 3: Cheese */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <span className="font-mono text-xs font-bold text-[#6D1A2A]">PASO 03 //</span>
                <h3 className="font-display text-sm font-bold uppercase tracking-wider text-[#3B0A14]">
                  QUESO MADURADO O FUNDIDO
                </h3>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_OPTIONS.cheeses.map((cheese) => {
                  const isSelected = selectedCheese.id === cheese.id;
                  return (
                    <button
                      key={cheese.id}
                      onClick={() => setSelectedCheese(cheese)}
                      type="button"
                      className={`text-left p-4 rounded-sm border transition-all text-sm flex items-center justify-between ${
                        isSelected
                          ? 'border-[#3B0A14] bg-[#E3CFB0] shadow-md ring-2 ring-[#3B0A14]'
                          : 'border-[#3B0A14]/20 bg-white/60 hover:bg-white'
                      }`}
                    >
                      <span className="font-bold font-display text-xs uppercase">{cheese.name}</span>
                      <span className="font-mono text-xs text-[#3B0A14]">+{formatMoney(cheese.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Step 4: Extras (Multiple selection) */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <span className="font-mono text-xs font-bold text-[#6D1A2A]">PASO 04 //</span>
                <h3 className="font-display text-sm font-bold uppercase tracking-wider text-[#3B0A14]">
                  EXTRAS DE FIRMA (ELIGE LOS QUE DESEES)
                </h3>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {BUILDER_OPTIONS.extras.map((extra) => {
                  const isSelected = selectedExtras.some((e) => e.id === extra.id);
                  return (
                    <button
                      key={extra.id}
                      onClick={() => toggleExtra(extra)}
                      type="button"
                      className={`text-left p-3 rounded-sm border transition-all text-xs flex flex-col justify-between ${
                        isSelected
                          ? 'border-[#3B0A14] bg-[#E3CFB0] shadow-sm font-semibold'
                          : 'border-[#3B0A14]/20 bg-white/60 hover:bg-white'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-2">
                        <span>{extra.name}</span>
                        {isSelected && <Check className="w-3.5 h-3.5 text-[#3B0A14]" />}
                      </div>
                      <span className="font-mono text-[11px] text-[#6D1A2A]">+{formatMoney(extra.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Step 5: Drinks */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <span className="font-mono text-xs font-bold text-[#6D1A2A]">PASO 05 //</span>
                <h3 className="font-display text-sm font-bold uppercase tracking-wider text-[#3B0A14]">
                  BEBIDA PARA ACOMPAÑAR
                </h3>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {BUILDER_OPTIONS.drinks.map((drink) => {
                  const isSelected = selectedDrink?.id === drink.id;
                  return (
                    <button
                      key={drink.id}
                      onClick={() => setSelectedDrink(isSelected ? null : drink)}
                      type="button"
                      className={`text-left p-3 rounded-sm border transition-all text-xs flex items-center justify-between ${
                        isSelected
                          ? 'border-[#3B0A14] bg-[#E3CFB0] shadow-sm ring-2 ring-[#3B0A14]'
                          : 'border-[#3B0A14]/20 bg-white/60 hover:bg-white'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <img src={drink.image} alt={drink.name} className="w-7 h-7 object-contain" />
                        <span className="font-medium">{drink.name}</span>
                      </div>
                      <span className="font-mono text-[11px] text-[#3B0A14]">+{formatMoney(drink.price)}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Sticky Summary / WhatsApp Trigger (4 cols) */}
          <div className="lg:col-span-4 lg:sticky lg:top-24">
            <div className="bg-[#3B0A14] text-[#F5EBDC] p-6 sm:p-8 rounded-sm border border-[#3B0A14] shadow-2xl">
              <div className="flex items-center gap-2 pb-4 border-b border-[#F5EBDC]/20 mb-6">
                <ShoppingBag className="w-5 h-5 text-[#E3CFB0]" />
                <h4 className="font-display text-sm font-bold tracking-widest uppercase">
                  RESUMEN DE TU CREACIÓN
                </h4>
              </div>

              {/* Items List */}
              <div className="flex flex-col gap-3 text-xs font-light mb-6">
                <div className="flex justify-between">
                  <span className="text-[#E3CFB0]">Proteína:</span>
                  <span className="font-medium text-right">{selectedBase.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#E3CFB0]">Pan:</span>
                  <span className="font-medium text-right">{selectedBread.name}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#E3CFB0]">Queso:</span>
                  <span className="font-medium text-right">{selectedCheese.name}</span>
                </div>
                {selectedExtras.length > 0 && (
                  <div className="flex justify-between">
                    <span className="text-[#E3CFB0]">Extras:</span>
                    <span className="font-medium text-right">{selectedExtras.map((e) => e.name).join(', ')}</span>
                  </div>
                )}
                {selectedDrink && (
                  <div className="flex justify-between">
                    <span className="text-[#E3CFB0]">Bebida:</span>
                    <span className="font-medium text-right">{selectedDrink.name}</span>
                  </div>
                )}
              </div>

              {/* Live Animated Total */}
              <div className="py-4 border-t border-[#F5EBDC]/20 mb-6">
                <span className="block text-[10px] uppercase font-mono tracking-widest text-[#E3CFB0]">
                  TOTAL DEL PEDIDO
                </span>
                <AnimatePresence mode="wait">
                  <motion.div
                    key={total}
                    initial={{ opacity: 0, y: -8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: 8 }}
                    transition={{ duration: 0.2 }}
                    className="font-mono text-3xl font-extrabold text-[#F5EBDC] tracking-tight mt-1"
                  >
                    {formatMoney(total)}
                  </motion.div>
                </AnimatePresence>
                <span className="block text-[10px] text-[#E3CFB0]/60 mt-1 font-mono">
                  Incluye empaque térmico premium
                </span>
              </div>

              {/* Pedir por WhatsApp Button */}
              <button
                type="button"
                onClick={handleWhatsAppOrder}
                className="w-full btn-primary py-4 px-4 rounded-sm text-xs flex items-center justify-center gap-2 shadow-lg"
              >
                <Send className="w-4 h-4" />
                <span>PEDIR POR WHATSAPP</span>
              </button>

              <p className="text-[10px] text-center text-[#E3CFB0]/70 mt-3 font-light">
                Envía tu configuración directamente a cocina. Tiempo de respuesta estimado: &lt;3 min.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
