# Seed de lecturas PQ4R

Este script carga las 12 lecturas PQ4R ubicadas en `database/SeedContent/Lecturas` al flujo de refuerzo lector de ComprenEasy. Crea/asegura los catálogos necesarios, registra las lecturas, configura sus fases PQ4R y crea una evaluación `ReadingPractice` por lectura con sus preguntas y opciones.

## Cómo ejecutarlo en SQL Server Management Studio

1. Abre SQL Server Management Studio.
2. Selecciona la base de datos de ComprenEasy/ReadingAdaptiveSystem.
3. Abre `Database/Scripts/Seed_Readings_PQ4R.sql`.
4. Revisa que estés conectado a la base correcta.
5. Ejecuta el script completo.

## Advertencias

No ejecutes este script si deseas conservar un banco de lecturas distinto con los mismos títulos, porque el script actualiza por claves naturales para mantener el contenido alineado con los TXT.

El script es idempotente: puede ejecutarse más de una vez y no debería duplicar lecturas, evaluaciones, preguntas ni opciones ya cargadas por el mismo contenido.

El script no crea pretest ni posttest. Solo crea contenido de tipo `ReadingPractice` asociado a las lecturas PQ4R.

## Resumen esperado

- Lecturas: 12
- ReadingPractice: 12
- Preguntas: 115
- Opciones: 410

## Dimensiones esperadas

- Literal: 36
- Inferencial: 52
- Crítica-Evaluativa: 27
