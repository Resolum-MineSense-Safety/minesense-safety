
Sin tecnicismos. Si algo suena raro, hay una comparación con la vida real.

## 1. La idea general en una frase

MineSense Safety cuida a los conductores de camiones mineros: **primero les toma el pulso del cansancio y, si hace falta, les toca la alarma en la cabina**.

## 2. Microservicio 1: el "chequeo de cansancio" (FatigueDetectionService)

Es como un **doctor que revisa 3 signos** del conductor y dice qué tan cansado está:
- **Parpadeo prolongado (PERCLOS)**: qué tanto del tiempo maneja con los ojos entrecerrados. De 0 (abiertos) a 1 (cerrados).
- **Parpadeos por minuto**: cuántas veces parpadea en un minuto. Lo normal es 15–20.
- **Variación del corazón (HRV)**: qué tan flexible late su corazón, en milisegundos. Mientras más bajo, más agotado.

Con esos 3 números, el sistema pone una **etiqueta**:
- 🟢 **Normal**: todo bien, sigue manejando.
- 🟡 **Warning (precaución)**: hay señales de sueño, hay que estar atentos.
- 🔴 **Critical (peligro)**: riesgo alto de quedarse dormido.

Reglas simples que usa (números de referencia, aún no calibrados con estudios reales):
- Peligro si pasa mucho tiempo con ojos cerrados (40% o más), o si están algo cerrados (25%) y el corazón además está muy bajo (< 20).
- Precaución si los ojos están algo cerrados (15%), o parpadea mucho (25+/min), o el corazón está bajo (< 30).

Cada chequeo se guarda con fecha y hora. Todo se borra si el sistema se apaga (aún no tiene memoria permanente).

### Sus 5 "ventanillas" (endpoints)

1. **Hacer un chequeo nuevo** — Le pasas los datos del conductor (su código, el código del turno y los 3 signos) y te devuelve la etiqueta (Normal/Warning/Critical) más un aviso de `requiresAlert` (¿hay que poner alarma? sí/no).
2. **Ver un chequeo por su código** — Con el código del chequeo, ves su resultado.
3. **Ver el historial de un conductor** — Todos sus chequeos, del más nuevo al más viejo.
4. **Ver el último chequeo** — Solo el más reciente de un conductor (¿cómo está *ahora*?).
5. **Ver los chequeos de un turno** — Todos los del mismo turno, en orden de hora (para ver cómo fue empeorando o mejorando).

## 3. Microservicio 2: la "alarma en cabina" (AlertService)

Es como una **alarma con acuse de recibo**: suena en la cabina, el conductor debe apretar "entendido", y si no lo hace a tiempo, avisa al supervisor.

Cada alarma tiene un **estado**:
- 📢 **Issued (sonando)**: recién creada.
- ✅ **Acknowledged (confirmada)**: el conductor apretó "entendido".
- ⏫ **Escalated (escalada)**: nadie la confirmó a tiempo y pasó al supervisor.

Tiempos para confirmar antes de que escale: **30 segundos** si es grave (Critical), **2 minutos** si es leve (Warning).

### Sus 7 "ventanillas" (endpoints)

1. **Crear una alarma** — Dices para qué conductor, de qué chequeo viene y qué tan grave es (Warning/Critical). Nace sonando.
2. **Ver una alarma por su código** — Su estado actual y sus horas.
3. **Ver alarmas de un conductor** — Todas las suyas, de la más nueva a la más vieja.
4. **Ver alarmas por estado** — La cola de trabajo: todas las "sonando", o las "confirmadas", o las "escaladas". Es lo que mira el supervisor.
5. **Confirmar (conductor)** — El conductor aprieta "entendido". Solo funciona si aún está sonando; si ya se confirmó o escaló, da error.
6. **Escalar a mano (supervisor)** — El supervisor la sube de nivel cuando lo cree necesario, sin esperar el tiempo.
7. **Escalar las vencidas (barrido)** — Revisa todas las que siguen sonando, y las que ya pasaron su tiempo las escala de golpe. Hay que apretarlo a mano cada cierto tiempo (aún no es automático).

## 4. Cómo trabajan juntos (y qué les falta)

Hoy funcionan **por separado**: el chequeo dice "hay que poner alarma" (`requiresAlert: true`), pero **nadie crea la alarma automáticamente** — una persona (o un programa futuro) debe crearla a mano con la ventanilla 1 del AlertService. Conectar eso es el paso pendiente más importante.

## 5. Ejemplo de la vida real

1. El sensor manda: ojos 28% cerrados, 22 parpadeos/min, corazón en 18 → el chequeo dice 🔴 **Critical, hay que poner alarma**.
2. Se crea la alarma **sonando** para ese conductor.
3. Si en 30 segundos no aprieta "entendido", al pasar el barrido se **escala** y el supervisor la ve en su cola de escaladas.
