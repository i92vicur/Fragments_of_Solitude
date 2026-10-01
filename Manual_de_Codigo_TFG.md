# Manual de Código del proyecto Fragments of Solitude

## 1. Introducción

Este documento revisa el estado actual del proyecto en el repositorio y actualiza la documentación técnica a partir de la implementación real disponible en Unity. La intención no es rehacer el manual desde cero ni sustituir su estructura general, sino conservar la línea académica y el nivel de detalle que siguen siendo válidos y ajustar los apartados que, por evolución del proyecto, ya no reflejan el código actual.

La revisión se ha realizado directamente sobre el contenido de `Assets/`, `Assets/Scripts/`, `Assets/DialogueSystem/` y las escenas del proyecto. Cuando un dato del manual anterior contradice la implementación actual, prevalece la estructura real del código y se documenta esa diferencia sin ocultarla.

### Nota sobre nomenclatura heredada: Level2/Level3

La discrepancia más importante es la siguiente: el nombre de la escena principal del bloque jugable del roguelite es `Level2` en `SceneLoader`, y además el menú principal muestra la etiqueta `Level 2 (RogueLite)`, mientras que la lógica del sistema se encuentra físicamente en `Assets/Scripts/Level3/`.

La estructura actual del repositorio muestra una nomenclatura interna heredada: la carpeta de scripts `Level3` contiene la implementación del segundo bloque jugable, pero el flujo de escena y la interfaz del menú siguen referenciando la escena como `Level2`. En otras palabras, la carpeta de scripts no se corresponde literalmente con el nombre de la escena, pero la implementación real actual es la que debe reflejarse en este manual. No se ha renombrado la carpeta de scripts para ajustarla a la nomenclatura conceptual del nivel; se documenta, por tanto, como una discrepancia histórica.

---

## 2. Estructura general del proyecto

La organización del proyecto sigue la convención habitual de Unity, con una separación clara entre activos artísticos, datos, escenas, lógica de gameplay y utilidades editor.

### Carpetas relevantes del repositorio actual

- `Assets/Scenes/`: escenas del juego. En la versión actual existen escenas principales como `MainMenu`, `InitialGameMenu`, `Level0`, `Level2`, `Level4`, `Level5` y `PuzzleMenu`. El repositorio también incluye subcarpetas de escenas por nivel.

- `Assets/Scripts/`: lógica principal del proyecto. La organización actual es por bloques funcionales, no por un único patrón global. Las carpetas relevantes son:
  - `CoreCommon`: sistemas transversales de flujo, menú, pausa, configuración, progreso, HUD y audio/VFX.
  - `Level0`: secuencias y lógica específica del nivel 0.
  - `Level1`: puzzles y sistemas de interacción por tablero, incluyendo el puzzle de restricciones y el longest path.
  - `Level3`: implementación del bloque jugable roguelite, con generación del mapa, combate, enemigos, experiencia y mejoras.
  - `Level4`: juego de plataformas y fase 2 con controles, cámaras y transiciones.
  - `Level5`: combate por turnos con controlador, UI y combatientes.

- `Assets/DialogueSystem/`: sistema de diálogo basado en `DetectPlayer`, `EasyDS`, `Parser` y `DialogueManager`. Incluye una estructura de editor y de scripts específicos para la lógica de conversaciones.

- `Assets/Editor/`: utilidades editor para crear o reconstruir escenarios, generar elementos de nivel y preparar escenas de pruebas.

- `Assets/Art/`: recursos visuales de personajes, niveles, UI y VFX. La estructura está organizada por tipos de contenido (Characters, Levels, Shared, UI, VFX).

- `Assets/Audio/`: clips de música y efectos sonoros del proyecto. Se usa como almacenamiento de audio de gameplay y UI.

- `Assets/Fonts/`: tipografías utilizadas por la interfaz, especialmente `PressStart2P-Regular.ttf`.

- `Assets/Resources/`: recursos cargados dinámicamente desde código, como mezcladores de audio y fuentes.

- `Assets/ScriptableObjects/`: data containers específicos del proyecto; en la versión actual el contenido relevante se concentra en `Level3`.

- `Assets/Settings/`: configuración del pipeline y de la escena renderizada por Unity.

- `Assets/Prefabs/`, `Assets/Materials/`, `Assets/Settings/` y `Assets/Tests/`: depósitos complementarios para prefabs, materiales, configuración técnica y pruebas de lógica si existen en la versión actual.

La estructura refleja una organización de gameplay por escenas y por secciones funcionales, con varios “hubs” reutilizables para audio, VFX y flujo general.

---

## 3. Organización de los scripts

La revisión del código del repositorio actual muestra que la lógica está repartida entre componentes reutilizables del proyecto y sistemas específicos de cada bloque jugable. A continuación se presenta un inventario actualizado según la estructura real existente.

### 3.1. `Assets/Scripts/CoreCommon`

| Script | Responsabilidad principal |
|---|---|
| `AudioHub.cs` | Hub central de reproducción de efectos de audio para hit, muerte, boss, nivel, impacto y victoria. |
| `CameraFollow.cs` | Seguimiento de cámara general para escenas o sistemas que requieran movimiento de cámara con objetivo y suavizado. |
| `DialogueGate.cs` | Señal estática de control de diálogo activo; actúa como puerta de bloqueo para evitar que sistemas de gameplay continúen mientras una conversación está abierta. |
| `DialoguePanelVisibility.cs` | Sincroniza la visibilidad del panel de diálogo con el contenido del texto activo. |
| `GameProgressManager.cs` | Gestor de progreso global del juego; persiste flags de miniboss/escenas completadas con `PlayerPrefs`. |
| `HudController.cs` | HUD global de gameplay con salud, experiencia, resúmenes de nivel, pausa y estado del jefe. |
| `InitialGameMenuController.cs` | Controlador del menú inicial, incluyendo animaciones y estado de carga. |
| `MainMenuController.cs` | Menú principal con navegación a niveles, minijuegos y flujo de nueva partida. |
| `MainMenuHoverPreview.cs` | Previsualización de elementos del menú principal. |
| `PauseManager.cs` | Gestor de pausas con razones múltiples (`LevelUp`, `Death`, `Victory`, etc.), usando un conjunto de razones activas. |
| `RestartController.cs` | Reinicio de escena y teclado de restart. |
| `SceneLoader.cs` | Carga de escenas principales y de transiciones entre fases del proyecto. |
| `SettingsManager.cs` | Configuración de audio y pantalla, persistida con `PlayerPrefs` y aplicada con `AudioMixer`. |
| `SettingsPanelController.cs` | Panel de configuración y actualización de valores visuales. |
| `VFXHub.cs` | Hub de partículas/efectos visuales para impactos, muertes y eventos de boss. |

### 3.2. `Assets/Scripts/Level0`

| Script | Responsabilidad principal |
|---|---|
| `Level0DefeatSequence.cs` | Secuencia de derrota asociada al nivel 0 y al flujo de fin de combate o caída. |

### 3.3. `Assets/Scripts/Level1`

| Script | Responsabilidad principal |
|---|---|
| `ConstraintPuzzleCheckHandler.cs` | Control de comprobación y validación de la disposición del puzzle de restricciones. |
| `ConstraintPuzzleIntegration.cs` | Integración del puzzle de restricciones con el flujo del nivel o la UI. |
| `ConstraintPuzzleManager.cs` | Lógica principal del puzzle de restricciones: posiciones, validación, permutaciones y comprobación de solución. |
| `ConstraintPuzzleStateManager.cs` | Estado del puzzle y seguimiento de estado interno. |
| `ConstraintPuzzleSpriteSkin.cs` | Ajuste visual de apariencia del puzzle según estado o elementos. |
| `DraggableLetter.cs` | Interacción drag-and-drop de letras o elementos del puzzle. |
| `LongestPathPuzzleBoardView.cs` | Vista visual del tablero del longest path puzzle. |
| `LongestPathPuzzleManager.cs` | Gestión del grafo, nodos, conexiones, recorrido y validación del longest path. |
| `PuzzleBoard.cs` | Tablero genérico y preparación del estado visual. |
| `PuzzleConnection.cs` | Conexión entre nodos para el puzzle de rutas. |
| `PuzzleHud.cs` | HUD del puzzle y feedback visual. |
| `PuzzleMenuController.cs` | Menú de selección de puzzles y navegación entre minigames. |
| `PuzzleNode.cs` | Nodo individual del grafo del longest path. |
| `PuzzleTile.cs` | Tile visual o interactivo del puzzle. |
| `SlotDropZone.cs` | Zonas de colocación del tablero o puzzle draggable. |

### 3.4. `Assets/Scripts/Level3`

La carpeta `Level3` contiene la implementación actual del bloque jugable de tipo roguelite, pese a la discrepancia de nombre con la escena `Level2`.

| Script | Responsabilidad principal |
|---|---|
| `ArenaBorder.cs` | Límite del área del combate/arena para controlar bordes y límites de movimiento. |
| `ChestController.cs` | Control del cofre y comprobación de interacción del jugador. |
| `ChestSpawner.cs` | Generación de cofres en la arena o mapa. |
| `GeneratedFloor.cs` | Generación de suelo o rejilla procedural. |
| `HealthBar.cs` | Barra de vida para entidades del roguelite. |
| `LevelMapData.cs` | Datos estructurales del mapa generado. |
| `LevelMapGenerator.cs` | Generación procedural del mapa del nivel. |
| `MapStructure.cs` | Modelo o definición estructural del mapa y sus elementos. |
| `Combat/Health.cs` | Sistema de salud para las entidades de combate. |
| `Combat/ExperiencePickup.cs` | Recolección de experiencia por pickups. |
| `Combat/PlayerAutoAttack.cs` | Ataque automático del jugador y lógica de proyectiles. |
| `Combat/PlayerBossMegaAttack.cs` | Ataque especial del jefe o mega ataque del jugador. |
| `Combat/PlayerProjectile.cs` | Proyectil del jugador con impacto, daño y VFX. |
| `Core/LevelUpController.cs` | Cola y selección de mejoras al subir de nivel. |
| `Core/Phase1Level6Staircase.cs` | Escalera o transición que lleva a la fase 2 del nivel del roguelite. |
| `Enemies/BossAnimationController.cs` | Animación del jefe. |
| `Enemies/BossFeedback.cs` | Feedback del jefe al recibir daño o morir. |
| `Enemies/BossMarker.cs` | Marcador para enemigo jefe. |
| `Enemies/EnemyContactDamage.cs` | Daño por contacto del enemigo con el jugador. |
| `Enemies/EnemyDeath.cs` | Lógica de muerte del enemigo y feedback de VFX/audio. |
| `Enemies/EnemyMovement.cs` | Movimiento del enemigo. |
| `Enemies/EnemySpawner.cs` | Generación de enemigos y lógica de oleadas. |
| `Enemies/RegularEnemyAnimationController.cs` | Animación de enemigos normales. |
| `Enemies/SimpleEnemySpriteAnimator.cs` | Animación básica con sprites simples. |
| `Player/PlayerAnimationController.cs` | Animación del jugador del roguelite. |
| `Player/PlayerBossAttackPieces.cs` | Piezas asociadas al ataque del jefe del jugador. |
| `Player/PlayerDeath.cs` | Muerte del jugador y tratamiento de pausa o reset. |
| `Player/PlayerExperience.cs` | Experiencia y niveles del personaje. |
| `Player/PlayerMovement.cs` | Movimiento del jugador. |

### 3.5. `Assets/Scripts/Level4`

| Script | Responsabilidad principal |
|---|---|
| `Core/BalancePlatform.cs` | Plataforma equilibrada o de balanceo. |
| `Core/DisappearingPlatform.cs` | Plataforma que desaparece temporalmente. |
| `Core/MovingPlatform.cs` | Plataforma móvil. |
| `Core/Phase2CameraScroller.cs` | Cámara de scroll vertical para la fase 2 del level 4. |
| `Core/Phase2DeathController.cs` | Control de muerte en la fase 2 del platformer. |
| `Core/Phase2GoalController.cs` | Objetivo y victoria de la fase 2. |
| `Core/PlatformerAudioHub.cs` | Audio específico del level 4. |
| `Core/PlatformerCameraFollow.cs` | Seguimiento de cámara lateral en el nivel de plataformas. |
| `Core/PlatformerFallRestart.cs` | Reinicio por caída a vacío. |
| `Core/PlatformerPhase2Transition.cs` | Transición hacia la fase 2. |
| `Player/Phase2CharacterAnimationController.cs` | Animación del personaje en la fase 2. |
| `Player/Phase2MovementController.cs` | Movimiento del personaje en la fase 2. |
| `Player/PlatformerCharacterAnimationController.cs` | Animación del personaje del platformer. |
| `Player/PlatformerJumpController.cs` | Salto cargado, dash y rebote al aterrizar. |
| `Player/PlatformerPlayerController.cs` | Estado del personaje del platformer: suelo, aire, salto, etc. |
| `UI/Phase2VictoryMenu.cs` | Menú de victoria de la fase 2. |
| `UI/PlatformerDashDebugUI.cs` | Depuración del dash. |
| `UI/PlatformerJumpChargeUI.cs` | UI del salto cargado. |
| `UI/PlatformerPauseMenu.cs` | Menú de pausa del platformer. |

### 3.6. `Assets/Scripts/Level5`

| Script | Responsabilidad principal |
|---|---|
| `Combatant.cs` | Entidad combatiente con HP, defensa, ataque, magia y defensa temporal. |
| `Level5BattleController.cs` | Control del flujo de turnos, daño, final de batalla y ataque enemigo. |
| `Level5BattleUI.cs` | Interfaz de acción del combate por turnos con botones de ataque, defensa, objeto y magia. |
| `Level5PauseMenu.cs` | Menú de pausa específico del Level 5. |

### 3.7. `Assets/DialogueSystem`

| Script | Responsabilidad principal |
|---|---|
| `DetectPlayer.cs` | Detección de proximidad del jugador para activar interacciones de diálogo. |
| `DialogueManager.cs` | Control del panel de diálogo, texto, velocidad de tipeo y referencias visuales. |
| `EasyDS.cs` | Secuencia principal del sistema de diálogo: inicio, avance, fin y habilitación/deshabilitación del control del jugador. |
| `Parser.cs` | Procesamiento de etiquetas de línea (`/line`, `/end`) para avanzar contenido de diálogo. |

### 3.8. Scripts del editor

Los scripts de editor no forman parte del gameplay principal, pero sí son relevantes para la generación y mantenimiento del proyecto actual:

- `AddPhase4StateManager.cs`
- `AssignMainMenuPreviewFrames.cs`
- `BuildConstraintPuzzleUI.cs`
- `CreateArenaBoundaries.cs`
- `CreateDialogueTestNPC.cs`
- `CreateFloorGenerator.cs`
- `CreateFloorVisuals.cs`
- `CreateLevel3DefaultMapAsset.cs`
- `CreateSpawnPoints.cs`
- `MigrateLevel3MapStructures.cs`
- `PlacePhase1Level6Staircase.cs`
- `RebuildLevel4Phase2Scene.cs`

Por su propósito, se entienden como utilidades de nivel de editor y no como sistema runtime del juego.

---

## 4. Arquitectura basada en componentes

La arquitectura actual del proyecto no es un MVC formal ni un Clean Architecture ni una arquitectura hexagonal; el código se organiza como una composición de componentes de Unity. Cada script encapsula una responsabilidad concreta, y la coordinación entre sistemas se mueve por referencias serializadas, búsquedas en escena y eventos de Unity.

### 4.1. Separación de responsabilidades

La división se observa de manera nítida entre:

- sistemas transversales (`CoreCommon`),
- sistemas de nivel (`Level1`, `Level3`, `Level4`, `Level5`),
- y utilidades auxiliares (`Editor`, `DialogueSystem`).

Ejemplos concretos:

- `SceneLoader` centraliza la navegación entre escenas, pero no implementa lógica de gameplay.
- `PauseManager` coordina el tiempo del juego y comparte el estado de pausa con UI y entidades del nivel.
- `GameProgressManager` y `SettingsManager` no mezclan acciones de nivel ni de UI; ambos persisten estado con `PlayerPrefs` y se mantienen vivos con `DontDestroyOnLoad`.
- `HudController` observa el estado del jugador y del nivel sin convertirse en el ejecutor del juego en sí.
- `Level5BattleController` gestiona la lógica de turno y daño; `Level5BattleUI` solo representa y permite interacciones de usuario; `Combatant` encapsula HP y atributos numéricos del combatiente.

### 4.2. Composición mediante componentes

Muchos objetos del proyecto son agregados de varios `MonoBehaviour` distintos. Un ejemplo claro es el jugador de Level 4 o Level 3, que puede combinar movimiento, animación, salud, experiencia, ataque, colisiones y cámara. La lógica de cada parte se mantiene separada y se conecta por referencias serializadas o por búsquedas del tipo `FindAnyObjectByType` / `GetComponent`.

### 4.3. Comunicación mediante eventos

La comunicación con eventos existe de forma puntual y concreta, especialmente en los sistemas de progresión y de UI:

- `PlayerExperience.OnLevelUp` dispara la cola de mejoras en `LevelUpController`.
- `PauseManager.OnPauseStateChanged` se usa para refrescar UI y otros componentes que necesitan responder a la pausa.
- `Level5BattleController` expone `OnTurnChanged`, `OnBattleEnded` y `OnEnemyAttack` para sincronizar la UI del combate.
- `ConstraintPuzzleManager` usa eventos `LayoutChanged` y `PuzzleSolved` para actualizar la representación visual y el estado del puzzle.

No existe una infraestructura de eventos global de todo el proyecto; la interrelación se da mediante eventos locales y state observers.

### 4.4. Separación entre estado, lógica y representación

La separación también es visible en el reparto de responsabilidades:

- `Combatant` almacena estado endurecido (HP, defensa, nombre, bandera de enemigo).
- `Level5BattleController` contiene la lógica atacante/defensiva de resolución del combate.
- `Level5BattleUI` organiza la representación visual y los botones de acción.

Lo mismo ocurre en otras áreas:

- `SettingsManager` conserva la configuración y la persistencia; `SettingsPanelController` actualiza la UI.
- `GameProgressManager` mantiene los flags de progresión; las escenas o menús marcan el estado correspondiente cuando se completa un bloque.
- `LevelUpController` aplica mejoras sobre `PlayerMovement` y `PlayerAutoAttack` sin mezclar la lógica de XP con la de recompensa visual o HUD.

### 4.5. Clases serializables y referencias desde Inspector

El proyecto usa con frecuencia campos serializados para conectar referencias entre componentes y evitar depender excesivamente del código de búsqueda. Este patrón se observa en:

- `SceneLoader` con nombres de escena públicos o serializados.
- `HudController` y `Level5BattleUI` con referencias a `Button`, `Text`, `Combatant`, etc.
- `AudioHub` y `VFXHub` con prefabs o clips asignados desde el Inspector.
- `PlatformerJumpController` con valores de balanceo y de salto.

Este uso de `SerializeField` ayuda a la edición desde el editor y a la separación entre datos configurables y lógica.

---

## 5. Sistemas comunes

### 5.1. `SceneLoader`

Componentes principales:

- `SceneLoader` en `Assets/Scripts/CoreCommon/SceneLoader.cs`.

Responsabilidad:

- Encapsula el flujo de carga de escenas del juego, incluyendo `Level2`, `Level4`, `Level5`, menus de puzzle y menú principal.
- Restablece `Time.timeScale` antes de la carga para garantizar que el siguiente escenario arranque en estado neutral.

Comunicación:

- `MainMenuController`, `PuzzleMenuController`, `RestartController`, `Phase1Level6Staircase` y otros sistemas llaman a sus métodos de carga.

### 5.2. `PauseManager`

Componentes principales:

- `PauseManager` en `Assets/Scripts/CoreCommon/PauseManager.cs`.

Responsabilidad:

- Mantiene un conjunto de razones de pausa (`LevelUp`, `Death`, `Victory`, `PlatformerMenu`, `Level5Menu`, etc.), de modo que el juego puede pausarse por varios motivos simultáneos y reanudar solo cuando todos hayan desaparecido.
- Actualiza `Time.timeScale` de forma centralizada.

Comunicación:

- `HudController`, `LevelUpController`, `Phase2VictoryMenu`, `PlatformerPauseMenu`, `Level5PauseMenu` y otros sistemas se suscriben a `OnPauseStateChanged` para reaccionar al cambio.

### 5.3. `AudioHub` y `VFXHub`

Componentes principales:

- `AudioHub.cs`
- `VFXHub.cs`

Responsabilidad:

- Centralizar reproducción de clips de sonido y partículas visuales para eventos repetidos del juego.
- Evitar que cada enemigo o atacante llame directamente a un audio source aislado.

Comunicación:

- `EnemySpawner`, `PlayerProjectile`, `BossFeedback`, `EnemyDeath`, `LevelUpController` y otros scripts llaman a `AudioHub.Instance` / `VFXHub.Instance` cuando ocurre un evento importante.

### 5.4. `HudController`

Componentes principales:

- `HudController` en `Assets/Scripts/CoreCommon/HudController.cs`.

Responsabilidad:

- Observa la salud, experiencia, estado de pausa, puertas de interacción y nivel del jugador.
- Crea un HUD si no existe en la escena y sincroniza la información con `Health`, `PlayerExperience`, `LevelUpController` y `PauseManager`.

Comunicación:

- Reacciona a `OnHealthChanged`, `OnExperienceChanged`, `OnLevelUp`, `OnPendingLevelUpsChanged`, `OnPauseStateChanged` y utiliza `DetectPlayer` para activar indicaciones de interacción.

### 5.5. `MainMenuController` y `InitialGameMenuController`

Componentes principales:

- `MainMenuController.cs`
- `InitialGameMenuController.cs`

Responsabilidad:

- Dar acceso a los distintos bloques del juego (roguelite, platformer, combate por turnos, minijuegos) y activar la navegación entre escenas.
- Gestión del estado visual del menú principal y de la carga de la nueva partida.

### 5.6. `SettingsManager` y `GameProgressManager`

Se documentan con más detalle en la sección específica de persistencia, pero ambos son sistemas comunes del proyecto.

### 5.7. Sistema de diálogos

Componentes principales:

- `DialogueManager` (`Assets/DialogueSystem/Scripts/DialogueManager.cs`)
- `EasyDS` (`Assets/DialogueSystem/Scripts/EasyDS.cs`)
- `Parser` (`Assets/DialogueSystem/Scripts/Parser.cs`)
- `DetectPlayer` (`Assets/DialogueSystem/Scripts/DetectPlayer.cs`)
- `DialogueGate` (`Assets/Scripts/CoreCommon/DialogueGate.cs`)
- `DialoguePanelVisibility` (`Assets/Scripts/CoreCommon/DialoguePanelVisibility.cs`)

Responsabilidad:

- Permitir la interacción con NPC y la visualización de diálogos por texto, con control de jugador temporalmente desactivado durante la conversación.
- `DetectPlayer` activa la zona de interacción cuando el jugador entra en alcance.
- `EasyDS` inicia la conversación, avanza línea a línea y gestiona el cierre del diálogo.
- `Parser` procesa etiquetas de finalización de línea (`/line` y `/end`).
- `DialogueGate` sirve como contador de diálogo activo para que otros sistemas sepan si la conversación está en curso.

Comunicación:

- El flujo principal es: `DetectPlayer` -> `EasyDS` -> `DialogueManager` -> `Parser` -> `DialogueGate`/UI; la interacción del jugador se bloquea mientras hay diálogo activo.

---

## 6. Implementaciones técnicas destacadas

### 6.1. Salto cargado del nivel de plataformas

La implementación actual del salto cargado se encuentra en `PlatformerJumpController.cs` y se apoya en `PlatformerPlayerController.cs` para el estado del personaje.

El sistema trabaja con los siguientes elementos reales:

- `TryBeginCharge`: se activa al presionar `Space` y se inicia una carga controlada.
- `chargeTimer`: mide cuánto tiempo se mantiene cargado el salto.
- `minimumJumpForce`, `maximumJumpForce` y `maximumHorizontalLaunchStrength`: determinan la fuerza mínima, máxima y vector de impulso horizontal.
- `LaunchJump`: se ejecuta al soltar la barra espaciadora; calcula un salto escalado por la carga acumulada.
- `PerformDash`: consume un dash en aire para aportar velocidad horizontal extra.
- `ApplyLandingBounce`: al aterrizar, convierte parte de la velocidad vertical en un rebote controlado si la colisión es suficientemente fuerte.

Esto produce el patrón de salto cargado que aparece como una peculiaridad principal del Level 4, con control de aire y dash situado en el mismo componente.

### 6.2. Experiencia y mejoras del nivel de combate roguelite

La experiencia del personaje se implementa en `PlayerExperience.cs`, con una progresión lineal y eventos que disparan subidas de nivel:

- `baseRequirement` establece el valor base requerido para subir de nivel.
- `AddExperience` acumula experiencia y, si supera `expToNext`, incrementa `level` y recalcula el siguiente umbral.
- `OnLevelUp` notifica a `LevelUpController`.

La lógica de mejoras se centraliza en `LevelUpController.cs`:

- `pendingLevelUps` guarda las mejoras pendientes.
- `PauseManager` pausa el juego al llegar a una nueva mejora pendiente.
- `SelectUpgrade` aplica una mejora concreta a `PlayerMovement` o a `PlayerAutoAttack`.
- `MovementSpeed`, `AttackSpeed` y `ProjectileDamage` son las opciones disponibles del sistema actual.

La estructura no implementa un sistema de niveles lógicos complejos ni árboles de habilidad; se trata de mejoras puntuales y encadenadas por evento.

### 6.3. Cámara de la segunda fase del nivel de plataformas

La cámara de la fase 2 se implementa en `Phase2CameraScroller.cs` y complementa a `PlatformerCameraFollow.cs`.

Características reales del componente:

- `minimumDescentSpeed` hace descender la cámara a una velocidad mínima.
- `holdUntilPlayerBelowY` permite mantener la cámara quieta mientras el jugador está en una zona inicial.
- `descentActive` se activa cuando el jugador baja por debajo del umbral configurado.
- `StopDescent` frena la cámara cuando se alcanza el objetivo o la transición concluye.

La cámara de la fase 2 tiene, por tanto, un comportamiento de scroll vertical controlado, no un seguimiento continuo de toda la escena.

### 6.4. Puzzle de restricciones

El puzzle de restricciones se implementa en `ConstraintPuzzleManager.cs` y está sustentado por una validación real del arreglo basado en restricciones lógicas:

- `PositionCount` fija 6 posiciones.
- `ItemIds` son los elementos a ordenar: `A`, `B`, `C`, `D`, `E`, `F`.
- `PlaceItemAtPosition`, `ClearItem` y `GetPositionFor` gestionan la asignación.
- `IsValidArrangement` verifica condiciones concretas de posición y orden.
- `CountSolutions` determina si la disposición es única o si hay varias soluciones.

El sistema no es un sistema generativo de puzzle complejo; es un tablero de restricciones con validación explícita y comprobación funcional determinista.

### 6.5. Sistema de combate por turnos del Level 5

El sistema de combate por turnos del Level 5 está implementado principalmente en:

- `Level5BattleController.cs`
- `Combatant.cs`
- `Level5BattleUI.cs`
- `Level5PauseMenu.cs`

#### Estructura del turno

`Level5BattleController` define un `turnOrder` fijo de `Combatant` y avanza en orden circular con `FindNextLivingIndex`. El turno actual se obtiene por `CurrentCombatant`, y el estado general puede ser:

- `InProgress`
- `Victory`
- `Defeat`

El script expone eventos `OnTurnChanged`, `OnBattleEnded` y `OnEnemyAttack` para sincronizar la interfaz.

#### Participantes

Cada combatiente es un `Combatant` con:

- `combatantName`
- `isEnemy`
- `maxHP`
- `attack`
- `defense`
- `currentHP`

Además, `Combatant` incluye un bono temporal de defensa `temporaryDefenseBonus` que se aplica con `ApplyDefendBonus` y se consume con `ClearDefendBonus`.

#### Acciones disponibles

`Level5BattleUI` ofrece cuatro acciones reales en la interfaz:

- `Attack`: daño físico normal.
- `Defend`: aplica un bonus de defensa temporal.
- `Use Item`: restaura salud a un miembro del equipo.
- `Magic`: ataque especial del mago, con daño fijo y sin defensa.

La UI solo activa estos botones cuando es el turno del jugador y el combate está activo.

#### Cálculo de daño y defensa

`PerformAttack` calcula:

- daño base: `max(1, attacker.Attack - target.EffectiveDefense)`;
- aplica `damageMultiplier` cuando procede;
- `RollMiss` y `RollCritical` introducen probabilidad de fallo y crítico;
- `MagicDamage` se usa en `PerformMagicAttack` y, a diferencia del ataque físico, ignora la defensa.

La lógica del final del combate se centraliza para evitar que un ataque pueda omitir la comprobación de victoria/derrota.

#### Ataque del enemigo/jefe

`TriggerEnemyAttack` gestiona el ataque final del enemigo. Implementa:

- un ataque principal;
- posibilidad de un segundo ataque (`SecondAttackChance`);
- multiplicador de daño si la salud del enemigo está por debajo del 50%;
- notificación por `OnEnemyAttack` con un texto legible para la UI.

Además, el código incluye un modo especial `enableLevel0BossTurnMode` que elimina directamente al primer aliado vivo en el orden de turnos, sin cálculo de daño estándar.

#### Comunicación entre controller, combatants y UI

La relación real es clara:

- `Level5BattleController` ejecuta la lógica del turno y del daño.
- `Combatant` encapsula antes y después de cada acción.
- `Level5BattleUI` observa el estado del controlador y actualiza la interfaz con el turno actual, HP, texto de acción y resaltado del combatiente activo.

No hay un sistema de inteligencias artificiales ni una jerarquía sofisticada de habilidades; la implementación es un prototipo de combate por turnos con flujo secuencial y reglas deterministas.

---

## 7. Sistema de configuración y persistencia

### 7.1. `SettingsManager`

Este sistema se encuentra en `Assets/Scripts/CoreCommon/SettingsManager.cs` y realmente guarda la configuración del juego en `PlayerPrefs`.

Los datos que persiste son:

- `Settings.MusicVolume`
- `Settings.SfxVolume`
- `Settings.Fullscreen`

Los valores se aplican sobre un `AudioMixer` con parámetros:

- `MusicVolume`
- `SFXVolume`

La clase también expone `Load`, `Save`, `ResetToDefaults`, `SetMusicVolume`, `SetSfxVolume` y `SetFullscreen`. El sistema se conserva entre escenas porque `DontDestroyOnLoad` mantiene el objeto vivo durante la navegación.

Lo que no hace este sistema es guardar una partida completa del jugador; sólo guarda configuración del proyecto.

### 7.2. `GameProgressManager`

`GameProgressManager` persiste banderas de progreso del juego usando `PlayerPrefs` y `DontDestroyOnLoad`.

Los datos que guarda son:

- progreso de puzzle deslizante,
- progreso de longest path,
- progreso del constraint puzzle,
- nivel 3 completado,
- nivel 4 completado,
- nivel 5 completado.

Se usa para indicar qué minigame o bloque del recorrido ya ha sido completado por el usuario, pero no guarda un estado completo de inventario o partida. Se puede reiniciar con `StartNewGame`, que borra los flags y vuelve a escribir `PlayerPrefs`.

---

## 8. Diálogos

El sistema de diálogos actual está basado en componentes simples y directos, sin una estructura narrativa compleja ni un árbol de decisión formal. Los archivos principales son:

- `DetectPlayer.cs`
- `DialogueManager.cs`
- `EasyDS.cs`
- `Parser.cs`
- `DialogueGate.cs`
- `DialoguePanelVisibility.cs`

### Flujo real

1. `DetectPlayer` comprueba si el jugador se encuentra dentro del rango de interacción.
2. `EasyDS` inicia el diálogo al detectar la pulsación del botón de interacción (`interactKey`).
3. `DialogueManager` expone los objetos UI del diálogo (texto, sprite, nombre, panel de fondo) y controla la velocidad de escritura.
4. `Parser` procesa etiquetas de línea, especialmente `/line` y `/end`, para decidir cuándo avanzar entre líneas o nodos.
5. `DialogueGate` actúa como contador estático para evitar que otros sistemas del gameplay sigan ejecutándose mientras una conversación está activa.
6. `DialoguePanelVisibility` muestra u oculta el panel visual basándose en si el texto del diálogo tiene contenido.

No se observa un sistema de ramificación narrativa, elecciones de diálogo ni persistencia de estado de conversación. El flujo actual es secuencial y depende de los nodos y líneas definidas en los `Dialogue` Scriptable Objects o listas de diálogo y de las etiquetas `/line` y `/end` para progresar.

---

## 9. Documentación interna y mantenibilidad

La parte de mantenibilidad del proyecto se mantiene bastante coherente con la estructura real del código. Las afirmaciones que siguen son correctas en la versión actual:

- los scripts se organizan por carpetas funcionales;
- el código usa comentarios y cabeceras descriptivas en varios archivos;
- se prioriza la separación por responsabilidad y no la fusión de todo en un único `GameManager`;
- el estado configurable se expone al Inspector mediante campos serializados;
- los componentes reutilizables se separan de los específicos de nivel;
- la comunicación se realiza con referencias explícitas, Singleton y eventos cuando corresponde.

### Criterios de organización observados

1. Un componente debe resolver una responsabilidad concreta.
2. Los datos persistentes se concentran en gestores globales, no dispersos por la escena.
3. Los prefabs, clips de audio y VFX se dejan configurados desde el Inspector.
4. La lógica de nivel y la lógica común conviven en carpetas separadas para facilitar el mantenimiento.
5. Los objetos de UI y de gameplay se conectan con eventos y referencias explícitas, evitando acoplamientos demasiado complejos.

Este enfoque facilita el mantenimiento del proyecto y hace más comprensible la extensión del mismo en futuras iteraciones.

---

## 10. Tabla resumen

| Sistema | Componentes principales | Responsabilidad |
|---|---|---|
| Navegación | `SceneLoader`, `MainMenuController`, `PuzzleMenuController` | Cambio entre escenas, menús y transiciones. |
| Pausa | `PauseManager`, `HudController`, `Level5PauseMenu`, `PlatformerPauseMenu` | Control central de pausas y sus efectos en el resto del juego. |
| Audio/VFX | `AudioHub`, `VFXHub` | Reproducción de efectos y partículas de eventos de combate y nivel. |
| Progreso | `GameProgressManager` | Persistencia de banderas de completado del juego. |
| Configuración | `SettingsManager`, `SettingsPanelController` | Ajustes de audio y pantalla. |
| Diálogo | `DialogueManager`, `EasyDS`, `Parser`, `DetectPlayer`, `DialogueGate` | Transiciones narrativas y control visual del diálogo. |
| Roguelite | `LevelMapGenerator`, `PlayerExperience`, `LevelUpController`, `EnemySpawner`, `PlayerAutoAttack` | Generación de mapa, progreso del personaje, mejoras y combate. |
| Platformer | `PlatformerJumpController`, `PlatformerCameraFollow`, `Phase2CameraScroller`, `PlatformerPlayerController` | Salto cargado, cámara, movimiento y progreso de la fase 2. |
| Combate Level 5 | `Level5BattleController`, `Combatant`, `Level5BattleUI` | Sistema de turnos, daño, defensa y acciones. |
| Puzzles | `ConstraintPuzzleManager`, `LongestPathPuzzleManager`, `PuzzleMenuController` | Generación y validación de minigames lógicos. |

---

## 11. Conclusión

La estructura actual del proyecto se apoya en una forma de diseño basada en componentes de Unity, con responsabilidades repartidas entre clases concretas y gestores transversales. La lógica del juego está organizada por bloques funcionales y no por un patrón formal único; la comunicación entre sistemas se produce mediante referencias serializadas, eventos, `Singleton` de configuración/progreso y control de escena por `SceneLoader`.

La principal discrepancia heredada es la nomenclatura entre `Level3` y `Level2`. La carpeta `Assets/Scripts/Level3` contiene la lógica del segundo bloque jugable del proyecto y la escena que se carga es `Level2`, por lo que el manual debe reflejar esta situación real y no inventar una reorganización que no existe en el código actual.

La documentación técnica actualizada debe, por tanto, mantenerse fiel al repositorio: no se inventan nuevas funcionalidades ni se eliminan conceptos que sigan siendo ciertos; simplemente se ajusta la descripción a la implementación efectiva disponible en el proyecto.

---

## 12. Resumen de la actualización

### Apartados actualizados

- estructura general del proyecto,
- inventario real de scripts por carpetas,
- sistemas comunes reutilizados,
- arquitectura basada en componentes,
- elementos técnicos destacables del Level 4 y del Level 5,
- persistencia de configuración y progreso,
- flujo de diálogo,
- mantenibilidad y organización del código,
- tabla resumen final.

### Scripts nuevos documentados según la implementación actual

- `Level5BattleController.cs`
- `Level5BattleUI.cs`
- `Combatant.cs`
- `Level5PauseMenu.cs`
- `PlatformerJumpController.cs`
- `PlatformerCameraFollow.cs`
- `Phase2CameraScroller.cs`
- `MainMenuController.cs`
- `HudController.cs`
- `SettingsManager.cs`
- `GameProgressManager.cs`
- `AudioHub.cs`
- `VFXHub.cs`
- `DialogueGate.cs`
- `DialoguePanelVisibility.cs`

### Scripts eliminados del inventario del manual

En la revisión del repositorio no se ha localizado una versión previa del manual dentro del workspace visible, por lo que no se puede confirmar un listado histórico de scripts suprimidos del documento anterior. Sin embargo, sí se ha descartado cualquier referencia que no esté presente en la estructura real del repositorio actual. En particular, no se documentan nombres de carpeta o scripts que no existan actualmente, ni se sobreentiende que haya una carpeta `Assets/Scripts/Level2` cuando la implementación real se encuentra en `Assets/Scripts/Level3`.

### Cambios estructurales realizados

- se conserva la estructura académica general del manual,
- se actualiza la organización de carpetas con el estado real del repositorio,
- se documenta la discrepancia Level2/Level3 como nomenclatura interna heredada,
- se incorpora el sistema de combate por turnos del Level 5 como pieza clave del proyecto,
- se reduce la documentación de elementos que no existen en el código actual.

### Posibles aspectos que deberían revisarse manualmente

- la nomenclatura funcional entre escena y carpeta de scripts puede requerir una decisión editorial si el proyecto se reorganiza más adelante,
- la documentación de algunos scripts editoriales puede ampliarse si se añade una descripción detallada de cada utilidad de editor,
- el sistema de diálogo puede requerir una revisión más profunda si se añaden más nodos narrativos, diálogos condicionales o nuevos personajes en versiones futuras.
