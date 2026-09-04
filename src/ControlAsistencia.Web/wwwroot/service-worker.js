const CACHE_NAME = "control-asistencia-pwa-v1";

self.addEventListener("install", function () {
    self.skipWaiting();
});

self.addEventListener("activate", function (event) {
    event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", function (event) {
    if (event.request.method !== "GET") {
        return;
    }

    // No se cachean páginas ni datos para evitar información de asistencia obsoleta.
    event.respondWith(fetch(event.request));
});
