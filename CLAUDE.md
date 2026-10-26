# HINENI — notas para trabajar en el proyecto

Juego de terror en Unity 6000.4.0f1 (built-in RP) con render ASCII. Repo público `Jorge-Polanco-Roque/hineni`; la web se
publica en GitHub Pages (rama `gh-pages`). Todo el texto del código y de los commits va en español.

## Compilar, publicar

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath . -executeMethod BuildDemo.Build -logFile build.log                    # Builds/macOS/Hineni.app
$UNITY -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod BuildDemo.BuildWeb -logFile web.log  # Builds/WebGL
Tools/web/deploy.sh          # publica Builds/WebGL + Tools/web/index.html en gh-pages (URLs con sello: sin mezclas de caché)
```

- El escenario se construye por código al arrancar (`HospitalWorld.Build`); la escena solo tiene `DemoGame`.
- Release de macOS: `ditto -c -k --keepParent Builds/macOS/Hineni.app Hineni-macOS.zip` y `gh release upload v0.3.0 ... --clobber`.
- `GH001 Large files` al hacer push a gh-pages es un aviso (el .data pesa ~56 MB; el límite es 100 MB).

## Probar

- El ejecutable trae sus pruebas: `--smoke-test` (16), `--horror-test` (22), `--lever-qa` (14), `--layout-qa`
  (objetos flotando, solapes, pintadas tapadas), `--press-kit`, `--ending-qa <id>`, `--touch-qa --touch`.
  Idioma: `--lang <es|en|pt|zh|ja>` y `--voice <...>`.
- **Con la pantalla del Mac apagada el player nativo no recibe fotogramas** y las pruebas se quedan al 0 % de CPU.
  Entonces validar en la web con Playwright (`~/Desktop/Pruebas/my-notion/node_modules/@playwright/test`, Chrome
  `--use-angle=metal`): `index.html?qa=press|tear|gestures|ending-<id>`; el juego escribe `QA_SNAP <nombre>` en consola
  y la página hace la captura. `?qa=smoke|horror` en web fallan por foco/timing de Chrome sin interfaz: no son verdad.
- Antes de afirmar que algo se ve bien, mirar capturas (crudas con `Ink.enabledEffect = false` y en ASCII).

## Idiomas

- Fuente única en español: `Loc.T(texto)` lo busca en `Assets/Resources/Lang/<código>.json`. Texto (`Loc.Lang`) y voz
  (`Loc.VoiceLang`) son independientes; por defecto texto `en`, voz `ja`.
- Los puntos de traducción están centralizados (`T`, `Center`, `Button`, `Toast`, `Say`, `SmallLabel`/`Sign`/
  `BloodWriting`, dibujo de Acts/Controls/Endings). Textos con números: `Loc.F("... {0} ...", n)`.
- Al cambiar o añadir textos: `python3 Tools/i18n.py extract`, traducir en `Tools/i18n/<código>.json` (guía en
  `Tools/i18n/BRIEF.md`), luego `build`, `chars`, `narrow`. Si cambian caracteres, re-hornear atlas (abajo).
- Las líneas del Testigo están escritas al revés en el código; `i18n.py` las traduce al derecho y las invierte.

## Voces y audio

- `~/.venvs/kokoro/bin/python Tools/voices.py`: español en `Resources/Voices/`, resto en `Voices/<código>/`, clave =
  FNV-1a de `HABLANTE\ntexto en ese idioma` (igual que `DemoGame.VoiceKey`). Chino/japonés usan misaki (`unidic-lite`;
  el `unidic` completo no está instalado). Verificar inteligibilidad con `whisper-cli` (`~/.whisper-models`).
- Hablantes nuevos necesitan entrada en `CAST` y en cada `LANGS[..]["cast"]`. Las acotaciones `[...]` no se leen.
- La ventana: `Tools/city_mix.py` mezcla sirenas y gritos de Commons (caché en `Tools/sounds/cache`) con `Tools/sounds/ciudad-sintetica.wav` (de `Tools/city_sound.py`). Las capas de fuera se ven por su cara trasera: las texturas se escriben invertidas. Recorte + emisión necesita `CutoutGlowTemplate.mat`. Música: `Resources/Music` (de antm_ai, con Suno).
- En WebGL no existe `AudioSource.time` ni `GetOutputData` para audio comprimido: usar relojes propios.

## Render ASCII

- `InkCamera.cs` + `Assets/Shaders/Ink.shader`. Atlas en `Resources/Glyphs`: `w<h>`/`u<h>` (Meslo LG S, mundo e
  interfaz) y `cjk-<zh|ja>-<h>` (Noto Sans, dos celdas por carácter), alturas 8–36, 48, 56.
- Re-hornear tras cambiar `AsciiGlyphs.txt` o los caracteres CJK: `swift Tools/BakeAsciiAtlas.swift` y
  `swift Tools/BakeCJKAtlas.swift` (fuentes en `~/Library/Fonts` y Google Fonts; ver README).
- Índices de glifo de 14 bits en la rejilla de la interfaz (R + bits altos en B); 256+ = atlas CJK.
- Las decals recortadas (sangre, huellas) dependen de `Assets/Scenes/CutoutTemplate.mat`; sin él salen cuadrados.

## Personajes y escenario

- El origen de Quaternius ya no está en disco: `Tools/build_characters.py` no se puede re-ejecutar. Los FBX de
  `Resources/Characters` se retocan sobre lo exportado (Blender sin interfaz: `/Applications/Blender.app/... -b -P`).
- El pelo ya viene orientado; no rotarlo. Las batas llevan un canesú añadido en lugar de muñones de manga.
- Gestos con IK (`Uncanny.cs`) solo para quien está de pie en pose neutra (Inés). Probado y descartado: rascar pared,
  manos en la pared, manos en las rodillas (deforman brazos o atraviesan paredes).
- `Prop(..., rest:true)` apoya un objeto sobre una superficie a la altura `position.y`.
