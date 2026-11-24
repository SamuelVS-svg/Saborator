// Google Analytics 4: el identificador llega en data-ga-id de la etiqueta <script>.
var _gaId = document.currentScript && document.currentScript.dataset.gaId;
window.dataLayer = window.dataLayer || [];
function gtag(){dataLayer.push(arguments);}
gtag('js', new Date());
if (_gaId) gtag('config', _gaId);
