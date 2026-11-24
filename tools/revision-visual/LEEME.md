# Revisión visual automática

Recorre 31 páginas (visitante y administrador) en modo claro y oscuro y avisa de: páginas que no cargan,
errores de consola o JavaScript, desborde horizontal, texto con poco contraste y cambios respecto a las
capturas de referencia. Sirve para detectar un error de colores **antes** de subirlo.

## Primera vez
```
cd tools/revision-visual
npm install
npx playwright install chromium
```

## Uso (con la aplicación corriendo, por ejemplo en http://localhost:5106)
```
node revisar.js                    # revisa y compara con la referencia
node revisar.js --actualizar-base  # guarda las capturas actuales como referencia nueva
node revisar.js --movil            # lo mismo en pantalla de celular (390 px)
```
Opciones: `--base=http://localhost:5106`, `--contraste=3`, `--tolerancia=0.2`, `--usuario=admin --clave=...`
(o las variables `SABORATOR_USUARIO` y `SABORATOR_CLAVE`). Termina con código 1 si encuentra problemas.

- Las capturas nuevas quedan en `salida/` y, si algo cambió, también la imagen `*.diferencia.png`.
- La primera ejecución guarda la referencia; después compara contra ella. Cuando un cambio sea intencional,
  corre `--actualizar-base`.
- `baseline/` y `salida/` no se suben al repositorio: cada persona genera las suyas.
- Usa el usuario de demostración `admin`. Si cambiaste su clave, pásala con `--clave`.
- El contraste solo mide texto visible que nadie tapa y avisa por debajo de 3:1. Puede dar falsos positivos
  con fondos de imagen, así que revisa la captura antes de "corregir".
