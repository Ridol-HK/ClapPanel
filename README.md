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
- Icono de bandeja: abrir o cerrar panel, ajustes, pausar y salir.
- Clic en un icono del panel: abre su destino sin cerrar el panel.
- Arrastrar un icono sobre otro: reordena el panel.

## Ajustes

- Micrófono, sensibilidad e inicio con Windows.
- Columnas del panel.
- Iconos: nombre, imagen, destino y argumentos. El destino puede ser cualquier archivo, carpeta o dirección del sistema, pensado sobre todo para ejecutables y accesos directos. Sin imagen propia se usa el icono del destino.

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
