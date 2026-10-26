# Dirección de arte · ASCII

Las reglas que mide `Assets/Scripts/ArtDirector.cs`. Si cambian aquí, cambian allí.

## Cómo se construye la imagen

1. **Mundo 3D normal.** Modelos reales (personajes de Quaternius, props de Poly Haven) e iluminación con sombras.
2. **Rejilla del mundo a resolución nativa.** En Retina, 8×16 píxeles físicos (unas 360×112 celdas en una ventana de 1440×900 puntos); sin Retina, 7×14. Cada celda se muestrea en 2×3 subregiones y se elige el glifo cuya forma se parece más (vectores de forma de 6 dimensiones, técnica de Alex Harri), con realce de contraste direccional.
3. **Tono contra forma.** Las celdas planas usan una rampa silenciosa (` .:-=+*#%@`). Solo las celdas con estructura usan glifos de forma (`/ \ | _ ( ) [ ] < > ^ L J T Y V 7`…). Nunca se usan letras para rellenar superficies.
4. **Contornos.** Las discontinuidades de profundidad y los pliegues de normales se inyectan como trazos, así que las siluetas se dibujan aunque estén en penumbra. Se desvanecen con la distancia.
5. **Color.** Cada glifo toma el matiz de la escena bajo su celda, con saturación realzada. Las sombras tiran hacia verde-petróleo. Paleta: fluorescentes verde enfermizo, emergencia roja que late, ventana azul frío, piel cálida.
6. **Exposición.** Curva con hombro (las altas luces se comprimen, no se cortan), exposición automática según la luminancia media del encuadre y contraste local ("claridad") para que los rasgos se separen de la piel en penumbra.
7. **Interfaz en su propia rejilla de 9×18 px**, con la misma fuente (Meslo LG, Apache 2.0) y caracteres de caja.

## Cuerpos y rostros

- Proporciones cotidianas: `Tools/build_characters.py` adelgaza brazos, muslos y cuello, reduce pecho y caderas, agranda un poco la cabeza (con pelo, ojos y cejas) y baja los hombros. Todo ponderado por los pesos de piel, sin costuras.
- Movimiento dentro de límites anatómicos: la mirada se reparte entre columna, cuello y cabeza (máximo 40–50°) con amortiguación; respiración en pecho y hombros, balanceo de peso, deriva de brazos, microcorrecciones de la cabeza. Nada de cuellos torcidos: la inquietud viene de la cara y de lo que dicen.

- Shape keys en todos los personajes: `Parpadeo`, `Miedo` (ojos desorbitados, pupilas como puntos, cejas arriba) y `Grito` (cavidad negra y mandíbula caída). Los genera `Tools/build_characters.py`.
- `Uncanny.cs` les da vida: parpadeo irregular (o ninguno, para los perturbadores), respiración, la cabeza que te sigue (hasta 170° en el hombre de la esquina), tics, temblor, balanceo, golpes de frente contra la pared y susurros incongruentes.
- Al conversar, la cámara encuadra cabeza y hombros por encima del panel y se enciende una luz de rostro desde abajo: el rostro debe leerse sin ayuda.

## Reglas de plano

| Regla | Objetivo | Por qué |
|---|---|---|
| Exposición | tono medio 0,12–0,32 | En ASCII la cobertura del glifo ya oscurece; más arriba, todo se vuelve textura. |
| Espacio negativo | 30–70 % de celdas en negro | El negro es el papel: sin él no hay forma. |
| Rango | p95 − p5 ≥ 0,55 | Una imagen plana se lee como ruido. |
| Contraste del sujeto | ≥ 2,2× el fondo | El ojo va a las personas, no a las paredes. |
| Definición del sujeto | ≥ 45 % de celdas de forma | La silueta debe estar dibujada, no rellena. |

## Luz

- Ambiente casi nulo, materiales de entorno oscuros (muros ≤ 0,3; suelo ≤ 0,25).
- Una luz clave **lateral** por sujeto (el frontal aplana el volumen) y un recorte desde atrás.
- Los fluorescentes forman charcos de luz, no relleno. Uno parpadea en el pasillo.
- Las caras emiten un poco de luz propia: el rostro manda.
- Nada de geometría fina repetida en superficies grandes (baldosas en textura, no en cubos), porque cada arista se convierte en trazo.

## Uso

```sh
# build + informe
"Builds/macOS/Los que quedan.app/Contents/MacOS/Los que quedan" --art-director --art-out /tmp/arte
# además, buscar la curva tonal y escribirla para el juego (requiere rebuild)
... --art-director --tune --preset-out Assets/Resources/LookPreset.json
```

Genera `informe.md` con una nota y observaciones por plano, `hoja-de-contactos.png` (ASCII junto al 3D) y cada plano a tamaño completo.

## Referencias

- Alex Harri, [ASCII characters are not pixels: a deep dive into ASCII rendering](https://alexharri.com/blog/ascii-rendering): vectores de forma y realce de contraste.
- Codrops, [Beyond the Luminance Ramp: A Shape-Aware ASCII Renderer in Three.js](https://tympanus.net/codrops/2026/09/04/beyond-the-luminance-ramp-a-shape-aware-ascii-renderer-in-three-js/).
