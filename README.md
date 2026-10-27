<div align="center">

<img src="Docs/media/01-menu.jpg" alt="HINENI — pantalla de título: a la izquierda el título, a la derecha los serafines en grabado ASCII" width="100%">

# HINENI · הִנֵּנִי

**«Heme aquí.» — Isaías 6:8**

*Terror filosófico en ASCII. Los últimos quince minutos antes del Juicio, en un hospital que sigue atendiendo pacientes.*

[![Jugar en el navegador](https://img.shields.io/badge/▶_Jugar-en_el_navegador-c2362c?style=for-the-badge)](https://jorge-polanco-roque.github.io/hineni/)
[![itch.io](https://img.shields.io/badge/itch.io-roqueio-fa5c5c?style=for-the-badge&logo=itchdotio&logoColor=white)](https://roqueio.itch.io/)
[![macOS](https://img.shields.io/badge/Descargar-macOS-2b2b2b?style=for-the-badge&logo=apple)](https://github.com/Jorge-Polanco-Roque/hineni/releases/latest)

![Unity](https://img.shields.io/badge/Unity-6000.4-black?logo=unity)
![Render](https://img.shields.io/badge/render-ASCII_por_forma-c2362c)
![Duración](https://img.shields.io/badge/partida-15_min-555)
![Finales](https://img.shields.io/badge/finales-6-555)
![Plataformas](https://img.shields.io/badge/plataformas-Web_·_Windows_·_macOS_·_Linux_·_móvil-555)
![Idiomas](https://img.shields.io/badge/idiomas-ES_·_EN_·_PT_·_中文_·_日本語-555)
[![Licencia MIT](https://img.shields.io/badge/licencia-MIT-555)](LICENSE)

</div>

---

Despiertas de un coma. Te llamas Álex, llevas diecinueve días en el Hospital del Umbral y al mundo le quedan quince minutos. Nadie se ha ido: una enfermera sigue cambiando sábanas, un médico apoya la frente contra la pared y cuenta hasta cuarenta y uno, un hombre mira una esquina y te pide que no mires el techo.

**Lo último que hagas decide cómo termina.** No hay combate ni una respuesta correcta que desbloquear: hay cinco actos posibles, un reloj que no se detiene y algo que te observa desde arriba.

<div align="center">
<img src="Docs/media/desgarro.gif" alt="Al mirar al techo la imagen se desgarra y aparece el cielo: ruedas llenas de ojos y criaturas aladas" width="640">
<br><sub>Mirar al techo. La imagen se desgarra y el hospital se convierte en un grabado.</sub>
</div>

<div align="center">
<img src="Docs/media/00-prologo.jpg" alt="El prólogo: Apocalipsis 6:8 y 9:6 en rojo, sangrando, con desgarros" width="100%">
<br><sub>Antes de despertar: el caballo amarillo y la muerte que huye (Apocalipsis 6:8 · 9:6), leídos por la voz más grave sobre campanas de funeral, hasta que eliges continuar.</sub>
</div>

## Índice

- [Jugar](#jugar)
- [Idiomas](#idiomas)
- [Lo último que hagas](#lo-último-que-hagas)
- [Seis finales](#seis-finales)
- [El hospital](#el-hospital)
- [Cómo está hecho](#cómo-está-hecho)
- [Compilar y probar](#compilar-y-probar)
- [Estructura](#estructura)
- [Créditos y licencias](#créditos-y-licencias)

## Jugar

| | |
|---|---|
| **itch.io** | [roqueio.itch.io/hineni](https://roqueio.itch.io/hineni) · en el navegador o descargable para Windows, macOS y Linux |
| **Navegador** | [jorge-polanco-roque.github.io/hineni](https://jorge-polanco-roque.github.io/hineni/) · ~63 MB · Chrome, Edge, Firefox, Safari |
| **macOS** | [Descargar desde *Releases*](https://github.com/Jorge-Polanco-Roque/hineni/releases/latest) · build de desarrollo sin firmar: clic derecho → *Abrir* la primera vez |
| **Móvil** | El mismo enlace del navegador. Se juega en horizontal |

Usa auriculares: la música, las voces y la respiración del Testigo son posicionales.

### Controles

| Acción | Teclado y ratón | Móvil |
|---|---|---|
| Caminar | `W` `A` `S` `D` | Palanca izquierda |
| Mirar | Ratón | Palanca derecha |
| Usar / hablar | `E` | Botón **USAR** (aparece cuando hay algo delante) |
| Linterna (empieza apagada) | `F` | **CONTROLES** → LINTERNA |
| Respirar / contener el aliento | `Espacio` | Botón **RESPIRAR** / **CONTENER** |
| Colgar el teléfono | `Q` | Botón **COLGAR** |
| Pausa y opciones | `Esc` | **CONTROLES** → PAUSA |
| Elegir en un diálogo | Clic o `1`–`9` | Toque |

El juego detecta si se está jugando en un teléfono: muestra dos palancas flotantes, un botón contextual y el botón **CONTROLES**, que despliega el resto. En vertical pide girar el teléfono.

<div align="center"><img src="Docs/media/movil.jpg" alt="Controles táctiles: palancas MOVER y MIRAR, fila de botones y botón CONTROLES" width="80%"></div>

## Idiomas

Texto y voces se eligen **por separado** en el menú (*Texto* y *Voces*): español, inglés, portugués, chino simplificado y japonés. Por defecto, texto en inglés y voces en japonés. En la web también por enlace: `?lang=pt&voice=es`.

<table><tr>
<td width="50%"><img src="Docs/media/idioma-ja.jpg" alt="Inés en japonés"><br>日本語</td>
<td width="50%"><img src="Docs/media/idioma-zh.jpg" alt="Pintada en chino con pincel"><br>中文: las pintadas usan una fuente de pincel</td>
</tr></table>

- 401 textos por idioma (`Tools/i18n.py` los extrae del código; `Assets/Resources/Lang/<código>.json`).
- 50 líneas habladas por idioma con Kokoro, mismo reparto de mujeres y hombres; chino y japonés con los fonemas de [misaki](https://github.com/hexgrad/misaki). El Testigo habla al revés en todos.
- Chino y japonés se dibujan en la rejilla ASCII con caracteres de doble celda (atlas Noto Sans CJK); las pintadas, con Ma Shan Zheng y Yuji Syuku.
- El pasaje inicial usa traducciones clásicas verificadas: Reina-Valera 1909, King James, Almeida Revista e Corrigida, 和合本 y 口語訳.

## Lo último que hagas

Cinco actos jugables. El primero que completes es el final que vives.

| Acto | Dónde | Cómo se juega |
|---|---|---|
| **Quedarse** | Sala común, junto al hombre de la cama | Siéntate con él y respira a su ritmo: pulsa en lo alto de cada respiración, seis veces |
| **La voz** | Teléfono de recuperación | Sintoniza la línea entre la estática (A/D, ratón o palanca) y quédate con Lucía hasta que se corte |
| **Las velas** | Capilla | Siete velas, nueve segundos cada una. Las siete encendidas a la vez |
| **El armario** | Sala eléctrica | Escóndete mientras el Testigo te busca. Contén el aliento cuando esté cerca. Cuarenta segundos |
| **El cielo** | Cualquier techo | Mira hacia arriba más tiempo del que se puede mirar |

Por el camino: un fusible en el altar, un cuadro eléctrico con tres interruptores cuyo orden está escrito en la pared y una presencia que solo se detiene si la iluminas y la miras. Y una **palanca roja, la 41**, que no alimenta ningún circuito. Bajarla tiene consecuencias: el médico del pasillo revienta, las pintadas se reescriben y todos los finales cambian una línea.

## Seis finales

Cada final tiene su propio tratamiento visual y se puede volver a ver desde **Finales** en el menú.

<table>
<tr>
<td width="33%"><img src="Docs/media/final-quedarse.jpg" alt="Final Quedarse"><br><b>Quedarse</b> · brasas ámbar sobre negro</td>
<td width="33%"><img src="Docs/media/final-voz.jpg" alt="Final La voz"><br><b>La voz</b> · su voz como traza de fósforo verde</td>
<td width="33%"><img src="Docs/media/final-velas.jpg" alt="Final Las velas"><br><b>Las velas</b> · la capilla grabada en pan de oro</td>
</tr>
<tr>
<td><img src="Docs/media/final-armario.jpg" alt="Final El armario"><br><b>El armario</b> · siete rendijas de luz y algo que pasa</td>
<td><img src="Docs/media/final-ojos.jpg" alt="Final Los ojos"><br><b>Los ojos</b> · el grabado desciende</td>
<td><img src="Docs/media/final-juicio.jpg" alt="Final El Juicio"><br><b>El Juicio</b> · el reloj llega a cero; cielo rojo</td>
</tr>
</table>

## El hospital

<table>
<tr>
<td width="50%"><img src="Docs/media/02-ines.jpg" alt="Inés, la enfermera, en la primera conversación"><br>Inés no se ha ido. Los diálogos detienen el reloj.</td>
<td width="50%"><img src="Docs/media/03-pasillo.jpg" alt="El pasillo con la mujer en silla de ruedas y el médico contra la pared"><br>El pasillo: la mujer de la silla, el médico, la puerta de la capilla.</td>
</tr>
<tr>
<td><img src="Docs/media/04-mujer.jpg" alt="Primer plano de la mujer de la silla"><br>Al hablar, la cámara encuadra el rostro. Cada personaje tiene voz propia.</td>
<td><img src="Docs/media/05-pintada.jpg" alt="Pintada en sangre: EL VERBO SE HIZO CARNE"><br>Todo lo escrito en las paredes está escrito en sangre.</td>
</tr>
<tr>
<td><img src="Docs/media/05c-testigo-rostro.jpg" alt="El Testigo: cara estirada, cuencas negras, lágrimas de sangre"><br>El Testigo. Habla al revés y llora sangre. Solo la luz y la mirada lo detienen.</td>
<td><img src="Docs/media/11-ventana.jpg" alt="La ventana: la ciudad en llamas"><br>La ciudad arde. Junto al cristal se oyen sirenas y gritos.</td>
</tr>
<tr>
<td><img src="Docs/media/10a-apagon.jpg" alt="El pasillo durante el apagón"><br>Apagón: solo la linterna.</td>
<td><img src="Docs/media/10b-luz.jpg" alt="El pasillo con la corriente restaurada"><br>Con los tres interruptores en orden, el hospital vuelve a iluminarse.</td>
</tr>
<tr>
<td><img src="Docs/media/07-esquina.jpg" alt="El hombre de la esquina, de espaldas"><br>«No mires el techo.»</td>
<td><img src="Docs/media/08-cielo.jpg" alt="El cielo: ruedas con ojos y criaturas de seis alas"><br>Ruedas dentro de ruedas, cuatro rostros, seis alas, ojos que lloran sangre. Rezan en lenguas; los subtítulos son símbolos.</td>
</tr>
<tr>
<td><img src="Docs/media/14-ines-b.jpg" alt="Inés llevándose una mano a la cara"><br>Nadie está quieto: cada personaje encadena gestos (manos, cara, nuca, abrazarse) con transiciones suaves.</td>
<td><img src="Docs/media/16-vision.jpg" alt="Una visión de los ángeles en mitad del pasillo"><br>En los últimos cinco minutos el cielo aparece aunque no lo mires, anunciado por una campana. Los últimos quince segundos, campanas cada vez más rápidas.</td>
</tr>
</table>

## Cómo está hecho

**Render ASCII por forma.** El mundo es una escena 3D normal; solo su imagen se convierte en caracteres (`Assets/Shaders/Ink.shader`, `InkCamera.cs`). El tamaño de celda se ajusta a la pantalla (≈165 filas; 5×10 px físicos a pantalla completa, 4×8 en ventanas pequeñas y teléfonos). Cada celda se divide en 2×3 subregiones y se compara con el vector de forma de cada glifo del atlas (Meslo LG S, horneado en 13 tamaños con `Tools/BakeAsciiAtlas.swift`); las celdas planas usan una rampa tonal. Los contornos salen de las discontinuidades de profundidad y normales; el color de cada glifo es el tono de la escena bajo él. La interfaz se dibuja en una rejilla de texto aparte, con los mismos glifos.

**Una sola tubería, varias paletas.** El mismo shader produce el grabado (tinta sobre papel hueso con la sangre en rojo), el fósforo verde, el ámbar, el pan de oro y el rojo del Juicio; un parámetro de desgarro desplaza bandas de filas, revuelve glifos e invierte la imagen durante unos fotogramas.

**El cielo** (`Heaven.cs`) se construye por código: ofanim (anillos con ojos sobre ejes distintos), cuerpos con seis alas y cuatro rostros (hombre, león, buey, águila) en postura de oración, lágrimas de sangre que caen hacia el jugador, coro y lenguas procesadas. El cielo guarda su propio tiempo: al bajar la mirada se detiene y al volver sigue exactamente donde estaba.

**Gestos.** `Uncanny.cs` resuelve los brazos con cinemática inversa de dos huesos, solo para quien está de pie en una pose neutra (Inés): alterna gestos lentos y gesticula al hablar. Un rayo del hombro a la mano impide atravesar paredes. Los demás conservan su pose con respiración, balanceo y temblor.

**Personas.** Cinco personajes basados en los Universal Base Characters de Quaternius, vestidos, proporcionados y posados en Blender sin interfaz (`Tools/build_characters.py`), con *blend shapes* de parpadeo, miedo y grito. `Uncanny.cs` les da respiración, mirada repartida por la columna, temblor y susurros con límites anatómicos.

**La ciudad.** Se ve por un hueco real en el muro: cielo en llamas, humo y dos hileras de edificios a distintas distancias, con pisos ardiendo. Su sonido (`Tools/city_mix.py`) mezcla sirenas y gritos grabados con los disparos y risas sintéticos (`Tools/city_sound.py`); solo se oye junto al cristal y sube a cada paso.

**Voces.** 50 líneas por idioma (cinco idiomas) generadas offline con [Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) (`Tools/voices.py`): mezclas de estilo por personaje (mujer u hombre, edad, ritmo, tono), radio filtrada, el Testigo grabado al revés, el prólogo con la voz más grave y eco de nave, y lenguas angélicas a partir de sílabas inventadas. Chino y japonés pasan por [misaki](https://github.com/hexgrad/misaki). Las acotaciones `[…]` quedan en el guion pero no se leen ni se muestran.

**Idiomas.** `Loc.cs` busca cada texto español en `Resources/Lang/<código>.json`; texto y voz tienen idioma propio. `Tools/i18n.py` extrae los textos del código, construye las tablas y lista los caracteres que necesitan los atlas.

**Validación automática.** El juego incluye sus propias pruebas, que se lanzan como argumentos del ejecutable: reglas de la amenaza, guardado, cada final, cada acto, la palanca de principio a fin, el cielo, la voz y una auditoría de escena que detecta objetos flotando, piezas que se atraviesan y pintadas tapadas por muebles.

## Compilar y probar

Requisitos: Unity **6000.4.0f1** (módulos macOS y WebGL). Opcional: Blender 5.1 para personajes y `~/.venvs/kokoro` (kokoro-onnx, `misaki[zh,ja]`, `unidic-lite`, fonttools) para voces y fuentes.

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity

# macOS → Builds/macOS/Hineni.app
$UNITY -batchmode -quit -projectPath . -executeMethod BuildDemo.Build -logFile build.log

# Web → Builds/WebGL (la página publicada usa Tools/web/index.html)
$UNITY -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod BuildDemo.BuildWeb -logFile web.log

# Publicar la web en GitHub Pages (rama gh-pages, URLs con sello por despliegue)
Tools/web/deploy.sh

# Textos: extraer, construir tablas, caracteres para los atlas
python3 Tools/i18n.py extract && python3 Tools/i18n.py build && python3 Tools/i18n.py chars && python3 Tools/i18n.py narrow

# Voces de los cinco idiomas (solo regenera las líneas que cambiaron) · sonido de la ciudad
~/.venvs/kokoro/bin/python Tools/voices.py
~/.venvs/kokoro/bin/python Tools/city_sound.py
```

| Prueba | Qué comprueba | Salida |
|---|---|---|
| `--smoke-test` | Arranque, diálogos, guardado, reloj, un final de acto (16 comprobaciones) | `/tmp/spooky-qa` |
| `--horror-test` | Testigo, linterna, fusible, interruptores, armario, captura (20) | `/tmp/spooky-horror-qa` |
| `--layout-qa` | Objetos flotando, solapes, pintadas tapadas, cielo, palanca, voz | `/tmp/spooky-layout-qa/report.txt` |
| `--lever-qa` | La palanca 41 de principio a fin (14) | `/tmp/spooky-layout-qa/lever.txt` |
| `--ending-qa <id>` | Fotografía un final (`quedarse`, `voz`, `velas`, `armario`, `ojos`, `juicio`) | `/tmp/spooky-layout-qa` |
| `--acts-qa` · `--sky-qa` · `--touch-qa --touch` | Velas, cielo en partida real, interfaz táctil | `/tmp/spooky-layout-qa` |
| `--press-kit` | Las capturas de este README (prólogo incluido) | `/tmp/spooky-press` |
| `--lang <código>` · `--voice <código>` | Cualquier prueba en otro idioma de texto o de voz | — |
| `index.html?qa=press` · `?qa=ending-<id>` · `?qa=tear` · `?qa=gestures` | Las mismas capturas en la versión web (la página las fotografía con Playwright) | consola `QA_SNAP` |

```bash
Builds/macOS/Hineni.app/Contents/MacOS/Hineni --smoke-test
```

Las pruebas nativas necesitan la pantalla encendida (con la pantalla apagada macOS no da fotogramas al juego) y sin otra instancia abierta.

## Estructura

| Archivo | Responsabilidad |
|---|---|
| `DemoGame.cs` | Flujo de partida, prólogo, diálogos, reloj, guardado, interfaz, menú (texto y voces) y galería |
| `Loc.cs` | Idiomas de texto y de voz, tablas de traducción, ancho de caracteres CJK |
| `HospitalWorld*.cs` | Arquitectura procedural, utilería, personajes, luces, pintadas y sus variantes, palanca 41 |
| `HorrorDirector.cs` | El Testigo (regla de luz y mirada), puzle eléctrico, armario, puertas |
| `Acts.cs` · `Endings.cs` | Los cinco actos y sus minijuegos · la puesta en escena de los seis finales |
| `Heaven.cs` | El cielo, el desgarro, las lenguas y sus subtítulos |
| `Watchers.cs` | Ojos en la oscuridad, huellas de manos, desgarros esporádicos, lágrimas de sangre (`Weeping`) |
| `Controls.cs` | Entrada unificada: teclado y ratón, palancas táctiles, botones, orientación |
| `InkCamera.cs` · `Ink.shader` | El render ASCII y la rejilla de texto de la interfaz |
| `Uncanny.cs` | Vida procedural de los personajes |
| `LayoutAudit.cs` · `EndingAudit.cs` · `*Test.cs` | Validación automática |
| `Tools/` | Atlas de glifos (latino y CJK), personajes, utilería, traducciones (`i18n.py`, `i18n/`), voces, sonido de la ciudad, página web y despliegue |

## Créditos y licencias

| Recurso | Autor | Licencia |
|---|---|---|
| Diseño, guion, código | Jorge Polanco | [MIT](LICENSE) |
| Música «Mechanical Hum» | antm_ai (hecha con Suno) | Con permiso del autor |
| Personajes base | [Quaternius · Universal Base Characters](https://quaternius.itch.io/universal-base-characters) | CC0 1.0 |
| Utilería | [Poly Haven](https://polyhaven.com) | CC0 1.0 |
| Campanas del prólogo | [«Kerkklokken begrafenis»](https://commons.wikimedia.org/wiki/File:Kerkklokken_begrafenis.ogg), Bert76 (Wikimedia Commons) | CC0 1.0 |
| Sirenas y gritos de la ventana | [Civil-defense-siren-waver](https://commons.wikimedia.org/wiki/File:Civil-defense-siren-waver.ogg) (Techtonic, dominio público) · [American police siren i](https://commons.wikimedia.org/wiki/File:American_police_siren_i.ogg) (lezer, dominio público) · [Kamagasaki riot](https://commons.wikimedia.org/wiki/File:20080618KamagasakiRiot.ogg) (Kamagasaki450, CC0) · [Scream-c/d/e](https://commons.wikimedia.org/wiki/File:Scream-c.wav) (Raspberry Pi Foundation, Andy Oakley) | Dominio público · CC0 · CC BY-SA 4.0 (la mezcla `Sounds/ciudad.wav` hereda CC BY-SA 4.0) |
| Voces | Generadas con [Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) | Apache 2.0 |
| Glifos | [Meslo LG](https://github.com/andreberg/Meslo-Font) (Nerd Font) | Apache 2.0 |
| Pintadas (web) | [Rubik Wet Paint](https://github.com/NaN-xyz/Rubik-Filtered) | SIL OFL 1.1 |
| Chino y japonés | Noto Sans SC/JP, Ma Shan Zheng, Yuji Syuku | SIL OFL 1.1 |
| Fonemas chino/japonés | [misaki](https://github.com/hexgrad/misaki) (herramienta, no se distribuye) | Apache 2.0 |
| Apocalipsis 6:8 · 9:6 | Reina-Valera 1909, King James, 和合本, 口語訳 (dominio público); Almeida Revista e Corrigida (cita breve, © SBB) | — |

El código, el guion y los textos originales están bajo [licencia MIT](LICENSE). Los recursos de terceros conservan sus propias licencias (tabla de arriba), y la música se incluye con permiso de su autor. Detalle en [`Assets/Resources/THIRD_PARTY.txt`](Assets/Resources/THIRD_PARTY.txt). En macOS las pintadas usan la fuente del sistema Chalkduster, que no se distribuye.

<div align="center"><sub>Prototipo 0.3 · Spooky Game Jam · sin combate · leer y elegir detiene el reloj</sub></div>
