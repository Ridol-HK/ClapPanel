# Registro de cambios

[English](CHANGELOG.en.md) | [Español](CHANGELOG.md)

Todos los cambios relevantes de ClapPanel se documentan en este archivo.

## [Unreleased]

## [1.6.0] - 2026-11-22

### Añadido

- Detección visual de accesos no disponibles: comprueba los destinos de aplicaciones, archivos y carpetas con ruta; los accesos rotos se identifican con un indicador de advertencia.
- Menú contextual con clic derecho sobre cada acceso para editarlo (abre Ajustes → Iconos y carpetas), duplicarlo, eliminarlo con confirmación o abrir su ubicación en el Explorador.
- Opción en Ajustes → General para cerrar automáticamente el panel después de ejecutar correctamente una aplicación o archivo. Está desactivada de forma predeterminada.
- Nueva pestaña «Estado del sistema» en Ajustes, con estado de captura/detector, micrófono, atajo global, configuración, imágenes personalizadas, copias de seguridad, inicio con Windows, versión y cantidad de destinos no disponibles.
- Localización al español e inglés de las nuevas opciones, acciones y estados.

### Cambiado

- La versión del proyecto pasa a 1.6.0.

## [1.5.1] - 2026-11-22

### Añadido

- Exportación de toda la configuración a un archivo ZIP portable, con accesos directos, categorías, carpetas anidadas, perfiles visuales e imágenes personalizadas.
- Importación de una configuración completa desde un ZIP, con remapeo de las rutas de las imágenes al directorio local de ClapPanel.
- Importación exclusiva de accesos directos: reemplaza los elementos, categorías, carpetas e imágenes, sin cambiar la apariencia ni las demás preferencias.
- Copias de seguridad locales rotativas (hasta 20 archivos) antes de guardar cambios importantes de configuración, importar o restablecer los valores predeterminados; las escrituras frecuentes de posición, tamaño, categoría y notas no generan copias repetidas.
- Restauración manual de la copia local más reciente y recuperación automática desde la última copia válida cuando `settings.json` está dañado o no se puede leer.
- Validación del contenido de los archivos ZIP y de las rutas de las imágenes para evitar entradas de archivo inseguras.

### Cambiado

- La versión del proyecto pasa a 1.5.1.

## [1.5.0] - 2026-11-22

### Añadido

- Monitor del micrófono en tiempo real: nivel máximo y RMS de la señal de entrada sin filtrar, frecuencia de muestreo, cantidad de canales y nivel procesado por el detector.
- Diagnóstico de captura: informa si el dispositivo está activo, si no llega señal o si Windows impide iniciar la captura; incluye una acción para probar o cambiar temporalmente el micrófono.
- Atajo de teclado global configurable para abrir o cerrar el panel desde cualquier aplicación. Permite activar/desactivar la función y elegir modificadores (Ctrl, Alt, Shift y Win) y una tecla; el valor predeterminado es Ctrl + Alt + Espacio.
- Asistente de primera ejecución con pasos de bienvenida, selección y prueba del micrófono, ajuste de sensibilidad y configuración del atajo global e inicio con Windows. Se puede omitir; las instalaciones existentes no vuelven a mostrarlo al actualizar.
- Personalización del acople inteligente: cantidad máxima de accesos visibles (1–12), prioridad opcional de favoritos y ancho de la zona de activación de borde (4–40 px). Los favoritos se priorizan manteniendo el orden manual dentro de cada grupo.
- La navegación entre accesos mediante las flechas del teclado reinicia el contador de inactividad: el panel permanece opaco mientras el usuario continúa navegando, aunque el cursor esté fuera.

### Cambiado

- La versión del proyecto pasa a 1.5.0.

## [1.4.3] - 2026-11-22

### Añadido

- Opción para recordar la última categoría seleccionada y restaurarla al volver a abrir el panel.
- Posibilidad de asignar un mismo acceso directo a varias categorías; aparece al seleccionar cualquiera de ellas.
- Tiempo configurable (0–5 segundos) antes de aplicar la opacidad reducida cuando el cursor está fuera del panel.
- Animación opcional de desvanecimiento suave (220 ms) al cambiar entre la opacidad activa e inactiva; se puede desactivar para aplicar el cambio al instante.
- Reordenamiento ampliado mediante arrastrar y soltar: se pueden insertar accesos antes o después de otros en el panel y ordenar las listas de Ajustes → Iconos y carpetas y el editor de carpetas anidadas. Soltar un acceso directamente sobre una carpeta sigue permitiendo moverlo dentro de ella.
- Menú de acople inteligente limitado a los primeros seis accesos según el orden de la lista. Se muestran en una columna vertical al acoplar a izquierda o derecha y en una fila horizontal al acoplar arriba o abajo; sin un borde fijo, se utiliza la disposición horizontal.

### Cambiado

- La versión del proyecto pasa a 1.4.3.

## [1.4.0] - 2026-11-22

### Añadido

- Navegación con teclado en el panel: flechas para moverse, Intro para abrir, teclas 1-9 y 0 para elegir uno de los diez primeros iconos, `/` o Ctrl+F para buscar (también al escribir una letra), Inicio y Fin para ir al primer o último icono, Retroceso o Alt+Izquierda para volver de una carpeta y Esc para limpiar la búsqueda o cerrar.
- Números de atajo en los iconos (opcional) y foco de teclado visible en el panel y en Ajustes.
- Accesibilidad: nombres para lectores de pantalla (icono, carpeta, favorito, categoría, no disponible y atajo), anuncio de resultados y modo de alto contraste propio. Si Windows tiene un tema de alto contraste activo, ClapPanel lo usa automáticamente.
- Selector de aplicaciones instaladas al añadir o editar un icono: lista las aplicaciones del menú Inicio y de la Tienda de Microsoft, con buscador.
- Widgets opcionales en la parte superior del panel: reloj (24 o 12 horas, segundos y fecha), notas rápidas con guardado automático, portapapeles (historial de texto solo en memoria) y control de reproducción (anterior, pausa y siguiente).
- Interfaz en español e inglés, con selector de idioma en Ajustes → General (sistema, español o English).
- Ventana «Acerca de» con versión, licencia, créditos de terceros y nota de privacidad, accesible desde la bandeja y desde Ajustes → General.
- `THIRD-PARTY-NOTICES.md` con los avisos de NAudio y de .NET.
- Pruebas de idioma (incluida una que comprueba que todas las claves usadas existen), teclado, widgets, aplicaciones instaladas, accesibilidad y Acerca de.

### Cambiado

- Esc cierra el panel en todos los modos (antes solo en pantalla completa); si hay texto en la búsqueda, primero lo limpia.
- Al abrir el panel, el foco queda en el primer icono para poder usar el teclado de inmediato.
- Cambiar el idioma cierra el panel si estaba abierto; la próxima apertura ya usa el idioma elegido.
- La versión del proyecto pasa a 1.4.0.

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

[1.6.0]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.6.0
[1.5.1]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.5.1
[1.5.0]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.5.0
[1.4.3]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.4.3
[1.4.0]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.4.0
[1.3.1]: https://github.com/Ridol-HK/ClapPanel/releases/tag/v1.3.1
