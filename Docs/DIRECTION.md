> **Propuesta anterior, sustituida el 25/10/2026.** La dirección vigente está en [Los que quedan — documento de diseño](GDD_ULTIMA_HORA.md). Este archivo se conserva como antecedente; la estación de transmisión ya no es la premisa del proyecto.

# Dirección propuesta: FRECUENCIA MUERTA

Concepto de trabajo; nombre y ambientación todavía no aprobados como diseño final.

Eres el operador nocturno de una estación de retransmisión abandonada. Una terminal recibe mensajes de una habitación que no existe en los planos. Para restablecer la señal debes recorrer el edificio, pero cada intervención cambia lo que aparece al volver.

## Identidad visual

Mundo 3D real de Unity, visto a través de una pantalla de caracteres. El ASCII es la representación visual: colisiones, iluminación y comportamiento siguen siendo sistemas 3D. No hace falta ejecutar HTML ni convertir toda la lógica a una cuadrícula textual.

Propuesta inicial: negro carbón, fósforo marfil y acentos ámbar; rojo reservado para peligro. Siluetas claras, composición con grandes masas de sombra y pocas luces. Evitar que la tipografía convierta toda la escena en ruido. Terminales y subtítulos mantienen texto nítido.

Probar dos variantes antes de comprometer la producción: ASCII de pantalla completa y mundo retro con ASCII solo en un visor. Elegir según legibilidad, tensión y rendimiento observados.

## Primera demo: 8–12 minutos como objetivo de diseño

Un nivel compacto: recepción, corredor, sala eléctrica y cabina de transmisión. Una amenaza, tres interacciones relevantes y un desenlace. Sin combate en esta propuesta.

Secuencia: recibir una transmisión → localizar un fusible → devolver energía → descubrir que el corredor ha cambiado → decidir si responder → escapar o completar la transmisión.

El jugador aprende una regla consistente: activar la señal revela información, pero también permite que algo localice su posición. La tensión debe venir de esa decisión y del sonido espacial, además de los cambios visuales.

## Arquitectura prevista

- Unity 6000.4.0f1 instalado; validar URP y paquetes compatibles al crear el proyecto.
- Controlador en primera persona, interacción por raycast y acciones configurables.
- Estado del juego separado del renderizado: objetivo, energía, inventario mínimo y amenaza.
- Pase ASCII con atlas de glifos ordenado por densidad y tamaño de celda ajustable.
- UI posterior al efecto; opciones para reducir parpadeo, desactivar balanceo de cámara y ajustar sensibilidad.
- Audio con canales independientes de ambiente, efectos y voz; subtítulos para pistas relevantes.

## Criterios de acabado

Una demo profesional necesita inicio, pausa, reinicio y final coherentes. Ningún objetivo debe depender de leer caracteres minúsculos o de distinguir solo un color. La amenaza debe comunicar sus reglas y no atravesar puertas o paredes por errores de navegación.

Objetivo provisional: 60 fps en el equipo de prueba, midiendo el coste del pase ASCII con el profiler. Registrar hardware, resolución y configuración antes de publicar cifras. Verificar sombras, orientación del atlas y efecto gráfico en una build real para macOS/Metal, no solamente en la vista del editor.

## Orden de producción

1. Prueba visual: habitación, luz, ASCII regulable, UI y medición de rendimiento.
2. Prototipo jugable: recorrer, interactuar, abrir puerta y completar un objetivo.
3. Demo completa: cuatro espacios, audio, amenaza y desenlace.
4. Pulido: transiciones, opciones, mezcla, corrección de errores y playtest con personas que no conozcan el mapa.

Estado actual: dirección y selección técnica documentadas. No hay todavía escena, build, assets integrados ni métricas de rendimiento.
