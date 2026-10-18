// Datos Maestros del Restaurante Saborator — Barranquilla
// Fácilmente editable para precios, descripciones y configuraciones de contacto.

export const RESTAURANT_INFO = {
  name: "SABORATOR",
  subname: "ALTA COCINA & BURGERS DE AUTOR",
  city: "BARRANQUILLA, COLOMBIA",
  address: "Cra 4 Sur # 50D-107, Barranquilla",
  phone: "+57 322 525 4289",
  whatsappNumber: "573225254289",
  hours: "Miércoles a Domingo: 5:00 PM — 11:30 PM",
  instagram: "@saborator.baq",
  tagline: "El punto exacto de la técnica, el fuego y la alta cocina contemporánea.",
  videoHeroPlaceholder: "/videos/hero.mp4",
  videoProcessPlaceholder: "/videos/proceso.mp4",
};

export const MENU_LINEUP = [
  {
    id: "burger-truffle",
    number: "01",
    name: "SMOKED BURGUNDY BURGER",
    tag: "SIGNATURE DISH",
    description: "200g de corte Certified Angus Beef macerado en reducción de vino borgoña, queso gouda ahumado, cebolla caramelizada lenta y pan brioche artesanal tostado a la mantequilla noisette.",
    price: 34000,
    formattedPrice: "$34.000 COP",
    image: "/img/hamburguesa.jpg",
    specs: ["200g Angus Beef", "Vino Borgoña", "Brioche Artesanal", "Gouda Fundido"],
  },
  {
    id: "perro-caliente-auteur",
    number: "02",
    name: "PERRO DE AUTOR // CHARCUTERIE",
    tag: "CHEF'S SPECIAL",
    description: "Salchicha artesanal de res madurada y especiada, crujiente de tocineta ahumada con leña de manzano, emulsión de ajo negro confitado y papas hilo crujientes.",
    price: 28000,
    formattedPrice: "$28.000 COP",
    image: "/img/perrocaliente.jpg",
    specs: ["Salchicha Madurada", "Ajo Negro", "Bacon Crunch", "Pan de Papa"],
  },
  {
    id: "salchipapa-truffle",
    number: "03",
    name: "SALCHIPAPA ROYALE // TRUFFLE CUT",
    tag: "BESTSELLER",
    description: "Papas rústicas doble cocción crujientes al romero, cortes seleccionados de salchicha Frankfurt, costilla ahumada desmechada, queso costeño fundido y alioli de trufa blanca.",
    price: 36000,
    formattedPrice: "$36.000 COP",
    image: "/img/salchipapa.jpg",
    specs: ["Doble Cocción", "Alioli Trufado", "Costilla Ahumada", "Queso Fundido"],
  },
  {
    id: "french-fries-rustic",
    number: "04",
    name: "PAPAS RÚSTICAS // HERB CRUST",
    tag: "SIDE SUPREME",
    description: "Papas francesas de corte grueso con sal marina en escamas, pimienta negra triturada al instante, romero fresco y dip artesanal de reducción de borgoña.",
    price: 16000,
    formattedPrice: "$16.000 COP",
    image: "/img/papasfrancesa.jpg",
    specs: ["Sal de Mar", "Romero Fresco", "Corte Grueso", "Dip Borgoña"],
  },
];

export const BUILDER_OPTIONS = {
  bases: [
    { id: "b1", name: "Doble Angus 200g", price: 18000, description: "Carne fresca premium smash grill" },
    { id: "b2", name: "Pechuga Grill Confitada", price: 15000, description: "Marinado de finas hierbas" },
    { id: "b3", name: "Costilla Ahumada Desmechada", price: 19000, description: "Cocción lenta 12 horas" },
  ],
  breads: [
    { id: "br1", name: "Brioche Francés Artesanal", price: 4000, description: "Glaseado a mantequilla noisette" },
    { id: "br2", name: "Pan de Papa & Romero", price: 4500, description: "Miga ultra suave y perfumada" },
    { id: "br3", name: "Bowl Verde (Sin Pan)", price: 2000, description: "Cama de lechugas hidropónicas" },
  ],
  cheeses: [
    { id: "c1", name: "Gouda Ahumado Holandés", price: 5000 },
    { id: "c2", name: "Cheddar Añejo Fundido", price: 4500 },
    { id: "c3", name: "Queso Costeño Dorado", price: 4000 },
  ],
  extras: [
    { id: "e1", name: "Tocineta Caramelizada", price: 4500 },
    { id: "e2", name: "Cebollas Reducción Borgoña", price: 3500 },
    { id: "e3", name: "Huevo Trufado", price: 3500 },
    { id: "e4", name: "Pepinillos Encurtidos Casa", price: 2500 },
  ],
  drinks: [
    { id: "d1", name: "Coca-Cola Original Fría", price: 6000, image: "/img/cocacola.png" },
    { id: "d2", name: "Pepsi Black / Regular", price: 5500, image: "/img/pepsi.webp" },
    { id: "d3", name: "Agua Manantial Cristalina", price: 4500, image: "/img/agua.webp" },
  ],
};

export const STATS_DATA = [
  { value: "100%", label: "CARNE FRESCA CERTIFICADA", detail: "Sin conservantes ni congelados" },
  { value: "280°C", label: "TEMPERATURA DE SELLADO", detail: "Reacción de Maillard milimétrica" },
  { value: "12H", label: "AHUMADO EN LEÑA", detail: "Maderas nobles seleccionadas" },
  { value: "+12K", label: "EXPERIENCIAS SERVIDAS", detail: "En el corazón de Barranquilla" },
];

export const PROCESS_STEPS = [
  {
    step: "01",
    title: "LA SELECCIÓN RIGUROSA",
    text: "Seleccionamos únicamente cortes de carne fresca y tubérculos locales de cosechas seleccionadas en el Caribe colombiano.",
  },
  {
    step: "02",
    title: "EL FUEGO Y LA REACCIÓN",
    text: "El sellado a 280°C sella los jugos interiores al instante, logrando una costra caramelizada inigualable de sabor profundo.",
  },
  {
    step: "03",
    title: "LA REDUCCIÓN BORGOÑA",
    text: "Nuestras salsas madre toman más de 8 horas de hervor pausado con vino borgoña, especias frescas y mantequilla pura.",
  },
  {
    step: "04",
    title: "EL MONTAJE CINEMATOGRÁFICO",
    text: "Cada plato se sirve en su cenit térmico: brillante, aromático y con una arquitectura impecable en tu mesa.",
  },
];

export const INGREDIENTS_GALLERY = [
  {
    name: "Cortes Angus Madurados",
    origin: "Selección Premium",
    desc: "Equilibrio 80/20 de grasa intramuscular que garantiza jugosidad extrema.",
    accent: "Protagonista",
  },
  {
    name: "Reducción de Vino Borgoña",
    origin: "Receta de Autor",
    desc: "Cuerpo aterciopelado con notas de frutos rojos, roble y pimienta negra.",
    accent: "Identidad",
  },
  {
    name: "Pan Brioche de Alta Fermentación",
    origin: "Taller Panadero Local",
    desc: "24 horas de fermentación lenta, tostado en mantequilla clarificada noisette.",
    accent: "Artesanal",
  },
  {
    name: "Papas Doble Cocción",
    origin: "Cosecha Fresca",
    desc: "Escaldadas a baja temperatura y terminadas al fuego vivo con romero fresco.",
    accent: "Textura",
  },
  {
    name: "Emulsión de Ajo Negro Confitado",
    origin: "Cocina Lenta",
    desc: "Toques umami intensos con notas balsámicas y dulzura natural equilibrada.",
    accent: "Firma",
  },
];

export const STORY_DATA = {
  quote: "«En Saborator no cocinamos comida rápida: diseñamos momentos que desafían el estándar gastronómico de Barranquilla.»",
  author: "Chef Creativo — Saborator",
  paragraphs: [
    "Nacimos bajo la convicción de que la comida casual merecía el mismo rigor técnico, estética y respeto por el ingrediente que los mejores restaurantes de mantel largo.",
    "Desde Barranquilla para los paladares más exigentes: combinamos técnicas de alta charcutería, ahumados lentos y salsas reducidas con una puesta en escena audaz y contemporánea.",
  ],
  milestones: [
    { year: "2023", title: "El Primer Fuego", desc: "Laboratorio de pruebas culinarias y formulación del marinado Borgoña." },
    { year: "2024", title: "Apertura en Barranquilla", desc: "El punto de encuentro para amantes del buen comer." },
    { year: "2026", title: "Consolidación de Autor", desc: "Reconocido por su arquitectura de sabores y diseño de experiencia." },
  ],
};
