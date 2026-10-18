import React, { useState, useEffect, useRef } from 'react';
import { 
  Phone, 
  MessageSquare, 
  Send, 
  CheckCircle2, 
  AlertCircle, 
  X, 
  ArrowLeft, 
  Clock, 
  User, 
  ShieldCheck, 
  Flame 
} from 'lucide-react';
import confetti from 'canvas-confetti';

export default function AuthModal({ isOpen, onClose, onAuthSuccess }) {
  // Estados: 'phone' -> 'otp' -> 'success'
  const [step, setStep] = useState('phone');
  const [phone, setPhone] = useState('');
  const [channel, setChannel] = useState('sms'); // 'sms' | 'whatsapp'
  const [otp, setOtp] = useState(['', '', '', '', '', '']);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [infoMsg, setInfoMsg] = useState(null);
  const [timeLeft, setTimeLeft] = useState(300); // 5 min
  const [guestName, setGuestName] = useState('');
  const [isGuestMode, setIsGuestMode] = useState(false);

  const otpInputsRef = useRef([]);

  // Temporizador de 5 minutos cuando entra al paso de OTP
  useEffect(() => {
    let timer = null;
    if (step === 'otp' && timeLeft > 0) {
      timer = setInterval(() => {
        setTimeLeft((prev) => prev - 1);
      }, 1000);
    }
    return () => {
      if (timer) clearInterval(timer);
    };
  }, [step, timeLeft]);

  // Reset al abrir/cerrar modal
  useEffect(() => {
    if (!isOpen) {
      setTimeout(() => {
        setStep('phone');
        setOtp(['', '', '', '', '', '']);
        setError(null);
        setInfoMsg(null);
        setTimeLeft(300);
        setIsGuestMode(false);
      }, 300);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  // Formato mm:ss
  const formatTime = (secs) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  // 1. SOLICITAR CÓDIGO OTP (SMS o WhatsApp)
  const handleRequestOTP = async (selectedChannel) => {
    setError(null);
    setInfoMsg(null);

    const clean = phone.replace(/\D/g, '');
    if (clean.length !== 10) {
      setError('Ingresa un número de celular colombiano válido de 10 dígitos (ej: 3001234567).');
      return;
    }

    setChannel(selectedChannel);
    setLoading(true);

    try {
      console.log(`[Frontend] Solicitando OTP vía ${selectedChannel} para +57${clean}...`);
      const res = await fetch('/api/auth/request-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          phone: clean,
          channel: selectedChannel
        })
      });

      const data = await res.json();
      console.log('[Frontend] Respuesta de Twilio / Backend:', data);

      if (!res.ok || !data.success) {
        throw new Error(data.error || 'No se pudo enviar el código OTP.');
      }

      setInfoMsg(data.message || `Código enviado por ${selectedChannel.toUpperCase()}`);
      setTimeLeft(300);
      setStep('otp');
      setTimeout(() => {
        if (otpInputsRef.current[0]) {
          otpInputsRef.current[0].focus();
        }
      }, 150);
    } catch (err) {
      console.error('Error al solicitar OTP:', err);
      setError(err.message || 'Error de conexión con el servicio de mensajería Twilio.');
    } finally {
      setLoading(false);
    }
  };

  // 2. VERIFICAR CÓDIGO OTP
  const handleVerifyOTP = async (e) => {
    if (e) e.preventDefault();
    setError(null);

    const codeStr = otp.join('').trim();
    if (codeStr.length !== 6) {
      setError('Por favor ingresa los 6 dígitos del código.');
      return;
    }

    setLoading(true);
    const clean = phone.replace(/\D/g, '');

    try {
      console.log(`[Frontend] Verificando OTP ${codeStr} para +57${clean}...`);
      const res = await fetch('/api/auth/verify-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          phone: clean,
          code: codeStr,
          isGuest: isGuestMode,
          name: guestName.trim()
        })
      });

      const data = await res.json();
      console.log('[Frontend] Verificación respuesta:', data);

      if (!res.ok || !data.success) {
        throw new Error(data.error || 'Código incorrecto o expirado.');
      }

      // Guardar sesión y token JWT
      localStorage.setItem('saborator_token', data.token);
      localStorage.setItem('saborator_user', JSON.stringify(data.user));

      // Celebración con confeti
      try {
        confetti({
          particleCount: 80,
          spread: 70,
          origin: { y: 0.6 }
        });
      } catch (cErr) {
        // Ignorar si canvas-confetti no carga
      }

      setStep('success');
      setTimeout(() => {
        if (onAuthSuccess) onAuthSuccess(data.user, data.token);
        onClose();
        // Redirigir al menú
        window.location.hash = 'lineup';
      }, 1400);

    } catch (err) {
      console.error('Error al verificar OTP:', err);
      setError(err.message || 'Error al validar el código.');
    } finally {
      setLoading(false);
    }
  };

  // 3. PEDIR COMO INVITADO DIRECTO (Sin esperar código si el comensal tiene prisa)
  const handleGuestQuickLogin = async () => {
    setError(null);
    setLoading(true);
    const clean = phone.replace(/\D/g, '');

    try {
      const res = await fetch('/api/auth/guest-login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          phone: clean || '3000000000',
          name: guestName.trim() || 'Invitado Soledad'
        })
      });

      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.error || 'Error al iniciar como invitado.');
      }

      localStorage.setItem('saborator_token', data.token);
      localStorage.setItem('saborator_user', JSON.stringify(data.user));

      setStep('success');
      setTimeout(() => {
        if (onAuthSuccess) onAuthSuccess(data.user, data.token);
        onClose();
        window.location.hash = 'lineup';
      }, 1000);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Control de inputs OTP individuales de 6 dígitos
  const handleOtpChange = (index, value) => {
    if (value.length > 1) {
      // Manejar paste de código completo
      const pasted = value.replace(/\D/g, '').slice(0, 6).split('');
      const newOtp = [...otp];
      pasted.forEach((char, i) => {
        if (i < 6) newOtp[i] = char;
      });
      setOtp(newOtp);
      const nextIdx = Math.min(pasted.length, 5);
      if (otpInputsRef.current[nextIdx]) otpInputsRef.current[nextIdx].focus();
      return;
    }

    const cleanChar = value.replace(/\D/g, '');
    const newOtp = [...otp];
    newOtp[index] = cleanChar;
    setOtp(newOtp);

    if (cleanChar && index < 5) {
      otpInputsRef.current[index + 1]?.focus();
    }
  };

  const handleOtpKeyDown = (index, e) => {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      otpInputsRef.current[index - 1]?.focus();
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/85 backdrop-blur-md animate-in fade-in duration-200">
      <div 
        className="w-full max-w-md bg-[#121212] border border-[#ff5500]/30 rounded-2xl p-6 sm:p-8 text-white shadow-2xl relative overflow-hidden"
        style={{
          boxShadow: '0 25px 60px -15px rgba(255, 85, 0, 0.25), 0 0 40px rgba(0, 0, 0, 0.9)'
        }}
      >
        {/* Glow de acento superior */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-[#ff5500] to-transparent" />

        {/* Botón Cerrar */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-[#888888] hover:text-white rounded-full hover:bg-white/5 transition-colors focus:outline-none"
          aria-label="Cerrar modal"
        >
          <X className="w-5 h-5" />
        </button>

        {/* ── PASO 1: NÚMERO DE CELULAR (+57) ────────────────────────────── */}
        {step === 'phone' && (
          <div>
            {/* Header del Negocio */}
            <div className="flex items-center gap-2.5 mb-2">
              <div className="w-8 h-8 rounded-lg bg-[#ff5500]/10 border border-[#ff5500]/40 flex items-center justify-center text-[#ff5500]">
                <Flame className="w-4 h-4" />
              </div>
              <div>
                <h3 className="font-condensed-heading text-xl sm:text-2xl text-white tracking-wide uppercase leading-none">
                  Acceso Comensal
                </h3>
                <p className="text-[10px] text-[#ffaa00] font-mono-telemetry uppercase">
                  Saborator Soledad // Domicilios & Salón
                </p>
              </div>
            </div>

            <p className="text-xs text-[#999999] mb-5 leading-relaxed">
              Ingresa tu celular colombiano para verificar tu orden por SMS o WhatsApp sin contraseñas difíciles.
            </p>

            {/* Mensaje de error si ocurre */}
            {error && (
              <div className="mb-4 p-3 bg-red-950/50 border border-red-500/40 rounded-lg text-xs text-red-200 flex items-start gap-2 animate-in slide-in-from-top-1">
                <AlertCircle className="w-4 h-4 text-red-400 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            )}

            {/* Input Celular con Prefijo Colombiano +57 */}
            <div className="mb-4">
              <label className="block text-[11px] font-mono-telemetry uppercase text-[#aaaaaa] mb-1.5">
                Número de Celular (+57)
              </label>
              <div className="flex items-center bg-[#1a1a1a] border border-[#333333] focus-within:border-[#ff5500] rounded-xl overflow-hidden transition-all shadow-inner">
                {/* Prefijo Colombiano */}
                <div className="flex items-center gap-1.5 px-3 py-3 bg-[#242424] border-r border-[#333333] select-none">
                  <span className="text-base" role="img" aria-label="Bandera Colombia">🇨🇴</span>
                  <span className="text-xs font-bold text-white tracking-wider">+57</span>
                </div>
                {/* Input 10 dígitos */}
                <input
                  type="tel"
                  placeholder="300 123 4567"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value.replace(/\D/g, '').slice(0, 10))}
                  className="w-full px-3.5 py-3 bg-transparent text-sm text-white placeholder-[#555555] font-mono-telemetry tracking-widest focus:outline-none"
                  autoFocus
                />
              </div>
              <span className="text-[10px] text-[#666666] mt-1 block">
                Solo 10 dígitos colombianos sin espacios.
              </span>
            </div>

            {/* Modo Invitado Opcional */}
            <div className="mb-5 p-3 bg-[#181818] border border-[#2a2a2a] rounded-xl flex items-center justify-between">
              <div className="flex items-center gap-2">
                <User className="w-4 h-4 text-[#888888]" />
                <span className="text-xs text-[#cccccc]">¿Tienes afán? Pedir como invitado</span>
              </div>
              <button
                type="button"
                onClick={() => setIsGuestMode(!isGuestMode)}
                className={`text-[11px] font-bold px-2.5 py-1 rounded transition-colors ${
                  isGuestMode ? 'bg-[#ff5500] text-black' : 'bg-[#2a2a2a] text-[#aaaaaa] hover:text-white'
                }`}
              >
                {isGuestMode ? 'ACTIVADO' : 'ACTIVAR'}
              </button>
            </div>

            {isGuestMode && (
              <div className="mb-5 animate-in fade-in duration-200">
                <label className="block text-[11px] font-mono-telemetry uppercase text-[#aaaaaa] mb-1.5">
                  Tu Nombre (Para la entrega)
                </label>
                <input
                  type="text"
                  placeholder="Ej: Carlos Ramírez"
                  value={guestName}
                  onChange={(e) => setGuestName(e.target.value)}
                  className="w-full px-3.5 py-2.5 bg-[#1a1a1a] border border-[#333333] focus:border-[#ff5500] rounded-xl text-sm text-white focus:outline-none"
                />
                <button
                  type="button"
                  onClick={handleGuestQuickLogin}
                  disabled={loading}
                  className="w-full mt-2.5 py-2 bg-[#2a2a2a] hover:bg-[#333333] text-xs font-bold text-white rounded-xl transition-all border border-white/10"
                >
                  ⚡ Continuar de inmediato sin código OTP
                </button>
              </div>
            )}

            {/* Dos Botones de Envío: [Enviar por SMS] y [Enviar por WhatsApp] */}
            <div className="grid grid-cols-2 gap-3 mt-4">
              {/* Botón SMS */}
              <button
                type="button"
                onClick={() => handleRequestOTP('sms')}
                disabled={loading || phone.length < 10}
                className="flex items-center justify-center gap-2 px-3 py-3 bg-[#242424] hover:bg-[#2e2e2e] disabled:opacity-40 disabled:cursor-not-allowed border border-[#3a3a3a] hover:border-[#ff5500] text-xs font-bold text-white rounded-xl transition-all active:scale-95 shadow-md"
              >
                <MessageSquare className="w-4 h-4 text-[#ffaa00]" />
                <span>Enviar por SMS</span>
              </button>

              {/* Botón WhatsApp */}
              <button
                type="button"
                onClick={() => handleRequestOTP('whatsapp')}
                disabled={loading || phone.length < 10}
                className="flex items-center justify-center gap-2 px-3 py-3 bg-[#1e2e24] hover:bg-[#253d2f] disabled:opacity-40 disabled:cursor-not-allowed border border-[#25D366]/40 hover:border-[#25D366] text-xs font-bold text-white rounded-xl transition-all active:scale-95 shadow-md"
              >
                <Send className="w-4 h-4 text-[#25D366]" />
                <span>Enviar WhatsApp</span>
              </button>
            </div>

            {loading && (
              <p className="text-center text-xs text-[#ffaa00] mt-4 flex items-center justify-center gap-2 font-mono-telemetry animate-pulse">
                <Clock className="w-3.5 h-3.5 animate-spin" /> Conectando con Twilio...
              </p>
            )}
          </div>
        )}

        {/* ── PASO 2: VERIFICACIÓN OTP DE 6 DÍGITOS ──────────────────────── */}
        {step === 'otp' && (
          <div>
            {/* Botón volver */}
            <button
              onClick={() => { setStep('phone'); setError(null); }}
              className="inline-flex items-center gap-1.5 text-xs text-[#888888] hover:text-white mb-4 transition-colors focus:outline-none"
            >
              <ArrowLeft className="w-3.5 h-3.5" />
              <span>Cambiar número</span>
            </button>

            <div className="flex items-center gap-2.5 mb-2">
              <div className="w-8 h-8 rounded-lg bg-[#25D366]/10 border border-[#25D366]/40 flex items-center justify-center text-[#25D366]">
                <ShieldCheck className="w-4 h-4" />
              </div>
              <div>
                <h3 className="font-condensed-heading text-xl sm:text-2xl text-white tracking-wide uppercase leading-none">
                  Código de 6 Dígitos
                </h3>
                <p className="text-[10px] text-[#25D366] font-mono-telemetry uppercase">
                  Vía {channel === 'whatsapp' ? 'WhatsApp' : 'SMS'} a +57 {phone}
                </p>
              </div>
            </div>

            <p className="text-xs text-[#aaaaaa] mb-5 leading-relaxed">
              Ingresa el código enviado por Twilio. Tienes <strong className="text-white">{formatTime(timeLeft)}</strong> antes de que expire.
            </p>

            {/* Mensaje de error si falla verificación */}
            {error && (
              <div className="mb-4 p-3 bg-red-950/50 border border-red-500/40 rounded-lg text-xs text-red-200 flex items-start gap-2">
                <AlertCircle className="w-4 h-4 text-red-400 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            )}

            {/* Cajas de 6 dígitos */}
            <form onSubmit={handleVerifyOTP}>
              <div className="flex justify-between gap-2 mb-6">
                {otp.map((digit, idx) => (
                  <input
                    key={idx}
                    ref={(el) => (otpInputsRef.current[idx] = el)}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={digit}
                    onChange={(e) => handleOtpChange(idx, e.target.value)}
                    onKeyDown={(e) => handleOtpKeyDown(idx, e)}
                    className="w-12 h-13 text-center text-xl font-bold bg-[#1a1a1a] border border-[#333333] focus:border-[#ff5500] rounded-xl text-white focus:outline-none transition-all shadow-inner font-mono-telemetry"
                  />
                ))}
              </div>

              {/* Botón Verificar */}
              <button
                type="submit"
                disabled={loading || otp.join('').length !== 6 || timeLeft <= 0}
                className="w-full py-3.5 bg-gradient-to-r from-[#ff5500] to-[#ffaa00] hover:brightness-110 disabled:opacity-40 disabled:cursor-not-allowed font-bold text-xs uppercase tracking-wider text-black rounded-xl transition-all shadow-lg active:scale-98 flex items-center justify-center gap-2"
              >
                {loading ? (
                  <>
                    <Clock className="w-4 h-4 animate-spin" />
                    <span>Verificando...</span>
                  </>
                ) : (
                  <>
                    <CheckCircle2 className="w-4 h-4" />
                    <span>Verificar y Ordenar</span>
                  </>
                )}
              </button>
            </form>

            {/* Reenvío de código */}
            <div className="mt-5 text-center flex items-center justify-between text-xs text-[#888888]">
              <span>¿No llegó el código?</span>
              <button
                type="button"
                onClick={() => handleRequestOTP(channel)}
                disabled={loading || timeLeft > 270}
                className="text-[#ffaa00] hover:underline font-bold disabled:opacity-40 disabled:no-underline"
              >
                Reenviar {timeLeft > 270 ? `(${formatTime(timeLeft - 270)})` : ''}
              </button>
            </div>
          </div>
        )}

        {/* ── PASO 3: ÉXITO Y REDIRECCIÓN AL MENÚ ────────────────────────── */}
        {step === 'success' && (
          <div className="text-center py-6 animate-in zoom-in-95 duration-200">
            <div className="w-16 h-16 bg-[#25D366]/20 border border-[#25D366] rounded-full flex items-center justify-center mx-auto mb-4 text-[#25D366] shadow-[0_0_30px_rgba(37,211,102,0.4)]">
              <CheckCircle2 className="w-8 h-8" />
            </div>
            <h4 className="font-condensed-heading text-2xl text-white uppercase tracking-wide mb-1">
              ¡Autenticado con Éxito!
            </h4>
            <p className="text-xs text-[#cccccc] font-mono-telemetry mb-4">
              Bienvenido a Saborator Soledad (+57 {phone})
            </p>
            <div className="inline-flex items-center gap-2 text-xs text-[#ffaa00] bg-[#ff5500]/10 px-4 py-1.5 rounded-full border border-[#ff5500]/30 animate-pulse font-mono-telemetry">
              <span>Redirigiendo a la carta...</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
