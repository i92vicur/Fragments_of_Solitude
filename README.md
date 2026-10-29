# Fragments of Solitude

**Fragments of Solitude** es un videojuego desarrollado en Unity como Trabajo de Fin de Grado de Ingeniería Informática en la Universidad de Córdoba (UCO).

El proyecto explora temas relacionados con la soledad, la culpa y la depresión mediante una experiencia interactiva que combina acción, puzzles, combates por turnos y plataformas, con variaciones estéticas y mecánicas a lo largo de sus niveles.

La versión jugable incluye varias experiencias diferenciadas que abarcan desde el combate con ataques a distancia y el enfrentamiento contra jefes hasta puzles lógicos, desafíos de plataformas y combates por turnos. También incorpora sistemas de progresión, habilidades y gestión de ajustes y partidas. 

## Requisitos

- **Motor:** Unity `6000.0.55f1`.
- **Lenguaje de programación:** C#.
- **Plataforma de destino:** Windows.

## Instalación y apertura

Clona el repositorio mediante Git:

```bash
git clone https://github.com/i92vicur/Fragments_of_Solitude.git
```

A continuación:

1. Abre Unity Hub.
2. Añade el proyecto seleccionando el directorio clonado.
3. Ábrelo utilizando Unity `6000.0.55f1`.
4. Espera a que finalice la importación de los recursos.

## Ejecución

La escena inicial del juego es `InitialGameMenu`.

Para ejecutar el proyecto desde el editor, abre esta escena y pulsa **Play**.

## Generación de la build

Para generar una versión ejecutable del juego para Windows:

1. Abre el proyecto en Unity.
2. Accede a **File > Build Profiles**.
3. Selecciona Windows como plataforma de destino.
4. Comprueba que las escenas necesarias están incluidas en la configuración de compilación y en el orden correcto.
5. Selecciona la ubicación de salida y genera la build.

Una vez finalizado el proceso, ejecuta el archivo `.exe` generado. Conserva los archivos y directorios auxiliares incluidos junto al ejecutable.

## Pruebas

El proyecto incluye pruebas automatizadas de Edit Mode.

Para ejecutarlas:

1. Abre **Window > General > Test Runner**.
2. Selecciona **Edit Mode**.
3. Ejecuta las pruebas disponibles y consulta los resultados en el Test Runner.

Los archivos de pruebas se encuentran en `Assets/Tests/EditMode/Editor/`.

## Estructura del proyecto

El repositorio contiene el proyecto de Unity y sus recursos. Entre sus directorios principales se encuentran:

- `Assets/`: recursos del juego, escenas, scripts y pruebas.
- `Packages/`: dependencias y paquetes del proyecto.
- `ProjectSettings/`: configuración del proyecto de Unity.

## Documentación

- **Manual de Usuario:** https://docs.google.com/document/d/1pS2wxV9G6Va9y94s4NSmoYekUe6Jqzk67AP1H2gO_fs/edit?usp=sharing
- **Manual Técnico:** https://docs.google.com/document/d/1jZ21XDcRryopObLN7BuTTpXDBtcxaKxJxuKMeqg2vXg/edit?usp=sharing
- **Manual de Código:** https://docs.google.com/document/d/1iH_4umVlOw9dzuTXa3c0UBMIuLmfuqQ6Trs9LWPug68/edit?usp=sharing

## Créditos y licencia

Licencia y recursos de terceros

Fragments of Solitude es un proyecto académico desarrollado como Trabajo de Fin de Grado, sin finalidad comercial ni intención de distribución pública como producto. La entrega del prototipo se realiza mediante los canales académicos establecidos.

Los permisos aplicables al código y a los recursos originales del proyecto no se extienden a los materiales de terceros incorporados, que conservan sus respectivas condiciones de uso y derechos de autor. El acceso al repositorio no implica autorización para redistribuir dichos materiales ni reutilizarlos de forma independiente.

## Versión

Versión de entrega: `v1.0-alpha`.

La etiqueta de Git correspondiente a la entrega definitiva se publicará en el repositorio y se citará en la documentación del proyecto.
