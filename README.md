# Generador de Sudokus

# 

# Proyecto de Unidad II: Generación Procedural de Puzzles (Unity).

# 

# Integrantes: Felipe Pérez Jugar en itch.io: \[enlace]

# 

# ¿Qué hace?

# 

# Crea Sudokus con una sola solución y los va mejorando para que se acerquen a la dificultad que tú elijas.

# 

# Puzzle y cómo se guarda

# 

# El puzzle es Sudoku (9x9). Puedes verlo en la biblioteca de Ludii: https://ludii.games/library.php (buscar "Sudoku").

# 

# Cada Sudoku se guarda con tres cosas:

# 

# Una grilla con los números (0 significa celda vacía).

# Una marca de cuáles celdas son pistas fijas.

# Los números que todavía caben en cada celda.

# Cómo funciona

# Generar: crea un Sudoku completo y le va quitando pistas, cuidando que siempre quede una sola solución. Para revisarlo, el programa prueba primero la celda con menos opciones (heurística MRV).

# Evolucionar (algoritmo genético): crea varias versiones del puzzle, las mezcla y se queda con las mejores.

# Recocido (simulated annealing): hace pequeños cambios al puzzle (mueve una pista de lugar). Acepta los cambios buenos y, al principio, a veces también los malos para no quedarse atascado.

# Pipeline: usa el algoritmo genético y después el recocido, uno detrás del otro.

# 

# Todas las técnicas usan el mismo puntaje para decidir qué puzzle es mejor. Menor es mejor, y 0 significa un Sudoku con una sola solución y con la dificultad pedida.

# 

# Qué puedes cambiar

# Pistas iniciales: cuántos números se muestran al generar.

# Dificultad objetivo: la dificultad que quieres lograr.

# Algoritmo genético: población, generaciones y tasa de mutación.

# Recocido: temperatura inicial, enfriamiento, temperatura mínima y vecinos por temperatura.

# Cómo usarlo

# Pulsa Generar para crear un Sudoku.

# Ajusta la dificultad objetivo y los demás controles.

# Pulsa Evolucionar, Recocido o Pipeline para mejorarlo.

# Mira el registro para ver cuánto cambió.

# Pulsa Volver al inicial para probar otra configuración sobre el mismo puzzle.

# 

# Otros botones: Parar detiene la búsqueda y la casilla Solución muestra la respuesta completa.

# 

# Para abrir el proyecto

# Unity \[versión]

# Abrir la escena \[nombre de la escena] y pulsar Play.

