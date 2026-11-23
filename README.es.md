# ClapPanel

[English](README.md) | [Español](README.es.md)

ClapPanel es una aplicación para Windows 10/11 que abre y cierra un panel de accesos directos al detectar un doble aplauso.

**Última versión: 1.5.0**

## Características

- Detección de doble aplauso diseñada para distinguir los aplausos de la voz y del ruido ambiente.
- Panel de accesos directos con carpetas, categorías, favoritos y buscador.
- Un mismo acceso directo puede pertenecer a varias categorías al mismo tiempo.
- Opción para restaurar la última categoría seleccionada al volver a abrir el panel.
- Apariencia personalizable con perfiles y un panel móvil, redimensionable, acoplable o de pantalla completa.
- Transparencia dinámica configurable, con animación de desvanecimiento suave opcional y tiempo de espera antes de reducir la opacidad cuando el cursor está fuera del panel.
- Monitor de entrada del micrófono con nivel máximo, promedio, frecuencia de muestreo, canales y diagnóstico de señal o errores de captura.
- Calibración asistida del micrófono.
- Atajo de teclado global configurable para abrir o cerrar el panel aunque ClapPanel esté en segundo plano.
- Navegación con teclado, soporte para lectores de pantalla y modo de alto contraste.
- Widgets opcionales: reloj, notas rápidas, portapapeles y control de reproducción.
- Interfaz en español e inglés.
- Asistente de primera ejecución para elegir el micrófono, comprobar la entrada, ajustar la sensibilidad y configurar el atajo global.
- El menú de acople inteligente permite elegir entre 1 y 12 accesos visibles, priorizar favoritos y ajustar el ancho de la zona de activación del borde.
- Inicio opcional con Windows e icono en la bandeja del sistema.

## Requisitos

- Windows 10 u 11 y un micrófono.
- .NET 10 SDK para compilar desde el código.

## Instalación

### Ejecutable

1. Descarga `ClapPanel-vX.Y.Z-win-x64.zip` desde [Releases](https://github.com/Ridol-HK/ClapPanel/releases).
2. Extrae el archivo y ejecuta `ClapPanel.exe`.

No hace falta instalar .NET. Como el ejecutable no está firmado, Windows SmartScreen puede mostrar un aviso. Selecciona **Más información** y luego **Ejecutar de todos modos**.

### Desde el código

```powershell
git clone https://github.com/Ridol-HK/ClapPanel.git
cd ClapPanel
dotnet run --project src/ClapPanel
```

Para publicar un ejecutable autocontenido:

```powershell
dotnet publish src/ClapPanel -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Uso

- **Doble aplauso:** abre el panel; otro doble aplauso lo cierra.
- **Atajo global:** por defecto, Ctrl + Alt + Espacio abre o cierra el panel desde cualquier aplicación. Se puede cambiar o desactivar en Ajustes → General.
- **Icono de la bandeja:** permite abrir o cerrar el panel, abrir Ajustes, pausar la captura del micrófono y salir de la aplicación.
- **Clic en un icono:** abre su destino sin cerrar el panel. Las carpetas internas de ClapPanel se despliegan dentro del mismo panel.
- **Botón ⋯:** abre directamente la pestaña Apariencia.
- **Barra superior ClapPanel:** permite arrastrar el panel cuando el movimiento está habilitado. Al acercarlo a los bordes izquierdo o derecho, se acopla y cambia a una disposición vertical de una columna.
- **Bordes y esquinas del panel:** permiten cambiar su tamaño arrastrando el cursor. Los tamaños horizontal y vertical se guardan por monitor.
- **Arrastrar un icono sobre otro:** lo reordena al soltarlo antes o después del icono de destino. Soltarlo directamente sobre una carpeta lo mueve a esa carpeta; soltarlo en el espacio vacío lo lleva al final de la lista actual.
- **Ajustes → Iconos y carpetas:** arrastra los elementos de la lista para cambiar su orden; también funciona en el editor del contenido de carpetas anidadas.
- **Arrastrar ejecutables, accesos directos, archivos o carpetas de Windows al panel:** crea accesos directos. Al soltar un elemento sobre una carpeta interna, se agrega a esa carpeta.
- **Esc:** primero limpia la búsqueda; si ya está vacía, cierra el panel. El botón × hace lo mismo.
- **Controles de teclado:** consulta [Teclado y accesibilidad](#teclado-y-accesibilidad).

## Teclado y accesibilidad

Al abrirse, el panel deja el foco en el primer icono, por lo que se puede usar sin ratón.

| Tecla | Acción |
| --- | --- |
| Flechas | Moverse entre iconos y controles; cada movimiento reinicia el contador de inactividad y mantiene el panel opaco mientras se navega |
| Intro o Espacio | Abrir el icono o la carpeta seleccionada |
| 1–9 y 0 | Abrir directamente los elementos 1 al 10 de la lista visible |
| `/`, Ctrl+F o una letra | Ir al buscador |
| Flecha abajo (en el buscador) | Pasar a los iconos |
| Inicio / Fin | Seleccionar el primer o último icono |
| Retroceso o Alt+Izquierda | Volver a la carpeta anterior |
| Esc | Limpiar la búsqueda o cerrar el panel |

- Los números de atajo pueden mostrarse sobre los iconos (Ajustes → General → Accesibilidad).
- El foco de teclado se ve en el panel y en Ajustes. Los iconos, carpetas y botones tienen nombres para lectores de pantalla, que además anuncian la cantidad de resultados al buscar o filtrar.
- El modo de alto contraste (negro, blanco y amarillo) se puede activar en Ajustes → General → Accesibilidad. ClapPanel lo usa automáticamente si Windows tiene activo un tema de alto contraste.

## Widgets

En **Ajustes → Widgets** se elige qué aparece en la parte superior del panel:

- **Reloj:** formato de 12 o 24 horas, segundos y fecha opcionales.
- **Notas rápidas:** cuadro de texto que se guarda automáticamente en `settings.json`.
- **Portapapeles:** recuerda entre 3 y 20 textos recientes. Los elementos se conservan solo mientras ClapPanel está en ejecución, no se guardan en disco y se borran al cerrar la aplicación. Se ignora el contenido de gestores de contraseñas que lo marcan como privado. Un clic vuelve a copiar el texto.
- **Control de reproducción:** pista anterior, reproducir o pausar y pista siguiente, mediante las teclas multimedia de Windows.

## Organización de aplicaciones y categorías

En **Ajustes → Iconos y carpetas** se pueden agregar y editar aplicaciones y carpetas internas, asignar categorías, marcar favoritos y elegir una imagen personalizada. Un acceso directo puede pertenecer a más de una categoría: escribe los nombres separados por comas o punto y coma, por ejemplo `Juegos, Multimedia`. Las carpetas admiten subcarpetas y aplicaciones.

En el panel:

- El buscador encuentra nombres, destinos y categorías, incluso dentro de carpetas anidadas.
- El selector de categorías y el filtro de favoritos también incluyen elementos dentro de carpetas.
- Un elemento asignado a varias categorías aparece al seleccionar cualquiera de ellas.
- Las carpetas internas se abren dentro del panel y tienen un botón para volver.
- Los elementos se pueden reorganizar arrastrándolos tanto en el panel como en **Ajustes → Iconos y carpetas**. En el panel, suéltalos antes o después de otro acceso para definir su posición; suéltalos directamente sobre una carpeta para moverlos dentro. La lista de Ajustes y el editor de carpetas anidadas también admiten reordenamiento por arrastre.
- Cuando está activada la opción **Recordar la última categoría seleccionada** en Ajustes → General, el panel restaura esa categoría la próxima vez que se abre. Desactiva la opción para comenzar siempre mostrando todas las categorías.

Al añadir o editar un acceso directo, **Aplicaciones instaladas…** abre una lista con buscador de las aplicaciones del menú Inicio y de Microsoft Store, para no tener que localizar manualmente el `.exe`.

Los accesos directos admiten destino, argumentos y una imagen opcional. Los destinos pueden ser ejecutables, accesos directos de Windows, archivos, carpetas o direcciones compatibles con el controlador de Windows. Una carpeta añadida como destino abre el Explorador de archivos; una carpeta interna de ClapPanel despliega sus propios elementos.

## Ajustes

En una instalación nueva, la primera vez que se ejecuta ClapPanel aparece un asistente para configurar el micrófono, comprobar la señal y elegir el atajo global; se puede omitir y volver a modificar sus opciones desde Ajustes. Las instalaciones anteriores conservan su configuración y no vuelven a mostrar el asistente al actualizar. La ventana de Ajustes tiene cinco pestañas: **General, Micrófono, Apariencia, Iconos y carpetas y Widgets**. Sus pestañas, botones, campos y listas adoptan la misma paleta del panel.

Los cambios de apariencia se previsualizan mientras se editan. Selecciona **Guardar** para conservarlos; selecciona **Cancelar** o cierra la ventana para revertir la vista previa.

### General

- Atajo global configurable (Ctrl, Alt, Shift o Win más una tecla) para abrir o cerrar el panel incluso cuando otra aplicación está activa.
- Inicio opcional con Windows.
- Permitir o bloquear el movimiento manual del panel arrastrando su barra superior.
- Recordar la última categoría seleccionada al volver a abrir el panel.
- Idioma de la interfaz: Sistema, Español o English. Se aplica al guardar; vuelve a abrir Ajustes para actualizar todos los textos de esa ventana.
- Accesibilidad: modo de alto contraste y números de atajo sobre los iconos.
- **Acerca de…:** versión, licencia, créditos de terceros y nota de privacidad (también está en el menú de la bandeja).
- Detener la captura del micrófono, restablecer los valores predeterminados o cerrar ClapPanel. El restablecimiento también elimina los accesos directos y perfiles personalizados.

### Micrófono

- Seleccionar el micrófono y ajustar la sensibilidad de detección.
- Monitor en tiempo real del nivel máximo y promedio de la señal sin filtrar, además del nivel usado por el detector de aplausos.
- Diagnóstico del estado de captura, detección de señal baja o ausente, nombre del dispositivo, frecuencia de muestreo, canales y errores que impidan iniciar la captura. Usa **Probar micrófono** para iniciar o cambiar temporalmente el dispositivo de entrada.
- Ejecutar el asistente de calibración.

### Apariencia

- Tema del sistema, claro u oscuro; color de fondo y color de acento.
- Opacidad base del panel y reducción opcional de opacidad cuando el cursor está fuera del panel.
- Tiempo de espera configurable antes de aplicar la opacidad reducida (de 0 a 5 segundos).
- Desvanecimiento suave opcional de 220 ms al pasar entre la opacidad activa e inactiva. Desactiva **Animar suavemente la transición de opacidad** para aplicar el cambio al instante.
- Tamaño de iconos, nombres visibles y esquinas redondeadas.
- Animación configurable de apertura y cierre: ninguna, desvanecer, escala, deslizar, aparecer desde arriba, zoom con rebote suave o rebote ligero.
- Perfiles visuales que guardan y restauran la apariencia, el número de columnas y el modo de pantalla completa.

### Posición y monitores

- Selección del monitor. Cada pantalla conserva su última posición manual y sus tamaños horizontal y vertical.
- Posición inicial en cualquiera de las cuatro esquinas o en el centro.
- Acoplamiento opcional a los bordes izquierdo, derecho, superior o inferior. El acoplamiento a izquierda o derecha cambia el panel a formato vertical.
- **Pantalla completa:** el panel ocupa todo el monitor seleccionado. En este modo se desactivan el movimiento, el redimensionamiento y el ocultamiento automático.
- **Ocultamiento automático / acople inteligente:** configurable entre 1 y 30 segundos. Permite mostrar entre 1 y 12 accesos, opcionalmente colocando primero los favoritos sin alterar el orden manual dentro de cada grupo. El acoplamiento a izquierda o derecha usa una columna vertical; arriba o abajo, una fila horizontal. También se puede ajustar entre 4 y 40 píxeles el ancho de la zona del borde que revela el panel. El panel vuelve a aparecer al acercar el cursor al borde correspondiente.

## Calibración

En **Ajustes → Micrófono**, selecciona **Calibrar…** para abrir un asistente de tres pasos:

1. **Silencio (3 segundos):** mide el ruido de fondo.
2. **Voz (4 segundos):** mide cuánto se registra la voz después del filtro de graves.
3. **Aplausos (hasta 14 segundos):** cuatro dobles aplausos, con un segundo de pausa entre cada par.

El asistente calcula un valor de sensibilidad y lo comprueba reproduciendo el audio grabado a través del detector real. Informa cuántos dobles aplausos detectó y si hubo falsos disparos. Al aplicar el resultado se actualiza la sensibilidad; selecciona **Guardar** para conservarla.

## Detección

- Se filtran los graves asociados a la voz antes de analizar el sonido.
- El aplauso debe producir un pico seco: una subida brusca y una caída rápida.
- Debe haber silencio antes del primer aplauso y entre ambos.
- Los dos aplausos deben tener una fuerza parecida.
- El umbral se adapta al ruido ambiente y nunca baja del nivel fijado por el control de sensibilidad.
- Los parámetros avanzados están en la sección `Detection` de `settings.json`.

## Estructura del proyecto

```text
ClapPanel/
├── src/ClapPanel/
│   ├── Audio/         captura del micrófono
│   ├── Calibration/   asistente de calibración
│   ├── Detection/     filtro y detector de doble aplauso
│   ├── Launcher/      destinos, imágenes, iconos y aplicaciones instaladas
│   ├── Localization/  textos en español e inglés
│   ├── Resources/     tema visual compartido
│   ├── Settings/      modelos y almacenamiento de la configuración
│   ├── Startup/       inicio con Windows y atajos globales
│   ├── Theming/       paleta, diseño y posición del panel
│   ├── Tray/          icono de la bandeja
│   ├── Views/         panel, ajustes, calibración y diálogos
│   └── Widgets/       reloj, portapapeles y teclas multimedia
├── tests/ClapPanel.Tests/
├── CHANGELOG.md       (Español)
├── CHANGELOG.en.md    (English)
├── LICENSE
├── README.md
└── THIRD-PARTY-NOTICES.md
```

## Pruebas

```powershell
dotnet test
```

## Privacidad

El audio del micrófono se analiza en tiempo real para detectar los aplausos. No se guarda en disco ni se envía a ningún sitio. La calibración conserva el audio grabado en memoria únicamente mientras el asistente está abierto.

## Licencia

ClapPanel se distribuye bajo la **GNU General Public License v3.0 o posterior (GPL-3.0-or-later)**. Copyright (C) 2026 Ridol-HK. Consulta `LICENSE`. Los componentes de terceros se detallan en `THIRD-PARTY-NOTICES.md`.

## Datos personales

La configuración y las imágenes de los iconos se guardan en `%APPDATA%\ClapPanel\` y no forman parte del repositorio.
