// Telemetría & Datos Maestros — SMASH // THE SEAR
// Estética técnica HUD / brutalista oscura

export const BRAND_CONFIG = {
  name: "SMASH // THE SEAR",
  tagline: "HIGH-CRUST ARTISANAL SMASH BURGERS // THERMAL PRECISION",
  location: "BARRANQUILLA CORE LAB // CARIBE TERMINAL",
  address: "Cra 4 Sur # 50D-107, Barranquilla",
  phone: "+57 322 525 4289",
  whatsappNumber: "573225254289",
  telemetryStatus: "SYSTEM ONLINE // MAILLARD REACTION ACTIVE",
};

export const SCROLLYTELLING_STAGES = [
  {
    step: 1,
    time: "00:00s",
    temp: 93,
    title: "STAGE 01 // THE BALL & SUSPENSION",
    subtitle: "2x 102g Cortes seleccionados Angus Black. Esfera fría a 4°C, recién molida en proporción 80/20.",
    hudDetail: "MASS: 204G // FAT RATIO: 20.4% // COLD CORE",
  },
  {
    step: 2,
    time: "00:07s",
    temp: 145,
    title: "STAGE 02 // THE ULTRA-SMASH PRESSURE",
    subtitle: "Impacto con prensa de acero sólido de 4.2 kg contra la plancha de cromo duro. Espesor milimétrico.",
    hudDetail: "FORCE: 120 PSI // TIME: 7.2 SEC // FULL FLATTEN",
  },
  {
    step: 3,
    time: "00:45s",
    temp: 230,
    title: "STAGE 03 // MAILLARD CRUST FORMATION",
    subtitle: "Caramelización violenta de azúcares y aminoácidos a 230°C. Costra crujiente de encaje de alta fidelidad.",
    hudDetail: "MAILLARD RATE: 99.4% // CRUNCH LEVEL: PEAK // SMOKE ACTIVE",
  },
  {
    step: 4,
    time: "01:20s",
    temp: 185,
    title: "STAGE 04 // AMERICAN CHEESE MELTDOWN",
    subtitle: "Doble lámina de queso americano emulsionado al vapor de mantequilla, fusionándose con la costra.",
    hudDetail: "VISCOSITY: FLUID // TEMP DROP: 185°C // VAPOR DOME",
  },
  {
    step: 5,
    time: "02:10s",
    temp: 160,
    title: "STAGE 05 // FINAL ASSEMBLY & LAUNCH",
    subtitle: "Brioche dorado con mantequilla clarificada, pickles de la casa encurtidos al eneldo y salsa secreta Sear-01.",
    hudDetail: "ARCHITECTURE COMPLETE // HEAT RETENTION: 100% // READY FOR CONSUMPTION",
  },
];

export const LINEUP_BURGER_PRODUCTS = [
  {
    id: "sear-atomic",
    code: "SKU-001 // THE ATOMIC CRUST",
    name: "DOUBLE SMASH // ATOMIC SEAR",
    description: "2x 100g de carne Certified Angus con sellado de encaje crocante extremo, triple queso americano fundido, cebollas prensadas ultrafinas a la plancha y salsa Sear Umami.",
    price: 32000,
    formattedPrice: "$32.000 COP",
    image: "/img/hamburguesa.jpg",
    specs: ["2x 100g Angus", "Crust Maillard 230°C", "Triple American", "Brioche Potato"],
    badge: "BENCHMARK 01",
  },
  {
    id: "truffle-brutal",
    code: "SKU-002 // BLACK TRUFFLE MELT",
    name: "TRUFFLE SMASH // BLACK LAB",
    description: "Doble disco smash ultracrujiente con reducción de hongos silvestres, queso gouda ahumado en leña de cerezo, tocineta glaseada en maple-bourbon y alioli trufado.",
    price: 36000,
    formattedPrice: "$36.000 COP",
    image: "/img/salchipapa.jpg",
    specs: ["Truffle Emulsion", "Double Smash 200g", "Smoked Gouda", "Bourbon Bacon"],
    badge: "CHEF LAB",
  },
  {
    id: "jalapeno-fire",
    code: "SKU-003 // THERMAL OVERLOAD",
    name: "SPICY SMASH // INFERNO GRID",
    description: "Doble carne prensada sobre jalapeños asados al carbón, pepper jack cheese fundido, cebolla frita crujiente y reducción de chiles fermentados con miel de abeja.",
    price: 34000,
    formattedPrice: "$34.000 COP",
    image: "/img/perrocaliente.jpg",
    specs: ["Fire Jalapeño", "Pepper Jack", "Crispy Onions", "Hot Honey Glaze"],
    badge: "HIGH HEAT",
  },
  {
    id: "fries-crust",
    code: "SKU-004 // CRYOGENIC CRUNCH",
    name: "SMASH LOADED FRIES // SEAR CUT",
    description: "Papas corte rustico fritas en triple cocción con grasa de res wagyu, salchicha artesanal Frankfurt en rodajas, salsa de queso fundido y carne smash desmenuzada.",
    price: 24000,
    formattedPrice: "$24.000 COP",
    image: "/img/papasfrancesa.jpg",
    specs: ["Triple Fry", "Wagyu Tallow", "Cheese Fondue", "Smash Crumbles"],
    badge: "SIDE WEAPON",
  },
];

export const BUILDER_CUSTOM_SPECS = {
  patties: [
    { id: "p1", name: "Single Smash (1x 102g)", price: 14000, grams: 102 },
    { id: "p2", name: "Double Smash (2x 102g)", price: 22000, grams: 204 },
    { id: "p3", name: "Triple Smash Overkill (3x 102g)", price: 29000, grams: 306 },
  ],
  buns: [
    { id: "b1", name: "Brioche de Papa Martin's Style", price: 4000, tag: "LIGHT TOAST" },
    { id: "b2", name: "Pan Artesanal Mantequilla Noisette", price: 4500, tag: "DEEP CRUST" },
    { id: "b3", name: "Protein Wrap (Sin Carbohidratos)", price: 2500, tag: "LOW CARB" },
  ],
  cheeses: [
    { id: "c1", name: "Doble American Kraft Melt", price: 4000 },
    { id: "c2", name: "Gouda Holandés Ahumado", price: 5000 },
    { id: "c3", name: "Pepper Jack Picante", price: 4500 },
  ],
  toppings: [
    { id: "t1", name: "Cebollas Smash Prensadas", price: 2500 },
    { id: "t2", name: "Tocineta Glaseada en Maple", price: 4500 },
    { id: "t3", name: "Pickles Casa Fermentados", price: 2000 },
    { id: "t4", name: "Jalapeños Asados al Fuego", price: 2500 },
  ],
  sauces: [
    { id: "s1", name: "Salsa Secreta Sear-01", price: 2000 },
    { id: "s2", name: "Alioli de Ajo Negro & Trufa", price: 3500 },
    { id: "s3", name: "Mayo Chipotle Ahumada", price: 2000 },
  ],
  beverages: [
    { id: "d1", name: "Coca-Cola Vidrio Helada", price: 6000, image: "/img/cocacola.png" },
    { id: "d2", name: "Pepsi Black Fría", price: 5500, image: "/img/pepsi.webp" },
    { id: "d3", name: "Agua Manantial Cristalina", price: 4500, image: "/img/agua.webp" },
  ],
};

export const SEAR_TELEMETRY_STATS = [
  { id: "mass", value: "102", unit: "GRAMS", label: "PESO POR DISCO", note: "Densidad calibrada para máxima superficie de contacto" },
  { id: "force", value: "7.2", unit: "SECONDS", label: "TIEMPO DE PRENSADO", note: "Presión sostenida de 120 PSI sobre el acero" },
  { id: "temp", value: "230", unit: "°CELSIUS", label: "PUNTO DE REACCIÓN MAILLARD", note: "Temperatura de superficie para caramelización instantánea" },
  { id: "crust", value: "0.8", unit: "MM", label: "ESPESOR DEL ENCAJE CROCANTE", note: "Micro-borde crocante con sabor concentrado" },
];
