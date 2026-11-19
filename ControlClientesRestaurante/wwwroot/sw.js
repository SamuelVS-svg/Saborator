// Saborator Service Worker — PWA & Soporte Offline para Tablets de Salón y Cocina
const CACHE_NAME = 'saborator-pwa-v2';
const STATIC_ASSETS = [
    '/',
    '/css/site.css',
    '/css/dashboard-v12.css',
    '/css/login.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/site.webmanifest',
    '/img/logo-burger.svg',
    '/img/logo-burger.png',
    '/img/logo-burger-gold.png'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(STATIC_ASSETS).catch(err => {
                console.warn('Pre-cache parcial completado con advertencias:', err);
            });
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) {
                        return caches.delete(key);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    // Para peticiones POST o APIs no almacenar en caché, ir directo a la red
    if (event.request.method !== 'GET' || event.request.url.includes('/api/') || event.request.url.includes('/CocinaApi')) {
        return;
    }

    event.respondWith(
        fetch(event.request)
            .then(networkResponse => {
                // Solo se guardan recursos estáticos: las páginas (pedidos, pagos, comprobantes) pueden traer datos privados
                if (networkResponse && networkResponse.status === 200 && networkResponse.type === 'basic' && event.request.mode !== 'navigate') {
                    const responseClone = networkResponse.clone();
                    caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, responseClone);
                    });
                }
                return networkResponse;
            })
            .catch(() => {
                // Fallback a caché local si la red falla o está offline
                return caches.match(event.request).then(cachedResponse => {
                    if (cachedResponse) {
                        return cachedResponse;
                    }
                    if (event.request.mode === 'navigate') {
                        // Sin conexión: avisar en vez de mostrar el inicio bajo la dirección de otra página
                        return new Response(
                            '<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Sin conexión</title>' +
                            '<body style="font-family:sans-serif;text-align:center;padding:3rem 1rem"><h1>Sin conexión</h1><p>No pudimos cargar esta página. Revisa tu internet e inténtalo de nuevo.</p>',
                            { status: 503, headers: { 'Content-Type': 'text/html; charset=utf-8' } });
                    }
                    return Response.error();
                });
            })
    );
});
