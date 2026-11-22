# ClapPanel

Aplicación para Windows 10/11 que abre y cierra un panel de accesos directos al detectar un doble aplauso.

## Requisitos

- .NET 10 SDK

## Compilar y ejecutar

```
dotnet run --project src/ClapPanel
```

## Probar

```
dotnet test
```

## Publicar

```
dotnet publish src/ClapPanel -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Uso

- Doble aplauso: abre el panel; con el panel abierto, lo cierra.
- Icono de bandeja: abrir o cerrar panel, abrir ajustes, pausar la captura del micrófono y salir.
- Clic en un icono: abre su destino sin cerrar el panel. Las carpetas internas de ClapPanel se despliegan dentro del mismo panel.
- Botón «⋯»: abre directamente la pestaña de apariencia.
- Barra superior «ClapPanel»: permite arrastrar el panel cuando el movimiento está habilitado. Al acercarlo a los bordes izquierdo o derecho, se acopla y cambia a una disposición vertical de una columna.
- Bordes y esquinas del panel: permiten cambiar su tamaño arrastrando el cursor; los tamaños horizontal y vertical se guardan por monitor.
- Arrastrar un icono sobre otro lo reordena; arrastrarlo sobre una carpeta lo mueve a esa carpeta. Soltarlo en el espacio vacío lo lleva al final de su lista.
- Arrastrar ejecutables, accesos directos, archivos o carpetas de Windows al panel crea accesos directos. Al soltar sobre una carpeta interna, el acceso se agrega a esa carpeta.

## Organización de aplicaciones

En Ajustes → Iconos y carpetas se pueden agregar y editar aplicaciones y carpetas internas, asignar una categoría, marcar favoritos y elegir una imagen personalizada. Las carpetas admiten subcarpetas y aplicaciones. En el panel:

- El buscador encuentra nombres, destinos y categorías, incluso dentro de carpetas anidadas.
- El selector de categorías y el filtro de favoritos también incluyen elementos dentro de carpetas.
- Las carpetas internas se abren en el panel y tienen un botón para volver.
- Los elementos se pueden reorganizar arrastrándolos; también es posible moverlos entre carpetas.

Los accesos directos admiten destino, argumentos y una imagen opcional. Los destinos pueden ser ejecutables, accesos directos de Windows, archivos, carpetas o direcciones compatibles con el controlador de Windows. Una carpeta añadida como destino abre el Explorador; una carpeta interna de ClapPanel despliega sus propios elementos.

## Ajustes

- Micrófono, sensibilidad, calibración e inicio con Windows.
- En General: permitir o bloquear el movimiento manual del panel, detener la captura del micrófono, restablecer los valores predeterminados o cerrar ClapPanel.
- El restablecimiento elimina también accesos directos y perfiles personalizados.
- Columnas del panel, accesos directos, carpetas, categorías, favoritos e imágenes.
- Apariencia en vivo: tema, fondo, acento, opacidad, transparencia dinámica, tamaño de iconos, nombres, esquinas y animaciones. La ventana de ajustes, sus pestañas, botones, campos y listas adoptan la misma paleta del panel.
- Perfiles visuales: guardar, aplicar y eliminar combinaciones de apariencia y columnas.
- Posición y monitores: elegir el monitor, usar una esquina o el centro, o acoplar el panel a uno de los bordes. El acoplamiento a izquierda o derecha cambia el panel a formato vertical. Se conservan la posición y los tamaños horizontal/vertical por monitor.
- Tamaño manual: arrastrar cualquier borde o esquina para agrandar o reducir el panel. El contenido se reorganiza según el ancho disponible.
- Ocultamiento automático: el panel se esconde tras el tiempo configurado y vuelve a aparecer al acercar el puntero al borde asociado a su posición o acoplamiento.

Los cambios de apariencia se previsualizan mientras se editan. Guardar conserva todos los ajustes; Cancelar o cerrar la ventana revierte la vista previa.

## Calibración

En Ajustes, pestaña Micrófono, el botón Calibrar abre un asistente de tres pasos:

1. Silencio (3 s): mide el ruido de fondo.
2. Voz (4 s): mide hasta dónde llega tu voz con el filtro de graves.
3. Aplausos (hasta 14 s): cuatro dobles aplausos, con un segundo de pausa entre cada uno.

El asistente calcula la sensibilidad y la comprueba reproduciendo lo grabado con el detector real. Muestra cuántos dobles aplausos reconoce y si hay falsos disparos. Al aplicarla se actualiza el valor de sensibilidad y hay que pulsar Guardar.

## Apariencia y perfiles

En Ajustes → Apariencia, los cambios se aplican como vista previa mientras se editan:

- Tema del sistema, claro u oscuro; color de fondo y color de acento.
- Opacidad base y reducción de opacidad cuando el puntero no está sobre el panel.
- Tamaño de iconos, nombres visibles y esquinas redondeadas.
- Animación configurable: ninguna, desvanecer, escala, deslizar, entrada desde arriba, zoom con rebote suave o rebote ligero.
- Posición inicial en cualquiera de las cuatro esquinas o el centro.
- Acoplamiento opcional a los bordes izquierdo, derecho, superior o inferior.
- Ocultamiento automático configurable entre 1 y 30 segundos. Para volver a mostrarlo, acerca el puntero al borde asociado con el acoplamiento o más cercano a la ubicación actual.
- Selección del monitor. Cada pantalla conserva su última posición manual personalizada.
- Perfiles visuales que guardan y restauran la apariencia y el número de columnas.
- Los botones y pestañas tienen realimentación visual discreta al pasar el cursor y al seleccionar una sección, sin imágenes ni dependencias adicionales.

El botón «⋯» del panel abre directamente esta pestaña. Guardar conserva los cambios; Cancelar o cerrar la ventana revierte la vista previa.

## Detección

- Se filtran los graves (voz) antes de analizar el sonido.
- El pico debe ser seco: subida brusca y caída rápida.
- Debe haber silencio antes del primer aplauso y entre ambos.
- Los dos aplausos deben tener una fuerza parecida.
- El umbral se adapta al ruido ambiente y nunca baja del nivel fijado por la sensibilidad.
- Los parámetros avanzados están en la sección `Detection` de `settings.json`.

## Licencia

Este proyecto se distribuye bajo la GNU General Public License v3.0 o posterior (GPL-3.0-or-later). Copyright (C) 2026 Ridol-HK. Consulta el archivo `LICENSE`.

## Datos personales

La configuración y las imágenes de los iconos se guardan en `%APPDATA%\ClapPanel\` y no forman parte del repositorio.
