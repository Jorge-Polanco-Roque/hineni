# Repositorios para terror retro / ASCII en Unity

Revisión: 25 de octubre de 2026. Recomendación: proyecto propio en Unity + URP, tomando osASCII como primer candidato visual. Evitar empezar fusionando proyectos completos antiguos.

## Selección priorizada

### 1. michael-sacco/osASCII — primer candidato visual

https://github.com/michael-sacco/osASCII

Convierte escenas 2D o 3D a caracteres mediante un shader para URP. Aporta precisamente el componente que da identidad a la idea: renderizar un mundo normal de Unity como ASCII.

Licencia MIT comprobada en el archivo LICENSE: conservar aviso de copyright y licencia al redistribuir código. El proyecto declara Unity **2021.2.5f1** y URP **12.1.2**. No está comprobado en el Unity 6000.4.0f1 instalado. La primera tarea debe ser una prueba de adaptación del efecto, no adoptar todo su manifest antiguo.

Fuentes técnicas:
- https://github.com/michael-sacco/osASCII/blob/main/LICENSE
- https://github.com/michael-sacco/osASCII/blob/main/ProjectSettings/ProjectVersion.txt
- https://github.com/michael-sacco/osASCII/blob/main/Packages/manifest.json

Veredicto: candidato preferido por ajuste visual, pipeline y licencia; pendiente validar la implementación en Unity 6.

### 2. StefanJo3107/ASCII-Rendering-Shader-in-Unity — alternativa y referencia sencilla

https://github.com/StefanJo3107/ASCII-Rendering-Shader-in-Unity

Efecto de imagen con atlas de caracteres, resolución de celdas, brillo y opción monocroma. El README indica añadir ASCIIRendering a la cámara. Licencia MIT indicada por el repositorio.

Su shader usa el esquema clásico de efectos de imagen y UnityCG; no tratarlo como un Renderer Feature listo para URP. Sirve para comprender el muestreo de luminosidad y la selección de glifos o para una adaptación acotada.

Veredicto: buena segunda opción técnica; no combinarlo con osASCII sin una razón visual concreta.

### 3. Kodrin/URP-PSX — alternativa de estética PlayStation

https://github.com/Kodrin/URP-PSX

Aporta efectos retro como pixelación, reducción de precisión de color, dithering y niebla. El README declara Unity **2019.3.7f1** y URP/Shader Graph **7.1.8**; licencia MIT verificada.

La distancia con Unity 6 es considerable: conviene estudiar o portar efectos seleccionados. No sustituye sistemas de juego. Útil si decidimos mezclar geometría de pocos polígonos con ASCII localizado en monitores o en una herramienta del jugador.

Licencia: https://github.com/Kodrin/URP-PSX/blob/master/LICENSE

Veredicto: referencia secundaria, con mayor riesgo de migración que una implementación nueva pequeña.

### 4. inkle/ink-unity-integration — narrativa interactiva

https://github.com/inkle/ink-unity-integration

Integra historias Ink, elecciones, variables y herramientas de depuración en Unity. Licencia MIT; el README indica Unity 2022.3 LTS o superior para la línea 2.0. Resulta útil para terminales, comunicaciones y decisiones que cambian el desenlace.

Instalación publicada: `https://github.com/inkle/ink-unity-integration.git#upm`. Para producción, fijar una versión o revisión comprobada en lugar de depender indefinidamente de una rama móvil.

Veredicto: complemento sólido si hay narrativa ramificada. Para tres notas estáticas, bastan datos propios y UI.

## Candidatos que no elegiría como dependencia principal

- **NullTale/AsciiFx:** efecto URP con paletas y caracteres animados, pero su README restringe esta versión a uso no comercial y dice que ya no tiene soporte. La edición comercial se ofrece separadamente. No integrarlo en una base pensada para poder publicar comercialmente sin resolver esa licencia. https://github.com/NullTale/AsciiFx
- **ClemGG/ASCII-Unity-Shader:** demo para Unity 2021.3 con atlas intercambiable; no se verificó una licencia explícita durante esta revisión. Mantener como referencia visual hasta aclararla. https://github.com/ClemGG/ASCII-Unity-Shader
- **GarrettGunnell/AcerolaFX:** suite de shaders GShade; no es un paquete Unity. Requiere adaptación y revisión de la licencia concreta antes de copiar código. https://github.com/GarrettGunnell/AcerolaFX
- **VeryHotShark/First-Person-Controller-VeryHotShark:** referencia para movimiento de walking simulator; incorpora otras herramientas y no se verificó aquí la licencia completa. No incorporarlo automáticamente. https://github.com/VeryHotShark/First-Person-Controller-VeryHotShark

## Opción que adelanta mecánicas, aunque no es un repositorio

**PSX Horror Game Template, de metrixgo:** el autor publica controlador en primera persona, interacción, puertas, triggers, inventario, menús y efectos; declara CC0. Podría ahorrar más trabajo de gameplay que un shader aislado. Su propia página advierte problemas con la demo web. No se ha descargado, comprobado la versión del editor ni auditado el contenido del ZIP; verificar también procedencia y licencias de recursos incluidos antes de integrarlo.

https://metrixgo.itch.io/psx-horror-game-template

## Qué significa la referencia compartida

https://theolundqvist.github.io/frontier-games/

Es una galería de juegos y películas con enlaces a publicaciones de sus creadores. Su repositorio, https://github.com/theolundqvist/frontier-games, corresponde a la galería: no equivale al código fuente de los juegos mostrados ni concede derechos sobre ellos. Útil para estudiar impacto visual y presentación de demos.

No se identificó con certeza el juego HTML/ASCII recordado. Una pista es el prototipo de ciudad ASCII de Grow Now! Games, descrito como un archivo HTML, pero no debe confundirse con una identificación confirmada ni con un recurso disponible para reutilización.

## Decisión propuesta

1. Probar osASCII en un proyecto aislado de Unity 6/URP, con una habitación sencilla y una luz móvil.
2. Si exige demasiado mantenimiento, implementar un pase ASCII pequeño propio tomando el funcionamiento general como referencia.
3. Mantener la UI después del efecto para no destruir legibilidad.
4. Añadir movimiento e interacción mínimos; evaluar la plantilla metrixgo solo si sus versiones y recursos encajan.
5. Añadir Ink cuando la narrativa lo justifique.

No se afirma que estos componentes sean compatibles entre sí sin pruebas. La búsqueda y revisión documental no sustituyen una importación, compilación y prueba gráfica en Metal/macOS.
