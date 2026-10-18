import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import SectionHeader from './SectionHeader';
import { RESTAURANT_INFO } from '../data/restaurantData';
import { Calendar, Clock, User, Phone, CheckCircle2, Send } from 'lucide-react';

export default function ReservationSection() {
  const [formData, setFormData] = useState({
    name: '',
    phone: '',
    guests: '2',
    date: new Date().toISOString().split('T')[0],
    time: '19:30',
    notes: '',
  });

  const [submitted, setSubmitted] = useState(false);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name || !formData.phone) return;

    // Simulate animated confirmation
    setSubmitted(true);

    // Also trigger WhatsApp reservation link
    const msg = `🍷 *NUEVA RESERVA — SABORATOR*
👤 Nombre: ${formData.name}
📞 Teléfono: ${formData.phone}
👥 Personas: ${formData.guests}
📅 Fecha: ${formData.date}
⏰ Hora: ${formData.time}
📝 Notas: ${formData.notes || 'Ninguna'}

Por favor confirmar disponibilidad.`;

    const url = `https://wa.me/${RESTAURANT_INFO.whatsappNumber}?text=${encodeURIComponent(msg)}`;
    window.open(url, '_blank');
  };

  return (
    <section id="reserva" className="relative w-full py-24 sm:py-32 bg-[#F5EBDC] text-[#3B0A14] px-6">
      <div className="max-w-4xl mx-auto">
        <SectionHeader
          number="07"
          name="RESERVA TU EXPERIENCIA // MESA PRIVADA"
          subtitle="Asegura tu lugar en nuestro espacio en Barranquilla o coordina un evento gastronómico exclusivo."
          light={true}
        />

        <div className="bg-white border border-[#3B0A14]/20 rounded-sm p-6 sm:p-10 shadow-xl relative overflow-hidden">
          <AnimatePresence mode="wait">
            {!submitted ? (
              <motion.form
                key="form"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                onSubmit={handleSubmit}
                className="space-y-6"
              >
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                  {/* Name */}
                  <div>
                    <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                      Nombre Completo *
                    </label>
                    <div className="relative">
                      <User className="w-4 h-4 text-[#6D1A2A] absolute left-3 top-1/2 -translate-y-1/2" />
                      <input
                        type="text"
                        name="name"
                        required
                        value={formData.name}
                        onChange={handleChange}
                        placeholder="Ej. Andrés Martínez"
                        className="w-full pl-10 pr-4 py-3 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14] focus:ring-1 focus:ring-[#3B0A14]"
                      />
                    </div>
                  </div>

                  {/* Phone */}
                  <div>
                    <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                      Número Telefónico *
                    </label>
                    <div className="relative">
                      <Phone className="w-4 h-4 text-[#6D1A2A] absolute left-3 top-1/2 -translate-y-1/2" />
                      <input
                        type="tel"
                        name="phone"
                        required
                        value={formData.phone}
                        onChange={handleChange}
                        placeholder="Ej. 300 123 4567"
                        className="w-full pl-10 pr-4 py-3 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14] focus:ring-1 focus:ring-[#3B0A14]"
                      />
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
                  {/* Date */}
                  <div>
                    <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                      Fecha
                    </label>
                    <div className="relative">
                      <Calendar className="w-4 h-4 text-[#6D1A2A] absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                      <input
                        type="date"
                        name="date"
                        value={formData.date}
                        onChange={handleChange}
                        className="w-full pl-10 pr-4 py-3 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14]"
                      />
                    </div>
                  </div>

                  {/* Time */}
                  <div>
                    <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                      Hora
                    </label>
                    <div className="relative">
                      <Clock className="w-4 h-4 text-[#6D1A2A] absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                      <select
                        name="time"
                        value={formData.time}
                        onChange={handleChange}
                        className="w-full pl-10 pr-4 py-3 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14]"
                      >
                        <option value="17:00">5:00 PM</option>
                        <option value="18:00">6:00 PM</option>
                        <option value="19:00">7:00 PM</option>
                        <option value="19:30">7:30 PM</option>
                        <option value="20:00">8:00 PM</option>
                        <option value="20:30">8:30 PM</option>
                        <option value="21:00">9:00 PM</option>
                        <option value="22:00">10:00 PM</option>
                      </select>
                    </div>
                  </div>

                  {/* Guests */}
                  <div>
                    <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                      Comensales
                    </label>
                    <select
                      name="guests"
                      value={formData.guests}
                      onChange={handleChange}
                      className="w-full px-4 py-3 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14]"
                    >
                      <option value="1">1 Persona</option>
                      <option value="2">2 Personas (Pareja)</option>
                      <option value="4">3 - 4 Personas</option>
                      <option value="6">5 - 6 Personas</option>
                      <option value="8+">8+ Personas (Grupo)</option>
                    </select>
                  </div>
                </div>

                {/* Notes */}
                <div>
                  <label className="block text-xs uppercase font-mono tracking-wider font-semibold text-[#6D1A2A] mb-2">
                    Petición Especial o Alergias (Opcional)
                  </label>
                  <textarea
                    name="notes"
                    rows="3"
                    value={formData.notes}
                    onChange={handleChange}
                    placeholder="Ej. Celebración de aniversario, mesa en terraza si está disponible."
                    className="w-full p-4 bg-[#FBF6EC] border border-[#3B0A14]/20 rounded-sm text-sm text-[#3B0A14] focus:outline-none focus:border-[#3B0A14]"
                  />
                </div>

                {/* Submit button */}
                <button
                  type="submit"
                  className="w-full btn-light-primary py-4 px-6 rounded-sm text-xs flex items-center justify-center gap-2 shadow-lg"
                >
                  <Send className="w-4 h-4" />
                  <span>CONFIRMAR Y ENVIAR POR WHATSAPP</span>
                </button>
              </motion.form>
            ) : (
              <motion.div
                key="confirmed"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="py-12 text-center flex flex-col items-center gap-4"
              >
                <CheckCircle2 className="w-16 h-16 text-[#6D1A2A]" />
                <h3 className="font-display text-2xl font-bold uppercase tracking-wider text-[#3B0A14]">
                  ¡SOLICITUD DE RESERVA GENERADA!
                </h3>
                <p className="text-sm text-[#6D1A2A] max-w-md">
                  Hemos transferido los detalles de tu mesa a nuestro anfitrión por WhatsApp. Recibirás tu confirmación de reserva en minutos.
                </p>
                <button
                  onClick={() => setSubmitted(false)}
                  className="mt-4 text-xs font-mono uppercase tracking-widest text-[#3B0A14] underline hover:text-[#6D1A2A]"
                >
                  Modificar datos de la reserva
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </div>
    </section>
  );
}
