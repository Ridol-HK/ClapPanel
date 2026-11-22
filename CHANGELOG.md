# Registro de cambios

Todos los cambios relevantes de ClapPanel se documentan en este archivo.

## [1.3.1] - 2026-11-22

Esta entrada agrupa todo lo añadido desde la 1.1.0 (versiones 1.2.x y 1.3.x).

### Añadido

- Organización de aplicaciones: carpetas internas con subcarpetas, categorías, favoritos y buscador que también recorre carpetas anidadas.
- Filtros del panel por categoría y por favoritos.
- Arrastrar y soltar: reordenar iconos, moverlos entre carpetas y crear accesos directos soltando ejecutables, accesos directos, archivos o carpetas de Windows sobre el panel.
- Panel móvil: arrastre desde la barra superior, acoplamiento a los bordes con disposición vertical de una columna, y redimensionado por bordes y esquinas con tamaños guardados por monitor.
- Selección de monitor y conservación de la posición manual de cada pantalla.
- Modo de pantalla completa para ocupar todo el monitor seleccionado.
- Ocultamiento automático configurable entre 1 y 30 segundos.
- Opacidad base con transparencia dinámica cuando el puntero no está sobre el panel.
- Animaciones configurables: ninguna, desvanecer, escala, deslizar, entrada desde arriba, zoom con rebote suave y rebote ligero.
- Perfiles visuales que guardan y restauran la apariencia, las columnas y el modo de pantalla completa.
- Botón «⋯» del panel que abre directamente la pestaña de apariencia.
- Ventana de ajustes con la misma paleta que el panel y vista previa que se revierte al cancelar.
- En Ajustes → General: permitir o bloquear el movimiento del panel, detener la captura del micrófono, restablecer valores y cerrar ClapPanel.
- Pruebas para `PanelItem`.
- Flujos de GitHub Actions para compilar y probar, y para publicar el ejecutable al crear una etiqueta `v*`.
- Registro de cambios y notas de la versión.

### Cambiado

- La versión del proyecto pasa a 1.3.1.
- README reorganizado: instalación, uso, ajustes, calibración, detección y estructura del proyecto.
- `.gitignore` ampliado y `.gitattributes` añadido.

## [1.1.0] - 2026-11-21

### Añadido

- Personalización visual del panel: tema, colores de fondo y acento, opacidad, tamaño de iconos, nombres, esquinas redondeadas, posición y animación, con vista previa en vivo.
- Calibración asistida del micrófono en tres pasos, verificada con el detector real.
- Ajustes organizados en pestañas.

## [1.0.0] - 2026-11-21

### Añadido

- Detección de doble aplauso con filtro de graves, pico seco, silencio previo y entre aplausos, fuerza similar y umbral adaptativo al ruido ambiente.
- Panel con iconos configurables que abren aplicaciones, con imagen propia, argumentos y reordenación por arrastre.
- Icono en la bandeja con menú para abrir el panel, ajustes, pausar y salir.
- Selección del micrófono, sensibilidad e inicio opcional con Windows.
- Instancia única y configuración en JSON dentro del perfil del usuario.
- Licencia GPL-3.0 o posterior.
