import React, { useEffect, useRef, useState } from 'react';
import * as THREE from 'three';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { SCROLLYTELLING_STAGES } from '../data/smashData';
import { Flame, Thermometer, Gauge, ChevronDown, Sliders } from 'lucide-react';

gsap.registerPlugin(ScrollTrigger);

export default function ScrollytellingCanvas() {
  const containerRef = useRef(null);
  const canvasRef = useRef(null);
  const [currentProgress, setCurrentProgress] = useState(0);
  const [currentStageIdx, setCurrentStageIdx] = useState(0);
  const [currentTemp, setCurrentTemp] = useState(93);

  // References for Three.js objects
  const sceneRef = useRef(null);
  const burgerPartsRef = useRef({});
  const particlesRef = useRef(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    // 1. Setup Three.js Scene, Camera, Renderer
    const width = canvas.parentElement.clientWidth;
    const height = canvas.parentElement.clientHeight;

    const scene = new THREE.Scene();
    sceneRef.current = scene;
    scene.fog = new THREE.FogExp2(0x0a0a0a, 0.05);

    const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);
    camera.position.set(0, 1.2, 5.5);
    camera.lookAt(0, 0, 0);

    const renderer = new THREE.WebGLRenderer({
      canvas,
      antialias: true,
      alpha: true,
    });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;

    // 2. Lighting (Amber / Fire accents + cool rim lights)
    const ambientLight = new THREE.AmbientLight(0x221100, 1.5);
    scene.add(ambientLight);

    const grillLight = new THREE.PointLight(0xff5500, 4, 15);
    grillLight.position.set(0, -1.5, 1);
    scene.add(grillLight);

    const keyLight = new THREE.DirectionalLight(0xffeedd, 2.5);
    keyLight.position.set(3, 4, 4);
    scene.add(keyLight);

    const rimLight = new THREE.DirectionalLight(0xffaa00, 2);
    rimLight.position.set(-4, -1, -3);
    scene.add(rimLight);

    // 3. Create Procedural Mesh Geometry for the 5 Cooking Stages
    // Planche / Griddle Base
    const griddleGeo = new THREE.CylinderGeometry(2.5, 2.5, 0.1, 32);
    const griddleMat = new THREE.MeshStandardMaterial({
      color: 0x1a1a1a,
      roughness: 0.3,
      metalness: 0.85,
    });
    const griddle = new THREE.Mesh(griddleGeo, griddleMat);
    griddle.position.y = -1.2;
    scene.add(griddle);

    // Patty 1 (Ground beef / Smash patty)
    const pattyGeo = new THREE.CylinderGeometry(0.9, 0.9, 0.25, 32);
    const pattyMat = new THREE.MeshStandardMaterial({
      color: 0x5c1a0e, // Raw reddish brown
      roughness: 0.9,
      metalness: 0.1,
    });
    const patty = new THREE.Mesh(pattyGeo, pattyMat);
    patty.position.y = 0.5;
    scene.add(patty);

    // Patty 2 (Upper patty)
    const patty2 = new THREE.Mesh(pattyGeo.clone(), pattyMat.clone());
    patty2.position.y = 1.2;
    scene.add(patty2);

    // Cheese layer (Melted cheese slice)
    const cheeseGeo = new THREE.BoxGeometry(1.6, 0.05, 1.6);
    const cheeseMat = new THREE.MeshStandardMaterial({
      color: 0xffaa00,
      roughness: 0.4,
      metalness: 0.1,
      transparent: true,
      opacity: 0,
    });
    const cheese = new THREE.Mesh(cheeseGeo, cheeseMat);
    cheese.rotation.y = Math.PI / 4;
    cheese.position.y = 1.4;
    scene.add(cheese);

    // Top Bun (Brioche)
    const topBunGeo = new THREE.SphereGeometry(1.05, 32, 16, 0, Math.PI * 2, 0, Math.PI / 2);
    const bunMat = new THREE.MeshStandardMaterial({
      color: 0xd9822b,
      roughness: 0.6,
      metalness: 0.05,
      transparent: true,
      opacity: 0,
    });
    const topBun = new THREE.Mesh(topBunGeo, bunMat);
    topBun.position.y = 3.0;
    scene.add(topBun);

    // Bottom Bun
    const bottomBunGeo = new THREE.CylinderGeometry(1.0, 1.0, 0.25, 32);
    const bottomBun = new THREE.Mesh(bottomBunGeo, bunMat.clone());
    bottomBun.position.y = -2.0;
    bottomBun.material.transparent = true;
    bottomBun.material.opacity = 0;
    scene.add(bottomBun);

    // Smash Spatula Tool (The press)
    const pressGeo = new THREE.CylinderGeometry(1.2, 1.2, 0.08, 32);
    const pressMat = new THREE.MeshStandardMaterial({
      color: 0x999999,
      metalness: 0.9,
      roughness: 0.2,
      transparent: true,
      opacity: 0,
    });
    const press = new THREE.Mesh(pressGeo, pressMat);
    press.position.y = 2.5;
    scene.add(press);

    burgerPartsRef.current = {
      patty,
      patty2,
      cheese,
      topBun,
      bottomBun,
      press,
    };

    // 4. Fire & Smoke Steam Particle System
    const particleCount = 200;
    const particleGeo = new THREE.BufferGeometry();
    const positions = new Float32Array(particleCount * 3);
    const scales = new Float32Array(particleCount);

    for (let i = 0; i < particleCount * 3; i += 3) {
      positions[i] = (Math.random() - 0.5) * 2.2;
      positions[i + 1] = Math.random() * 2.5 - 1;
      positions[i + 2] = (Math.random() - 0.5) * 2.2;
      scales[i / 3] = Math.random() * 0.08 + 0.02;
    }

    particleGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    const particleMat = new THREE.PointsMaterial({
      color: 0xff5500,
      size: 0.06,
      transparent: true,
      opacity: 0.4,
      blending: THREE.AdditiveBlending,
    });
    const particles = new THREE.Points(particleGeo, particleMat);
    scene.add(particles);
    particlesRef.current = particles;

    // 5. Render Loop
    let animationFrameId;
    const clock = new THREE.Clock();

    const animate = () => {
      animationFrameId = requestAnimationFrame(animate);
      const elapsedTime = clock.getElapsedTime();

      // Subtle ambient rotation
      if (patty) patty.rotation.y = elapsedTime * 0.15;
      if (patty2) patty2.rotation.y = elapsedTime * 0.15;

      // Particle floating animation
      const pos = particles.geometry.attributes.position.array;
      for (let i = 1; i < pos.length; i += 3) {
        pos[i] += 0.015;
        if (pos[i] > 2.5) pos[i] = -1.0;
      }
      particles.geometry.attributes.position.needsUpdate = true;

      renderer.render(scene, camera);
    };
    animate();

    // 6. GSAP ScrollTrigger timeline to drive cooking stages frame by frame
    const trigger = ScrollTrigger.create({
      trigger: containerRef.current,
      start: 'top top',
      end: '+=400%',
      pin: true,
      scrub: 0.8,
      onUpdate: (self) => {
        const progress = self.progress; // 0 to 1
        setCurrentProgress(progress);

        // Update stage index (0 to 4)
        const stageIdx = Math.min(4, Math.floor(progress * 5));
        setCurrentStageIdx(stageIdx);

        // Interpolate temperature: 93°C -> 230°C -> 160°C
        let temp;
        if (progress < 0.6) {
          temp = Math.round(93 + (progress / 0.6) * (230 - 93));
        } else {
          temp = Math.round(230 - ((progress - 0.6) / 0.4) * (230 - 160));
        }
        setCurrentTemp(temp);

        // STAGE 1: Suspension of cold raw ball (progress 0 - 0.2)
        if (progress <= 0.2) {
          const t = progress / 0.2;
          patty.position.y = 0.5 - t * 1.4; // Drops to grill
          patty.scale.set(0.7, 1.2, 0.7); // Round sphere-like
          patty.material.color.setHex(0x7a2214); // Cold raw red
          patty2.position.y = 1.4 - t * 1.5;
          patty2.scale.set(0.7, 1.2, 0.7);
          press.material.opacity = t * 0.8;
          press.position.y = 2.0 - t * 1.8;
          cheese.material.opacity = 0;
          topBun.material.opacity = 0;
          bottomBun.material.opacity = 0;
        }

        // STAGE 2: The Smash impact & pressure (progress 0.2 - 0.4)
        else if (progress <= 0.4) {
          const t = (progress - 0.2) / 0.2;
          press.position.y = 0.2 - t * 0.95; // Press crashes down
          press.material.opacity = 0.9;
          // Flatten patties into ultra-wide smash disks
          patty.scale.set(0.7 + t * 0.7, 1.2 - t * 0.95, 0.7 + t * 0.7);
          patty.position.y = -0.95;
          patty.material.color.setHex(0x52180e);
          patty2.scale.set(0.7 + t * 0.7, 1.2 - t * 0.95, 0.7 + t * 0.7);
          patty2.position.y = -0.75;
          cheese.material.opacity = 0;
          topBun.material.opacity = 0;
          bottomBun.material.opacity = 0;
        }

        // STAGE 3: Maillard Crust formation & violent sear (progress 0.4 - 0.6)
        else if (progress <= 0.6) {
          const t = (progress - 0.4) / 0.2;
          press.position.y = -0.75 + t * 2.5; // Spatula lifts up
          press.material.opacity = (1 - t) * 0.9;
          // Patties turn deep crispy Maillard brown/black lace
          patty.material.color.setHex(0x240d07);
          patty2.material.color.setHex(0x240d07);
          // Particle color turns fiery amber
          particleMat.color.setHex(0xffaa00);
          particleMat.size = 0.09;
          cheese.material.opacity = 0;
          topBun.material.opacity = 0;
          bottomBun.material.opacity = 0;
        }

        // STAGE 4: Cheese meltdown (progress 0.6 - 0.8)
        else if (progress <= 0.8) {
          const t = (progress - 0.6) / 0.2;
          cheese.material.opacity = Math.min(1, t * 1.5);
          cheese.position.y = -0.65;
          cheese.scale.set(1.0 + t * 0.15, 0.8, 1.0 + t * 0.15);
          press.material.opacity = 0;
          topBun.material.opacity = 0;
          bottomBun.material.opacity = 0;
        }

        // STAGE 5: Final burger assembly & stack explosion (progress 0.8 - 1.0)
        else {
          const t = (progress - 0.8) / 0.2;
          cheese.material.opacity = 1;
          // Bottom bun enters from below
          bottomBun.material.opacity = Math.min(1, t * 2);
          bottomBun.position.y = -1.8 + t * 0.65; // settles at -1.15
          patty.position.y = -0.95 + t * 0.1;
          patty2.position.y = -0.75 + t * 0.1;
          cheese.position.y = -0.65 + t * 0.1;
          // Top bun drops into final lock position
          topBun.material.opacity = Math.min(1, t * 2);
          topBun.position.y = 2.5 - t * 2.85; // settles at -0.35
        }
      },
    });

    // Resize handler
    const handleResize = () => {
      if (!canvas || !canvas.parentElement) return;
      const newWidth = canvas.parentElement.clientWidth;
      const newHeight = canvas.parentElement.clientHeight;
      camera.aspect = newWidth / newHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(newWidth, newHeight);
    };
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      cancelAnimationFrame(animationFrameId);
      trigger.kill();
      renderer.dispose();
    };
  }, []);

  const activeStage = SCROLLYTELLING_STAGES[currentStageIdx];

  return (
    <section
      ref={containerRef}
      id="scrolly"
      className="relative w-full h-screen bg-[#0a0a0a] text-white overflow-hidden grid-pattern"
    >
      {/* 3D WebGL Canvas Layer */}
      <div className="absolute inset-0 z-10 flex items-center justify-center">
        <canvas ref={canvasRef} className="w-full h-full block cursor-grab active:cursor-grabbing" />
      </div>

      {/* Crosshairs & HUD Border Frame */}
      <div className="absolute inset-6 z-20 pointer-events-none border border-[#222222]">
        {/* Corners */}
        <div className="absolute -top-1 -left-1 w-3 h-3 border-t-2 border-l-2 border-[#ff5500]" />
        <div className="absolute -top-1 -right-1 w-3 h-3 border-t-2 border-r-2 border-[#ff5500]" />
        <div className="absolute -bottom-1 -left-1 w-3 h-3 border-b-2 border-l-2 border-[#ff5500]" />
        <div className="absolute -bottom-1 -right-1 w-3 h-3 border-b-2 border-r-2 border-[#ff5500]" />

        {/* Top Telemetry Header Inside HUD */}
        <div className="absolute top-4 left-6 flex items-center gap-4 text-xs font-mono-telemetry">
          <span className="text-[#ff5500] bg-[#1a0800] border border-[#ff5500]/40 px-2 py-0.5 font-bold">
            THERMAL SCROLLYTELLING
          </span>
          <span className="hidden sm:inline text-[#888888]">
            CHOP_ID: #BEEF-8020
          </span>
        </div>

        <div className="absolute top-4 right-6 flex items-center gap-3 text-xs font-mono-telemetry">
          <div className="flex items-center gap-2 bg-[#111111] border border-[#333333] px-3 py-1">
            <Thermometer className="w-3.5 h-3.5 text-[#ff5500] animate-pulse" />
            <span className="text-[#888888]">GRILL TEMP:</span>
            <span className="text-white font-bold text-sm">{currentTemp}°C</span>
          </div>
        </div>
      </div>

      {/* Floating Active Stage Telemetry Card */}
      <div className="absolute bottom-28 left-6 sm:left-12 z-20 max-w-lg bg-[#0d0d0d]/90 backdrop-blur-md border border-[#ff5500]/40 p-6 glow-amber">
        <div className="flex items-center justify-between gap-4 mb-2">
          <span className="text-[11px] font-mono-telemetry text-[#ffaa00] uppercase tracking-wider font-bold">
            {activeStage.time} // {activeStage.title}
          </span>
          <span className="text-[10px] font-mono-telemetry bg-[#ff5500] text-black font-extrabold px-1.5 py-0.5">
            STAGE 0{activeStage.step}/05
          </span>
        </div>

        <h2 className="font-condensed-heading text-2xl sm:text-3xl text-white uppercase tracking-tight mb-2">
          {activeStage.title.split('//')[1] || activeStage.title}
        </h2>

        <p className="text-xs sm:text-sm text-[#aaaaaa] font-light leading-relaxed mb-4">
          {activeStage.subtitle}
        </p>

        <div className="pt-3 border-t border-[#222222] flex items-center justify-between text-[10px] font-mono-telemetry text-[#777777]">
          <span>{activeStage.hudDetail}</span>
          <span className="text-[#ff5500]">STATUS: CALIBRATED</span>
        </div>
      </div>

      {/* Bottom Scrubber Timeline with Ticks & Scroller Indicator */}
      <div className="absolute bottom-6 left-6 right-6 z-20 flex flex-col gap-2">
        <div className="flex items-center justify-between text-[10px] font-mono-telemetry text-[#777777] px-1">
          <span>00:00s // RAW SPHERE</span>
          <span>00:07s // THE SMASH</span>
          <span>00:45s // MAILLARD 230°C</span>
          <span>01:20s // CHEESE MELT</span>
          <span>02:10s // STACK LAUNCH</span>
        </div>

        {/* Timeline Scrubber Track */}
        <div className="relative w-full h-5 bg-[#141414] border border-[#2a2a2a] timeline-scrubber flex items-center px-1">
          {/* Progress fill bar */}
          <div
            className="absolute top-0 left-0 bottom-0 bg-[#ff5500]/25 border-r-2 border-[#ffaa00] transition-all duration-75"
            style={{ width: `${Math.min(100, Math.max(0, currentProgress * 100))}%` }}
          />

          {/* Needle / Scrubber Thumb */}
          <div
            className="absolute top-0 bottom-0 w-2 bg-[#ff5500] shadow-[0_0_12px_#ff5500] -translate-x-1/2 pointer-events-none transition-all duration-75 flex items-center justify-center"
            style={{ left: `${Math.min(100, Math.max(0, currentProgress * 100))}%` }}
          >
            <div className="w-0.5 h-full bg-white" />
          </div>
        </div>
      </div>

      {/* Floating Scroll Prompt */}
      {currentProgress < 0.05 && (
        <div className="absolute top-1/2 right-8 -translate-y-1/2 z-20 hidden md:flex flex-col items-center gap-2 text-[#777777] font-mono-telemetry text-[10px]">
          <span className="rotate-90 uppercase tracking-widest text-[#ffaa00]">
            SCROLL TO SMASH
          </span>
          <ChevronDown className="w-4 h-4 text-[#ff5500] animate-bounce mt-4" />
        </div>
      )}
    </section>
  );
}
