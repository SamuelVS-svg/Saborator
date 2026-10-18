import React, { useRef, useEffect } from 'react';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { PROCESS_STEPS, RESTAURANT_INFO } from '../data/restaurantData';
import SectionHeader from './SectionHeader';

gsap.registerPlugin(ScrollTrigger);

export default function ProcessVideoSection() {
  const sectionRef = useRef(null);
  const containerRef = useRef(null);
  const cardsRef = useRef([]);

  useEffect(() => {
    const ctx = gsap.context(() => {
      // Check if user prefers reduced motion
      const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (prefersReducedMotion) return;

      // Pin the section and animate steps
      const tl = gsap.timeline({
        scrollTrigger: {
          trigger: sectionRef.current,
          start: 'top top',
          end: '+=200%',
          pin: true,
          scrub: 1,
          anticipatePin: 1,
        },
      });

      cardsRef.current.forEach((card, index) => {
        if (!card) return;
        tl.fromTo(
          card,
          { opacity: 0.15, y: 30, scale: 0.95 },
          {
            opacity: 1,
            y: 0,
            scale: 1,
            duration: 1,
            ease: 'power2.out',
          },
          index * 0.8
        );
      });
    }, sectionRef);

    return () => ctx.revert();
  }, []);

  return (
    <section
      ref={sectionRef}
      id="proceso"
      className="relative w-full min-h-screen bg-[#3B0A14] text-[#F5EBDC] overflow-hidden flex flex-col justify-center px-6 py-20"
    >
      {/* Background Video / Atmosphere with Burgundy Gradient Overlay */}
      <div className="absolute inset-0 z-0">
        <video
          autoPlay
          loop
          muted
          playsInline
          poster="/img/hamburguesa.jpg"
          className="w-full h-full object-cover filter brightness-50 contrast-125"
        >
          {/* Placeholder: Replace with /videos/proceso.mp4 */}
          <source src={RESTAURANT_INFO.videoProcessPlaceholder} type="video/mp4" />
        </video>
        {/* Strict brand gradient overlay: from dark burgundy to transparent, never black */}
        <div className="absolute inset-0 bg-gradient-to-b from-[#3B0A14] via-[#3B0A14]/85 to-[#3B0A14]" />
        <div className="absolute inset-0 bg-gradient-to-r from-[#3B0A14]/90 via-transparent to-[#3B0A14]/90" />
      </div>

      {/* Content Container */}
      <div className="relative z-10 max-w-6xl mx-auto w-full">
        <SectionHeader
          number="04"
          name="EL PROCESO // FUEGO & TÉCNICA"
          subtitle="Del corte a la brasa: un recorrido paso a paso por la alquimia de nuestros platos."
          light={false}
        />

        {/* Steps Grid */}
        <div
          ref={containerRef}
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mt-8"
        >
          {PROCESS_STEPS.map((step, idx) => (
            <div
              key={step.step}
              ref={(el) => (cardsRef.current[idx] = el)}
              className="bg-[#6D1A2A]/40 border border-[#F5EBDC]/20 hover:border-[#F5EBDC] p-6 rounded-sm backdrop-blur-md flex flex-col justify-between min-h-[220px] transition-all"
            >
              <div>
                <span className="font-mono text-2xl font-bold text-[#E3CFB0] block mb-2">
                  {step.step}
                </span>
                <h4 className="font-display text-base font-bold uppercase tracking-wider text-[#F5EBDC] mb-3">
                  {step.title}
                </h4>
                <p className="text-xs text-[#E3CFB0]/80 font-light leading-relaxed">
                  {step.text}
                </p>
              </div>

              <div className="mt-4 pt-3 border-t border-[#F5EBDC]/10 flex items-center justify-between">
                <span className="text-[10px] font-mono tracking-widest text-[#E3CFB0]/60 uppercase">
                  ETAPA CULINARIA
                </span>
                <div className="w-2 h-2 rounded-full bg-[#E3CFB0]" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
