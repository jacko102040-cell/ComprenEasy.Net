/*
    Seed_Readings_PQ4R.sql
    Carga idempotente de las 12 lecturas PQ4R desde database/SeedContent/Lecturas.
    Generado a partir de los TXT del workspace; no crea Pretest ni Posttest.
    Resumen esperado: 12 lecturas, 12 ReadingPractice, 115 preguntas, 410 opciones.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------
    -- 0) Docente técnico para CreatedByUserId de Readings
    -----------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Teacher')
    BEGIN
        INSERT INTO Roles (Name, IsActive) VALUES ('Teacher', 1);
    END
    ELSE
    BEGIN
        UPDATE Roles SET IsActive = 1 WHERE Name = 'Teacher' AND IsActive = 0;
    END;

    DECLARE @TeacherRoleId SMALLINT = (SELECT TOP (1) RoleId FROM Roles WHERE Name = 'Teacher');
    DECLARE @SeedTeacherUsername VARCHAR(30) = 'seed.teacher';
    DECLARE @SeedTeacherPasswordHash VARCHAR(255) = 'PBKDF2SHA256.100000.KAr/hsMEI+V3uC8Y5i4Vkw==.jhj1W9COvYrxuBLDKnIckYNzOnLDr1ycHajJC4GxV48=';
    DECLARE @CreatedByUserId INT;

    SELECT TOP (1)
        @CreatedByUserId = t.TeacherId
    FROM Teachers t
    INNER JOIN Users u ON u.UserId = t.TeacherId
    WHERE u.IsActive = 1
    ORDER BY t.IsHiddenAdmin DESC, t.CanResetPasswords DESC, t.TeacherId;

    IF @CreatedByUserId IS NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = @SeedTeacherUsername)
        BEGIN
            INSERT INTO Users (RoleId, FullName, Username, PasswordHash, IsActive)
            VALUES (@TeacherRoleId, N'Seed Teacher PQ4R', @SeedTeacherUsername, @SeedTeacherPasswordHash, 1);
        END;

        SELECT @CreatedByUserId = UserId FROM Users WHERE Username = @SeedTeacherUsername;

        IF NOT EXISTS (SELECT 1 FROM Teachers WHERE TeacherId = @CreatedByUserId)
        BEGIN
            INSERT INTO Teachers (TeacherId, CanResetPasswords, IsHiddenAdmin)
            VALUES (@CreatedByUserId, 1, 1);
        END
        ELSE
        BEGIN
            UPDATE Teachers
            SET CanResetPasswords = 1,
                IsHiddenAdmin = 1
            WHERE TeacherId = @CreatedByUserId;
        END;
    END;

    -----------------------------------------------------------------------
    -- 1) Catálogos base: dificultades, dimensiones y fases PQ4R
    --    AssessmentType no tiene tabla catálogo en el modelo actual;
    --    se usa Assessments.AssessmentType = 'ReadingPractice'.
    -----------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM DifficultyLevels WHERE Name = N'Básico')
    BEGIN
        IF EXISTS (SELECT 1 FROM DifficultyLevels WHERE RankOrder = 1)
        BEGIN
            UPDATE DifficultyLevels SET Name = N'Básico' WHERE RankOrder = 1;
        END
        ELSE
        BEGIN
            INSERT INTO DifficultyLevels (Name, RankOrder) VALUES (N'Básico', 1);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM DifficultyLevels WHERE Name = N'Intermedio')
    BEGIN
        IF EXISTS (SELECT 1 FROM DifficultyLevels WHERE RankOrder = 2)
        BEGIN
            UPDATE DifficultyLevels SET Name = N'Intermedio' WHERE RankOrder = 2;
        END
        ELSE
        BEGIN
            INSERT INTO DifficultyLevels (Name, RankOrder) VALUES (N'Intermedio', 2);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM DifficultyLevels WHERE Name = N'Avanzado')
    BEGIN
        IF EXISTS (SELECT 1 FROM DifficultyLevels WHERE RankOrder = 3)
        BEGIN
            UPDATE DifficultyLevels SET Name = N'Avanzado' WHERE RankOrder = 3;
        END
        ELSE
        BEGIN
            INSERT INTO DifficultyLevels (Name, RankOrder) VALUES (N'Avanzado', 3);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = N'Literal')
    BEGIN
        INSERT INTO Dimensions (Name, Description) VALUES (N'Literal', N'Comprensión literal de información explícita.');
    END
    ELSE
    BEGIN
        UPDATE Dimensions SET Description = N'Comprensión literal de información explícita.' WHERE Name = N'Literal';
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = N'Inferencial')
    BEGIN
        INSERT INTO Dimensions (Name, Description) VALUES (N'Inferencial', N'Comprensión inferencial a partir de pistas del texto.');
    END
    ELSE
    BEGIN
        UPDATE Dimensions SET Description = N'Comprensión inferencial a partir de pistas del texto.' WHERE Name = N'Inferencial';
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = N'Crítica-Evaluativa') AND EXISTS (SELECT 1 FROM Dimensions WHERE Name = N'Critica-Evaluativa')
    BEGIN
        UPDATE Dimensions SET Name = N'Crítica-Evaluativa', Description = N'Juicio crítico y valoración de ideas del texto.' WHERE Name = N'Critica-Evaluativa';
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = N'Crítica-Evaluativa')
    BEGIN
        INSERT INTO Dimensions (Name, Description) VALUES (N'Crítica-Evaluativa', N'Juicio crítico y valoración de ideas del texto.');
    END
    ELSE
    BEGIN
        UPDATE Dimensions SET Description = N'Juicio crítico y valoración de ideas del texto.' WHERE Name = N'Crítica-Evaluativa';
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Preview')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 1)
        BEGIN
            UPDATE Phases SET Code = 'Preview', DisplayName = 'Preview' WHERE DefaultOrder = 1;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Preview', 'Preview', 1);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Question')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 2)
        BEGIN
            UPDATE Phases SET Code = 'Question', DisplayName = 'Question' WHERE DefaultOrder = 2;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Question', 'Question', 2);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Read')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 3)
        BEGIN
            UPDATE Phases SET Code = 'Read', DisplayName = 'Read' WHERE DefaultOrder = 3;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Read', 'Read', 3);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Reflect')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 4)
        BEGIN
            UPDATE Phases SET Code = 'Reflect', DisplayName = 'Reflect' WHERE DefaultOrder = 4;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Reflect', 'Reflect', 4);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Recite')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 5)
        BEGIN
            UPDATE Phases SET Code = 'Recite', DisplayName = 'Recite' WHERE DefaultOrder = 5;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Recite', 'Recite', 5);
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM Phases WHERE Code = 'Review')
    BEGIN
        IF EXISTS (SELECT 1 FROM Phases WHERE DefaultOrder = 6)
        BEGIN
            UPDATE Phases SET Code = 'Review', DisplayName = 'Review' WHERE DefaultOrder = 6;
        END
        ELSE
        BEGIN
            INSERT INTO Phases (Code, DisplayName, DefaultOrder) VALUES ('Review', 'Review', 6);
        END;
    END;

    DECLARE @DifficultyBasicoId TINYINT = (SELECT TOP (1) DifficultyLevelId FROM DifficultyLevels WHERE Name IN (N'Básico', N'Basico') OR RankOrder = 1 ORDER BY CASE WHEN Name = N'Básico' THEN 0 ELSE 1 END, DifficultyLevelId);
    DECLARE @DifficultyIntermedioId TINYINT = (SELECT TOP (1) DifficultyLevelId FROM DifficultyLevels WHERE Name = N'Intermedio' OR RankOrder = 2 ORDER BY CASE WHEN Name = N'Intermedio' THEN 0 ELSE 1 END, DifficultyLevelId);
    DECLARE @DifficultyAvanzadoId TINYINT = (SELECT TOP (1) DifficultyLevelId FROM DifficultyLevels WHERE Name = N'Avanzado' OR RankOrder = 3 ORDER BY CASE WHEN Name = N'Avanzado' THEN 0 ELSE 1 END, DifficultyLevelId);
    DECLARE @DimensionLiteralId TINYINT = (SELECT TOP (1) DimensionId FROM Dimensions WHERE Name = N'Literal');
    DECLARE @DimensionInferencialId TINYINT = (SELECT TOP (1) DimensionId FROM Dimensions WHERE Name = N'Inferencial');
    DECLARE @DimensionCriticaId TINYINT = (SELECT TOP (1) DimensionId FROM Dimensions WHERE Name IN (N'Crítica-Evaluativa', N'Critica-Evaluativa') ORDER BY CASE WHEN Name = N'Crítica-Evaluativa' THEN 0 ELSE 1 END, DimensionId);
    DECLARE @PhasePreviewId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Preview');
    DECLARE @PhaseQuestionId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Question');
    DECLARE @PhaseReadId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Read');
    DECLARE @PhaseReflectId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Reflect');
    DECLARE @PhaseReciteId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Recite');
    DECLARE @PhaseReviewId TINYINT = (SELECT TOP (1) PhaseId FROM Phases WHERE Code = 'Review');

    IF @CreatedByUserId IS NULL OR @DifficultyBasicoId IS NULL OR @DifficultyIntermedioId IS NULL OR @DifficultyAvanzadoId IS NULL
       OR @DimensionLiteralId IS NULL OR @DimensionInferencialId IS NULL OR @DimensionCriticaId IS NULL
       OR @PhasePreviewId IS NULL OR @PhaseQuestionId IS NULL OR @PhaseReadId IS NULL OR @PhaseReflectId IS NULL OR @PhaseReciteId IS NULL OR @PhaseReviewId IS NULL
    BEGIN
        THROW 51000, 'No se pudieron resolver todos los IDs de catálogos requeridos para cargar lecturas PQ4R.', 1;
    END;

    -----------------------------------------------------------------------
    -- 2) Semillas parseadas desde los TXT
    -----------------------------------------------------------------------
    DECLARE @ReadingSeed TABLE
    (
        ReadingNo INT NOT NULL PRIMARY KEY,
        Title NVARCHAR(150) NOT NULL,
        DifficultyName NVARCHAR(20) NOT NULL,
        TextType NVARCHAR(80) NOT NULL,
        Summary NVARCHAR(300) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO @ReadingSeed (ReadingNo, Title, DifficultyName, TextType, Summary, Content)
    VALUES
        -- Lectura 1: La mochila de los objetos perdidos
        (1, N'La mochila de los objetos perdidos', N'Básico', N'Narrativo', N'Una mochila aparece en el aula con una brújula, una llave pequeña y una libreta llena de dibujos. Tres compañeros deciden descubrir a quién pertenece antes de que termine la jornada escolar.', N'La mochila de los objetos perdidos

Al terminar el recreo, Valeria encontró una mochila azul debajo de una carpeta vacía. No tenía nombre, pero dentro había una brújula, una llave pequeña y una libreta con dibujos de árboles, ventanas y un perro con manchas.

—Tal vez es de alguien nuevo —dijo Mateo, mirando la brújula.

Sofía revisó la libreta con cuidado. En la última página había un dibujo del patio del colegio y una flecha que apuntaba hacia la biblioteca. Los tres fueron hasta allí y preguntaron si alguien había perdido una mochila, pero nadie respondió.

Cuando estaban por regresar al salón, escucharon a un niño de otro grado decir que había dejado sus cosas mientras buscaba un libro de mapas. Valeria le preguntó por el color de su mochila. El niño sonrió y respondió:

—Azul, con una brújula que me regaló mi abuelo.

Mateo le mostró la mochila y el niño la reconoció de inmediato. Agradeció varias veces y explicó que la llave era de una caja donde guardaba sus lápices.

Al volver al aula, Sofía dijo que no habían resuelto un gran misterio, pero sí habían hecho algo importante: mirar con atención antes de sacar conclusiones.'),
        -- Lectura 2: El viaje de una gota en casa
        (2, N'El viaje de una gota en casa', N'Básico', N'Expositivo / informativo', N'Durante una mañana común, una gota de agua pasa por distintas partes de una casa: el baño, la cocina, el lavadero y una maceta del patio. En cada lugar, descubre que una acción pequeña puede cambiar todo su recorrido.', N'El viaje de una gota en casa

A las seis de la mañana, una gota salió del caño del baño mientras Diego se lavaba los dientes. Durante unos segundos, cayó sin que nadie la usara, hasta que él cerró la llave para buscar su cepillo.

Más tarde, otra corriente de agua llegó a la cocina. La mamá de Diego lavó unas frutas en un recipiente y luego usó esa misma agua para regar una planta pequeña que estaba junto a la ventana. La gota terminó en la tierra, cerca de una raíz delgada.

En el lavadero, el hermano mayor llenó un balde para limpiar sus zapatillas. Al principio quiso dejar el caño abierto, pero recordó que solo necesitaba poca agua. Con el balde fue suficiente.

Al mediodía, la familia notó que muchas actividades de la casa dependían del agua: preparar alimentos, limpiar, asearse y cuidar las plantas. También descubrieron que no siempre era necesario usar más de la cuenta.

La gota no habló ni dio consejos. Solo siguió su camino por la casa. Pero, al final del día, cada persona había cambiado algo pequeño en su manera de usarla.'),
        -- Lectura 3: ¿Quién debe ser el delegado?
        (3, N'¿Quién debe ser el delegado?', N'Básico', N'Argumentativo / reflexivo', N'El salón debe elegir a su delegado. Algunos estudiantes se dejan llevar por propuestas divertidas, mientras otros empiezan a pensar qué candidato podría ayudar realmente al grupo.', N'¿QUIÉN DEBE SER EL DELEGADO?

El lunes por la mañana, la profesora anunció que el salón elegiría a su delegado. Explicó que esa persona debía escuchar a sus compañeros, comunicar acuerdos y ayudar a organizar algunas actividades del aula.

Durante el recreo, varios estudiantes empezaron a conversar sobre los candidatos. Bruno dijo que, si lo elegían, pediría más minutos de recreo y organizaría un día sin tareas. Muchos se rieron y algunos pensaron que sería divertido votar por él.

Después habló Ana. Ella propuso ordenar mejor los turnos para usar los materiales del aula, avisar cuando hubiera problemas entre compañeros y hacer una lista de ideas para mejorar la convivencia. Su propuesta no sonó tan emocionante, pero varios estudiantes la escucharon con atención.

También participó Diego, quien prometió decorar el salón cada viernes. Sin embargo, cuando le preguntaron cómo conseguiría los materiales, no supo responder.

Antes de votar, la profesora pidió que pensaran no solo en la propuesta más llamativa, sino en la más útil para todos. Entonces, Valeria levantó la mano y dijo que un delegado no debía prometer solo cosas agradables, sino cumplir responsabilidades.

Al final, el salón comprendió que elegir no era solo marcar un nombre, sino pensar en lo que el grupo realmente necesitaba.'),
        -- Lectura 4: El club de los recreos silenciosos
        (4, N'El club de los recreos silenciosos', N'Intermedio', N'Narrativo', N'Durante varios recreos, Lucía nota que algunos compañeros pasan el descanso solos o mirando el celular. Junto con dos amigos, decide crear una pequeña actividad para que más estudiantes puedan participar sin sentirse obligados.', N'EL CLUB DE LOS RECREOS SILENCIOSOS

Durante la primera semana de clases, Lucía notó algo que casi nadie comentaba. En cada recreo, mientras algunos corrían hacia la cancha y otros compraban en el quiosco, tres o cuatro estudiantes se quedaban cerca de las bancas, mirando el piso o revisando el celular sin hablar con nadie.

Al principio pensó que tal vez preferían estar solos. Sin embargo, el viernes vio a Renato guardar un libro apenas se acercaron dos compañeros que se burlaban de quienes leían en el recreo. Lucía no dijo nada en ese momento, pero la escena le quedó dando vueltas.

El lunes siguiente conversó con Camila y Diego. Los tres decidieron hacer algo sencillo: llevar juegos de mesa pequeños, historietas cortas y una caja llamada “tema sorpresa”, donde cualquiera podía dejar una pregunta para conversar. No lo anunciaron como una campaña ni como una obligación. Solo pusieron una cartulina que decía: “Si quieres pasar un recreo diferente, acércate”.

El primer día solo llegó Renato. Escogió una historieta y se sentó en silencio. Luego se acercó Mariana, quien propuso hablar sobre películas. Al tercer día ya eran siete estudiantes. Algunos hablaban bastante; otros solo escuchaban.

Cuando la profesora tutora preguntó quién había organizado la actividad, Lucía explicó que no querían formar un grupo cerrado, sino abrir un espacio donde nadie se sintiera extraño por no jugar fútbol o por preferir conversar.

Desde entonces, algunos recreos siguieron siendo ruidosos, pero en una esquina del patio también existía un lugar tranquilo. No era el club más grande del colegio, pero para varios estudiantes se convirtió en el primer sitio donde podían acercarse sin miedo a no encajar.'),
        -- Lectura 5: La alarma que organiza el día
        (5, N'La alarma que organiza el día', N'Intermedio', N'Expositivo / informativo', N'Una aplicación promete ayudar a los estudiantes a ordenar tareas, descansos y horarios de estudio. Al principio parece resolverlo todo, pero pronto surgen dudas sobre cuánto conviene depender de una herramienta digital.', N'LA ALARMA QUE ORGANIZA EL DÍA

Mateo solía olvidar algunas tareas. A veces terminaba los ejercicios tarde, otras veces estudiaba apurado antes de dormir. Por eso, cuando su prima le recomendó una aplicación para organizar el día, decidió probarla.

La aplicación permitía colocar horarios para estudiar, descansar, ordenar la mochila y revisar tareas pendientes. Cada actividad aparecía con una alarma distinta. Una campana suave indicaba que debía empezar a estudiar; un sonido corto avisaba que era momento de descansar; y una luz azul recordaba revisar los cuadernos antes de dormir.

Durante la primera semana, Mateo se sintió más tranquilo. Ya no tenía que recordar todo de memoria y pudo entregar sus tareas a tiempo. Incluso notó que estudiar en bloques pequeños le ayudaba a concentrarse mejor.

Sin embargo, con los días empezó a depender demasiado de la aplicación. Si la alarma no sonaba, se olvidaba de revisar sus pendientes. Una tarde, el celular se quedó sin batería y Mateo no preparó los materiales para una exposición. Al día siguiente, tuvo que pedir hojas prestadas y llegó nervioso al aula.

Después de esa experiencia, conversó con su tutora. Ella le dijo que una herramienta digital podía ser útil, pero no debía reemplazar por completo sus propios hábitos. Mateo decidió seguir usando la aplicación, aunque también empezó a escribir una lista corta en su cuaderno. Además, cada noche revisaba por sí mismo qué necesitaba para el día siguiente.

Con el tiempo, comprendió que organizarse no consistía solo en obedecer alarmas. La tecnología podía ayudar, pero la responsabilidad seguía dependiendo de él.'),
        -- Lectura 6: El mapa del mercado del barrio
        (6, N'El mapa del mercado del barrio', N'Intermedio', N'Argumentativo / reflexivo', N'En una visita escolar al mercado, los estudiantes descubren que cada puesto revela algo distinto: historias familiares, precios, desperdicio de alimentos y formas de cooperación entre vecinos.', N'EL MAPA DEL MERCADO DEL BARRIO

La profesora de Comunicación llevó a sus estudiantes al mercado del barrio para realizar una actividad distinta: construir un mapa de historias. No se trataba de dibujar solo pasillos y puestos, sino de observar qué ocurría en cada lugar.

Al llegar, el grupo se dividió en equipos. Uno conversó con la señora Rosa, quien vendía frutas desde hacía quince años. Ella explicó que muchas familias compraban menos cuando los precios subían, pero también contó que algunos vendedores separaban frutas maduras para ofrecerlas más baratas antes de que se malograran.

Otro equipo habló con don Emilio, dueño de un puesto de verduras. Él dijo que cada tarde quedaban hojas, tallos y productos golpeados que casi nadie quería comprar. Sin embargo, algunas personas los usaban para preparar sopas o alimento para animales. “No todo lo imperfecto es inútil”, comentó.

Mientras avanzaban, los estudiantes notaron que el mercado no era solo un lugar para comprar. Allí se saludaban vecinos, se recomendaban recetas, se fiaba a quienes no podían pagar en el momento y se compartían noticias del barrio. Pero también había problemas: bolsas tiradas en el suelo, restos de comida mezclados con basura y poco espacio para caminar en ciertas horas.

Al regresar al colegio, cada equipo presentó su parte del mapa. Algunos propusieron colocar carteles para separar residuos; otros sugirieron promover bolsas reutilizables o crear una pequeña campaña para comprar frutas maduras a menor precio.

La profesora cerró la actividad diciendo que un mercado puede mostrar cómo vive una comunidad. Si las personas solo miran los precios, ven una parte; si observan las relaciones, los problemas y las soluciones posibles, descubren un mapa mucho más completo.'),
        -- Lectura 7: La última página del cuaderno rojo
        (7, N'La última página del cuaderno rojo', N'Avanzado', N'Narrativo', N'Camila encuentra un antiguo cuaderno de su abuelo con relatos sobre cómo era estudiar cuando había pocos libros y casi ninguna tecnología. Al leerlo, comienza a comparar esas dificultades con las oportunidades que tiene hoy.', N'LA ÚLTIMA PÁGINA DEL CUADERNO ROJO

Camila encontró el cuaderno rojo mientras ayudaba a ordenar una repisa antigua de la sala. Estaba cubierto de polvo y tenía las esquinas dobladas. En la primera página, con letra inclinada, decía: “Cuaderno de tareas. 1978”. Abajo aparecía el nombre de su abuelo: Julián.

Al principio pensó que solo encontraría ejercicios viejos, pero al pasar las hojas descubrió pequeñas notas escritas entre problemas de matemática y copias de lectura. En una de ellas, Julián contaba que debía caminar casi una hora para llegar a la escuela. En otra, explicaba que en su salón solo había tres libros de consulta y que los estudiantes se turnaban para leerlos durante pocos minutos.

Camila se sorprendió. Ella a veces se quejaba cuando el internet estaba lento o cuando debía buscar información en varias páginas. Sin embargo, su abuelo había estudiado copiando mapas a mano, compartiendo libros y repasando con una lámpara pequeña cuando se iba la luz.

La última página del cuaderno tenía un texto breve. Julián escribió que no siempre podía terminar las tareas porque ayudaba en la tienda familiar por las tardes. También escribió que estudiar le parecía difícil, pero necesario, porque le permitía imaginar una vida con más opciones.

Esa noche, Camila llevó el cuaderno a la mesa. Su abuelo sonrió al verlo, aunque dijo que no recordaba haber escrito tanto. Ella le preguntó si le habría gustado tener una computadora cuando era niño. Él respondió que sí, pero añadió algo que la dejó pensando:

—Las herramientas ayudan, pero no estudian por uno. Lo importante es saber para qué las usas.

Al día siguiente, Camila abrió su laptop para hacer una tarea. Esta vez no empezó quejándose por la cantidad de información. Primero escribió una pregunta en su cuaderno: “¿Qué quiero entender?”. Luego buscó, comparó fuentes y resumió con sus propias palabras.

El cuaderno rojo volvió a la repisa, pero ya no parecía un objeto viejo. Para Camila, se había convertido en una prueba silenciosa de que las oportunidades no siempre se notan cuando están frente a nosotros.'),
        -- Lectura 8: La ciudad que escuchaba a sus árboles
        (8, N'La ciudad que escuchaba a sus árboles', N'Avanzado', N'Expositivo / informativo', N'Un grupo de estudiantes usa sensores para medir sombra, temperatura y ruido en distintas calles de su ciudad. Los datos muestran diferencias inesperadas entre barrios y abren una discusión sobre cómo se toman las decisiones urbanas.', N'LA CIUDAD QUE ESCUCHABA A SUS ÁRBOLES

En la clase de Ciencia y Tecnología, la profesora propuso una pregunta poco común: “¿Todos los barrios de una ciudad sienten el calor de la misma manera?”. Al principio, varios estudiantes pensaron que la respuesta era obvia. Si el sol era el mismo, el calor también debía ser parecido. Sin embargo, el proyecto demostró algo distinto.

Durante dos semanas, los estudiantes recorrieron tres zonas cercanas al colegio. En cada lugar midieron la temperatura del suelo, el nivel de ruido y la cantidad de sombra durante el mediodía. Para hacerlo, usaron sensores pequeños conectados a una tableta. También tomaron fotografías de veredas, parques, avenidas y árboles.

La primera zona tenía varias calles con árboles grandes. Allí, la temperatura del suelo era menor y muchas personas caminaban por la sombra. La segunda zona tenía edificios altos, pero pocas áreas verdes. Aunque había algo de sombra, el ruido de los autos era constante. La tercera zona tenía pistas amplias, veredas angostas y casi ningún árbol. En ese lugar, el suelo estaba mucho más caliente y varias personas esperaban el transporte cubriéndose con carpetas o bolsas.

Cuando organizaron los datos, los estudiantes notaron que los árboles no solo servían para decorar. También reducían el calor, daban sombra, hacían más agradable caminar y podían disminuir la sensación de ruido. Incluso una vecina contó que antes había un pequeño parque cerca de su casa, pero fue reemplazado por una zona de estacionamiento.

El grupo preparó un informe para presentarlo en la municipalidad escolar. No pidieron sembrar árboles en cualquier lugar, sino estudiar primero dónde hacían más falta. También propusieron cuidar los árboles existentes, colocar bancas en zonas con sombra y evitar que las veredas se redujeran demasiado.

Al final, la profesora dijo que una ciudad también “habla”, aunque no use palabras. Habla cuando una calle quema más que otra, cuando una persona no encuentra sombra para esperar o cuando un barrio tiene menos espacios para descansar. Los estudiantes comprendieron entonces que escuchar a los árboles no significaba imaginar que tenían voz, sino aprender a leer las señales que la ciudad mostraba todos los días.'),
        -- Lectura 9: El algoritmo de las tareas
        (9, N'El algoritmo de las tareas', N'Avanzado', N'Argumentativo / reflexivo', N'Una escuela prueba un sistema que recomienda ejercicios según los resultados de cada estudiante. Algunos mejoran rápidamente, pero otros se preguntan si una máquina puede comprender todo lo que ocurre cuando alguien aprende.', N'EL ALGORITMO DE LAS TAREAS

Cuando la escuela anunció que usaría un sistema inteligente para recomendar tareas, muchos estudiantes pensaron que sería una forma de recibir menos ejercicios. Sin embargo, la idea era distinta: el sistema revisaría los resultados de cada estudiante y sugeriría actividades según sus avances y dificultades.

La primera semana, todos resolvieron una lectura breve y respondieron preguntas. Luego, el sistema mostró recomendaciones diferentes. A quienes habían fallado preguntas literales les propuso ejercicios para identificar datos explícitos. A quienes tuvieron problemas con preguntas inferenciales les ofreció textos donde debían relacionar pistas. Y a quienes respondieron con dificultad las preguntas críticas les recomendó actividades para comparar opiniones y justificar respuestas.

Al inicio, varios estudiantes se sintieron motivados. Paula, por ejemplo, recibió lecturas más cortas y preguntas guiadas. Después de algunos días, empezó a responder con mayor seguridad. En cambio, Joaquín se sorprendió porque el sistema le sugirió textos más difíciles, aunque él sentía que no había comprendido del todo la lectura anterior.

Durante una tutoría, la profesora pidió comentar la experiencia. Algunos dijeron que el sistema era útil porque no todos necesitaban practicar lo mismo. Otros señalaron que una recomendación basada solo en respuestas correctas podía dejar fuera detalles importantes: cansancio, nervios, falta de tiempo o incluso una mala lectura de la pregunta.

La profesora explicó que el algoritmo no era un juez perfecto, sino una herramienta de apoyo. Podía detectar patrones, como errores repetidos o avances en ciertos niveles de comprensión, pero no podía conocer por completo lo que una persona sentía o pensaba al aprender. Por eso, las recomendaciones debían combinarse con la revisión del estudiante y la orientación del docente.

Después de la conversación, Joaquín revisó sus respuestas y descubrió que había fallado no porque el texto fuera imposible, sino porque leyó demasiado rápido. Decidió repetir una actividad de nivel intermedio antes de intentar otra más avanzada. El sistema registró su nuevo intento y ajustó la recomendación.

Al terminar el mes, la clase no llegó a una conclusión simple. El sistema había ayudado a organizar prácticas y mostrar rutas diferentes, pero también quedó claro que aprender no era solo seguir lo que una pantalla indicaba. Un algoritmo podía sugerir caminos; aun así, cada estudiante necesitaba detenerse, revisar sus errores, pedir ayuda cuando fuera necesario y participar activamente en su propio aprendizaje.'),
        -- Lectura 10: El mensaje dentro de la botella
        (10, N'El mensaje dentro de la botella', N'Básico', N'Narrativo / cotidiano', N'Durante una limpieza del patio escolar, un grupo encuentra una botella con un papel enrollado. Al leerlo, descubren que no es un tesoro, sino una lista de deseos escrita por estudiantes de años anteriores.', N'EL MENSAJE DENTRO DE LA BOTELLA

El sábado por la mañana, algunos estudiantes fueron al colegio para ayudar en la limpieza del patio. Había hojas secas, envolturas y botellas olvidadas cerca de las bancas. Mientras recogían los residuos, Tomás encontró una botella transparente con un papel enrollado dentro.

—¡Parece un mensaje secreto! —dijo, levantándola con cuidado.

La profesora abrió la botella y sacó el papel. No era un mapa del tesoro, como algunos imaginaron, sino una lista escrita con letra pequeña. Decía: “Deseos para nuestro patio: más sombra, menos basura, juegos pintados en el suelo y un lugar para leer”.

Micaela preguntó quién había escrito eso. La profesora explicó que, años atrás, otro grupo de estudiantes había realizado una actividad parecida y dejó sus ideas dentro de la botella para que alguien las encontrara después.

El grupo miró alrededor. Algunas cosas seguían igual: todavía había basura en ciertas zonas y pocas bancas con sombra. Entonces decidieron no volver a esconder el papel. Lo pegaron en un mural y agregaron una nueva lista con propuestas actuales.

Al terminar la jornada, Tomás dijo que la botella no guardaba un tesoro de monedas, pero sí una pista importante: el patio también necesitaba que alguien lo escuchara.'),
        -- Lectura 11: Cuando el recreo también enseña
        (11, N'Cuando el recreo también enseña', N'Intermedio', N'Expositivo / reflexivo', N'El recreo no solo sirve para descansar. En ese tiempo, los estudiantes también aprenden a convivir, resolver desacuerdos, organizar juegos, compartir espacios y tomar decisiones con otros.', N'CUANDO EL RECREO TAMBIÉN ENSEÑA

Para muchos estudiantes, el recreo es el momento más esperado del día. Algunos corren hacia la cancha, otros compran algo en el quiosco y algunos prefieren conversar bajo la sombra. A simple vista, parece solo un descanso entre clases. Sin embargo, en esos minutos también ocurren aprendizajes importantes.

Durante el recreo, los estudiantes toman decisiones sin que un profesor indique cada paso. Deciden con quién jugar, cómo organizar equipos, cuándo esperar su turno y qué hacer si surge un desacuerdo. Por ejemplo, si dos grupos quieren usar la misma pelota, deben conversar, turnarse o buscar otra actividad. Aunque parezca algo pequeño, resolver ese tipo de situaciones ayuda a practicar el respeto y la comunicación.

El recreo también muestra cómo se relaciona un grupo. Cuando alguien invita a participar a un compañero que está solo, está construyendo convivencia. Cuando otro estudiante escucha una opinión distinta antes de decidir un juego, está aprendiendo a considerar a los demás. Incluso cuando un grupo recoge sus envolturas después de comer, demuestra responsabilidad por un espacio compartido.

Claro que no todos los recreos son perfectos. A veces hay empujones, burlas o discusiones. Por eso, el recreo no debe verse como un momento sin reglas, sino como un espacio donde los estudiantes pueden demostrar autonomía. La libertad de esos minutos funciona mejor cuando va acompañada de cuidado por los demás.

Algunas escuelas han empezado a observar el recreo con más atención. No para llenarlo de órdenes, sino para entender qué necesitan los estudiantes: más espacios de sombra, juegos tranquilos, zonas para conversar o acuerdos para usar la cancha. De ese modo, el recreo deja de ser solo una pausa y se convierte en una parte importante de la vida escolar.

Aprender no ocurre únicamente frente a una pizarra. A veces también sucede cuando alguien espera su turno, comparte una banca o decide incluir a quien parecía invisible.'),
        -- Lectura 12: La biblioteca que nadie visitaba
        (12, N'La biblioteca que nadie visitaba', N'Avanzado', N'Argumentativo / reflexivo', N'Una biblioteca escolar casi vacía empieza a cambiar cuando un grupo de estudiantes propone convertirla en un espacio de lectura, conversación y creación. El texto plantea si el problema era realmente la falta de interés por leer o la manera en que se presentaban los libros a los estudiantes.', N'LA BIBLIOTECA QUE NADIE VISITABA

La biblioteca del colegio estaba al final de un pasillo largo, junto al laboratorio antiguo. Tenía estantes ordenados, mesas limpias y un cartel que decía “Silencio”. Sin embargo, casi nadie entraba. Durante los recreos, la puerta permanecía abierta, pero los estudiantes pasaban de largo como si ese lugar no perteneciera a su vida escolar.

La profesora Andrea notó la situación y preguntó en una tutoría por qué casi nadie usaba la biblioteca. Algunos estudiantes dijeron que no sabían qué libros había. Otros comentaron que les parecía un lugar demasiado serio, donde solo se podía leer en silencio. También hubo quienes dijeron que preferían buscar información en internet porque era más rápido.

Entonces, un grupo de estudiantes propuso hacer una encuesta. No querían obligar a nadie a leer, sino entender qué alejaba a sus compañeros de ese espacio. Los resultados fueron interesantes: muchos no rechazaban la lectura, pero no encontraban libros relacionados con sus intereses; otros querían leer, pero no sabían por dónde empezar; varios dijeron que les gustaría conversar sobre lo que leían, dibujar escenas o recomendar historias a sus amigos.

Con esos datos, el grupo presentó una propuesta. La biblioteca seguiría siendo un lugar de lectura, pero también tendría rincones diferentes: una mesa de recomendaciones, una sección de lecturas cortas, un mural para opiniones y un espacio semanal llamado “libro en diez minutos”, donde alguien contaría de qué trataba un texto sin revelar el final. Además, sugirieron colocar etiquetas más claras en los estantes: misterio, ciencia, humor, aventura, tecnología, historias reales y cuidado del ambiente.

Al principio, la idea recibió dudas. Algunos pensaban que una biblioteca debía mantenerse como siempre: silenciosa, ordenada y reservada para tareas. Pero la profesora Andrea explicó que el orden no estaba peleado con la participación. Una biblioteca podía ser tranquila sin parecer prohibida.

El primer viernes del cambio, entraron doce estudiantes. Unos fueron por curiosidad, otros porque vieron el mural de recomendaciones. Al mes siguiente, la biblioteca ya no estaba llena, pero tampoco vacía. Había estudiantes leyendo, otros buscando libros para una exposición y algunos escribiendo comentarios breves sobre sus historias favoritas.

La profesora no dijo que el problema se había solucionado por completo. Más bien, explicó que leer no depende solo de tener libros disponibles. También importa cómo se invita a acercarse a ellos. La biblioteca no necesitaba convertirse en un lugar ruidoso ni perder su propósito; necesitaba dejar de parecer una sala cerrada para quienes aún no habían descubierto qué podía ofrecerles.');

    DECLARE @ReadingPhaseSeed TABLE
    (
        ReadingTitle NVARCHAR(150) NOT NULL,
        PhaseCode VARCHAR(20) NOT NULL,
        DisplayOrder TINYINT NOT NULL,
        GuidanceText NVARCHAR(500) NOT NULL,
        MinQuestionsToUnlockNext TINYINT NULL
    );

    INSERT INTO @ReadingPhaseSeed (ReadingTitle, PhaseCode, DisplayOrder, GuidanceText, MinQuestionsToUnlockNext)
    VALUES
        (N'La mochila de los objetos perdidos', 'Preview', 1, N'Anticipar de qué tratará la historia a partir del título.', 1),
        (N'La mochila de los objetos perdidos', 'Question', 2, N'Formular una pregunta inicial sobre el problema del texto.', 1),
        (N'La mochila de los objetos perdidos', 'Read', 3, N'Identificar información explícita sobre objetos, personajes y acciones.', 1),
        (N'La mochila de los objetos perdidos', 'Reflect', 4, N'Interpretar cómo las pistas ayudaron a resolver la situación.', 1),
        (N'La mochila de los objetos perdidos', 'Recite', 5, N'Recordar las ideas principales de la historia.', 1),
        (N'La mochila de los objetos perdidos', 'Review', 6, N'Valorar la decisión de los personajes y relacionarla con una acción responsable.

DISTRIBUCIÓN DE PREGUNTAS

Literal: 3 preguntas
Inferencial: 2 preguntas
Crítica-evaluativa: 2 preguntas
Total: 7 preguntas', 1),
        (N'El viaje de una gota en casa', 'Preview', 1, N'Anticipar el tema a partir del título y relacionarlo con actividades diarias.', 1),
        (N'El viaje de una gota en casa', 'Question', 2, N'Formular una pregunta sobre cómo se usa el agua en casa.', 1),
        (N'El viaje de una gota en casa', 'Read', 3, N'Identificar acciones concretas donde aparece el uso del agua.', 1),
        (N'El viaje de una gota en casa', 'Reflect', 4, N'Inferir por qué algunas decisiones cambiaron el recorrido de la gota.', 1),
        (N'El viaje de una gota en casa', 'Recite', 5, N'Recordar las actividades principales mencionadas en el texto.', 1),
        (N'El viaje de una gota en casa', 'Review', 6, N'Valorar qué acción cotidiana fue más útil y justificar una postura.', 1),
        (N'¿Quién debe ser el delegado?', 'Preview', 1, N'Anticipar el tema de la lectura a partir del título.', 1),
        (N'¿Quién debe ser el delegado?', 'Question', 2, N'Formular una pregunta sobre la elección del delegado.', 1),
        (N'¿Quién debe ser el delegado?', 'Read', 3, N'Identificar candidatos, propuestas y acciones mencionadas.', 1),
        (N'¿Quién debe ser el delegado?', 'Reflect', 4, N'Comparar propuestas y deducir cuál resulta más responsable.', 1),
        (N'¿Quién debe ser el delegado?', 'Recite', 5, N'Recordar las ideas principales del texto.', 1),
        (N'¿Quién debe ser el delegado?', 'Review', 6, N'Valorar qué criterio debería usarse para elegir a un representante.', 1),
        (N'El club de los recreos silenciosos', 'Preview', 1, N'Anticipar el tema de convivencia a partir del título.', 1),
        (N'El club de los recreos silenciosos', 'Question', 2, N'Formular una pregunta sobre el problema que enfrentan algunos estudiantes en el recreo.', 1),
        (N'El club de los recreos silenciosos', 'Read', 3, N'Reconocer personajes, acciones y hechos principales del texto.', 1),
        (N'El club de los recreos silenciosos', 'Reflect', 4, N'Inferir por qué algunos estudiantes se alejaban o evitaban participar.', 1),
        (N'El club de los recreos silenciosos', 'Recite', 5, N'Recordar cómo se creó y desarrolló el club.', 1),
        (N'El club de los recreos silenciosos', 'Review', 6, N'Valorar si la propuesta fue adecuada para mejorar la convivencia escolar.', 1),
        (N'La alarma que organiza el día', 'Preview', 1, N'Anticipar el tema de organización personal y tecnología a partir del título.', 1),
        (N'La alarma que organiza el día', 'Question', 2, N'Formular una pregunta sobre el uso de herramientas digitales para estudiar.', 1),
        (N'La alarma que organiza el día', 'Read', 3, N'Reconocer hechos, funciones de la aplicación y acciones de Mateo.', 1),
        (N'La alarma que organiza el día', 'Reflect', 4, N'Inferir ventajas y riesgos de depender demasiado de una herramienta digital.', 1),
        (N'La alarma que organiza el día', 'Recite', 5, N'Recordar el problema, la solución inicial y el aprendizaje final.', 1),
        (N'La alarma que organiza el día', 'Review', 6, N'Valorar el uso equilibrado de la tecnología en la vida escolar.', 1),
        (N'El mapa del mercado del barrio', 'Preview', 1, N'Anticipar qué puede revelar un mercado sobre la vida del barrio.', 1),
        (N'El mapa del mercado del barrio', 'Question', 2, N'Formular una pregunta sobre los problemas y oportunidades que aparecen en el mercado.', 1),
        (N'El mapa del mercado del barrio', 'Read', 3, N'Identificar personajes, hechos y situaciones observadas durante la visita.', 1),
        (N'El mapa del mercado del barrio', 'Reflect', 4, N'Inferir qué problemas y valores comunitarios se muestran en el texto.', 1),
        (N'El mapa del mercado del barrio', 'Recite', 5, N'Recordar las ideas principales del recorrido y las propuestas de los estudiantes.', 1),
        (N'El mapa del mercado del barrio', 'Review', 6, N'Valorar qué acciones podrían mejorar el mercado y beneficiar a la comunidad.', 1),
        (N'La última página del cuaderno rojo', 'Preview', 1, N'Anticipar el tema de memoria familiar y oportunidades educativas a partir del título.', 1),
        (N'La última página del cuaderno rojo', 'Question', 2, N'Formular una pregunta sobre lo que Camila puede aprender del cuaderno de su abuelo.', 1),
        (N'La última página del cuaderno rojo', 'Read', 3, N'Identificar hechos, personajes y detalles explícitos del texto.', 1),
        (N'La última página del cuaderno rojo', 'Reflect', 4, N'Inferir contrastes entre la experiencia escolar del abuelo y la de Camila.', 1),
        (N'La última página del cuaderno rojo', 'Recite', 5, N'Recordar las ideas centrales de la lectura y el cambio de actitud de Camila.', 1),
        (N'La última página del cuaderno rojo', 'Review', 6, N'Valorar el mensaje final sobre el uso de herramientas y oportunidades educativas.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Preview', 1, N'Anticipar el tema ambiental y urbano a partir del título.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Question', 2, N'Formular una pregunta sobre la relación entre árboles, temperatura y vida en la ciudad.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Read', 3, N'Identificar zonas observadas, datos recolectados y hallazgos principales.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Reflect', 4, N'Inferir cómo los árboles influyen en la experiencia de las personas en una ciudad.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Recite', 5, N'Recordar las conclusiones del proyecto y las propuestas del grupo.', 1),
        (N'La ciudad que escuchaba a sus árboles', 'Review', 6, N'Valorar decisiones urbanas a partir de datos y necesidades de la comunidad.', 1),
        (N'El algoritmo de las tareas', 'Preview', 1, N'Anticipar el tema de inteligencia artificial y aprendizaje a partir del título.', 1),
        (N'El algoritmo de las tareas', 'Question', 2, N'Formular una pregunta sobre cómo un sistema puede recomendar tareas.', 1),
        (N'El algoritmo de las tareas', 'Read', 3, N'Identificar cómo funcionaba el sistema y qué recomendaciones ofrecía.', 1),
        (N'El algoritmo de las tareas', 'Reflect', 4, N'Inferir ventajas y límites del uso de algoritmos en el aprendizaje.', 1),
        (N'El algoritmo de las tareas', 'Recite', 5, N'Recordar las ideas principales sobre el sistema, los estudiantes y la profesora.', 1),
        (N'El algoritmo de las tareas', 'Review', 6, N'Valorar críticamente el papel de la tecnología y del criterio humano en la educación.', 1),
        (N'El mensaje dentro de la botella', 'Preview', 1, N'Anticipar qué puede contener la botella a partir del título.', 1),
        (N'El mensaje dentro de la botella', 'Question', 2, N'Formular una pregunta sobre el mensaje encontrado.', 1),
        (N'El mensaje dentro de la botella', 'Read', 3, N'Identificar personajes, lugar, objeto encontrado y contenido del mensaje.', 1),
        (N'El mensaje dentro de la botella', 'Reflect', 4, N'Inferir por qué el mensaje era importante para el grupo actual.', 1),
        (N'El mensaje dentro de la botella', 'Recite', 5, N'Recordar las acciones principales realizadas por los estudiantes.', NULL),
        (N'El mensaje dentro de la botella', 'Review', 6, N'Valorar la importancia de cuidar y mejorar los espacios compartidos.', 1),
        (N'Cuando el recreo también enseña', 'Preview', 1, N'Anticipar que el recreo puede tener un valor más allá del descanso.', 1),
        (N'Cuando el recreo también enseña', 'Question', 2, N'Formular una pregunta sobre lo que se puede aprender durante el recreo.', 1),
        (N'Cuando el recreo también enseña', 'Read', 3, N'Identificar acciones y ejemplos mencionados en el texto.', 1),
        (N'Cuando el recreo también enseña', 'Reflect', 4, N'Inferir cómo el recreo favorece la convivencia y la autonomía.', 1),
        (N'Cuando el recreo también enseña', 'Recite', 5, N'Recordar las ideas principales sobre el recreo como espacio educativo.', 1),
        (N'Cuando el recreo también enseña', 'Review', 6, N'Valorar la importancia de usar el recreo con responsabilidad y respeto.', 1),
        (N'La biblioteca que nadie visitaba', 'Preview', 1, N'Anticipar el problema de una biblioteca poco visitada y sus posibles causas.', 1),
        (N'La biblioteca que nadie visitaba', 'Question', 2, N'Formular una pregunta sobre por qué los estudiantes no usan la biblioteca.', 1),
        (N'La biblioteca que nadie visitaba', 'Read', 3, N'Identificar hechos, opiniones de estudiantes y propuestas presentadas.', 1),
        (N'La biblioteca que nadie visitaba', 'Reflect', 4, N'Inferir que el desinterés por la biblioteca puede depender de cómo se presenta el espacio.', 1),
        (N'La biblioteca que nadie visitaba', 'Recite', 5, N'Recordar el problema inicial, los datos de la encuesta y los cambios realizados.', 1),
        (N'La biblioteca que nadie visitaba', 'Review', 6, N'Valorar críticamente cómo una biblioteca escolar puede motivar la lectura sin imponerla.', 1);

    DECLARE @QuestionSeed TABLE
    (
        ReadingTitle NVARCHAR(150) NOT NULL,
        QuestionOrder TINYINT NOT NULL,
        PhaseCode VARCHAR(20) NOT NULL,
        DimensionName NVARCHAR(30) NOT NULL,
        Stem NVARCHAR(500) NOT NULL,
        Points DECIMAL(5, 2) NOT NULL,
        PRIMARY KEY (ReadingTitle, QuestionOrder)
    );

    INSERT INTO @QuestionSeed (ReadingTitle, QuestionOrder, PhaseCode, DimensionName, Stem, Points)
    VALUES
        (N'La mochila de los objetos perdidos', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué situación podría ocurrir en la historia?', 100.00),
        (N'La mochila de los objetos perdidos', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a iniciar la lectura?', 100.00),
        (N'La mochila de los objetos perdidos', 3, 'Read', N'Literal', N'¿Dónde encontró Valeria la mochila azul?', 100.00),
        (N'La mochila de los objetos perdidos', 4, 'Read', N'Literal', N'¿Qué objetos había dentro de la mochila?', 100.00),
        (N'La mochila de los objetos perdidos', 5, 'Reflect', N'Literal', N'¿Qué pista llevó a los estudiantes hacia la biblioteca?', 100.00),
        (N'La mochila de los objetos perdidos', 6, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor la acción de Valeria, Mateo y Sofía?', 100.00),
        (N'La mochila de los objetos perdidos', 7, 'Review', N'Crítica-Evaluativa', N'¿Qué enseñanza se relaciona mejor con la decisión de los estudiantes?', 100.00),
        (N'El viaje de una gota en casa', 1, 'Preview', N'Inferencial', N'A partir del título, ¿sobre qué tratará principalmente la lectura?', 100.00),
        (N'El viaje de una gota en casa', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer este texto con propósito?', 100.00),
        (N'El viaje de una gota en casa', 3, 'Read', N'Literal', N'¿Qué hizo Diego mientras la gota salió del caño del baño?', 100.00),
        (N'El viaje de una gota en casa', 4, 'Read', N'Literal', N'¿Qué hizo la mamá de Diego con el agua después de lavar las frutas?', 100.00),
        (N'El viaje de una gota en casa', 5, 'Read', N'Literal', N'¿Para qué llenó un balde el hermano mayor?', 100.00),
        (N'El viaje de una gota en casa', 6, 'Reflect', N'Inferencial', N'¿Por qué el hermano mayor decidió no dejar el caño abierto?', 100.00),
        (N'El viaje de una gota en casa', 7, 'Recite', N'Inferencial', N'¿Cuál es una idea principal del texto?', 100.00),
        (N'El viaje de una gota en casa', 8, 'Review', N'Crítica-Evaluativa', N'¿Cuál de estas acciones muestra una decisión más responsable según la lectura?', 100.00),
        (N'El viaje de una gota en casa', 9, 'Review', N'Crítica-Evaluativa', N'¿Qué conclusión se relaciona mejor con el final del texto?', 100.00),
        (N'¿Quién debe ser el delegado?', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué tema se tratará principalmente en la lectura?', 100.00),
        (N'¿Quién debe ser el delegado?', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'¿Quién debe ser el delegado?', 3, 'Read', N'Literal', N'¿Qué debía hacer el delegado según la profesora?', 100.00),
        (N'¿Quién debe ser el delegado?', 4, 'Read', N'Literal', N'¿Qué prometió Bruno si lo elegían delegado?', 100.00),
        (N'¿Quién debe ser el delegado?', 5, 'Read', N'Literal', N'¿Qué propuso Ana para mejorar el aula?', 100.00),
        (N'¿Quién debe ser el delegado?', 6, 'Reflect', N'Inferencial', N'¿Por qué algunos estudiantes se interesaron al inicio por la propuesta de Bruno?', 100.00),
        (N'¿Quién debe ser el delegado?', 7, 'Reflect', N'Inferencial', N'¿Por qué la propuesta de Diego parecía incompleta?', 100.00),
        (N'¿Quién debe ser el delegado?', 8, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor el mensaje de la lectura?', 100.00),
        (N'¿Quién debe ser el delegado?', 9, 'Review', N'Crítica-Evaluativa', N'¿Cuál sería el mejor criterio para elegir al delegado?', 100.00),
        (N'El club de los recreos silenciosos', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué situación podría tratar la lectura?', 100.00),
        (N'El club de los recreos silenciosos', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'El club de los recreos silenciosos', 3, 'Read', N'Literal', N'¿Qué notó Lucía durante la primera semana de clases?', 100.00),
        (N'El club de los recreos silenciosos', 4, 'Read', N'Literal', N'¿Qué objeto guardó Renato cuando se acercaron dos compañeros?', 100.00),
        (N'El club de los recreos silenciosos', 5, 'Read', N'Literal', N'¿Qué materiales llevaron Lucía, Camila y Diego para iniciar la actividad?', 100.00),
        (N'El club de los recreos silenciosos', 6, 'Reflect', N'Inferencial', N'¿Por qué Renato pudo haber guardado su libro cuando llegaron los compañeros?', 100.00),
        (N'El club de los recreos silenciosos', 7, 'Reflect', N'Inferencial', N'¿Por qué Lucía y sus amigos no anunciaron la actividad como una obligación?', 100.00),
        (N'El club de los recreos silenciosos', 8, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor la acción de Lucía, Camila y Diego?', 100.00),
        (N'El club de los recreos silenciosos', 9, 'Review', N'Crítica-Evaluativa', N'¿Por qué la propuesta del club puede considerarse positiva para la convivencia?', 100.00),
        (N'La alarma que organiza el día', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué tema tratará principalmente la lectura?', 100.00),
        (N'La alarma que organiza el día', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'La alarma que organiza el día', 3, 'Read', N'Literal', N'¿Por qué Mateo decidió probar la aplicación?', 100.00),
        (N'La alarma que organiza el día', 4, 'Read', N'Literal', N'¿Qué actividades permitía organizar la aplicación?', 100.00),
        (N'La alarma que organiza el día', 5, 'Read', N'Literal', N'¿Qué ocurrió cuando el celular de Mateo se quedó sin batería?', 100.00),
        (N'La alarma que organiza el día', 6, 'Reflect', N'Inferencial', N'¿Por qué Mateo se sintió más tranquilo durante la primera semana?', 100.00),
        (N'La alarma que organiza el día', 7, 'Reflect', N'Inferencial', N'¿Qué problema muestra la experiencia del celular sin batería?', 100.00),
        (N'La alarma que organiza el día', 8, 'Recite', N'Inferencial', N'¿Cuál fue el cambio principal que hizo Mateo después de hablar con su tutora?', 100.00),
        (N'La alarma que organiza el día', 9, 'Review', N'Crítica-Evaluativa', N'¿Cuál es la idea más razonable sobre el uso de la aplicación?', 100.00),
        (N'La alarma que organiza el día', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué enseñanza final deja la lectura?', 100.00),
        (N'El mapa del mercado del barrio', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué podría significar “el mapa del mercado del barrio”?', 100.00),
        (N'El mapa del mercado del barrio', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'El mapa del mercado del barrio', 3, 'Read', N'Literal', N'¿Qué actividad pidió realizar la profesora en el mercado?', 100.00),
        (N'El mapa del mercado del barrio', 4, 'Read', N'Literal', N'¿Qué hacía la señora Rosa en el mercado?', 100.00),
        (N'El mapa del mercado del barrio', 5, 'Read', N'Literal', N'¿Qué problemas observaron los estudiantes en el mercado?', 100.00),
        (N'El mapa del mercado del barrio', 6, 'Reflect', N'Inferencial', N'¿Qué quiso decir don Emilio con la frase “No todo lo imperfecto es inútil”?', 100.00),
        (N'El mapa del mercado del barrio', 7, 'Reflect', N'Inferencial', N'¿Por qué el texto afirma que el mercado no era solo un lugar para comprar?', 100.00),
        (N'El mapa del mercado del barrio', 8, 'Recite', N'Inferencial', N'¿Cuál de estas ideas resume mejor lo aprendido por los estudiantes?', 100.00),
        (N'El mapa del mercado del barrio', 9, 'Review', N'Crítica-Evaluativa', N'¿Cuál propuesta parece más adecuada para reducir residuos en el mercado?', 100.00),
        (N'El mapa del mercado del barrio', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué actitud demuestra una mejor comprensión del texto?', 100.00),
        (N'La última página del cuaderno rojo', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué podría representar el cuaderno rojo en la lectura?', 100.00),
        (N'La última página del cuaderno rojo', 2, 'Question', N'Inferencial', N'¿Qué pregunta orienta mejor la lectura del texto?', 100.00),
        (N'La última página del cuaderno rojo', 3, 'Read', N'Literal', N'¿Dónde encontró Camila el cuaderno rojo?', 100.00),
        (N'La última página del cuaderno rojo', 4, 'Read', N'Literal', N'¿Qué decía la primera página del cuaderno?', 100.00),
        (N'La última página del cuaderno rojo', 5, 'Read', N'Literal', N'Según las notas del cuaderno, ¿qué dificultad tenía Julián para estudiar?', 100.00),
        (N'La última página del cuaderno rojo', 6, 'Reflect', N'Inferencial', N'¿Por qué Camila se sorprendió al leer las notas de su abuelo?', 100.00),
        (N'La última página del cuaderno rojo', 7, 'Reflect', N'Inferencial', N'¿Qué quiso decir Julián con la frase “Las herramientas ayudan, pero no estudian por uno”?', 100.00),
        (N'La última página del cuaderno rojo', 8, 'Reflect', N'Inferencial', N'¿Qué cambio muestra Camila al hacer su tarea al día siguiente?', 100.00),
        (N'La última página del cuaderno rojo', 9, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor el sentido de la lectura?', 100.00),
        (N'La última página del cuaderno rojo', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué actitud de Camila demuestra una mejor comprensión del mensaje de su abuelo?', 100.00),
        (N'La última página del cuaderno rojo', 11, 'Review', N'Crítica-Evaluativa', N'¿Cuál es una conclusión crítica adecuada sobre la lectura?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué idea podría anticiparse sobre la lectura?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 2, 'Question', N'Inferencial', N'¿Qué pregunta orienta mejor la lectura del texto?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 3, 'Read', N'Literal', N'¿Qué pregunta propuso la profesora al iniciar el proyecto?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 4, 'Read', N'Literal', N'¿Qué datos midieron los estudiantes durante el recorrido?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 5, 'Read', N'Literal', N'¿Qué característica tenía la tercera zona observada por los estudiantes?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 6, 'Reflect', N'Inferencial', N'¿Por qué los estudiantes concluyeron que los árboles no solo servían para decorar?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 7, 'Reflect', N'Inferencial', N'¿Qué sugiere el caso del parque reemplazado por estacionamiento?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 8, 'Recite', N'Inferencial', N'¿Cuál fue una propuesta del grupo después de organizar los datos?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 9, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor el sentido del proyecto escolar?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué decisión sería más responsable según la lectura?', 100.00),
        (N'La ciudad que escuchaba a sus árboles', 11, 'Review', N'Crítica-Evaluativa', N'¿Qué significa mejor la frase final sobre “escuchar a los árboles”?', 100.00),
        (N'El algoritmo de las tareas', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué tema podría tratar principalmente la lectura?', 100.00),
        (N'El algoritmo de las tareas', 2, 'Question', N'Inferencial', N'¿Qué pregunta orienta mejor la lectura del texto?', 100.00),
        (N'El algoritmo de las tareas', 3, 'Read', N'Literal', N'¿Qué hacía el sistema después de revisar los resultados de cada estudiante?', 100.00),
        (N'El algoritmo de las tareas', 4, 'Read', N'Literal', N'¿Qué tipo de ejercicios recibían quienes fallaban preguntas literales?', 100.00),
        (N'El algoritmo de las tareas', 5, 'Read', N'Literal', N'¿Qué le ocurrió a Paula durante los primeros días de uso del sistema?', 100.00),
        (N'El algoritmo de las tareas', 6, 'Reflect', N'Inferencial', N'¿Por qué Joaquín se sorprendió con la recomendación del sistema?', 100.00),
        (N'El algoritmo de las tareas', 7, 'Reflect', N'Inferencial', N'¿Qué límite del sistema señalaron algunos estudiantes durante la tutoría?', 100.00),
        (N'El algoritmo de las tareas', 8, 'Reflect', N'Inferencial', N'¿Qué quiso decir la profesora al afirmar que el algoritmo no era un juez perfecto?', 100.00),
        (N'El algoritmo de las tareas', 9, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor el sentido central de la lectura?', 100.00),
        (N'El algoritmo de las tareas', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué decisión de Joaquín demuestra una actitud responsable frente a la recomendación del sistema?', 100.00),
        (N'El algoritmo de las tareas', 11, 'Review', N'Crítica-Evaluativa', N'¿Cuál es una conclusión crítica adecuada sobre el uso de algoritmos en educación?', 100.00),
        (N'El mensaje dentro de la botella', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué podría ocurrir en la lectura?', 100.00),
        (N'El mensaje dentro de la botella', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'El mensaje dentro de la botella', 3, 'Read', N'Literal', N'¿Para qué fueron algunos estudiantes al colegio el sábado?', 100.00),
        (N'El mensaje dentro de la botella', 4, 'Read', N'Literal', N'¿Qué encontró Tomás cerca de las bancas?', 100.00),
        (N'El mensaje dentro de la botella', 5, 'Read', N'Literal', N'¿Qué decía la lista encontrada en la botella?', 100.00),
        (N'El mensaje dentro de la botella', 6, 'Reflect', N'Inferencial', N'¿Por qué el grupo decidió pegar el mensaje en un mural?', 100.00),
        (N'El mensaje dentro de la botella', 7, 'Review', N'Crítica-Evaluativa', N'¿Qué enseñanza se relaciona mejor con la lectura?', 100.00),
        (N'Cuando el recreo también enseña', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué idea podría anticiparse sobre la lectura?', 100.00),
        (N'Cuando el recreo también enseña', 2, 'Question', N'Inferencial', N'¿Qué pregunta ayuda mejor a leer el texto con propósito?', 100.00),
        (N'Cuando el recreo también enseña', 3, 'Read', N'Literal', N'¿Qué hacen algunos estudiantes durante el recreo según el texto?', 100.00),
        (N'Cuando el recreo también enseña', 4, 'Read', N'Literal', N'¿Qué deben hacer dos grupos si quieren usar la misma pelota?', 100.00),
        (N'Cuando el recreo también enseña', 5, 'Read', N'Literal', N'¿Qué demuestra un grupo cuando recoge sus envolturas después de comer?', 100.00),
        (N'Cuando el recreo también enseña', 6, 'Reflect', N'Inferencial', N'¿Por qué el texto dice que en el recreo los estudiantes practican autonomía?', 100.00),
        (N'Cuando el recreo también enseña', 7, 'Reflect', N'Inferencial', N'¿Qué se puede inferir cuando un estudiante invita a participar a un compañero que está solo?', 100.00),
        (N'Cuando el recreo también enseña', 8, 'Recite', N'Inferencial', N'¿Cuál de estas ideas resume mejor el contenido del texto?', 100.00),
        (N'Cuando el recreo también enseña', 9, 'Review', N'Crítica-Evaluativa', N'¿Qué acción sería más adecuada para mejorar el recreo según la lectura?', 100.00),
        (N'Cuando el recreo también enseña', 10, 'Review', N'Crítica-Evaluativa', N'¿Qué conclusión se relaciona mejor con el mensaje final del texto?', 100.00),
        (N'La biblioteca que nadie visitaba', 1, 'Preview', N'Inferencial', N'A partir del título, ¿qué problema podría presentar la lectura?', 100.00),
        (N'La biblioteca que nadie visitaba', 2, 'Question', N'Inferencial', N'¿Qué pregunta orienta mejor la lectura del texto?', 100.00),
        (N'La biblioteca que nadie visitaba', 3, 'Read', N'Literal', N'¿Dónde estaba ubicada la biblioteca del colegio?', 100.00),
        (N'La biblioteca que nadie visitaba', 4, 'Read', N'Literal', N'¿Qué decía el cartel de la biblioteca?', 100.00),
        (N'La biblioteca que nadie visitaba', 5, 'Read', N'Literal', N'¿Qué hicieron los estudiantes para conocer por qué sus compañeros no usaban la biblioteca?', 100.00),
        (N'La biblioteca que nadie visitaba', 6, 'Reflect', N'Inferencial', N'¿Qué muestran los resultados de la encuesta?', 100.00),
        (N'La biblioteca que nadie visitaba', 7, 'Reflect', N'Inferencial', N'¿Por qué algunos estudiantes veían la biblioteca como un lugar demasiado serio?', 100.00),
        (N'La biblioteca que nadie visitaba', 8, 'Reflect', N'Inferencial', N'¿Qué significa la idea de que “el orden no estaba peleado con la participación”?', 100.00),
        (N'La biblioteca que nadie visitaba', 9, 'Recite', N'Crítica-Evaluativa', N'¿Cuál de estas ideas resume mejor la transformación de la biblioteca?', 100.00),
        (N'La biblioteca que nadie visitaba', 10, 'Review', N'Crítica-Evaluativa', N'¿Cuál propuesta parece más adecuada para motivar la lectura según el texto?', 100.00),
        (N'La biblioteca que nadie visitaba', 11, 'Review', N'Crítica-Evaluativa', N'¿Qué conclusión crítica se relaciona mejor con la lectura?', 100.00);

    DECLARE @OptionSeed TABLE
    (
        ReadingTitle NVARCHAR(150) NOT NULL,
        QuestionOrder TINYINT NOT NULL,
        DisplayOrder TINYINT NOT NULL,
        OptionText NVARCHAR(300) NOT NULL,
        IsCorrect BIT NOT NULL,
        PRIMARY KEY (ReadingTitle, QuestionOrder, DisplayOrder)
    );

    INSERT INTO @OptionSeed (ReadingTitle, QuestionOrder, DisplayOrder, OptionText, IsCorrect)
    VALUES
        (N'La mochila de los objetos perdidos', 1, 1, N'Unos estudiantes intentarán descubrir a quién pertenece una mochila.', 1),
        (N'La mochila de los objetos perdidos', 1, 2, N'Un grupo organizará una competencia de mochilas.', 0),
        (N'La mochila de los objetos perdidos', 1, 3, N'Una profesora enseñará a coser una mochila nueva.', 0),
        (N'La mochila de los objetos perdidos', 2, 1, N'¿Cuántos libros hay en la biblioteca?', 0),
        (N'La mochila de los objetos perdidos', 2, 2, N'¿Quién perdió la mochila y cómo podrán encontrarlo?', 1),
        (N'La mochila de los objetos perdidos', 2, 3, N'¿Por qué todos los estudiantes usan mochila azul?', 0),
        (N'La mochila de los objetos perdidos', 3, 1, N'Debajo de una carpeta vacía.', 1),
        (N'La mochila de los objetos perdidos', 3, 2, N'Encima de una mesa de la biblioteca.', 0),
        (N'La mochila de los objetos perdidos', 3, 3, N'Junto a la puerta del colegio.', 0),
        (N'La mochila de los objetos perdidos', 4, 1, N'Una brújula, una llave pequeña y una libreta con dibujos.', 1),
        (N'La mochila de los objetos perdidos', 4, 2, N'Un celular, una pelota y una botella de agua.', 0),
        (N'La mochila de los objetos perdidos', 4, 3, N'Un cuaderno roto, una regla y una lonchera.', 0),
        (N'La mochila de los objetos perdidos', 5, 1, N'Una nota escrita por la profesora.', 0),
        (N'La mochila de los objetos perdidos', 5, 2, N'Una flecha dibujada en la última página de la libreta.', 1),
        (N'La mochila de los objetos perdidos', 5, 3, N'Una llamada por los parlantes del colegio.', 0),
        (N'La mochila de los objetos perdidos', 6, 1, N'Decidieron quedarse con la mochila porque no tenía nombre.', 0),
        (N'La mochila de los objetos perdidos', 6, 2, N'Observaron las pistas y buscaron al dueño de la mochila.', 1),
        (N'La mochila de los objetos perdidos', 6, 3, N'Ignoraron la mochila porque no era de su salón.', 0),
        (N'La mochila de los objetos perdidos', 7, 1, N'Es mejor sacar conclusiones rápidas cuando algo parece extraño.', 0),
        (N'La mochila de los objetos perdidos', 7, 2, N'Observar con atención puede ayudar a resolver una situación de forma responsable.', 1),
        (N'La mochila de los objetos perdidos', 7, 3, N'Los objetos perdidos siempre deben dejarse donde fueron encontrados.', 0),
        (N'El viaje de una gota en casa', 1, 1, N'Sobre el recorrido del agua en distintas actividades de una casa.', 1),
        (N'El viaje de una gota en casa', 1, 2, N'Sobre una competencia de natación entre estudiantes.', 0),
        (N'El viaje de una gota en casa', 1, 3, N'Sobre una tormenta que destruye varias viviendas.', 0),
        (N'El viaje de una gota en casa', 2, 1, N'¿Cuántos años tiene Diego?', 0),
        (N'El viaje de una gota en casa', 2, 2, N'¿Cómo se usa el agua en diferentes momentos del día?', 1),
        (N'El viaje de una gota en casa', 2, 3, N'¿Qué marca de zapatillas usa el hermano de Diego?', 0),
        (N'El viaje de una gota en casa', 3, 1, N'Se lavaba los dientes.', 1),
        (N'El viaje de una gota en casa', 3, 2, N'Regaba una planta del patio.', 0),
        (N'El viaje de una gota en casa', 3, 3, N'Limpiaba sus zapatillas.', 0),
        (N'El viaje de una gota en casa', 4, 1, N'La tiró inmediatamente al desagüe.', 0),
        (N'El viaje de una gota en casa', 4, 2, N'La usó para regar una planta pequeña.', 1),
        (N'El viaje de una gota en casa', 4, 3, N'La guardó para lavar ropa.', 0),
        (N'El viaje de una gota en casa', 5, 1, N'Para limpiar sus zapatillas.', 1),
        (N'El viaje de una gota en casa', 5, 2, N'Para bañar a una mascota.', 0),
        (N'El viaje de una gota en casa', 5, 3, N'Para lavar una bicicleta.', 0),
        (N'El viaje de una gota en casa', 6, 1, N'Porque recordó que solo necesitaba poca agua para limpiar.', 1),
        (N'El viaje de una gota en casa', 6, 2, N'Porque el caño se rompió de repente.', 0),
        (N'El viaje de una gota en casa', 6, 3, N'Porque ya no quería limpiar sus zapatillas.', 0),
        (N'El viaje de una gota en casa', 7, 1, N'El agua aparece en varias actividades diarias dentro de una casa.', 1),
        (N'El viaje de una gota en casa', 7, 2, N'Diego nunca usa agua durante la mañana.', 0),
        (N'El viaje de una gota en casa', 7, 3, N'Las plantas no necesitan agua para crecer.', 0),
        (N'El viaje de una gota en casa', 8, 1, N'Dejar el caño abierto mientras se busca el cepillo.', 0),
        (N'El viaje de una gota en casa', 8, 2, N'Usar un balde con solo el agua necesaria para limpiar.', 1),
        (N'El viaje de una gota en casa', 8, 3, N'Lavar frutas y botar el agua sin pensar en otro uso.', 0),
        (N'El viaje de una gota en casa', 9, 1, N'Los cambios pequeños en casa pueden mejorar la forma en que se usa el agua.', 1),
        (N'El viaje de una gota en casa', 9, 2, N'Solo los adultos pueden decidir cómo se usa el agua en casa.', 0),
        (N'El viaje de una gota en casa', 9, 3, N'El agua solo es importante cuando falta por completo.', 0),
        (N'¿Quién debe ser el delegado?', 1, 1, N'La elección de un representante del salón.', 1),
        (N'¿Quién debe ser el delegado?', 1, 2, N'La preparación de una fiesta escolar.', 0),
        (N'¿Quién debe ser el delegado?', 1, 3, N'La compra de materiales deportivos.', 0),
        (N'¿Quién debe ser el delegado?', 2, 1, N'¿Qué candidato tiene la propuesta más útil para el salón?', 1),
        (N'¿Quién debe ser el delegado?', 2, 2, N'¿Cuántos estudiantes llegaron tarde ese día?', 0),
        (N'¿Quién debe ser el delegado?', 2, 3, N'¿Qué curso tuvo el salón después del recreo?', 0),
        (N'¿Quién debe ser el delegado?', 3, 1, N'Escuchar a sus compañeros, comunicar acuerdos y ayudar a organizar actividades.', 1),
        (N'¿Quién debe ser el delegado?', 3, 2, N'Repartir premios todos los viernes.', 0),
        (N'¿Quién debe ser el delegado?', 3, 3, N'Elegir las tareas de todos los cursos.', 0),
        (N'¿Quién debe ser el delegado?', 4, 1, N'Pedir más minutos de recreo y organizar un día sin tareas.', 1),
        (N'¿Quién debe ser el delegado?', 4, 2, N'Comprar libros nuevos para todos.', 0),
        (N'¿Quién debe ser el delegado?', 4, 3, N'Limpiar el patio después de clases.', 0),
        (N'¿Quién debe ser el delegado?', 5, 1, N'Ordenar turnos, avisar problemas y reunir ideas para mejorar la convivencia.', 1),
        (N'¿Quién debe ser el delegado?', 5, 2, N'Cancelar todas las actividades del salón.', 0),
        (N'¿Quién debe ser el delegado?', 5, 3, N'Cambiar de aula todos los días.', 0),
        (N'¿Quién debe ser el delegado?', 6, 1, N'Porque parecía divertida y agradable para ellos.', 1),
        (N'¿Quién debe ser el delegado?', 6, 2, N'Porque resolvía todos los problemas del aula.', 0),
        (N'¿Quién debe ser el delegado?', 6, 3, N'Porque era la propuesta más organizada.', 0),
        (N'¿Quién debe ser el delegado?', 7, 1, N'Porque prometió decorar el salón, pero no sabía cómo conseguir los materiales.', 1),
        (N'¿Quién debe ser el delegado?', 7, 2, N'Porque no quiso participar en la elección.', 0),
        (N'¿Quién debe ser el delegado?', 7, 3, N'Porque propuso escuchar a todos sus compañeros.', 0),
        (N'¿Quién debe ser el delegado?', 8, 1, N'Elegir a un delegado requiere pensar en lo que beneficia al grupo.', 1),
        (N'¿Quién debe ser el delegado?', 8, 2, N'Siempre debe ganar quien promete cosas más divertidas.', 0),
        (N'¿Quién debe ser el delegado?', 8, 3, N'Las elecciones del aula no necesitan reflexión.', 0),
        (N'¿Quién debe ser el delegado?', 9, 1, N'Elegir a quien haga promesas llamativas aunque no explique cómo cumplirlas.', 0),
        (N'¿Quién debe ser el delegado?', 9, 2, N'Elegir a quien proponga acciones responsables y útiles para todos.', 1),
        (N'¿Quién debe ser el delegado?', 9, 3, N'Elegir al compañero que hable más fuerte durante el recreo.', 0),
        (N'El club de los recreos silenciosos', 1, 1, N'Un grupo de estudiantes crea un espacio tranquilo durante el recreo.', 1),
        (N'El club de los recreos silenciosos', 1, 2, N'Un salón organiza una competencia deportiva durante todo el día.', 0),
        (N'El club de los recreos silenciosos', 1, 3, N'Una profesora cancela los recreos por mal comportamiento.', 0),
        (N'El club de los recreos silenciosos', 2, 1, N'¿Por qué algunos estudiantes no participan en los juegos del recreo?', 1),
        (N'El club de los recreos silenciosos', 2, 2, N'¿Cuántas pelotas hay en la cancha del colegio?', 0),
        (N'El club de los recreos silenciosos', 2, 3, N'¿Qué comida vende el quiosco durante el recreo?', 0),
        (N'El club de los recreos silenciosos', 3, 1, N'Que algunos estudiantes se quedaban solos o mirando el celular durante el recreo.', 1),
        (N'El club de los recreos silenciosos', 3, 2, N'Que todos los estudiantes jugaban fútbol en la cancha.', 0),
        (N'El club de los recreos silenciosos', 3, 3, N'Que la profesora tutora prohibió las historietas.', 0),
        (N'El club de los recreos silenciosos', 4, 1, N'Un libro.', 1),
        (N'El club de los recreos silenciosos', 4, 2, N'Una pelota.', 0),
        (N'El club de los recreos silenciosos', 4, 3, N'Una lonchera.', 0),
        (N'El club de los recreos silenciosos', 5, 1, N'Juegos de mesa pequeños, historietas cortas y una caja de temas sorpresa.', 1),
        (N'El club de los recreos silenciosos', 5, 2, N'Uniformes deportivos, silbatos y medallas.', 0),
        (N'El club de los recreos silenciosos', 5, 3, N'Carteles de castigo, reglas nuevas y exámenes.', 0),
        (N'El club de los recreos silenciosos', 6, 1, N'Porque temía que se burlaran de él por leer en el recreo.', 1),
        (N'El club de los recreos silenciosos', 6, 2, N'Porque ya no quería leer nunca más.', 0),
        (N'El club de los recreos silenciosos', 6, 3, N'Porque la profesora le había quitado el libro.', 0),
        (N'El club de los recreos silenciosos', 7, 1, N'Porque querían que los estudiantes se acercaran libremente, sin presión.', 1),
        (N'El club de los recreos silenciosos', 7, 2, N'Porque no querían que nadie supiera dónde estaban.', 0),
        (N'El club de los recreos silenciosos', 7, 3, N'Porque la actividad estaba prohibida por el colegio.', 0),
        (N'El club de los recreos silenciosos', 8, 1, N'Crearon una alternativa para que más estudiantes pudieran participar a su manera.', 1),
        (N'El club de los recreos silenciosos', 8, 2, N'Formaron un grupo cerrado para excluir a quienes jugaban fútbol.', 0),
        (N'El club de los recreos silenciosos', 8, 3, N'Obligaron a todos los estudiantes a dejar de usar el recreo libremente.', 0),
        (N'El club de los recreos silenciosos', 9, 1, N'Porque ofrece un espacio donde distintos estudiantes pueden sentirse incluidos.', 1),
        (N'El club de los recreos silenciosos', 9, 2, N'Porque elimina todos los juegos ruidosos del colegio.', 0),
        (N'El club de los recreos silenciosos', 9, 3, N'Porque demuestra que solo deben participar quienes leen historietas.', 0),
        (N'La alarma que organiza el día', 1, 1, N'Una aplicación que ayuda a organizar actividades diarias.', 1),
        (N'La alarma que organiza el día', 1, 2, N'Una alarma que se pierde dentro de una mochila.', 0),
        (N'La alarma que organiza el día', 1, 3, N'Un concurso de sonidos en el colegio.', 0),
        (N'La alarma que organiza el día', 1, 4, N'Una clase sobre reparación de celulares.', 0),
        (N'La alarma que organiza el día', 2, 1, N'¿Cuánto cuesta comprar un celular nuevo?', 0),
        (N'La alarma que organiza el día', 2, 2, N'¿Cómo puede ayudar una aplicación a organizar el estudio sin reemplazar la responsabilidad personal?', 1),
        (N'La alarma que organiza el día', 2, 3, N'¿Qué sonido de alarma es más fuerte?', 0),
        (N'La alarma que organiza el día', 2, 4, N'¿Cuántos cursos tiene Mateo en el colegio?', 0),
        (N'La alarma que organiza el día', 3, 1, N'Porque solía olvidar algunas tareas y quería organizarse mejor.', 1),
        (N'La alarma que organiza el día', 3, 2, N'Porque quería jugar durante más tiempo.', 0),
        (N'La alarma que organiza el día', 3, 3, N'Porque su tutora le prohibió usar cuadernos.', 0),
        (N'La alarma que organiza el día', 4, 1, N'Estudiar, descansar, ordenar la mochila y revisar tareas pendientes.', 1),
        (N'La alarma que organiza el día', 4, 2, N'Comprar comida, mirar películas y cambiar de salón.', 0),
        (N'La alarma que organiza el día', 4, 3, N'Jugar fútbol, vender rifas y elegir delegado.', 0),
        (N'La alarma que organiza el día', 5, 1, N'No preparó los materiales para una exposición.', 1),
        (N'La alarma que organiza el día', 5, 2, N'Terminó todas sus tareas más temprano.', 0),
        (N'La alarma que organiza el día', 5, 3, N'La aplicación organizó sus materiales sola.', 0),
        (N'La alarma que organiza el día', 6, 1, N'Porque la aplicación le ayudó a recordar actividades y entregar tareas a tiempo.', 1),
        (N'La alarma que organiza el día', 6, 2, N'Porque ya no necesitaba estudiar ningún curso.', 0),
        (N'La alarma que organiza el día', 6, 3, N'Porque la profesora dejó de revisar tareas.', 0),
        (N'La alarma que organiza el día', 6, 4, N'Porque su celular nunca se descargaba.', 0),
        (N'La alarma que organiza el día', 7, 1, N'Que depender totalmente de una herramienta digital puede causar dificultades.', 1),
        (N'La alarma que organiza el día', 7, 2, N'Que las exposiciones no necesitan preparación.', 0),
        (N'La alarma que organiza el día', 7, 3, N'Que las aplicaciones siempre resuelven todos los problemas.', 0),
        (N'La alarma que organiza el día', 7, 4, N'Que estudiar en bloques pequeños es inútil.', 0),
        (N'La alarma que organiza el día', 8, 1, N'Dejó de estudiar por completo.', 0),
        (N'La alarma que organiza el día', 8, 2, N'Siguió usando la aplicación, pero también empezó a revisar sus pendientes por cuenta propia.', 1),
        (N'La alarma que organiza el día', 8, 3, N'Cambió de colegio para no usar alarmas.', 0),
        (N'La alarma que organiza el día', 8, 4, N'Decidió preparar sus tareas solo cuando sonara una campana.', 0),
        (N'La alarma que organiza el día', 9, 1, N'Es útil si ayuda a organizarse, pero no debe reemplazar los hábitos personales.', 1),
        (N'La alarma que organiza el día', 9, 2, N'Es mejor dejar que la aplicación tome todas las decisiones.', 0),
        (N'La alarma que organiza el día', 9, 3, N'No sirve para nada porque una vez falló.', 0),
        (N'La alarma que organiza el día', 9, 4, N'Debe usarse solo para escuchar sonidos.', 0),
        (N'La alarma que organiza el día', 10, 1, N'La tecnología puede apoyar el estudio, pero la responsabilidad sigue siendo del estudiante.', 1),
        (N'La alarma que organiza el día', 10, 2, N'Los estudiantes no deben usar ninguna herramienta digital.', 0),
        (N'La alarma que organiza el día', 10, 3, N'Las tareas solo se pueden recordar con alarmas.', 0),
        (N'La alarma que organiza el día', 10, 4, N'Los cuadernos siempre son menos útiles que las aplicaciones.', 0),
        (N'El mapa del mercado del barrio', 1, 1, N'Un dibujo que muestra únicamente la ubicación de los puestos.', 0),
        (N'El mapa del mercado del barrio', 1, 2, N'Una forma de observar lugares, personas, problemas e historias del mercado.', 1),
        (N'El mapa del mercado del barrio', 1, 3, N'Una lista de precios de frutas y verduras.', 0),
        (N'El mapa del mercado del barrio', 1, 4, N'Una competencia para encontrar el puesto más grande.', 0),
        (N'El mapa del mercado del barrio', 2, 1, N'¿Qué puede enseñar un mercado sobre la comunidad que lo rodea?', 1),
        (N'El mapa del mercado del barrio', 2, 2, N'¿Cuántos metros mide cada pasillo del mercado?', 0),
        (N'El mapa del mercado del barrio', 2, 3, N'¿Qué estudiante compró más frutas durante la visita?', 0),
        (N'El mapa del mercado del barrio', 2, 4, N'¿Qué color tenían todos los puestos?', 0),
        (N'El mapa del mercado del barrio', 3, 1, N'Construir un mapa de historias observando lo que ocurría en el mercado.', 1),
        (N'El mapa del mercado del barrio', 3, 2, N'Comprar alimentos para preparar una comida en el colegio.', 0),
        (N'El mapa del mercado del barrio', 3, 3, N'Hacer una competencia de ventas entre estudiantes.', 0),
        (N'El mapa del mercado del barrio', 4, 1, N'Vendía frutas desde hacía quince años.', 1),
        (N'El mapa del mercado del barrio', 4, 2, N'Reparaba bicicletas cerca de la entrada.', 0),
        (N'El mapa del mercado del barrio', 4, 3, N'Enseñaba matemática a los vendedores.', 0),
        (N'El mapa del mercado del barrio', 5, 1, N'Bolsas tiradas, restos de comida mezclados con basura y poco espacio para caminar.', 1),
        (N'El mapa del mercado del barrio', 5, 2, N'Falta total de vendedores y ausencia de productos.', 0),
        (N'El mapa del mercado del barrio', 5, 3, N'Puestos cerrados durante toda la mañana.', 0),
        (N'El mapa del mercado del barrio', 6, 1, N'Que algunos alimentos golpeados o con partes dañadas todavía pueden aprovecharse.', 1),
        (N'El mapa del mercado del barrio', 6, 2, N'Que solo deben comprarse productos perfectos y brillantes.', 0),
        (N'El mapa del mercado del barrio', 6, 3, N'Que las verduras imperfectas deben botarse siempre.', 0),
        (N'El mapa del mercado del barrio', 6, 4, N'Que el mercado no necesita mejorar nada.', 0),
        (N'El mapa del mercado del barrio', 7, 1, N'Porque también era un espacio donde los vecinos conversaban, se ayudaban y compartían noticias.', 1),
        (N'El mapa del mercado del barrio', 7, 2, N'Porque nadie vendía productos en realidad.', 0),
        (N'El mapa del mercado del barrio', 7, 3, N'Porque los estudiantes fueron únicamente a jugar.', 0),
        (N'El mapa del mercado del barrio', 7, 4, N'Porque todos los puestos estaban vacíos.', 0),
        (N'El mapa del mercado del barrio', 8, 1, N'Un mercado puede mostrar problemas, relaciones y posibles soluciones de una comunidad.', 1),
        (N'El mapa del mercado del barrio', 8, 2, N'Un mercado solo sirve para comparar precios.', 0),
        (N'El mapa del mercado del barrio', 8, 3, N'Las visitas escolares no permiten aprender sobre la vida diaria.', 0),
        (N'El mapa del mercado del barrio', 8, 4, N'Los vendedores nunca colaboran con sus vecinos.', 0),
        (N'El mapa del mercado del barrio', 9, 1, N'Separar residuos y promover el uso de bolsas reutilizables.', 1),
        (N'El mapa del mercado del barrio', 9, 2, N'Prohibir que las personas compren frutas maduras.', 0),
        (N'El mapa del mercado del barrio', 9, 3, N'Mezclar todos los restos de comida con bolsas y papeles.', 0),
        (N'El mapa del mercado del barrio', 9, 4, N'Evitar que los estudiantes vuelvan a observar el mercado.', 0),
        (N'El mapa del mercado del barrio', 10, 1, N'Mirar el mercado solo como un lugar donde los precios suben o bajan.', 0),
        (N'El mapa del mercado del barrio', 10, 2, N'Observar el mercado como un espacio con problemas, vínculos y oportunidades de mejora.', 1),
        (N'El mapa del mercado del barrio', 10, 3, N'Ignorar los problemas porque no ocurren dentro del colegio.', 0),
        (N'El mapa del mercado del barrio', 10, 4, N'Pensar que los vendedores son los únicos responsables de todo lo que sucede.', 0),
        (N'La última página del cuaderno rojo', 1, 1, N'Un objeto antiguo que permite descubrir una experiencia importante del pasado.', 1),
        (N'La última página del cuaderno rojo', 1, 2, N'Un libro nuevo comprado por Camila para sus clases actuales.', 0),
        (N'La última página del cuaderno rojo', 1, 3, N'Un manual tecnológico usado para reparar computadoras.', 0),
        (N'La última página del cuaderno rojo', 1, 4, N'Un cuaderno perdido sin relación con la familia.', 0),
        (N'La última página del cuaderno rojo', 2, 1, N'¿Qué puede aprender Camila al conocer cómo estudiaba su abuelo?', 1),
        (N'La última página del cuaderno rojo', 2, 2, N'¿Cuántos colores de cuadernos tenía Julián en 1978?', 0),
        (N'La última página del cuaderno rojo', 2, 3, N'¿Qué marca de laptop usa Camila para hacer tareas?', 0),
        (N'La última página del cuaderno rojo', 2, 4, N'¿Dónde compró la familia la repisa de la sala?', 0),
        (N'La última página del cuaderno rojo', 3, 1, N'En una repisa antigua de la sala.', 1),
        (N'La última página del cuaderno rojo', 3, 2, N'En la biblioteca de su colegio.', 0),
        (N'La última página del cuaderno rojo', 3, 3, N'Debajo de una carpeta del aula.', 0),
        (N'La última página del cuaderno rojo', 3, 4, N'En una caja dentro de la tienda familiar.', 0),
        (N'La última página del cuaderno rojo', 4, 1, N'“Cuaderno de tareas. 1978” y el nombre Julián.', 1),
        (N'La última página del cuaderno rojo', 4, 2, N'“Libro de cuentos. 2025” y el nombre Camila.', 0),
        (N'La última página del cuaderno rojo', 4, 3, N'“Manual de computación” y una lista de programas.', 0),
        (N'La última página del cuaderno rojo', 4, 4, N'“Diario de vacaciones” y una dirección del colegio.', 0),
        (N'La última página del cuaderno rojo', 5, 1, N'Debía caminar casi una hora y compartir pocos libros de consulta.', 1),
        (N'La última página del cuaderno rojo', 5, 2, N'Tenía demasiadas computadoras y no sabía cuál usar.', 0),
        (N'La última página del cuaderno rojo', 5, 3, N'No quería asistir a la escuela porque le aburrían los recreos.', 0),
        (N'La última página del cuaderno rojo', 5, 4, N'Siempre encontraba todos los materiales listos en casa.', 0),
        (N'La última página del cuaderno rojo', 6, 1, N'Porque comparó sus quejas actuales con las dificultades reales que vivió Julián.', 1),
        (N'La última página del cuaderno rojo', 6, 2, N'Porque descubrió que su abuelo nunca había ido a la escuela.', 0),
        (N'La última página del cuaderno rojo', 6, 3, N'Porque encontró dibujos de animales en todas las páginas.', 0),
        (N'La última página del cuaderno rojo', 6, 4, N'Porque el cuaderno estaba completamente vacío.', 0),
        (N'La última página del cuaderno rojo', 7, 1, N'Que la tecnología puede apoyar el aprendizaje, pero el esfuerzo y el propósito siguen siendo personales.', 1),
        (N'La última página del cuaderno rojo', 7, 2, N'Que las computadoras son inútiles para cualquier tipo de tarea escolar.', 0),
        (N'La última página del cuaderno rojo', 7, 3, N'Que estudiar solo depende de tener muchos aparatos modernos.', 0),
        (N'La última página del cuaderno rojo', 7, 4, N'Que los cuadernos antiguos siempre son mejores que las herramientas digitales.', 0),
        (N'La última página del cuaderno rojo', 8, 1, N'Empieza preguntándose qué quiere entender y luego busca información con más cuidado.', 1),
        (N'La última página del cuaderno rojo', 8, 2, N'Decide copiar todo lo que encuentra sin comparar fuentes.', 0),
        (N'La última página del cuaderno rojo', 8, 3, N'Deja de usar cuadernos porque ya no los considera necesarios.', 0),
        (N'La última página del cuaderno rojo', 8, 4, N'Se niega a conversar nuevamente con su abuelo.', 0),
        (N'La última página del cuaderno rojo', 9, 1, N'Conocer las dificultades de otra generación puede ayudar a valorar mejor las oportunidades actuales.', 1),
        (N'La última página del cuaderno rojo', 9, 2, N'Las tareas escolares de antes eran siempre más fáciles que las actuales.', 0),
        (N'La última página del cuaderno rojo', 9, 3, N'La tecnología elimina por completo la necesidad de pensar y organizarse.', 0),
        (N'La última página del cuaderno rojo', 9, 4, N'Los objetos antiguos solo sirven para ocupar espacio en una repisa.', 0),
        (N'La última página del cuaderno rojo', 10, 1, N'Usar la laptop con un propósito claro y resumir con sus propias palabras.', 1),
        (N'La última página del cuaderno rojo', 10, 2, N'Quejarse del internet sin intentar organizar su búsqueda.', 0),
        (N'La última página del cuaderno rojo', 10, 3, N'Guardar el cuaderno sin leerlo ni preguntar nada.', 0),
        (N'La última página del cuaderno rojo', 10, 4, N'Copiar información de la primera página que encuentra.', 0),
        (N'La última página del cuaderno rojo', 11, 1, N'Tener más recursos puede ser una oportunidad, pero requiere responsabilidad para aprovecharlos.', 1),
        (N'La última página del cuaderno rojo', 11, 2, N'Las personas solo aprenden cuando tienen computadoras modernas.', 0),
        (N'La última página del cuaderno rojo', 11, 3, N'Las dificultades del pasado no tienen ninguna relación con la vida actual.', 0),
        (N'La última página del cuaderno rojo', 11, 4, N'El esfuerzo personal ya no importa si existe internet.', 0),
        (N'La ciudad que escuchaba a sus árboles', 1, 1, N'Que los árboles darán señales para comprender mejor algunos problemas de la ciudad.', 1),
        (N'La ciudad que escuchaba a sus árboles', 1, 2, N'Que los árboles hablarán literalmente con los estudiantes durante la clase.', 0),
        (N'La ciudad que escuchaba a sus árboles', 1, 3, N'Que la ciudad será abandonada porque no tiene edificios modernos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 1, 4, N'Que los estudiantes competirán para ver quién planta más árboles en un día.', 0),
        (N'La ciudad que escuchaba a sus árboles', 2, 1, N'¿Cómo pueden los datos sobre sombra, calor y ruido ayudar a mejorar una ciudad?', 1),
        (N'La ciudad que escuchaba a sus árboles', 2, 2, N'¿Qué estudiante tomó más fotografías durante el paseo?', 0),
        (N'La ciudad que escuchaba a sus árboles', 2, 3, N'¿Cuántos autos pasaron exactamente por cada avenida?', 0),
        (N'La ciudad que escuchaba a sus árboles', 2, 4, N'¿Qué marca tenían las tabletas usadas por el grupo?', 0),
        (N'La ciudad que escuchaba a sus árboles', 3, 1, N'“¿Todos los barrios de una ciudad sienten el calor de la misma manera?”', 1),
        (N'La ciudad que escuchaba a sus árboles', 3, 2, N'“¿Qué árbol crece más rápido en el patio del colegio?”', 0),
        (N'La ciudad que escuchaba a sus árboles', 3, 3, N'“¿Cuántos parques existen en todo el país?”', 0),
        (N'La ciudad que escuchaba a sus árboles', 3, 4, N'“¿Qué barrio tiene más tiendas cerca del colegio?”', 0),
        (N'La ciudad que escuchaba a sus árboles', 4, 1, N'Temperatura del suelo, nivel de ruido y cantidad de sombra.', 1),
        (N'La ciudad que escuchaba a sus árboles', 4, 2, N'Altura exacta de cada edificio, precio de viviendas y número de tiendas.', 0),
        (N'La ciudad que escuchaba a sus árboles', 4, 3, N'Cantidad de estudiantes por aula, notas y horarios escolares.', 0),
        (N'La ciudad que escuchaba a sus árboles', 4, 4, N'Color de las fachadas, tamaño de puertas y nombres de calles.', 0),
        (N'La ciudad que escuchaba a sus árboles', 5, 1, N'Pistas amplias, veredas angostas y casi ningún árbol.', 1),
        (N'La ciudad que escuchaba a sus árboles', 5, 2, N'Calles con árboles grandes y suelo menos caliente.', 0),
        (N'La ciudad que escuchaba a sus árboles', 5, 3, N'Muchas áreas verdes y poco ruido de autos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 5, 4, N'Un parque nuevo con bancas y fuentes de agua.', 0),
        (N'La ciudad que escuchaba a sus árboles', 6, 1, N'Porque los datos mostraron que ayudaban a reducir el calor, dar sombra y mejorar la experiencia al caminar.', 1),
        (N'La ciudad que escuchaba a sus árboles', 6, 2, N'Porque descubrieron que todos los árboles tenían la misma altura y el mismo color.', 0),
        (N'La ciudad que escuchaba a sus árboles', 6, 3, N'Porque las fotografías salieron mejor cuando aparecían árboles.', 0),
        (N'La ciudad que escuchaba a sus árboles', 6, 4, N'Porque la profesora les pidió escribir una frase bonita sobre la naturaleza.', 0),
        (N'La ciudad que escuchaba a sus árboles', 7, 1, N'Que algunas decisiones urbanas pueden reducir espacios útiles para el descanso y la sombra.', 1),
        (N'La ciudad que escuchaba a sus árboles', 7, 2, N'Que los estacionamientos siempre mejoran la vida de todos los vecinos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 7, 3, N'Que los parques no tienen ninguna función en una ciudad.', 0),
        (N'La ciudad que escuchaba a sus árboles', 7, 4, N'Que las personas prefieren caminar bajo el sol cuando esperan transporte.', 0),
        (N'La ciudad que escuchaba a sus árboles', 8, 1, N'Estudiar primero dónde hacían más falta los árboles antes de sembrarlos.', 1),
        (N'La ciudad que escuchaba a sus árboles', 8, 2, N'Cortar todos los árboles para ampliar las pistas.', 0),
        (N'La ciudad que escuchaba a sus árboles', 8, 3, N'Colocar árboles solo en los lugares donde ya había suficiente sombra.', 0),
        (N'La ciudad que escuchaba a sus árboles', 8, 4, N'Reemplazar todas las veredas por estacionamientos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 9, 1, N'Observar datos del entorno puede ayudar a tomar mejores decisiones para la comunidad.', 1),
        (N'La ciudad que escuchaba a sus árboles', 9, 2, N'Las ciudades no necesitan áreas verdes si tienen edificios altos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 9, 3, N'Los estudiantes solo deben estudiar problemas dentro del aula.', 0),
        (N'La ciudad que escuchaba a sus árboles', 9, 4, N'Las decisiones urbanas deben tomarse sin considerar a los vecinos.', 0),
        (N'La ciudad que escuchaba a sus árboles', 10, 1, N'Usar los datos para identificar zonas con menos sombra y priorizar allí nuevas áreas verdes.', 1),
        (N'La ciudad que escuchaba a sus árboles', 10, 2, N'Sembrar árboles al azar sin revisar el espacio ni las necesidades de cada zona.', 0),
        (N'La ciudad que escuchaba a sus árboles', 10, 3, N'Quitar veredas para que entren más autos cerca del colegio.', 0),
        (N'La ciudad que escuchaba a sus árboles', 10, 4, N'Ignorar las mediciones porque los estudiantes aún están aprendiendo.', 0),
        (N'La ciudad que escuchaba a sus árboles', 11, 1, N'Aprender a interpretar señales del entorno para comprender necesidades de la ciudad.', 1),
        (N'La ciudad que escuchaba a sus árboles', 11, 2, N'Esperar que los árboles expliquen con palabras qué problema tiene cada calle.', 0),
        (N'La ciudad que escuchaba a sus árboles', 11, 3, N'Pensar que solo los árboles importan y que las personas no deben opinar.', 0),
        (N'La ciudad que escuchaba a sus árboles', 11, 4, N'Evitar cualquier cambio urbano porque la ciudad ya está completa.', 0),
        (N'El algoritmo de las tareas', 1, 1, N'Un sistema que usa datos para recomendar tareas según el aprendizaje de los estudiantes.', 1),
        (N'El algoritmo de las tareas', 1, 2, N'Un estudiante que inventa una tarea para evitar ir al colegio.', 0),
        (N'El algoritmo de las tareas', 1, 3, N'Una competencia para resolver ejercicios en menos tiempo que otros salones.', 0),
        (N'El algoritmo de las tareas', 1, 4, N'Un robot que reemplaza completamente a todos los docentes.', 0),
        (N'El algoritmo de las tareas', 2, 1, N'¿Cómo puede un sistema inteligente ayudar a practicar sin reemplazar el criterio humano?', 1),
        (N'El algoritmo de las tareas', 2, 2, N'¿Cuántas computadoras tiene exactamente la escuela?', 0),
        (N'El algoritmo de las tareas', 2, 3, N'¿Qué estudiante terminó primero todas las actividades?', 0),
        (N'El algoritmo de las tareas', 2, 4, N'¿Qué marca de pantalla usaba la profesora durante la tutoría?', 0),
        (N'El algoritmo de las tareas', 3, 1, N'Sugerir actividades según los avances y dificultades de cada estudiante.', 1),
        (N'El algoritmo de las tareas', 3, 2, N'Eliminar todas las tareas para que nadie practicara.', 0),
        (N'El algoritmo de las tareas', 3, 3, N'Cambiar automáticamente las notas finales del curso.', 0),
        (N'El algoritmo de las tareas', 3, 4, N'Elegir al mejor estudiante para dirigir la clase.', 0),
        (N'El algoritmo de las tareas', 4, 1, N'Ejercicios para identificar datos explícitos.', 1),
        (N'El algoritmo de las tareas', 4, 2, N'Actividades para decorar el aula.', 0),
        (N'El algoritmo de las tareas', 4, 3, N'Textos sin preguntas ni instrucciones.', 0),
        (N'El algoritmo de las tareas', 4, 4, N'Problemas de matemática avanzada.', 0),
        (N'El algoritmo de las tareas', 5, 1, N'Recibió lecturas más cortas y empezó a responder con mayor seguridad.', 1),
        (N'El algoritmo de las tareas', 5, 2, N'Dejó de asistir a clases porque no quería usar tecnología.', 0),
        (N'El algoritmo de las tareas', 5, 3, N'Recibió textos más difíciles desde el primer día y se negó a leerlos.', 0),
        (N'El algoritmo de las tareas', 5, 4, N'Fue elegida para corregir las respuestas de todo el salón.', 0),
        (N'El algoritmo de las tareas', 6, 1, N'Porque le sugirió textos más difíciles aunque él sentía que aún no comprendía bien la lectura anterior.', 1),
        (N'El algoritmo de las tareas', 6, 2, N'Porque el sistema le quitó todas las actividades de lectura.', 0),
        (N'El algoritmo de las tareas', 6, 3, N'Porque la profesora le pidió que dejara de participar en clase.', 0),
        (N'El algoritmo de las tareas', 6, 4, N'Porque recibió exactamente las mismas tareas que Paula.', 0),
        (N'El algoritmo de las tareas', 7, 1, N'Que una recomendación basada solo en respuestas correctas podía ignorar factores como cansancio, nervios o falta de tiempo.', 1),
        (N'El algoritmo de las tareas', 7, 2, N'Que el sistema no podía encenderse porque no había electricidad en la escuela.', 0),
        (N'El algoritmo de las tareas', 7, 3, N'Que todos los estudiantes siempre necesitaban exactamente las mismas tareas.', 0),
        (N'El algoritmo de las tareas', 7, 4, N'Que los algoritmos solo sirven para juegos y no pueden mostrar ningún patrón.', 0),
        (N'El algoritmo de las tareas', 8, 1, N'Que podía apoyar con patrones de aprendizaje, pero no comprender por completo todo lo que vive un estudiante.', 1),
        (N'El algoritmo de las tareas', 8, 2, N'Que el sistema debía decidir solo todas las actividades sin intervención humana.', 0),
        (N'El algoritmo de las tareas', 8, 3, N'Que los estudiantes no debían revisar nunca sus errores.', 0),
        (N'El algoritmo de las tareas', 8, 4, N'Que las recomendaciones tecnológicas siempre son incorrectas.', 0),
        (N'El algoritmo de las tareas', 9, 1, N'La tecnología puede orientar la práctica, pero el aprendizaje también necesita reflexión, revisión y acompañamiento.', 1),
        (N'El algoritmo de las tareas', 9, 2, N'Los sistemas inteligentes siempre entienden mejor que los docentes y estudiantes.', 0),
        (N'El algoritmo de las tareas', 9, 3, N'La mejor forma de aprender es aceptar cualquier recomendación sin pensar.', 0),
        (N'El algoritmo de las tareas', 9, 4, N'Las tareas adaptativas solo sirven para que los estudiantes reciban menos trabajo.', 0),
        (N'El algoritmo de las tareas', 10, 1, N'Revisar sus respuestas, reconocer que leyó rápido y repetir una actividad intermedia antes de avanzar.', 1),
        (N'El algoritmo de las tareas', 10, 2, N'Ignorar sus errores porque el sistema debía resolverlos por él.', 0),
        (N'El algoritmo de las tareas', 10, 3, N'Abandonar todas las lecturas al recibir una actividad difícil.', 0),
        (N'El algoritmo de las tareas', 10, 4, N'Pedir que todos reciban exactamente las mismas tareas aunque tengan necesidades distintas.', 0),
        (N'El algoritmo de las tareas', 11, 1, N'Pueden ser útiles si apoyan la práctica personalizada, pero deben combinarse con criterio humano y participación del estudiante.', 1),
        (N'El algoritmo de las tareas', 11, 2, N'Deben reemplazar por completo a docentes, estudiantes y cualquier forma de diálogo.', 0),
        (N'El algoritmo de las tareas', 11, 3, N'Son inútiles porque nunca pueden reconocer ningún avance o dificultad.', 0),
        (N'El algoritmo de las tareas', 11, 4, N'Solo deben usarse para decidir quién merece estudiar y quién no.', 0),
        (N'El mensaje dentro de la botella', 1, 1, N'Un grupo encuentra una botella con un mensaje en su interior.', 1),
        (N'El mensaje dentro de la botella', 1, 2, N'Un estudiante compra una botella nueva para su lonchera.', 0),
        (N'El mensaje dentro de la botella', 1, 3, N'Una profesora enseña a fabricar botellas de vidrio.', 0),
        (N'El mensaje dentro de la botella', 2, 1, N'¿Qué decía el mensaje encontrado y por qué era importante?', 1),
        (N'El mensaje dentro de la botella', 2, 2, N'¿Cuántas botellas compraron los estudiantes ese día?', 0),
        (N'El mensaje dentro de la botella', 2, 3, N'¿De qué color eran todas las bancas del colegio?', 0),
        (N'El mensaje dentro de la botella', 3, 1, N'Para ayudar en la limpieza del patio.', 1),
        (N'El mensaje dentro de la botella', 3, 2, N'Para rendir un examen de matemática.', 0),
        (N'El mensaje dentro de la botella', 3, 3, N'Para participar en una competencia de canto.', 0),
        (N'El mensaje dentro de la botella', 4, 1, N'Una botella transparente con un papel enrollado dentro.', 1),
        (N'El mensaje dentro de la botella', 4, 2, N'Una mochila azul con una brújula.', 0),
        (N'El mensaje dentro de la botella', 4, 3, N'Un cuaderno rojo lleno de tareas.', 0),
        (N'El mensaje dentro de la botella', 5, 1, N'Deseos para mejorar el patio, como más sombra, menos basura y juegos pintados.', 1),
        (N'El mensaje dentro de la botella', 5, 2, N'Instrucciones para buscar monedas enterradas en el patio.', 0),
        (N'El mensaje dentro de la botella', 5, 3, N'Nombres de estudiantes que debían limpiar el salón.', 0),
        (N'El mensaje dentro de la botella', 6, 1, N'Porque querían que otros vieran las ideas y agregaran nuevas propuestas.', 1),
        (N'El mensaje dentro de la botella', 6, 2, N'Porque la botella se rompió antes de terminar la limpieza.', 0),
        (N'El mensaje dentro de la botella', 6, 3, N'Porque la profesora les pidió ocultar el mensaje otra vez.', 0),
        (N'El mensaje dentro de la botella', 7, 1, N'Los espacios compartidos pueden mejorar si las personas observan sus necesidades y proponen soluciones.', 1),
        (N'El mensaje dentro de la botella', 7, 2, N'Los mensajes antiguos siempre deben guardarse sin leerlos.', 0),
        (N'El mensaje dentro de la botella', 7, 3, N'La limpieza del colegio solo es responsabilidad de los adultos.', 0),
        (N'Cuando el recreo también enseña', 1, 1, N'Que el recreo puede ser un espacio donde también se aprende.', 1),
        (N'Cuando el recreo también enseña', 1, 2, N'Que el recreo debe eliminarse para estudiar más.', 0),
        (N'Cuando el recreo también enseña', 1, 3, N'Que todos los estudiantes prefieren jugar fútbol.', 0),
        (N'Cuando el recreo también enseña', 1, 4, N'Que las clases solo deben darse en el patio.', 0),
        (N'Cuando el recreo también enseña', 2, 1, N'¿Qué aprendizajes pueden ocurrir durante el recreo?', 1),
        (N'Cuando el recreo también enseña', 2, 2, N'¿Cuántos minutos exactos dura cada recreo?', 0),
        (N'Cuando el recreo también enseña', 2, 3, N'¿Qué estudiante compra más en el quiosco?', 0),
        (N'Cuando el recreo también enseña', 2, 4, N'¿Cuántas bancas tiene el patio escolar?', 0),
        (N'Cuando el recreo también enseña', 3, 1, N'Corren hacia la cancha, compran en el quiosco o conversan bajo la sombra.', 1),
        (N'Cuando el recreo también enseña', 3, 2, N'Permanecen siempre dentro del aula copiando tareas.', 0),
        (N'Cuando el recreo también enseña', 3, 3, N'Salen del colegio sin permiso todos los días.', 0),
        (N'Cuando el recreo también enseña', 4, 1, N'Conversar, turnarse o buscar otra actividad.', 1),
        (N'Cuando el recreo también enseña', 4, 2, N'Esconder la pelota para que nadie la use.', 0),
        (N'Cuando el recreo también enseña', 4, 3, N'Pedir que se suspenda el recreo.', 0),
        (N'Cuando el recreo también enseña', 5, 1, N'Responsabilidad por un espacio compartido.', 1),
        (N'Cuando el recreo también enseña', 5, 2, N'Falta de interés por el recreo.', 0),
        (N'Cuando el recreo también enseña', 5, 3, N'Deseo de terminar antes las clases.', 0),
        (N'Cuando el recreo también enseña', 6, 1, N'Porque toman decisiones sin que un profesor indique cada paso.', 1),
        (N'Cuando el recreo también enseña', 6, 2, N'Porque nadie debe respetar reglas durante el descanso.', 0),
        (N'Cuando el recreo también enseña', 6, 3, N'Porque pueden hacer cualquier cosa sin pensar en los demás.', 0),
        (N'Cuando el recreo también enseña', 6, 4, N'Porque el recreo reemplaza todas las clases.', 0),
        (N'Cuando el recreo también enseña', 7, 1, N'Que está contribuyendo a una convivencia más inclusiva.', 1),
        (N'Cuando el recreo también enseña', 7, 2, N'Que quiere terminar el recreo más rápido.', 0),
        (N'Cuando el recreo también enseña', 7, 3, N'Que busca evitar cualquier tipo de juego.', 0),
        (N'Cuando el recreo también enseña', 7, 4, N'Que no comprende las reglas del colegio.', 0),
        (N'Cuando el recreo también enseña', 8, 1, N'El recreo puede enseñar convivencia, respeto, autonomía y responsabilidad.', 1),
        (N'Cuando el recreo también enseña', 8, 2, N'El recreo solo sirve para dejar de aprender durante unos minutos.', 0),
        (N'Cuando el recreo también enseña', 8, 3, N'Los estudiantes no necesitan acuerdos cuando juegan.', 0),
        (N'Cuando el recreo también enseña', 8, 4, N'Las escuelas deben convertir todos los recreos en clases formales.', 0),
        (N'Cuando el recreo también enseña', 9, 1, N'Observar qué necesitan los estudiantes y organizar mejor los espacios.', 1),
        (N'Cuando el recreo también enseña', 9, 2, N'Prohibir toda conversación durante el descanso.', 0),
        (N'Cuando el recreo también enseña', 9, 3, N'Usar el patio solo para castigos.', 0),
        (N'Cuando el recreo también enseña', 9, 4, N'Permitir empujones si ocurren durante un juego.', 0),
        (N'Cuando el recreo también enseña', 10, 1, N'La escuela también enseña cuando los estudiantes conviven y toman decisiones responsables.', 1),
        (N'Cuando el recreo también enseña', 10, 2, N'Solo se aprende cuando el profesor escribe en la pizarra.', 0),
        (N'Cuando el recreo también enseña', 10, 3, N'Los recreos deben ser espacios sin acuerdos ni cuidado.', 0),
        (N'Cuando el recreo también enseña', 10, 4, N'Compartir o esperar turno no tiene relación con aprender.', 0),
        (N'La biblioteca que nadie visitaba', 1, 1, N'Una biblioteca escolar que existe, pero casi no es usada por los estudiantes.', 1),
        (N'La biblioteca que nadie visitaba', 1, 2, N'Una biblioteca que desaparece porque se pierden todos sus libros.', 0),
        (N'La biblioteca que nadie visitaba', 1, 3, N'Un grupo que decide reemplazar todos los libros por exámenes.', 0),
        (N'La biblioteca que nadie visitaba', 1, 4, N'Una competencia para construir la biblioteca más grande del país.', 0),
        (N'La biblioteca que nadie visitaba', 2, 1, N'¿Por qué los estudiantes no visitaban la biblioteca y qué podía hacerse para acercarlos a ella?', 1),
        (N'La biblioteca que nadie visitaba', 2, 2, N'¿Cuántas mesas exactas tenía la biblioteca del colegio?', 0),
        (N'La biblioteca que nadie visitaba', 2, 3, N'¿Qué estudiante llegó primero al laboratorio antiguo?', 0),
        (N'La biblioteca que nadie visitaba', 2, 4, N'¿Cuánto costaba cada libro de los estantes?', 0),
        (N'La biblioteca que nadie visitaba', 3, 1, N'Al final de un pasillo largo, junto al laboratorio antiguo.', 1),
        (N'La biblioteca que nadie visitaba', 3, 2, N'En medio del patio principal, al lado de la cancha.', 0),
        (N'La biblioteca que nadie visitaba', 3, 3, N'Dentro del quiosco escolar.', 0),
        (N'La biblioteca que nadie visitaba', 3, 4, N'En una sala compartida con la dirección.', 0),
        (N'La biblioteca que nadie visitaba', 4, 1, N'“Silencio”.', 1),
        (N'La biblioteca que nadie visitaba', 4, 2, N'“Prohibido leer”.', 0),
        (N'La biblioteca que nadie visitaba', 4, 3, N'“Solo docentes”.', 0),
        (N'La biblioteca que nadie visitaba', 4, 4, N'“Sala de juegos”.', 0),
        (N'La biblioteca que nadie visitaba', 5, 1, N'Propusieron hacer una encuesta.', 1),
        (N'La biblioteca que nadie visitaba', 5, 2, N'Cerraron la biblioteca durante una semana.', 0),
        (N'La biblioteca que nadie visitaba', 5, 3, N'Escondieron los libros más antiguos.', 0),
        (N'La biblioteca que nadie visitaba', 5, 4, N'Obligaron a todos a leer el mismo texto.', 0),
        (N'La biblioteca que nadie visitaba', 6, 1, N'Que muchos estudiantes no rechazaban la lectura, pero necesitaban formas más cercanas de acceder a los libros.', 1),
        (N'La biblioteca que nadie visitaba', 6, 2, N'Que todos odiaban leer y no había ninguna solución posible.', 0),
        (N'La biblioteca que nadie visitaba', 6, 3, N'Que la biblioteca estaba vacía porque no tenía mesas ni estantes.', 0),
        (N'La biblioteca que nadie visitaba', 6, 4, N'Que los estudiantes solo querían usar la biblioteca para comer durante el recreo.', 0),
        (N'La biblioteca que nadie visitaba', 7, 1, N'Porque parecía un espacio donde solo se podía leer en silencio y sin participar de otras formas.', 1),
        (N'La biblioteca que nadie visitaba', 7, 2, N'Porque estaba llena de juegos ruidosos durante todos los recreos.', 0),
        (N'La biblioteca que nadie visitaba', 7, 3, N'Porque la profesora prohibía entrar a quienes no tenían libros propios.', 0),
        (N'La biblioteca que nadie visitaba', 7, 4, N'Porque no existía ningún cartel ni norma dentro del lugar.', 0),
        (N'La biblioteca que nadie visitaba', 8, 1, N'Que la biblioteca podía mantenerse organizada y, al mismo tiempo, permitir mayor participación estudiantil.', 1),
        (N'La biblioteca que nadie visitaba', 8, 2, N'Que para participar era necesario desordenar todos los libros.', 0),
        (N'La biblioteca que nadie visitaba', 8, 3, N'Que una biblioteca solo funciona si nadie habla ni recomienda lecturas.', 0),
        (N'La biblioteca que nadie visitaba', 8, 4, N'Que el orden impedía cualquier actividad relacionada con la lectura.', 0),
        (N'La biblioteca que nadie visitaba', 9, 1, N'Pasó de ser un espacio poco visitado a un lugar más cercano, con recomendaciones, secciones y participación.', 1),
        (N'La biblioteca que nadie visitaba', 9, 2, N'Dejó de ser una biblioteca para convertirse en una cancha deportiva.', 0),
        (N'La biblioteca que nadie visitaba', 9, 3, N'Se cerró porque los estudiantes no querían entrar.', 0),
        (N'La biblioteca que nadie visitaba', 9, 4, N'Eliminó los libros para que todos buscaran información solo en internet.', 0),
        (N'La biblioteca que nadie visitaba', 10, 1, N'Crear espacios de recomendación, lectura breve y conversación sin obligar a todos a leer lo mismo.', 1),
        (N'La biblioteca que nadie visitaba', 10, 2, N'Mantener la biblioteca igual aunque nadie la visite.', 0),
        (N'La biblioteca que nadie visitaba', 10, 3, N'Castigar a quienes no entren a leer durante el recreo.', 0),
        (N'La biblioteca que nadie visitaba', 10, 4, N'Quitar las etiquetas de los estantes para que nadie sepa dónde buscar.', 0),
        (N'La biblioteca que nadie visitaba', 11, 1, N'Para acercar a los estudiantes a la lectura, no basta tener libros; también importa cómo se presenta el espacio y se invita a participar.', 1),
        (N'La biblioteca que nadie visitaba', 11, 2, N'Una biblioteca debe estar siempre vacía para conservar el silencio.', 0),
        (N'La biblioteca que nadie visitaba', 11, 3, N'Los estudiantes solo leen cuando se les obliga con una nota.', 0),
        (N'La biblioteca que nadie visitaba', 11, 4, N'Internet vuelve inútiles todos los espacios de lectura escolar.', 0);

    -----------------------------------------------------------------------
    -- 3) Insertar o actualizar las 12 lecturas y sus ReadingPractice
    -----------------------------------------------------------------------
    DECLARE @ReadingNo INT;
    DECLARE @ReadingTitle NVARCHAR(150);
    DECLARE @DifficultyName NVARCHAR(20);
    DECLARE @TextType NVARCHAR(80);
    DECLARE @Summary NVARCHAR(300);
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @DifficultyLevelId TINYINT;
    DECLARE @ReadingId INT;
    DECLARE @AssessmentId INT;

    DECLARE reading_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ReadingNo, Title, DifficultyName, TextType, Summary, Content
        FROM @ReadingSeed
        ORDER BY ReadingNo;

    OPEN reading_cursor;
    FETCH NEXT FROM reading_cursor INTO @ReadingNo, @ReadingTitle, @DifficultyName, @TextType, @Summary, @Content;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @DifficultyLevelId = CASE @DifficultyName
            WHEN N'Básico' THEN @DifficultyBasicoId
            WHEN N'Intermedio' THEN @DifficultyIntermedioId
            WHEN N'Avanzado' THEN @DifficultyAvanzadoId
        END;

        IF NOT EXISTS (SELECT 1 FROM Readings WHERE Title = @ReadingTitle)
        BEGIN
            INSERT INTO Readings (Title, Summary, Content, ImageUrl, DifficultyLevelId, EstimatedMinutes, IsActive, CreatedByUserId)
            VALUES (@ReadingTitle, @Summary, @Content, NULL, @DifficultyLevelId, NULL, 1, @CreatedByUserId);
        END
        ELSE
        BEGIN
            UPDATE Readings
            SET Summary = @Summary,
                Content = @Content,
                DifficultyLevelId = @DifficultyLevelId,
                IsActive = 1
            WHERE Title = @ReadingTitle;
        END;

        SELECT @ReadingId = ReadingId FROM Readings WHERE Title = @ReadingTitle;

        IF NOT EXISTS (SELECT 1 FROM Assessments WHERE AssessmentType = 'ReadingPractice' AND ReadingId = @ReadingId)
        BEGIN
            INSERT INTO Assessments (AssessmentType, ReadingId, Title, Description, DifficultyLevelId, IsActive)
            VALUES ('ReadingPractice', @ReadingId, @ReadingTitle, CONCAT(N'Práctica PQ4R. Tipo de texto: ', @TextType), @DifficultyLevelId, 1);
        END
        ELSE
        BEGIN
            UPDATE Assessments
            SET Title = @ReadingTitle,
                Description = CONCAT(N'Práctica PQ4R. Tipo de texto: ', @TextType),
                DifficultyLevelId = @DifficultyLevelId,
                IsActive = 1
            WHERE AssessmentType = 'ReadingPractice'
              AND ReadingId = @ReadingId;
        END;

        FETCH NEXT FROM reading_cursor INTO @ReadingNo, @ReadingTitle, @DifficultyName, @TextType, @Summary, @Content;
    END;

    CLOSE reading_cursor;
    DEALLOCATE reading_cursor;

    -----------------------------------------------------------------------
    -- 4) Fases PQ4R por lectura
    -----------------------------------------------------------------------
    INSERT INTO ReadingPhases (ReadingId, PhaseId, DisplayOrder, IsEnabled, IsRequired, GuidanceText, MinQuestionsToUnlockNext)
    SELECT r.ReadingId, p.PhaseId, seed.DisplayOrder, 1, 1, seed.GuidanceText, seed.MinQuestionsToUnlockNext
    FROM @ReadingPhaseSeed seed
    INNER JOIN Readings r ON r.Title = seed.ReadingTitle
    INNER JOIN Phases p ON p.Code = seed.PhaseCode
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM ReadingPhases rp
        WHERE rp.ReadingId = r.ReadingId
          AND rp.PhaseId = p.PhaseId
    );

    UPDATE rp
    SET rp.DisplayOrder = seed.DisplayOrder,
        rp.IsEnabled = 1,
        rp.IsRequired = 1,
        rp.GuidanceText = seed.GuidanceText,
        rp.MinQuestionsToUnlockNext = seed.MinQuestionsToUnlockNext
    FROM ReadingPhases rp
    INNER JOIN Readings r ON r.ReadingId = rp.ReadingId
    INNER JOIN Phases p ON p.PhaseId = rp.PhaseId
    INNER JOIN @ReadingPhaseSeed seed
        ON seed.ReadingTitle = r.Title
       AND seed.PhaseCode = p.Code;

    -----------------------------------------------------------------------
    -- 5) Preguntas, opciones y asociaciones AssessmentQuestions
    -----------------------------------------------------------------------
    DECLARE @QuestionOrder TINYINT;
    DECLARE @PhaseCode VARCHAR(20);
    DECLARE @DimensionName NVARCHAR(30);
    DECLARE @Stem NVARCHAR(500);
    DECLARE @Points DECIMAL(5, 2);
    DECLARE @DimensionId TINYINT;
    DECLARE @PhaseId TINYINT;
    DECLARE @QuestionId INT;
    DECLARE @SharedQuestionUseCount INT;

    DECLARE question_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT q.ReadingTitle, q.QuestionOrder, q.PhaseCode, q.DimensionName, q.Stem, q.Points
        FROM @QuestionSeed q
        INNER JOIN @ReadingSeed r ON r.Title = q.ReadingTitle
        ORDER BY r.ReadingNo, q.QuestionOrder;

    OPEN question_cursor;
    FETCH NEXT FROM question_cursor INTO @ReadingTitle, @QuestionOrder, @PhaseCode, @DimensionName, @Stem, @Points;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @DimensionId = CASE @DimensionName
            WHEN N'Literal' THEN @DimensionLiteralId
            WHEN N'Inferencial' THEN @DimensionInferencialId
            WHEN N'Crítica-Evaluativa' THEN @DimensionCriticaId
        END;

        SET @PhaseId = CASE @PhaseCode
            WHEN 'Preview' THEN @PhasePreviewId
            WHEN 'Question' THEN @PhaseQuestionId
            WHEN 'Read' THEN @PhaseReadId
            WHEN 'Reflect' THEN @PhaseReflectId
            WHEN 'Recite' THEN @PhaseReciteId
            WHEN 'Review' THEN @PhaseReviewId
        END;

        SELECT @ReadingId = ReadingId FROM Readings WHERE Title = @ReadingTitle;
        SELECT @AssessmentId = AssessmentId FROM Assessments WHERE AssessmentType = 'ReadingPractice' AND ReadingId = @ReadingId;
        SELECT @DifficultyLevelId = DifficultyLevelId FROM Readings WHERE ReadingId = @ReadingId;
        SET @QuestionId = NULL;
        SET @SharedQuestionUseCount = 0;

        SELECT TOP (1)
            @QuestionId = aq.QuestionId
        FROM AssessmentQuestions aq
        WHERE aq.AssessmentId = @AssessmentId
          AND aq.DisplayOrder = @QuestionOrder;

        IF @QuestionId IS NOT NULL
        BEGIN
            SELECT
                @SharedQuestionUseCount = COUNT(DISTINCT aq.AssessmentId)
            FROM AssessmentQuestions aq
            WHERE aq.QuestionId = @QuestionId;
        END;

        IF @QuestionId IS NOT NULL AND @SharedQuestionUseCount > 1
        BEGIN
            INSERT INTO Questions (DimensionId, Stem, QuestionType, Explanation, DifficultyLevelId, IsActive)
            VALUES (@DimensionId, @Stem, 'MultipleChoice', NULL, @DifficultyLevelId, 1);

            SET @QuestionId = CONVERT(INT, SCOPE_IDENTITY());

            UPDATE AssessmentQuestions
            SET QuestionId = @QuestionId
            WHERE AssessmentId = @AssessmentId
              AND DisplayOrder = @QuestionOrder;
        END;

        IF @QuestionId IS NULL
        BEGIN
            INSERT INTO Questions (DimensionId, Stem, QuestionType, Explanation, DifficultyLevelId, IsActive)
            VALUES (@DimensionId, @Stem, 'MultipleChoice', NULL, @DifficultyLevelId, 1);

            SET @QuestionId = CONVERT(INT, SCOPE_IDENTITY());

            INSERT INTO AssessmentQuestions (AssessmentId, QuestionId, PhaseId, DisplayOrder, Points, IsActive)
            VALUES (@AssessmentId, @QuestionId, @PhaseId, @QuestionOrder, @Points, 1);
        END
        ELSE
        BEGIN
            UPDATE Questions
            SET DimensionId = @DimensionId,
                Stem = @Stem,
                QuestionType = 'MultipleChoice',
                DifficultyLevelId = @DifficultyLevelId,
                IsActive = 1
            WHERE QuestionId = @QuestionId;
        END;

        UPDATE QuestionOptions
        SET IsCorrect = 0
        WHERE QuestionId = @QuestionId;

        INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect, DisplayOrder)
        SELECT @QuestionId, opt.OptionText, opt.IsCorrect, opt.DisplayOrder
        FROM @OptionSeed opt
        WHERE opt.ReadingTitle = @ReadingTitle
          AND opt.QuestionOrder = @QuestionOrder
          AND NOT EXISTS
          (
              SELECT 1
              FROM QuestionOptions qo
              WHERE qo.QuestionId = @QuestionId
                AND qo.DisplayOrder = opt.DisplayOrder
          );

        UPDATE qo
        SET qo.OptionText = opt.OptionText,
            qo.IsCorrect = opt.IsCorrect
        FROM QuestionOptions qo
        INNER JOIN @OptionSeed opt
            ON opt.ReadingTitle = @ReadingTitle
           AND opt.QuestionOrder = @QuestionOrder
           AND opt.DisplayOrder = qo.DisplayOrder
        WHERE qo.QuestionId = @QuestionId;

        UPDATE AssessmentQuestions
        SET PhaseId = @PhaseId,
            QuestionId = @QuestionId,
            Points = @Points,
            IsActive = 1
        WHERE AssessmentId = @AssessmentId
          AND DisplayOrder = @QuestionOrder;

        FETCH NEXT FROM question_cursor INTO @ReadingTitle, @QuestionOrder, @PhaseCode, @DimensionName, @Stem, @Points;
    END;

    CLOSE question_cursor;
    DEALLOCATE question_cursor;

    -----------------------------------------------------------------------
    -- 6) Validación final esperada
    --    Esperado: 12 lecturas, 12 ReadingPractice, 115 preguntas, 410 opciones.
    -----------------------------------------------------------------------
    SELECT
        ExpectedReadings = 12,
        LoadedReadings = COUNT(DISTINCT r.ReadingId),
        ExpectedReadingPracticeAssessments = 12,
        LoadedReadingPracticeAssessments = COUNT(DISTINCT a.AssessmentId),
        ExpectedQuestions = 115,
        LoadedQuestions = COUNT(DISTINCT aq.QuestionId),
        ExpectedOptions = 410,
        LoadedOptions = COUNT(qo.OptionId)
    FROM @ReadingSeed seed
    LEFT JOIN Readings r ON r.Title = seed.Title
    LEFT JOIN Assessments a ON a.ReadingId = r.ReadingId AND a.AssessmentType = 'ReadingPractice'
    LEFT JOIN AssessmentQuestions aq ON aq.AssessmentId = a.AssessmentId
    LEFT JOIN QuestionOptions qo ON qo.QuestionId = aq.QuestionId;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'question_cursor') >= 0
    BEGIN
        CLOSE question_cursor;
    END;

    IF CURSOR_STATUS('local', 'question_cursor') > -3
    BEGIN
        DEALLOCATE question_cursor;
    END;

    IF CURSOR_STATUS('local', 'reading_cursor') >= 0
    BEGIN
        CLOSE reading_cursor;
    END;

    IF CURSOR_STATUS('local', 'reading_cursor') > -3
    BEGIN
        DEALLOCATE reading_cursor;
    END;

    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
