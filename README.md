# ClapPanel

Aplicación para Windows 10/11 que abre y cierra un panel de accesos directos al detectar un doble aplauso.

## Características

- Detección de doble aplauso que distingue el aplauso de la voz y del ruido ambiente.
- Panel de accesos directos con carpetas, categorías, favoritos y buscador.
- Apariencia personalizable con perfiles, y panel móvil, redimensionable y acoplable.
- Calibración asistida del micrófono.
- Inicio opcional con Windows e icono en la bandeja del sistema.

## Requisitos

- Windows 10 u 11 y un micrófono.
- Para compilar desde el código: .NET 10 SDK.

## Instalación

### Ejecutable

1. Descarga `ClapPanel-vX.Y.Z-win-x64.zip` desde la sección Releases del repositorio.
2. Extrae el contenido y ejecuta `ClapPanel.exe`.

No hace falta instalar .NET. Como el ejecutable no está firmado, Windows SmartScreen puede mostrar un aviso: elige «Más información» y luego «Ejecutar de todos modos».

### Desde el código

```
dotnet run --project src/ClapPanel
```

Para generar el ejecutable:

```
dotnet publish src/ClapPanel -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Uso

- Doble aplauso: abre el panel; con el panel abierto, lo cierra.
- Icono de bandeja: abrir o cerrar el panel, abrir ajustes, pausar la captura del micrófono y salir.
- Clic en un icono: abre su destino sin cerrar el panel. Las carpetas internas de ClapPanel se despliegan dentro del mismo panel.
- Botón «⋯»: abre directamente la pestaña de apariencia.
- Barra superior «ClapPanel»: permite arrastrar el panel cuando el movimiento está habilitado. Al acercarlo a los bordes izquierdo o derecho, se acopla y cambia a una disposición vertical de una columna.
- Bordes y esquinas del panel: permiten cambiar su tamaño arrastrando el cursor; los tamaños horizontal y vertical se guardan por monitor.
- Arrastrar un icono sobre otro lo reordena; arrastrarlo sobre una carpeta lo mueve a esa carpeta. Soltarlo en el espacio vacío lo lleva al final de su lista.
- Arrastrar ejecutables, accesos directos, archivos o carpetas de Windows al panel crea accesos directos. Al soltar sobre una carpeta interna, el acceso se agrega a esa carpeta.
- Pantalla completa: pulsa Esc para limpiar primero la búsqueda y luego cerrar el menú, o usa el botón ×.

## Organización de aplicaciones

En Ajustes → Iconos y carpetas se pueden agregar y editar aplicaciones y carpetas internas, asignar una categoría, marcar favoritos y elegir una imagen personalizada. Las carpetas admiten subcarpetas y aplicaciones. En el panel:

- El buscador encuentra nombres, destinos y categorías, incluso dentro de carpetas anidadas.
- El selector de categorías y el filtro de favoritos también incluyen elementos dentro de carpetas.
- Las carpetas internas se abren en el panel y tienen un botón para volver.
- Los elementos se pueden reorganizar arrastrándolos; también es posible moverlos entre carpetas.

Los accesos directos admiten destino, argumentos y una imagen opcional. Los destinos pueden ser ejecutables, accesos directos de Windows, archivos, carpetas o direcciones compatibles con el controlador de Windows. Una carpeta añadida como destino abre el Explorador; una carpeta interna de ClapPanel despliega sus propios elementos.

## Ajustes

La ventana de ajustes tiene cuatro pestañas: General, Micrófono, Apariencia e Iconos y carpetas. Sus pestañas, botones, campos y listas adoptan la misma paleta del panel. Los cambios de apariencia se previsualizan mientras se editan: Guardar conserva todos los ajustes; Cancelar o cerrar la ventana revierte la vista previa.

### General y micrófono

- Inicio con Windows.
- Permitir o bloquear el movimiento manual del panel, detener la captura del micrófono, restablecer los valores predeterminados o cerrar ClapPanel. El restablecimiento elimina también accesos directos y perfiles personalizados.
- Selección del micrófono, sensibilidad y calibración.

### Apariencia

- Tema del sistema, claro u oscuro; color de fondo y color de acento.
- Opacidad base y reducción de opacidad cuando el puntero no está sobre el panel.
- Tamaño de iconos, nombres visibles y esquinas redondeadas.
- Animación configurable: ninguna, desvanecer, escala, deslizar, entrada desde arriba, zoom con rebote suave o rebote ligero.
- Columnas del panel.
- Perfiles visuales que guardan y restauran la apariencia, el número de columnas y el modo de pantalla completa.

### Posición y monitores

- Selección del monitor. Cada pantalla conserva su última posición manual y sus tamaños horizontal y vertical.
- Posición inicial en cualquiera de las cuatro esquinas o el centro.
- Acoplamiento opcional a los bordes izquierdo, derecho, superior o inferior. El acoplamiento a izquierda o derecha cambia el panel a formato vertical.
- Pantalla completa: el menú ocupa todo el monitor seleccionado. En este modo se desactivan el movimiento, el redimensionamiento y el ocultamiento automático.
- Ocultamiento automático configurable entre 1 y 30 segundos. El panel vuelve a aparecer al acercar el puntero al borde asociado a su posición o acoplamiento, o al más cercano a su ubicación actual.

## Calibración

En Ajustes, pestaña Micrófono, el botón Calibrar abre un asistente de tres pasos:

1. Silencio (3 s): mide el ruido de fondo.
2. Voz (4 s): mide hasta dónde llega tu voz con el filtro de graves.
3. Aplausos (hasta 14 s): cuatro dobles aplausos, con un segundo de pausa entre cada uno.

El asistente calcula la sensibilidad y la comprueba reproduciendo lo grabado con el detector real. Muestra cuántos dobles aplausos reconoce y si hay falsos disparos. Al aplicarla se actualiza el valor de sensibilidad y hay que pulsar Guardar.

## Detección

- Se filtran los graves (voz) antes de analizar el sonido.
- El pico debe ser seco: subida brusca y caída rápida.
- Debe haber silencio antes del primer aplauso y entre ambos.
- Los dos aplausos deben tener una fuerza parecida.
- El umbral se adapta al ruido ambiente y nunca baja del nivel fijado por la sensibilidad.
- Los parámetros avanzados están en la sección `Detection` de `settings.json`.

## Estructura del proyecto

```
ClapPanel/
├── src/ClapPanel/
│   ├── Audio/         captura del micrófono
│   ├── Calibration/   asistente de calibración
│   ├── Detection/     filtro y detector de doble aplauso
│   ├── Launcher/      apertura de destinos, imágenes e iconos
│   ├── Resources/     tema visual compartido
│   ├── Settings/      modelos y almacenamiento de la configuración
│   ├── Startup/       inicio con Windows
│   ├── Theming/       paleta, diseño y posición del panel
│   ├── Tray/          icono de la bandeja
│   └── Views/         panel, ajustes, calibración y diálogos
├── tests/ClapPanel.Tests/
├── CHANGELOG.md
├── LICENSE
└── README.md
```

## Pruebas

```
dotnet test
```

## Licencia

Este proyecto se distribuye bajo la GNU General Public License v3.0 o posterior (GPL-3.0-or-later). Copyright (C) 2026 Ridol-HK. Consulta el archivo `LICENSE`.

## Datos personales

La configuración y las imágenes de los iconos se guardan en `%APPDATA%\ClapPanel\` y no forman parte del repositorio.
