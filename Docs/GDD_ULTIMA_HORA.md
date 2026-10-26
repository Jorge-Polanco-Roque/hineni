# LOS QUE QUEDAN
## Documento de diseño de juego · La última hora de la Tierra

**Versión:** 1.0 · 25 de octubre de 2026  
**Estado:** propuesta de diseño para producción; todavía no implementada.  
**Proyecto:** SpookyGameJam · Unity · experiencia individual en primera persona.  
**Título de trabajo:** Los que quedan. Su disponibilidad comercial no se ha comprobado.

> Te despiertas de un coma. Una enfermera está limpiándote la cara. Faltan cincuenta y nueve minutos para que termine la Tierra. Ella lo sabe desde hace tres días. Sigue limpiándote la cara.

---

## 1. Visión

Una experiencia de terror filosófico sobre recuperar la conciencia cuando ya no queda futuro. El jugador despierta en un hospital durante la última hora del planeta. Mientras recupera movimiento, lenguaje y fragmentos de memoria, atraviesa un edificio donde todavía conviven el cuidado, la crueldad, la burocracia, el deseo de poseer y la necesidad de ser acompañado.

La Tierra termina en todos los finales. El jugador puede cambiar quién sabe la verdad, quién llega a otra habitación, qué historia queda dicha y junto a quién transcurre el último minuto.

La pregunta central es: **¿qué merece hacerse cuando nada de lo que hagamos podrá durar?**

El juego explora respuestas incompatibles sin convertir a sus personajes en portavoces de una conclusión única. Una mentira puede proteger y también arrebatar autonomía. Una confesión puede reparar algo o utilizar a la víctima como instrumento de alivio. Permanecer junto a alguien puede ser amor, miedo a estar solo o ambas cosas.

### Promesa al jugador

«Vas a habitar una última hora que no puedes salvar. Lo que hagas dentro de ella importará para personas concretas.»

### Parámetros iniciales

| Aspecto | Decisión de diseño |
|---|---|
| Género | Terror psicológico y corporal, exploración y decisiones narrativas |
| Cámara | Primera persona, manos visibles en interacciones clave |
| Duración completa | 60 minutos de tiempo activo; aproximadamente 75–100 con pausas y lectura |
| Primera entrega | Demo de 12–15 minutos que contenga el núcleo de la experiencia |
| Plataforma inicial | Escritorio; desarrollo y primera validación en macOS |
| Motor | Unity; editor encontrado: 6000.4.0f1; URP por validar |
| Estética | Manga de horror monocromo, geometría retro y tramas de glifos |
| Público previsto | Adulto; clasificación oficial pendiente si se publica |
| Combate | No hay armas, ataques ni sistema de daño |
| Fracaso | Pérdida de oportunidades y consecuencias; sin muerte que obligue a repetir |

## 2. Pilares y límites

1. **El fin es cierto; la conducta sigue abierta.** Ningún coleccionable, combinación de decisiones o habitación secreta salva el mundo.
2. **La filosofía se realiza con el cuerpo.** Sujetar una puerta, acercar una silla o escuchar una respuesta tiene prioridad sobre elegir una tesis en un menú.
3. **Lo hermoso y lo monstruoso coexisten.** Una misma persona puede haber sido cruel y cuidar de alguien con sinceridad. El cuidado no cancela sus responsabilidades.
4. **El horror invade lo reconocible.** Antes de deformar un pasillo, el jugador debe conocer su longitud, sus sonidos y a quien espera al final.
5. **La atención es un recurso.** Elegir a quién escuchar y dónde permanecer consume una parte finita de la hora.

Quedan fuera del alcance inicial: mundo abierto, supervivencia basada en hambre, crafting, enemigos de combate, multijugador, árbol de habilidades y diagnóstico médico simulado. Tampoco se plantea un giro que declare que todo era un sueño del paciente. El coma es una condición real dentro de la ficción; no explica ni invalida a las otras personas.

## 3. El mundo y su final

La historia sucede en el **Hospital del Umbral**, un hospital público de una ciudad hispanohablante ficticia. Lleva tres días atendiendo la noticia de un fenómeno astronómico irreversible llamado provisionalmente **el Frente**. La información disponible predice la destrucción de la Tierra; sus causas exactas quedan fuera del conocimiento de los personajes.

El Frente es ciencia ficción deliberada, no una hipótesis científica que el juego pretenda demostrar. Las comunicaciones coinciden en la hora de llegada. Hay personas que niegan la noticia, pero el guion no convierte la duda en una posibilidad secreta de supervivencia.

No queda una sociedad uniformemente salvaje. Hay calles abandonadas y cocinas comunitarias, personas que han huido y otras que regresan a cumplir una promesa. El hospital concentra esa contradicción: las normas han perdido buena parte de su autoridad, pero sus hábitos siguen vivos. Alguien exige una firma que nunca será archivada. Alguien mantiene una esterilización porque el dolor de los próximos diez minutos todavía importa.

Las anomalías empiezan antes de la llegada del Frente. El interior de los cuerpos y el de la arquitectura parecen confundirse: paredes con pliegues de piel, tuberías que laten, sombras que conservan una postura después de que su dueño se mueve. Son observables por otras personas. Su sentido permanece abierto: el mundo pierde estabilidad y los humanos intentan leer una intención en ello.

**Regla dramática:** la deformación no castiga el pecado, no revela quién es «malo» y no presenta la enfermedad o la discapacidad como corrupción moral. Puede alcanzar a cualquiera. El monstruo cósmico nunca confirma una doctrina.

## 4. Protagonista y despertar

**Álex**, de unos cuarenta años, trabajaba en la administración de recursos del propio hospital. Lleva diecinueve días en coma después de un accidente de tráfico. Su despertar empieza cuando faltan 59:40 para el final.

La recuperación está condensada para la ficción. No pretende reproducir una evolución clínica habitual. Durante los primeros minutos Álex necesita ayuda para incorporarse y un apoyo para caminar. La cámara no debe utilizar desenfoque permanente ni balanceo intenso para representar su estado.

Antes del accidente, Álex firmó una decisión que permitió cerrar una sala nocturna de atención. Defendió el cálculo como una forma de mantener otros servicios abiertos. La medida también dejó a personas esperando sin atención suficiente. El juego permite conocer ese daño sin fabricar una ecuación que lo resuelva moralmente.

En una ocasión Álex desobedeció otra norma para conseguir transporte para un paciente. Ambas acciones pertenecen a la misma vida. La revelación debe provocar reconocimiento de una persona contradictoria, no un giro de «eras el villano».

El accidente no fue un castigo y el coma no concede inocencia. Tampoco se exige que el jugador acepte una versión de sus recuerdos como verdad absoluta: registros y testimonios aportan datos; los fragmentos subjetivos aportan sensaciones.

### Recuperación como tutorial

| Momento | Acción aprendida | Función dramática |
|---|---|---|
| Abrir los ojos | Mirar y enfocar un objeto | Volver a distinguir el mundo |
| Mover una mano | Alcanzar un vaso con ayuda | Aceptar dependencia |
| Sentarse | Confirmar una interacción | Elegir participar |
| Nombrar algo | Primera respuesta breve | Recuperar una voz |
| Llegar a la puerta | Caminar y orientarse | Encontrar a quienes siguieron viviendo |

No hay minijuegos de rehabilitación ni penalización por dificultades motoras. Las acciones sostenidas tienen alternativa de pulsación única.

## 5. Personajes

### Inés — enfermera

Ha permanecido porque dos pacientes no pueden estar solos. También está enfadada con su familia por haberse marchado sin ella; le resulta más fácil seguir trabajando que admitirlo. A veces habla de una manera hiriente, y después vuelve para corregirse.

**Deseo:** escuchar a su hermana una vez más.  
**Contradicción:** considera imprescindible su presencia, pero necesita que alguien le diga que puede descansar.  
**Acción del jugador:** acompañar a un paciente mientras Inés intenta una llamada.  
**Límite:** su agotamiento no desaparece porque el jugador la elogie.

### Darío — responsable de seguridad

Controla la llave del patio interior. Acumula pilas y exige favores por abrir accesos que ya no debería vigilar. Ha pasado años siendo invisible y descubre una forma de sentirse necesario cuando la jerarquía se derrumba.

**Deseo:** que alguien le pida permiso hasta el final.  
**Contradicción:** protege con cuidado a un perro que entró al hospital.  
**Acción del jugador:** negociar, buscar una ruta alternativa o conseguir que otra persona lo confronte.  
**Límite:** su afecto por el animal no excusa la coacción sobre las personas.

### Teresa — antigua trabajadora de limpieza

Su pareja murió después de pasar una noche esperando atención, durante el periodo del cierre firmado por Álex. No conoce todos los eslabones de aquella decisión. El jugador puede ayudarle a reconstruirlos, pero ella no tiene obligación de ofrecer perdón.

**Deseo:** que alguien pronuncie el nombre de su pareja sin reducirlo a un expediente.  
**Contradicción:** sabe cuidar de un desconocido y puede rechazar cualquier cercanía de Álex.  
**Acción del jugador:** entregar documentación, reconocer responsabilidad o escuchar sin convertir la conversación en una petición de absolución.  
**Límite:** no se desbloquea su afecto mediante objetos o una respuesta «correcta».

### Samuel — técnico de mantenimiento

Mantiene una radio de emergencias y propone grabar testimonios aunque no haya nadie que pueda conservarlos. Puede pasar tanto tiempo intentando registrar una voz que deja de escuchar lo que esa persona le está pidiendo.

**Deseo:** dejar constancia de que estuvieron aquí.  
**Contradicción:** la memoria puede convertirse en una forma de evitar el presente.  
**Acción del jugador:** reparar la transmisión, grabar, escuchar en directo o apagar el equipo para conversar.  
**Límite:** ninguna grabación sobrevive milagrosamente al final.

### Lucía — hija adulta de Álex, presente por teléfono

La relación estaba deteriorada antes del accidente. Ya se había despedido una vez de alguien que no podía contestar. Una conexión breve le permite descubrir que ahora sí puede escucharla.

**Deseo:** decidir qué decir sin volver a asumir el cuidado emocional de su padre o madre.  
**Contradicción:** puede querer a Álex y no querer reconciliarse.  
**Acción del jugador:** llamar y aceptar que la conversación tiene límites.  
**Límite:** si la llamada no sucede, el juego no concluye que Álex nunca la quiso.

## 6. La filosofía como conflicto jugable

Los siguientes ejes son herramientas para escribir escenas, no nombres de escuelas que deban aparecer en los diálogos.

| Pregunta | Situación | Lo que debe quedar sin resolver |
|---|---|---|
| ¿Vale algo lo que no permanece? | Samuel prepara un archivo condenado a desaparecer | Registrar puede ser amor o evasión |
| ¿Tenemos derecho a una verdad que nos destruye? | Un paciente pregunta cuánto falta | Informar con cuidado no equivale a decidir por él |
| ¿Soy responsable de quien fui? | Álex encuentra su firma y escucha a Teresa | Recordar poco no borra el daño ni impone una identidad simple |
| ¿Qué le debemos a quien nos ha dañado? | Darío termina necesitando ayuda | Auxiliar no exige absolver |
| ¿Existe una acción desinteresada? | Inés necesita dejar el trabajo para llamar | Los motivos mezclados no vuelven falso todo cuidado |
| ¿Importa la libertad cuando el desenlace está fijado? | Todos los caminos llegan al Frente | Un destino común no vuelve idénticas todas las vidas |

Evitar discursos largos que expliquen el tema después de que la acción ya lo ha mostrado. Una respuesta puede terminar en una tarea doméstica, una corrección o una negativa. Los personajes necesitan agua, aire, privacidad y descanso además de ideas.

## 7. Bucle de juego y verbos

**Percibir una necesidad → acercarse → escuchar o inspeccionar → actuar → observar una consecuencia → decidir dónde gastar el tiempo restante.**

Los verbos principales son mirar, caminar, escuchar, preguntar, sostener, llevar, abrir, entregar, llamar y permanecer. El inventario tiene tres espacios para objetos de tamaño manejable. No hay economía, objetos vendibles ni coleccionables que cuenten como porcentaje moral.

### Escuchar

Acercarse inicia una conversación solo tras una confirmación clara. Mirar hacia otro lado no rompe automáticamente el diálogo. Retirarse permite despedirse o interrumpir. Las personas pueden pedir espacio y el jugador debe poder respetarlo.

### Sostener

Acciones como mantener una puerta o acompañar una respiración se realizan durante intervalos breves. No exigen pulsar un botón durante minutos. Tras iniciarlas, el jugador puede permanecer, hablar o terminar la ayuda. Debe saber qué deja pendiente si se marcha.

### Llevar

Objetos concretos: un vaso, una manta, una batería, una fotografía o una carpeta. Cada uno cambia una escena observable. La capacidad limitada obliga a priorizar, pero siempre se pueden dejar objetos en superficies identificables y recuperarlos.

### Decidir

Opciones expresadas en palabras que Álex diría: «Puedo quedarme mientras llamas», «No voy a prometerte eso», «La firma es mía». Evitar etiquetas abstractas como «empatía», «egoísmo» o «nihilismo».

Cuando una decisión cierra una posibilidad importante, la escena comunica el compromiso antes de confirmarlo: «Si vienes conmigo ahora, no llegaremos al teléfono antes de que corten esa línea». No revelar todas las consecuencias emocionales.

## 8. El reloj

El reloj maestro empieza en 59:40 tras abrir los ojos. Avanza durante movimiento, acciones y frases pronunciadas. Se detiene en pausa, ajustes, lectura de documentos y selección de respuestas. Esta convención se explica al inicio: el tiempo de lectura del jugador no consume el tiempo de Álex.

No hay cuenta atrás de selección de diálogo en la configuración predeterminada. Las escenas críticas se escriben con duración breve y permiten omitir una frase ya escuchada. Esperar deliberadamente es una acción disponible y distinta de pausar.

El tiempo aparece en relojes del hospital y en una consulta accesible del menú. Su consulta no requiere memorizar cifras de transmisiones. No hay barra de «bondad», puntuación final ni lista visible de personas salvadas.

### Ventanas de oportunidad

- Las llamadas al exterior funcionan hasta T−25:00; la radio anuncia la degradación antes del corte.
- La batería auxiliar puede alimentar una sesión de radio o abrir el acceso motorizado al patio. Existe una apertura manual que requiere cooperación y tiempo: el coste se entiende antes de usarla.
- A T−10:00 comienza la convergencia final; no se bloquea la pausa ni se obliga a correr.
- A T−03:00 ninguna misión nueva comienza. Se permiten conversaciones finales breves, caminar y permanecer.

Si una escena cruza un umbral, se resuelve en un punto de interrupción escrito y no cortando una palabra al azar. A cero llega el evento final; el diseñador debe asegurar que ninguna interacción pueda bloquearlo indefinidamente durante tiempo activo.

### Guardado

Guardado automático al iniciar capítulos y después de decisiones importantes. Guardar al salir conserva reloj, inventario, posiciones relevantes y estados narrativos. Reanudar no adelanta el reloj por el tiempo real fuera del juego. Tras terminar se habilita selección de capítulos para explorar otras acciones sin presentarlas como corrección de una mala puntuación.

## 9. Recorrido de la hora

La división siguiente regula el mundo, no obliga a terminar una misión por capítulo. Algunas escenas se pierden; el recorrido principal siempre sigue siendo transitable.

| Tiempo restante | Espacio y cambio | Conflicto principal |
|---|---|---|
| 59:40–50:00 | Habitación de recuperación; tutorial y noticia | ¿Quieres saber qué ocurre? |
| 50:00–38:00 | Sala común, puesto de enfermería, teléfono | ¿A quién dedicas tu primera capacidad de actuar? |
| 38:00–25:00 | Archivo y mantenimiento; anomalías visibles | ¿Qué haces con lo que descubres de tu vida? |
| 25:00–10:00 | Patio y corredor; caída de comunicaciones | ¿Qué obligaciones sobreviven a la autoridad? |
| 10:00–00:00 | Convergencia en sala, patio o habitación | ¿Cómo quieres estar presente? |

### Acto I — Alguien te estaba esperando

Inés ayuda a Álex. El primer gesto de bondad ocurre antes de que el jugador pueda merecerlo. Si pregunta la hora, ella responde primero con la hora del día. Después se corrige: «Te refieres a la otra». El cielo todavía parece normal.

Una persona ríe detrás de una cortina. Se esperaba un grito; es una broma privada. Introducir humanidad antes de mostrar deformación.

### Acto II — Lo que todavía se hace

Darío exige una autorización para abrir el patio. Inés necesita cinco minutos libres. El teléfono tiene conexión inestable. El jugador puede ayudar, llamar o investigar. Una ruta corta ofrece orientación; ninguna tarea secundaria es requisito para entender que el mundo termina.

El primer cambio imposible es pequeño: al volver de una habitación, el corredor tiene una puerta más. Los demás también la ven. Nadie sabe si abrirla.

### Acto III — Tu nombre en el papel

El archivo contiene la decisión firmada por Álex. Teresa aporta el nombre y la historia de la persona afectada. La escena puede ocurrir sin que Álex haya recuperado recuerdos visuales. La evidencia no depende de reunir una colección completa.

Entregar la carpeta no fuerza una reconciliación. Teresa puede pedir que Álex la deje sola. Cumplir ese pedido también es una acción significativa, aunque no dé una escena gratificante.

### Acto IV — Nadie va a comprobarlo

Los sistemas empiezan a apagarse. Darío pierde control del acceso y continúa dando órdenes. En una variante, queda atrapado detrás de una puerta deformada; ayudar requiere una colaboración previamente posible, no haber acumulado una puntuación de compasión.

Samuel recibe una voz que sigue enumerando nombres. No sabemos si alguien escucha los nuestros. La radio permite hablar, escuchar sin transmitir o dejarla para atender a una persona cercana.

### Acto V — Aquí

El jugador elige dónde quedarse o sigue caminando. Los personajes llegan según sus decisiones, deseos y posibilidades físicas; no se reúnen todos automáticamente alrededor del protagonista.

El último gesto sigue disponible aunque se hayan ignorado todas las misiones: acercarse a alguien que lo permita, abrir una ventana, sentarse o mirar el cielo. Una vida no queda reducida a su peor minuto, pero un gesto final tampoco borra sus daños.

## 10. Escena modelo: la silla

**Lugar:** sala común. **Duración activa:** 2–3 minutos. **Función:** demostrar la filosofía y las mecánicas con pocos recursos.

Inés quiere llamar a su hermana, pero un paciente se angustia al quedarse solo. Pide a Álex que acerque una silla. No pide salvarlo ni prometer que estará bien.

El jugador puede sentarse, hablar desde la puerta o decir que no puede. Si se sienta, el paciente pregunta si el cielo dolerá. Las respuestas disponibles reconocen lo que Álex sabe: «No lo sé», «Puedo cerrar la persiana», «¿Quieres que me quede?». No existe la opción informada de garantizar ausencia de dolor.

Después de un silencio, el paciente cuenta un hecho pequeño: una vez fingió perder una partida para que su hermano siguiera visitándolo. No se decide si aquello fue amor o manipulación. Pregunta si Álex tiene hermanos y acepta una respuesta incompleta.

Una anomalía recorre la pared: las juntas de los azulejos se alinean como una dentadura. El paciente la mira y pide que giren la silla hacia la ventana. La interacción continúa. El terror no cancela su preferencia.

Si Álex se marcha antes de que vuelva Inés, el paciente lo llama una sola vez. Si permanece, Inés puede completar su llamada. Más tarde tiene una frase distinta y puede ofrecer ayuda con la apertura manual. Esto se registra como un hecho, no como puntos.

**Criterio de calidad:** la escena debe resultar significativa sin música sentimental, sin primer plano de lágrimas y sin una notificación que felicite al jugador.

## 11. Horror y dirección visual

### Referencia estética

La referencia solicitada es el manga de Junji Ito: horror corporal, detalle obsesivo, contraste entre normalidad y aberración, y revelaciones visuales que obligan a detenerse. La obra tendrá personajes, composiciones y motivos propios. No se utilizarán personajes ni escenas de sus mangas como assets o guion.

El motivo original del proyecto será **la sutura**: uniones entre cosas que deberían permanecer separadas. Una grieta parece una costura; dos sombras comparten un borde; un edificio se pliega como si alguien intentara cerrar una herida. No utilizar espirales como emblema central.

### Gramática gráfica

- Blanco de papel envejecido y negro de tinta. El gris procede principalmente de trama, no de superficies lavadas.
- Geometría deliberadamente sencilla con siluetas inequívocas; detalle concentrado en rostros, manos y puntos de anomalía.
- Trama fina en zonas estables; glifos más visibles al aparecer una discontinuidad del espacio.
- El negro puede ocultar volumen, pero las rutas y objetos esenciales deben seguir siendo legibles.
- Revelaciones compuestas como cambios de página: giro de esquina, puerta que abre, cortina que se descorre. Evitar secuestrar la cámara para obligar a mirar.
- Rostros con expresión contenida. La exageración se reserva para momentos concretos, no para cada conversación.

### Integración del ASCII

El ASCII será parte de la textura visual, no una capa uniforme que destruya el dibujo. Se probará un render híbrido: contornos y volúmenes 3D, tramas de glifos en superficies y sombras, detalles de tinta en retratos e interacciones.

Paleta de glifos inicial: `. , : ; + x # @` y trazos diagonales para hachurado. Su selección responde a luminosidad y orientación visual; el atlas se estabiliza para evitar que pequeñas variaciones provoquen parpadeo constante. Las letras no contienen mensajes ocultos por defecto.

Si el render de pantalla completa impide leer expresiones o rutas, se limita a fondos y anomalías. La identidad manga tiene prioridad sobre demostrar la técnica ASCII. UI, subtítulos y opciones se dibujan después del efecto.

### Tres manifestaciones realizables

1. **La costura:** una línea atraviesa suelo y pared, y más tarde une dos puertas. Mesh y material animados; sin simulación de tejidos.
2. **La sala que respira:** paredes que se desplazan unos centímetros; objetos familiares conservan escala y posición para mostrar la imposibilidad.
3. **El cielo interior:** una ventana muestra el mismo corredor desde un ángulo imposible. Una composición específica evita necesitar un sistema general de portales.

El horror corporal se concentra en alteraciones de contorno, pliegues y relación entre superficies. La intensidad gráfica puede reducirse manteniendo el significado de las escenas. No habrá daño explícito a niños o animales como recurso fácil de impacto.

## 12. Sonido y lenguaje

La banda sonora principal es el hospital: ruedas, fluorescentes, ropa, respiración, un vaso apoyado, pasos que cambian cuando la electricidad se va. Las máquinas no deben convertirse en metrónomos que impongan ansiedad durante toda la partida.

Cada anomalía altera una regla sonora: un eco llega antes del paso, una voz conserva reverberación al entrar en una sala pequeña, una tubería parece respirar donde ya no hay corriente. Los efectos pueden anticipar información, pero ninguna pista necesaria depende exclusivamente de oír.

Música escasa. Reservar un motivo humano sencillo para una fuente situada en el mundo, por ejemplo alguien que tararea. Evitar un crescendo continuo durante los últimos diez minutos. La desaparición de sonidos familiares puede ser el acontecimiento principal.

Los diálogos deben poder decirse en voz alta sin sonar como ensayos. Frases provisionales de tono:

> **Inés:** «No sé qué se hace en el fin del mundo. Sé cambiar estas sábanas.»

> **Teresa:** «No te he pedido que te sientas peor. Te he pedido que lo llames por su nombre.»

> **Samuel:** «Si nadie va a escucharlo, ¿para qué grabamos?»  
> **Álex:** «Tú lo estás escuchando.»

> **Lucía:** «Podemos hablar. No sé si podemos arreglarlo. No me hagas prometer las dos cosas.»

Estas líneas orientan la escritura; el guion final deberá incluir también cansancio, humor y asuntos cotidianos para evitar una densidad sentenciosa constante.

## 13. Finales: estados de presencia

El desenlace físico es único. El encuadre, la compañía, las palabras y el sonido cambian. Se determina por la situación final y hechos concretos, no por sumar decisiones supuestamente buenas.

| Variante | Condición visible | Última imagen posible |
|---|---|---|
| Junto a alguien | Permanecer con una persona que acepte compañía | Dos manos cerca; tocar requiere consentimiento |
| Testigo | Seguir en la radio con el equipo disponible | La onda continúa mientras una voz busca otro nombre |
| A cielo abierto | Llegar al patio, solo o acompañado | La sombra de los cuerpos desaparece antes que ellos |
| Puerta cerrada | Elegir intimidad en una habitación | Luz bajo la puerta; el jugador decide si abrir |

Estar solo no es automáticamente un castigo. La voz final distingue entre haber pedido soledad y haber sido abandonado. La llamada a Lucía, la documentación entregada y otras decisiones alteran líneas y gestos, sin convertir cada combinación en una cinemática distinta.

No hay resumen de logros morales, supervivientes secretos ni narrador que explique cuál fue la respuesta correcta. Después del final se muestran créditos y una opción tranquila de volver al menú. No se exige repetir para descubrir «el verdadero final».

## 14. Arquitectura de implementación en Unity

La primera prueba debe resolver legibilidad y flujo narrativo antes de producir todo el hospital.

| Sistema | Responsabilidad |
|---|---|
| `WorldClock` | Tiempo restante, pausas autorizadas y umbrales; única fuente temporal |
| `NarrativeState` | Hechos tipados: llamada realizada, carpeta entregada, permiso de compañía |
| `InteractionController` | Enfoque, alcance, activación y cancelación accesible |
| `ConversationRunner` | Turnos de diálogo, elecciones, interrupciones y efectos de estado |
| `WorldEventDirector` | Cambios de fase y oportunidades, con eventos idempotentes |
| `CharacterSchedule` | Posiciones y disponibilidad según tiempo y decisiones |
| `PresentationController` | Tinta, tramas, anomalías y mezcla sonora; no decide moralidad |
| `SaveService` | Reloj, hechos, objetos y fase; formato versionado |

Ink es candidato para conversaciones y ramificaciones. Los sistemas físicos y el reloj permanecen en C#. Evitar dos relojes independientes o que una secuencia visual modifique directamente hechos narrativos.

Estados sugeridos: `InesCallCompleted`, `TeresaHasRecord`, `LuciaCallOutcome`, `BatteryUse`, `CourtyardAccess`, `CompanionshipConsent`. No crear variables `Goodness`, `HumanityScore` ni un equivalente oculto.

Para NPCs se priorizan posiciones preparadas y trayectos cortos. Si hay navegación, sus rutas deben seguir funcionando al activar anomalías. Transformaciones visuales no pueden dejar al jugador encerrado accidentalmente. Las alternativas de tránsito se diseñan antes de cerrar una ruta.

osASCII sigue siendo referencia técnica MIT pendiente de comprobar en Unity 6. No se promete que su integración produzca por sí sola la estética descrita. El render híbrido probablemente requerirá trabajo propio de shaders y arte.

## 15. Alcance y producción

### Demo de 12–15 minutos

Contenido: habitación inicial, sala común, un corredor y puesto de enfermería; Inés y un paciente; escena de la silla; llamada breve; una anomalía y una conclusión propia de la demo.

El reloj de esta demo representa un fragmento de la hora. No acelerar artificialmente todo el final del planeta para incluirlo. Termina cuando Álex llega a una ventana y comprende la dimensión del suceso, con continuidad clara hacia la versión completa.

### Versión completa acotada

- Un hospital con siete zonas reutilizables: recuperación, sala común, enfermería, archivo, mantenimiento/radio, corredor y patio.
- Cuatro NPCs principales físicamente presentes, una voz principal remota y pacientes secundarios limitados.
- Tres decisiones mayores y cuatro intervenciones pequeñas con consecuencias observables.
- Tres anomalías principales reutilizadas con variación.
- Cuatro encuadres finales con líneas variables.
- Presupuesto inicial de guion: 6.000–9.000 palabras totales, de las que una partida encuentra solo una parte. Ajustar tras medir duración real de lectura y voz.

### Hitos y condiciones de salida

1. **Prueba gráfica:** escena sencilla legible, rostro y manos reconocibles, tramas sin vibración molesta, UI intacta en una build macOS.
2. **Escena jugable:** la silla funciona de principio a fin, incluido marcharse, volver y guardar/reanudar.
3. **Recorrido completo en geometría básica:** se llega al final aunque se omitan todas las tareas opcionales; el reloj y los cortes se comportan correctamente.
4. **Arte y audio:** reemplazar recursos provisionales manteniendo tiempos y claridad del recorrido.
5. **Pruebas externas y pulido:** corregir lecturas involuntarias de recompensa moral, confusión espacial y diálogos que no admitan interrupción.

No fijar un calendario de producción fiable hasta conocer equipo, disponibilidad de arte y alcance de doblaje. Para una game jam, la entrega comprometida debe ser la demo; la hora completa es una expansión.

## 16. Validación, accesibilidad y riesgos

### Pruebas funcionales necesarias

- Cargar partida no repite entregas, llamadas ni consumo de batería.
- Pausa, lectura y elección detienen el reloj; caminar y esperar lo reanudan.
- Cada umbral ocurre una sola vez, incluso si una conversación se interrumpe.
- El final llega desde cualquier zona y acción sin bloqueo.
- Hay una ruta válida al tramo final sin completar misiones.
- Consentimiento y disponibilidad de personajes se respetan al activar finales.

### Pruebas de experiencia

Después de la demo, preguntar qué creía poder cambiar el jugador, qué gesto recuerda y por qué eligió quedarse o marcharse. Si la mayoría busca una cura secreta, reescribir la comunicación de la premisa. Si todos interpretan a un personaje como una tesis sin vida, añadir deseos cotidianos y fricción concreta. Si todos eligen una opción por recompensa instrumental obvia, revisar costes y consecuencias.

Objetivo de rendimiento inicial: 60 fps en el equipo de prueba, con hardware, resolución y ajustes registrados. Es una meta pendiente de medición, no un resultado actual.

Opciones mínimas: tamaño de texto, contraste, sensibilidad, inversión de cámara, subtítulos con hablante, controles reasignables, reducción de movimiento, desactivación de parpadeo y alternativa a acciones mantenidas. Aviso de contenido breve al inicio para hospitalización, muerte, fin del mundo y horror corporal; permitir consultar detalles sin imponerlos a todos.

| Riesgo | Respuesta de diseño |
|---|---|
| El reloj impide leer o escuchar | Pausa de lectura/elección; diálogos breves y umbrales anunciados |
| Todo parece una prueba de bondad | Hechos y consentimiento; ninguna puntuación ni final superior |
| La tragedia se vuelve melodrama uniforme | Humor cotidiano, silencios y conflictos pequeños |
| ASCII destruye la expresión manga | Render híbrido y prueba visual antes de producir assets |
| El coma convierte a Álex en una cámara vacía | Historia previa concreta y agencia gradual |
| El horror patologiza a los pacientes | Anomalías universales; separar enfermedad de monstruosidad |
| Las ramas multiplican el presupuesto | Convergencia espacial y variación de líneas/gestos |
| La certeza del fin anula la motivación | Consecuencias inmediatas para personas reconocibles |

## 17. Decisiones pendientes, con criterio provisional

- **Doblaje:** prototipo con texto y sonidos; grabar voces cuando el guion haya pasado pruebas.
- **Nombre y género de Álex:** mantener el nombre y evitar una personalización extensa; revisar en el guion para que Lucía tenga una relación concreta y consistente.
- **Intensidad del horror corporal:** empezar con deformación y detalle de tinta; subir intensidad solo si mejora la escena.
- **ASCII visible:** decidir tras comparar legibilidad de una misma habitación y un mismo rostro en dos tratamientos.
- **Producción final:** comenzar por la demo y conservar la hora completa como objetivo posterior.

## 18. Criterio rector

Antes de añadir una escena, preguntar: **¿qué puede hacer aquí el jugador que un espectador no podría hacer por él?**

Si la respuesta es únicamente «escuchar una explicación», falta diseño. Si puede conceder tiempo, aceptar una negativa, ayudar sin ser perdonado o decidir estar presente sin esperar recompensa, la escena pertenece a este juego.

La última imagen no tiene que demostrar que la humanidad merecía salvarse. Tiene que permitir recordar a alguien que estaba allí.
