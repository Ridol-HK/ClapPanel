[English](RELEASE_NOTES.en.md) | [Español](RELEASE_NOTES.md)

## ClapPanel 1.6.0

Aplicación para Windows 10/11 que abre y cierra un panel de accesos directos al detectar un doble aplauso.

### Novedades

- Detección de accesos rotos para aplicaciones, archivos y carpetas con destino configurado.
- Menú contextual con opciones para editar, duplicar, eliminar y abrir la ubicación de un acceso.
- Opción para cerrar automáticamente el panel después de ejecutar una aplicación o archivo.
- Nueva sección Estado del sistema en Ajustes con el estado de los componentes principales y la versión instalada.

- Exportación e importación de la configuración completa mediante ZIP, con accesos directos, categorías, carpetas anidadas, perfiles visuales e imágenes personalizadas.
- Importación de solo los accesos directos para conservar la apariencia y el resto de las preferencias.
- Copias de seguridad automáticas antes de sobrescribir ajustes, importar una configuración o restablecer los valores predeterminados.
- Recuperación automática desde la última copia válida cuando el archivo de configuración se daña y restauración manual desde Ajustes → General.
- Se mantienen hasta 20 copias de seguridad locales rotativas en `%APPDATA%\ClapPanel\backups`.

### Instalación

1. Descarga `ClapPanel-v1.6.0-win-x64.zip` desde los archivos de esta versión.
2. Extrae el contenido y ejecuta `ClapPanel.exe`.

No hace falta instalar .NET. Como el ejecutable no está firmado, Windows SmartScreen puede mostrar un aviso: elige «Más información» y luego «Ejecutar de todos modos».

Los cambios completos están en `CHANGELOG.md` y `CHANGELOG.en.md`.

Repositorio: https://github.com/Ridol-HK/ClapPanel

---

## ClapPanel 1.4.3

Aplicación para Windows 10/11 que abre y cierra un panel de accesos directos al detectar un doble aplauso.

### Novedades

- Recordar la última categoría seleccionada al volver a abrir el panel.
- Asignar un mismo acceso directo a varias categorías.
- Configurar el tiempo de espera (0–5 segundos) antes de reducir la opacidad cuando el cursor está fuera del panel.
- Activar o desactivar el desvanecimiento suave de la transparencia (220 ms).
- Reordenar accesos arrastrándolos en el panel, en Ajustes → Iconos y carpetas y en el editor de carpetas anidadas.
- Menú de acople inteligente que muestra hasta seis accesos y adapta su orientación al borde de acoplamiento.

### Instalación

1. Descarga `ClapPanel-v1.4.3-win-x64.zip` desde los archivos de esta versión.
2. Extrae el contenido y ejecuta `ClapPanel.exe`.

No hace falta instalar .NET. Como el ejecutable no está firmado, Windows SmartScreen puede mostrar un aviso: elige «Más información» y luego «Ejecutar de todos modos».

Los cambios completos están en `CHANGELOG.md` y `CHANGELOG.en.md`.

Repositorio: https://github.com/Ridol-HK/ClapPanel
