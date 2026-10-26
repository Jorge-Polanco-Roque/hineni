# Translation brief — HINENI

HINENI ("Here am I", Isaiah 6:8) is a short philosophical horror game rendered in ASCII. You wake from a coma in the
Hospital del Umbral; the world ends in fifteen minutes (the Judgment). Nurses still change sheets, a doctor counts to
forty-one with his forehead against a wall, a man in a corner says "don't look at the ceiling", graffiti in blood
quotes scripture. The tone is literary, restrained and eerie; people say slightly wrong, incoherent things on purpose.
Keep that strangeness — do not "fix" it into normal sentences.

## Input / output

- Input: `Tools/i18n/source.json`, a list of `{ "es": ..., "ctx": "File:line" }`. Some entries also have
  `"forward"`: those are lines of EL TESTIGO, displayed reversed in the game. Translate the `forward` text (normal
  reading order) and use **the `forward` text as the key**; the build tool reverses your translation.
- Output: `Tools/i18n/<code>.json`, one JSON object `{ "<spanish key>": "<translation>", ... }` with **every** entry
  (395). Keys must be byte-identical to the input (`es`, or `forward` when present). Valid JSON, UTF-8.

## Must preserve

- `\n` line breaks: same number of lines, broken at equivalent places (wall writing and dialogue layout depend on them).
- `[stage directions]`: translate the content, keep the square brackets.
- `{0}`, `{1}` placeholders, unchanged.
- ALL-CAPS strings stay all caps (Latin scripts). They are wall writing in blood, signs, titles, HUD warnings.
- Key names `WASD`, `E`, `ESC`, `F5`, `Q`, `F`, `T−` are not translated. `RATÓN` = the mouse.
- Names: Álex (English: Alex), Inés, Lucía. The game title HINENI never changes.
- Numbers that matter: the breaker puzzle "TRES NEGACIONES. UN SOLO NOMBRE. DOS TESTIGOS." encodes the order 3, 1, 2 —
  keep three / one / two, in that order. The doctor says forty-one ("cuarenta y uno"); the lever is number 41.
  "CUARENTA Y UNO.\nCUARENTA Y UNO.\nCUARENT" ends with a word cut off mid-word: cut yours the same way.
- Quotation marks: `« »` become the language's own (English “ ”, Portuguese “ ”, Chinese and Japanese 「」).
- Scripture uses the canonical wording of each language: "El Verbo se hizo carne" (John 1:14), "el séptimo día",
  "Heme aquí" (Isaiah 6:8), "No fue resurrección".
- "Hospital del Umbral": translate the name once and use it everywhere (threshold / liminal).

## Length and register

- Interface strings (buttons, HUD, prompts like "Hablar", "Leer la pintada", "Encender") must stay short: about the
  Spanish length; Chinese and Japanese should be concise (each character takes two cells).
- Dialogue: natural spoken language for each character; Inés is gentle and tired, the doctor clinical and broken, the
  woman in the wheelchair childlike and wrong, the man in the corner frightened, Lucía raw and guarded.
- Single lowercase words like "armario", "voz", "velas" are internal names that may appear; translate them plainly.
