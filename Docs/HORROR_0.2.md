# Ampliación de terror · 0.2

La amenaza interrumpe el cuidado: recuperar electricidad obliga a dejar a otras personas solas. El tiempo perdido no se recupera. El hospital no ofrece una explicación religiosa definitiva; sus paredes contienen palabras de alguien desesperado.

## Espacio y ritmo

La capilla sale del lado este del pasillo; mantenimiento está al oeste, más adelante. Ambas tienen puertas operables desde dentro y fuera. La capilla contiene bancos vacíos, velas y un altar con un fusible. Retirarlo oscurece el hospital y activa al Testigo. El recorrido de regreso transforma un espacio conocido en una situación peligrosa.

El Testigo avanza cuando dejas de mirarlo. Para inmovilizarlo, la linterna debe estar encendida, orientada hacia él y sin obstáculos. La proximidad cambia el sonido. Las puertas cerradas retrasan su avance; puede forzarlas. El armario de mantenimiento ofrece refugio. No hay combate ni batería consumible: la tensión nace de mirar, desplazarse y manipular el circuito.

Una captura devuelve al personaje a recuperación y consume treinta segundos. Conserva el fusible y el progreso del circuito, con una breve tregua para volver a orientarse. El reloj, la amenaza y el sonido de persecución se detienen durante pausa y conversación.

## Pintadas

Son textos originales de ficción con alusiones bíblicas, no citas atribuidas a versículos:

- «El Verbo se hizo carne. La carne sigue gritando.»
- «Padre, no contestes con mi voz.»
- «El séptimo día nos dejó solos.»
- «No fue resurrección. Algo aprendió a moverlos.»

La sangre se representa mediante letras rojas y escurrimientos físicos en las paredes, procesados por el mismo render ASCII. E permite leer el texto con claridad en la interfaz. Dos inscripciones aportan información práctica: la luz detiene al Testigo y la secuencia del cuadro eléctrico está codificada en palabras.

## Acertijo — contiene solución

Recoger fusible en el altar, instalarlo en el cuadro y activar 3 → 1 → 2. «Tres negaciones. Un solo nombre. Dos testigos.» Un error reinicia solamente la secuencia y atrae a la presencia con ruido. Resolver restaura la luz, retira la amenaza y permite avanzar hacia la conclusión narrativa.

## Implementación y conservación

`HorrorDirector.cs` controla amenaza, puzzle, linterna, sonido y estado persistente. `HospitalWorld.Horror.cs` construye la ampliación. `DemoGame.cs` conecta la progresión, guardado y HUD. Se preservaron los recursos y el shader ASCII existentes. Las versiones de los archivos anteriores a esta ampliación están en `Docs/Backups/horror-0.2`.

`--horror-test` ejecuta el recorrido de verificación y usa un archivo de guardado separado en `/tmp/spooky-horror-save.json`. Capturas y resultados salen en `/tmp/spooky-horror-qa`. Las pruebas incluyen interacción de habitaciones, mirada, movimiento, pausa, captura, escondite, circuito, guardado y acceso al cierre narrativo. No sustituyen una prueba humana del ritmo o la legibilidad en todas las pantallas.

## Verificación final

Compilación macOS completada con Unity 6000.4.0f1. Las 20 comprobaciones del ejecutable final pasaron; no se registraron errores ni excepciones de ejecución. Evidencias: `QA-Horror-0.2/results.txt`, `player.log` y siete capturas. La legibilidad directa de las pintadas depende de la distancia y la luz; la lectura con E conserva el texto completo.
