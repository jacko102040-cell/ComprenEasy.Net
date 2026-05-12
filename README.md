# ComprenEasy.Net - Sistema Adaptativo de Comprensión Lectora

**ComprenEasy.Net** es una plataforma educativa basada en .NET diseñada para mejorar la comprensión lectora en estudiantes mediante el método **PQ4R** (Preview, Question, Read, Reflect, Recite, Review) y algoritmos de **aprendizaje adaptativo**.

## 🚀 Características Principales

-   **Flujo Académico PQ4R:** Implementación guiada de las fases de estudio para maximizar la retención y comprensión.
-   **Motor de Adaptabilidad:** Recomendaciones personalizadas basadas en el desempeño del estudiante, utilizando lógica de negocio y Machine Learning.
-   **Evaluaciones Multinivel:** Pruebas que cubren dimensiones literales, inferenciales y crítico-evaluativas.
-   **Panel del Docente:** Herramientas de monitoreo y seguimiento del progreso de los estudiantes.
-   **Arquitectura Limpia:** Estructura modular y escalable dividida en capas (API, Application, Domain, Infrastructure, ML).

## 🛠️ Stack Tecnológico

-   **Backend:** ASP.NET Core 8.0 (Web API)
-   **Persistencia:** Entity Framework Core con SQL Server
-   **Seguridad:** Autenticación basada en Cookies
-   **Machine Learning:** ML.NET para predicciones y recomendaciones
-   **Documentación:** Swagger / OpenAPI

## 🏗️ Estructura del Proyecto

-   `ReadingAdaptive.Api`: Punto de entrada de la aplicación, controladores REST.
-   `ReadingAdaptive.Application`: Lógica de negocio, interfaces de servicio, DTOs y excepciones.
-   `ReadingAdaptive.Domain`: Entidades del dominio, constantes y lógica central.
-   `ReadingAdaptive.Infrastructure`: Implementaciones de persistencia, servicios externos y configuración de seguridad.
-   `ReadingAdaptive.ML`: Modelos y lógica de Machine Learning para el sistema adaptativo.
-   `database/`: Scripts SQL para la creación de la base de datos y carga de datos iniciales (Seed).

## 🚦 Requisitos Previos

-   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
-   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (o LocalDB)
-   Visual Studio 2022 o VS Code

## ⚙️ Configuración e Instalación

1.  **Clonar el repositorio:**
    ```bash
    git clone https://github.com/jacko102040-cell/ComprenEasy.Net.git
    cd ComprenEasy.Net
    ```

2.  **Configurar la base de datos:**
    -   Asegúrate de tener SQL Server ejecutándose.
    -   Ejecuta los scripts ubicados en `database/Scripts/` (especialmente `Seed_Readings_PQ4R.sql`) para inicializar los datos.
    -   Actualiza la cadena de conexión en `ReadingAdaptive.Api/appsettings.json` si es necesario.

3.  **Ejecutar las migraciones (opcional si usas el script SQL completo):**
    ```bash
    dotnet ef database update --project ReadingAdaptive.Infrastructure --startup-project ReadingAdaptive.Api
    ```

4.  **Iniciar la API:**
    ```bash
    dotnet run --project ReadingAdaptive.Api
    ```

## 📄 Documentación de la API

Una vez iniciada la aplicación, puedes acceder a la interfaz de Swagger para explorar y probar los endpoints en:
`http://localhost:<puerto>/swagger`

---
Desarrollado como parte de un proyecto de tesis enfocado en la mejora de procesos cognitivos mediante tecnología adaptativa.
