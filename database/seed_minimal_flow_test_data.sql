SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -----------------------------------------------------------------------
    -- 0) Roles base
    -----------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Student')
    BEGIN
        INSERT INTO Roles (Name, IsActive)
        VALUES ('Student', 1);
    END
    ELSE
    BEGIN
        UPDATE Roles
        SET IsActive = 1
        WHERE Name = 'Student' AND IsActive = 0;
    END;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Teacher')
    BEGIN
        INSERT INTO Roles (Name, IsActive)
        VALUES ('Teacher', 1);
    END
    ELSE
    BEGIN
        UPDATE Roles
        SET IsActive = 1
        WHERE Name = 'Teacher' AND IsActive = 0;
    END;

    DECLARE @StudentRoleId SMALLINT =
    (
        SELECT TOP (1) RoleId
        FROM Roles
        WHERE Name = 'Student'
    );

    DECLARE @TeacherRoleId SMALLINT =
    (
        SELECT TOP (1) RoleId
        FROM Roles
        WHERE Name = 'Teacher'
    );

    -----------------------------------------------------------------------
    -- 1) Docente semilla para CreatedByUserId de lecturas
    --    Password: Teacher123!
    -----------------------------------------------------------------------
    DECLARE @SeedTeacherUsername VARCHAR(30) = 'seed.teacher';
    DECLARE @SeedTeacherPasswordHash VARCHAR(255) =
        'PBKDF2SHA256.100000.KAr/hsMEI+V3uC8Y5i4Vkw==.jhj1W9COvYrxuBLDKnIckYNzOnLDr1ycHajJC4GxV48=';

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
            VALUES (@TeacherRoleId, 'Seed Teacher', @SeedTeacherUsername, @SeedTeacherPasswordHash, 1);
        END;

        SELECT @CreatedByUserId = UserId
        FROM Users
        WHERE Username = @SeedTeacherUsername;

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
    -- 2) Catalogos base
    -----------------------------------------------------------------------
    IF NOT EXISTS
    (
        SELECT 1
        FROM DifficultyLevels
        WHERE Name = 'Basico'
           OR RankOrder = 1
    )
    BEGIN
        INSERT INTO DifficultyLevels (Name, RankOrder)
        VALUES ('Basico', 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM DifficultyLevels
        WHERE Name = 'Intermedio'
           OR RankOrder = 2
    )
    BEGIN
        INSERT INTO DifficultyLevels (Name, RankOrder)
        VALUES ('Intermedio', 2);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM DifficultyLevels
        WHERE Name = 'Avanzado'
           OR RankOrder = 3
    )
    BEGIN
        INSERT INTO DifficultyLevels (Name, RankOrder)
        VALUES ('Avanzado', 3);
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = 'Literal')
    BEGIN
        INSERT INTO Dimensions (Name, Description)
        VALUES ('Literal', 'Comprension literal de informacion explicita.');
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name = 'Inferencial')
    BEGIN
        INSERT INTO Dimensions (Name, Description)
        VALUES ('Inferencial', 'Comprension inferencial a partir de pistas del texto.');
    END;

    IF NOT EXISTS (SELECT 1 FROM Dimensions WHERE Name IN ('Critica-Evaluativa', 'Crítica-Evaluativa'))
    BEGIN
        INSERT INTO Dimensions (Name, Description)
        VALUES ('Critica-Evaluativa', 'Juicio critico y valoracion de ideas del texto.');
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Preview'
           OR DefaultOrder = 1
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Preview', 'Preview', 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Question'
           OR DefaultOrder = 2
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Question', 'Question', 2);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Read'
           OR DefaultOrder = 3
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Read', 'Read', 3);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Reflect'
           OR DefaultOrder = 4
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Reflect', 'Reflect', 4);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Recite'
           OR DefaultOrder = 5
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Recite', 'Recite', 5);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Phases
        WHERE Code = 'Review'
           OR DefaultOrder = 6
    )
    BEGIN
        INSERT INTO Phases (Code, DisplayName, DefaultOrder)
        VALUES ('Review', 'Review', 6);
    END;

    DECLARE @DifficultyBasicoId TINYINT =
    (
        SELECT TOP (1) DifficultyLevelId
        FROM DifficultyLevels
        WHERE Name = 'Basico'
           OR RankOrder = 1
        ORDER BY CASE WHEN Name = 'Basico' THEN 0 ELSE 1 END, DifficultyLevelId
    );

    DECLARE @DifficultyIntermedioId TINYINT =
    (
        SELECT TOP (1) DifficultyLevelId
        FROM DifficultyLevels
        WHERE Name = 'Intermedio'
           OR RankOrder = 2
        ORDER BY CASE WHEN Name = 'Intermedio' THEN 0 ELSE 1 END, DifficultyLevelId
    );

    DECLARE @DifficultyAvanzadoId TINYINT =
    (
        SELECT TOP (1) DifficultyLevelId
        FROM DifficultyLevels
        WHERE Name = 'Avanzado'
           OR RankOrder = 3
        ORDER BY CASE WHEN Name = 'Avanzado' THEN 0 ELSE 1 END, DifficultyLevelId
    );

    DECLARE @DimensionLiteralId TINYINT =
    (
        SELECT TOP (1) DimensionId
        FROM Dimensions
        WHERE Name = 'Literal'
    );

    DECLARE @DimensionInferencialId TINYINT =
    (
        SELECT TOP (1) DimensionId
        FROM Dimensions
        WHERE Name = 'Inferencial'
    );

    DECLARE @DimensionCriticaId TINYINT =
    (
        SELECT TOP (1) DimensionId
        FROM Dimensions
        WHERE Name IN ('Critica-Evaluativa', 'Crítica-Evaluativa')
        ORDER BY DimensionId
    );

    -----------------------------------------------------------------------
    -- 3) Lecturas activas
    -----------------------------------------------------------------------
    DECLARE @ReadingSeed TABLE
    (
        SeedCode VARCHAR(20) PRIMARY KEY,
        Title VARCHAR(150) NOT NULL,
        Summary VARCHAR(300) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        DifficultyLevelId TINYINT NOT NULL,
        EstimatedMinutes INT NOT NULL
    );

    INSERT INTO @ReadingSeed (SeedCode, Title, Summary, Content, DifficultyLevelId, EstimatedMinutes)
    VALUES
        (
            'R1',
            '[Seed Flow] Los ecos del bosque',
            'Lectura breve sobre una caminata en un bosque y el hallazgo de pistas antiguas.',
            N'Lucia y Mateo recorrieron el bosque al amanecer. Encontraron huellas, un mapa viejo y varias notas que explicaban como los vecinos cuidaban el lugar. Al final comprendieron que el trabajo en equipo les permitio interpretar mejor cada pista y tomar decisiones mas seguras.',
            @DifficultyBasicoId,
            8
        ),
        (
            'R2',
            '[Seed Flow] El puente antiguo',
            'Lectura sobre la restauracion comunitaria de un puente y las decisiones tomadas por el barrio.',
            N'El barrio organizo una jornada para restaurar un puente antiguo. Algunas personas querian cambiarlo por completo, pero otras defendieron la idea de conservar su historia. Tras revisar testimonios y materiales, la comunidad eligio una reparacion que mantuvo el valor cultural y mejoro la seguridad.',
            @DifficultyIntermedioId,
            10
        ),
        (
            'R3',
            '[Seed Flow] Innovacion en el aula',
            'Lectura sobre una experiencia escolar con proyectos colaborativos y uso reflexivo de tecnologia.',
            N'Una escuela implemento proyectos colaborativos apoyados por tecnologia. Los estudiantes investigaron problemas reales, compararon fuentes y presentaron propuestas para mejorar su entorno. Los docentes observaron que la participacion aumento cuando las tareas tenian un proposito claro y permitian reflexion constante.',
            @DifficultyAvanzadoId,
            12
        );

    INSERT INTO Readings (Title, Summary, Content, ImageUrl, DifficultyLevelId, EstimatedMinutes, IsActive, CreatedByUserId)
    SELECT
        seed.Title,
        seed.Summary,
        seed.Content,
        NULL,
        seed.DifficultyLevelId,
        seed.EstimatedMinutes,
        1,
        @CreatedByUserId
    FROM @ReadingSeed seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM Readings r
        WHERE r.Title = seed.Title
    );

    UPDATE r
    SET
        r.Summary = seed.Summary,
        r.Content = seed.Content,
        r.DifficultyLevelId = seed.DifficultyLevelId,
        r.EstimatedMinutes = seed.EstimatedMinutes,
        r.IsActive = 1
    FROM Readings r
    INNER JOIN @ReadingSeed seed ON seed.Title = r.Title;

    -----------------------------------------------------------------------
    -- 4) Fases PQ4R por lectura
    -----------------------------------------------------------------------
    DECLARE @ReadingPhaseSeed TABLE
    (
        ReadingTitle VARCHAR(150) NOT NULL,
        PhaseCode VARCHAR(20) NOT NULL,
        DisplayOrder TINYINT NOT NULL,
        GuidanceText VARCHAR(500) NOT NULL,
        MinQuestionsToUnlockNext TINYINT NOT NULL
    );

    INSERT INTO @ReadingPhaseSeed (ReadingTitle, PhaseCode, DisplayOrder, GuidanceText, MinQuestionsToUnlockNext)
    SELECT seed.Title, phaseSeed.PhaseCode, phaseSeed.DisplayOrder, phaseSeed.GuidanceText, 1
    FROM @ReadingSeed seed
    CROSS JOIN
    (
        VALUES
            ('Preview', 1, 'Observa titulos y pistas antes de leer.'),
            ('Question', 2, 'Formula preguntas sobre lo que esperas encontrar.'),
            ('Read', 3, 'Lee con atencion buscando ideas centrales y detalles.'),
            ('Reflect', 4, 'Relaciona lo leido con conocimientos previos y situaciones reales.'),
            ('Recite', 5, 'Explica con tus palabras lo aprendido.'),
            ('Review', 6, 'Revisa ideas clave y verifica tus respuestas.')
    ) phaseSeed (PhaseCode, DisplayOrder, GuidanceText);

    INSERT INTO ReadingPhases
    (
        ReadingId,
        PhaseId,
        DisplayOrder,
        IsEnabled,
        IsRequired,
        GuidanceText,
        MinQuestionsToUnlockNext
    )
    SELECT
        r.ReadingId,
        p.PhaseId,
        seed.DisplayOrder,
        1,
        1,
        seed.GuidanceText,
        seed.MinQuestionsToUnlockNext
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
    SET
        rp.DisplayOrder = seed.DisplayOrder,
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
    -- 5) Evaluaciones activas: pretest, posttest y reading practices
    -----------------------------------------------------------------------
    IF NOT EXISTS
    (
        SELECT 1
        FROM Assessments
        WHERE Title = '[Seed Flow] Pretest Diagnostico'
          AND AssessmentType = 'Pretest'
    )
    BEGIN
        INSERT INTO Assessments (AssessmentType, ReadingId, Title, Description, DifficultyLevelId, IsActive)
        VALUES
        (
            'Pretest',
            NULL,
            '[Seed Flow] Pretest Diagnostico',
            'Pretest activo para validar el inicio del flujo academico.',
            @DifficultyIntermedioId,
            1
        );
    END
    ELSE
    BEGIN
        UPDATE Assessments
        SET
            Description = 'Pretest activo para validar el inicio del flujo academico.',
            DifficultyLevelId = @DifficultyIntermedioId,
            IsActive = 1
        WHERE Title = '[Seed Flow] Pretest Diagnostico'
          AND AssessmentType = 'Pretest';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM Assessments
        WHERE Title = '[Seed Flow] Posttest Final'
          AND AssessmentType = 'Posttest'
    )
    BEGIN
        INSERT INTO Assessments (AssessmentType, ReadingId, Title, Description, DifficultyLevelId, IsActive)
        VALUES
        (
            'Posttest',
            NULL,
            '[Seed Flow] Posttest Final',
            'Posttest activo para validar el cierre del flujo academico.',
            @DifficultyIntermedioId,
            1
        );
    END
    ELSE
    BEGIN
        UPDATE Assessments
        SET
            Description = 'Posttest activo para validar el cierre del flujo academico.',
            DifficultyLevelId = @DifficultyIntermedioId,
            IsActive = 1
        WHERE Title = '[Seed Flow] Posttest Final'
          AND AssessmentType = 'Posttest';
    END;

    INSERT INTO Assessments (AssessmentType, ReadingId, Title, Description, DifficultyLevelId, IsActive)
    SELECT
        'ReadingPractice',
        r.ReadingId,
        r.Title + ' - ReadingPractice',
        'Evaluacion de practica asociada a la lectura semilla.',
        r.DifficultyLevelId,
        1
    FROM Readings r
    WHERE r.Title IN
    (
        '[Seed Flow] Los ecos del bosque',
        '[Seed Flow] El puente antiguo',
        '[Seed Flow] Innovacion en el aula'
    )
      AND NOT EXISTS
      (
          SELECT 1
          FROM Assessments a
          WHERE a.AssessmentType = 'ReadingPractice'
            AND a.ReadingId = r.ReadingId
      );

    UPDATE a
    SET
        a.Title = r.Title + ' - ReadingPractice',
        a.Description = 'Evaluacion de practica asociada a la lectura semilla.',
        a.DifficultyLevelId = r.DifficultyLevelId,
        a.IsActive = 1
    FROM Assessments a
    INNER JOIN Readings r ON r.ReadingId = a.ReadingId
    WHERE a.AssessmentType = 'ReadingPractice'
      AND r.Title IN
      (
          '[Seed Flow] Los ecos del bosque',
          '[Seed Flow] El puente antiguo',
          '[Seed Flow] Innovacion en el aula'
      );

    -----------------------------------------------------------------------
    -- 6) Banco minimo de preguntas y opciones
    -----------------------------------------------------------------------
    DECLARE @QuestionSeed TABLE
    (
        SeedCode VARCHAR(30) PRIMARY KEY,
        Stem VARCHAR(500) NOT NULL,
        DimensionId TINYINT NOT NULL,
        DifficultyLevelId TINYINT NULL,
        Explanation VARCHAR(500) NULL,
        CorrectOption VARCHAR(300) NOT NULL,
        WrongOption1 VARCHAR(300) NOT NULL,
        WrongOption2 VARCHAR(300) NOT NULL
    );

    INSERT INTO @QuestionSeed
    (
        SeedCode,
        Stem,
        DimensionId,
        DifficultyLevelId,
        Explanation,
        CorrectOption,
        WrongOption1,
        WrongOption2
    )
    VALUES
        ('PRE01', '[Seed Flow][Pretest] Segun el texto, quien encontro el mapa antiguo?', @DimensionLiteralId, @DifficultyBasicoId, 'Identifica un dato explicito.', 'Lucia', 'El abuelo', 'La maestra'),
        ('PRE02', '[Seed Flow][Pretest] Donde inicio la caminata del grupo?', @DimensionLiteralId, @DifficultyBasicoId, 'Recupera informacion textual.', 'En la plaza del pueblo', 'En el rio', 'En la biblioteca'),
        ('PRE03', '[Seed Flow][Pretest] Por que el personaje dudo antes de entrar al bosque?', @DimensionInferencialId, @DifficultyIntermedioId, 'Infiera la motivacion principal.', 'Porque penso que podia perderse', 'Porque estaba cansado', 'Porque el clima era perfecto'),
        ('PRE04', '[Seed Flow][Pretest] Que se puede inferir sobre el abuelo al final del relato?', @DimensionInferencialId, @DifficultyIntermedioId, 'Relaciona pistas y conclusion.', 'Que conocia la historia del lugar', 'Que nunca visito el bosque', 'Que olvido el mapa'),
        ('PRE05', '[Seed Flow][Pretest] Que accion habria fortalecido mas la decision del grupo?', @DimensionCriticaId, @DifficultyIntermedioId, 'Evalua la mejor accion posible.', 'Verificar la informacion antes de actuar', 'Ignorar las pistas', 'Seguir rumores sin revisar'),
        ('PRE06', '[Seed Flow][Pretest] Que opinion esta mejor sustentada por el texto?', @DimensionCriticaId, @DifficultyIntermedioId, 'Selecciona un juicio con evidencia.', 'La colaboracion mejoro el resultado', 'El azar resolvio todo', 'No hubo ningun aprendizaje'),

        ('POST01', '[Seed Flow][Posttest] Que evidencia muestra que la comunidad valoro su historia?', @DimensionLiteralId, @DifficultyIntermedioId, 'Busca la evidencia explicita central.', 'Decidio reparar el puente sin destruirlo', 'Vendio el puente', 'Cerco el acceso al barrio'),
        ('POST02', '[Seed Flow][Posttest] Que recurso revisaron los vecinos antes de decidir?', @DimensionLiteralId, @DifficultyIntermedioId, 'Recuerda una accion puntual.', 'Testimonios y materiales', 'Resultados deportivos', 'Publicidad de la radio'),
        ('POST03', '[Seed Flow][Posttest] Que se infiere sobre la decision final del barrio?', @DimensionInferencialId, @DifficultyIntermedioId, 'Interpreta la intencion colectiva.', 'Busco equilibrio entre memoria y seguridad', 'Fue una decision improvisada', 'Se tomo sin escuchar a nadie'),
        ('POST04', '[Seed Flow][Posttest] Que se puede inferir sobre el trabajo comunitario?', @DimensionInferencialId, @DifficultyIntermedioId, 'Deduccion sobre la participacion ciudadana.', 'Permite construir soluciones mas sostenibles', 'Siempre retrasa cualquier mejora', 'Solo sirve para tareas menores'),
        ('POST05', '[Seed Flow][Posttest] Cual propuesta es mas consistente con el mensaje del texto?', @DimensionCriticaId, @DifficultyAvanzadoId, 'Valora una propuesta alineada con la evidencia.', 'Conservar patrimonio mientras se mejora su uso', 'Reemplazar todo sin analisis', 'Suspender toda intervencion futura'),
        ('POST06', '[Seed Flow][Posttest] Que criterio resulta mas solido para evaluar la solucion elegida?', @DimensionCriticaId, @DifficultyAvanzadoId, 'Escoge el criterio de evaluacion mas robusto.', 'Su impacto en seguridad e identidad local', 'Su popularidad momentanea', 'El costo mas bajo sin contexto'),

        ('R1P1', '[Seed Flow][R1][Preview] A partir del titulo, sobre que tema tratara la lectura?', @DimensionLiteralId, @DifficultyBasicoId, 'Anticipa el tema principal.', 'Una exploracion con pistas en un bosque', 'Una receta de cocina', 'Una carrera de autos'),
        ('R1P2', '[Seed Flow][R1][Question] Que pregunta orienta mejor la lectura?', @DimensionInferencialId, @DifficultyBasicoId, 'Formula una pregunta pertinente.', 'Como ayudan las pistas a tomar decisiones?', 'Cuantos colores hay en el cielo?', 'Que mascota prefiere el autor?'),
        ('R1P3', '[Seed Flow][R1][Read] Que hallaron Lucia y Mateo durante el recorrido?', @DimensionLiteralId, @DifficultyBasicoId, 'Recupera detalles del texto.', 'Huellas, un mapa viejo y notas', 'Un barco hundido', 'Entradas para un concierto'),
        ('R1P4', '[Seed Flow][R1][Reflect] Que aprendizaje deja la experiencia del grupo?', @DimensionInferencialId, @DifficultyIntermedioId, 'Relaciona hechos con aprendizaje.', 'Que colaborar mejora la interpretacion de pistas', 'Que caminar solo siempre es mejor', 'Que los mapas nunca ayudan'),
        ('R1P5', '[Seed Flow][R1][Recite] Que idea resume mejor la lectura?', @DimensionLiteralId, @DifficultyIntermedioId, 'Sintetiza la idea principal.', 'El grupo comprendio mejor el lugar al trabajar unido', 'Nada importante ocurrio en el bosque', 'La historia trata sobre tecnologia escolar'),
        ('R1P6', '[Seed Flow][R1][Review] Que juicio final esta mejor sustentado?', @DimensionCriticaId, @DifficultyIntermedioId, 'Evalua una conclusion con evidencia.', 'Tomar decisiones informadas reduce errores', 'Actuar sin leer siempre funciona', 'Las notas encontradas no tenian valor'),

        ('R2P1', '[Seed Flow][R2][Preview] Que anticipa el titulo sobre el texto?', @DimensionLiteralId, @DifficultyIntermedioId, 'Identifica el tema general.', 'Que se analizara un puente con valor historico', 'Que se describira un videojuego', 'Que se narrara una tormenta marina'),
        ('R2P2', '[Seed Flow][R2][Question] Que pregunta ayuda mas a comprender la lectura?', @DimensionInferencialId, @DifficultyIntermedioId, 'Formula una pregunta guia adecuada.', 'Como equilibraron historia y seguridad?', 'Que comida llevaron los vecinos?', 'Que dia nacio el puente?'),
        ('R2P3', '[Seed Flow][R2][Read] Que hizo la comunidad antes de tomar la decision final?', @DimensionLiteralId, @DifficultyIntermedioId, 'Busca una accion explicita.', 'Reviso testimonios y materiales', 'Cerco el puente definitivamente', 'Voto sin discutir'),
        ('R2P4', '[Seed Flow][R2][Reflect] Que se infiere sobre la solucion elegida?', @DimensionInferencialId, @DifficultyIntermedioId, 'Interpreta la razon de la eleccion.', 'Intento conservar la memoria del barrio', 'Elimino toda referencia al pasado', 'Ignoro la seguridad por completo'),
        ('R2P5', '[Seed Flow][R2][Recite] Que enunciado resume mejor la lectura?', @DimensionLiteralId, @DifficultyIntermedioId, 'Resume el contenido central.', 'La comunidad reparo el puente cuidando su valor cultural', 'El puente fue demolido inmediatamente', 'El texto trata sobre una feria escolar'),
        ('R2P6', '[Seed Flow][R2][Review] Que criterio evalua mejor la decision comunitaria?', @DimensionCriticaId, @DifficultyAvanzadoId, 'Selecciona el mejor criterio de evaluacion.', 'Su capacidad de proteger y conservar al mismo tiempo', 'La rapidez sin consulta', 'La opinion menos fundamentada'),

        ('R3P1', '[Seed Flow][R3][Preview] Que tema principal sugiere el titulo?', @DimensionLiteralId, @DifficultyAvanzadoId, 'Anticipa el tema de la lectura.', 'El uso reflexivo de proyectos y tecnologia en clase', 'La construccion de un puente', 'Una excursion al bosque'),
        ('R3P2', '[Seed Flow][R3][Question] Que pregunta seria mas util antes de leer?', @DimensionInferencialId, @DifficultyAvanzadoId, 'Escoge una pregunta de anticipacion.', 'Por que la participacion aumenta con tareas con proposito?', 'Que color tiene el aula?', 'Cuantos lapices hay en la mesa?'),
        ('R3P3', '[Seed Flow][R3][Read] Que hicieron los estudiantes en los proyectos?', @DimensionLiteralId, @DifficultyAvanzadoId, 'Recupera acciones concretas.', 'Investigaron problemas reales y compararon fuentes', 'Solo copiaron definiciones', 'Suspendieron todas las actividades'),
        ('R3P4', '[Seed Flow][R3][Reflect] Que se infiere sobre el papel del docente?', @DimensionInferencialId, @DifficultyAvanzadoId, 'Infiera una funcion del docente.', 'Guio procesos con proposito y reflexion', 'Se mantuvo totalmente ausente', 'Evito cualquier uso de tecnologia'),
        ('R3P5', '[Seed Flow][R3][Recite] Cual es la mejor sintesis del texto?', @DimensionLiteralId, @DifficultyAvanzadoId, 'Resume el mensaje del texto.', 'Los proyectos con sentido fortalecieron la participacion estudiantil', 'La tecnologia redujo todo aprendizaje', 'Ningun estudiante participo'),
        ('R3P6', '[Seed Flow][R3][Review] Que valoracion critica esta mejor sustentada?', @DimensionCriticaId, @DifficultyAvanzadoId, 'Elige la conclusion critica mejor fundada.', 'Las tareas con proposito favorecen aprendizajes mas activos', 'La innovacion siempre distrae', 'La reflexion no aporta al aula');

    INSERT INTO Questions
    (
        DimensionId,
        Stem,
        QuestionType,
        Explanation,
        DifficultyLevelId,
        IsActive
    )
    SELECT
        seed.DimensionId,
        seed.Stem,
        'MultipleChoice',
        seed.Explanation,
        seed.DifficultyLevelId,
        1
    FROM @QuestionSeed seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM Questions q
        WHERE q.Stem = seed.Stem
    );

    UPDATE q
    SET
        q.DimensionId = seed.DimensionId,
        q.QuestionType = 'MultipleChoice',
        q.Explanation = seed.Explanation,
        q.DifficultyLevelId = seed.DifficultyLevelId,
        q.IsActive = 1
    FROM Questions q
    INNER JOIN @QuestionSeed seed ON seed.Stem = q.Stem;

    DECLARE @QuestionOptionSeed TABLE
    (
        Stem VARCHAR(500) NOT NULL,
        DisplayOrder TINYINT NOT NULL,
        OptionText VARCHAR(300) NOT NULL,
        IsCorrect BIT NOT NULL
    );

    INSERT INTO @QuestionOptionSeed (Stem, DisplayOrder, OptionText, IsCorrect)
    SELECT Stem, 1, CorrectOption, 1 FROM @QuestionSeed
    UNION ALL
    SELECT Stem, 2, WrongOption1, 0 FROM @QuestionSeed
    UNION ALL
    SELECT Stem, 3, WrongOption2, 0 FROM @QuestionSeed;

    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect, DisplayOrder)
    SELECT
        q.QuestionId,
        seed.OptionText,
        seed.IsCorrect,
        seed.DisplayOrder
    FROM @QuestionOptionSeed seed
    INNER JOIN Questions q ON q.Stem = seed.Stem
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM QuestionOptions qo
        WHERE qo.QuestionId = q.QuestionId
          AND qo.DisplayOrder = seed.DisplayOrder
    );

    UPDATE qo
    SET
        qo.OptionText = seed.OptionText,
        qo.IsCorrect = seed.IsCorrect
    FROM QuestionOptions qo
    INNER JOIN Questions q ON q.QuestionId = qo.QuestionId
    INNER JOIN @QuestionOptionSeed seed
        ON seed.Stem = q.Stem
       AND seed.DisplayOrder = qo.DisplayOrder;

    -----------------------------------------------------------------------
    -- 7) Asociaciones AssessmentQuestions
    -----------------------------------------------------------------------
    DECLARE @AssessmentQuestionSeed TABLE
    (
        AssessmentTitle VARCHAR(150) NOT NULL,
        QuestionStem VARCHAR(500) NOT NULL,
        PhaseCode VARCHAR(20) NULL,
        DisplayOrder TINYINT NOT NULL,
        Points DECIMAL(5, 2) NOT NULL
    );

    INSERT INTO @AssessmentQuestionSeed (AssessmentTitle, QuestionStem, PhaseCode, DisplayOrder, Points)
    VALUES
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Segun el texto, quien encontro el mapa antiguo?', NULL, 1, 100.00),
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Donde inicio la caminata del grupo?', NULL, 2, 100.00),
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Por que el personaje dudo antes de entrar al bosque?', NULL, 3, 100.00),
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Que se puede inferir sobre el abuelo al final del relato?', NULL, 4, 100.00),
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Que accion habria fortalecido mas la decision del grupo?', NULL, 5, 100.00),
        ('[Seed Flow] Pretest Diagnostico', '[Seed Flow][Pretest] Que opinion esta mejor sustentada por el texto?', NULL, 6, 100.00),

        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Que evidencia muestra que la comunidad valoro su historia?', NULL, 1, 100.00),
        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Que recurso revisaron los vecinos antes de decidir?', NULL, 2, 100.00),
        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Que se infiere sobre la decision final del barrio?', NULL, 3, 100.00),
        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Que se puede inferir sobre el trabajo comunitario?', NULL, 4, 100.00),
        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Cual propuesta es mas consistente con el mensaje del texto?', NULL, 5, 100.00),
        ('[Seed Flow] Posttest Final', '[Seed Flow][Posttest] Que criterio resulta mas solido para evaluar la solucion elegida?', NULL, 6, 100.00),

        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Preview] A partir del titulo, sobre que tema tratara la lectura?', 'Preview', 1, 100.00),
        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Question] Que pregunta orienta mejor la lectura?', 'Question', 2, 100.00),
        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Read] Que hallaron Lucia y Mateo durante el recorrido?', 'Read', 3, 100.00),
        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Reflect] Que aprendizaje deja la experiencia del grupo?', 'Reflect', 4, 100.00),
        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Recite] Que idea resume mejor la lectura?', 'Recite', 5, 100.00),
        ('[Seed Flow] Los ecos del bosque - ReadingPractice', '[Seed Flow][R1][Review] Que juicio final esta mejor sustentado?', 'Review', 6, 100.00),

        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Preview] Que anticipa el titulo sobre el texto?', 'Preview', 1, 100.00),
        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Question] Que pregunta ayuda mas a comprender la lectura?', 'Question', 2, 100.00),
        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Read] Que hizo la comunidad antes de tomar la decision final?', 'Read', 3, 100.00),
        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Reflect] Que se infiere sobre la solucion elegida?', 'Reflect', 4, 100.00),
        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Recite] Que enunciado resume mejor la lectura?', 'Recite', 5, 100.00),
        ('[Seed Flow] El puente antiguo - ReadingPractice', '[Seed Flow][R2][Review] Que criterio evalua mejor la decision comunitaria?', 'Review', 6, 100.00),

        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Preview] Que tema principal sugiere el titulo?', 'Preview', 1, 100.00),
        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Question] Que pregunta seria mas util antes de leer?', 'Question', 2, 100.00),
        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Read] Que hicieron los estudiantes en los proyectos?', 'Read', 3, 100.00),
        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Reflect] Que se infiere sobre el papel del docente?', 'Reflect', 4, 100.00),
        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Recite] Cual es la mejor sintesis del texto?', 'Recite', 5, 100.00),
        ('[Seed Flow] Innovacion en el aula - ReadingPractice', '[Seed Flow][R3][Review] Que valoracion critica esta mejor sustentada?', 'Review', 6, 100.00);

    INSERT INTO AssessmentQuestions
    (
        AssessmentId,
        QuestionId,
        PhaseId,
        DisplayOrder,
        Points,
        IsActive
    )
    SELECT
        a.AssessmentId,
        q.QuestionId,
        p.PhaseId,
        seed.DisplayOrder,
        seed.Points,
        1
    FROM @AssessmentQuestionSeed seed
    INNER JOIN Assessments a ON a.Title = seed.AssessmentTitle
    INNER JOIN Questions q ON q.Stem = seed.QuestionStem
    LEFT JOIN Phases p ON p.Code = seed.PhaseCode
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM AssessmentQuestions aq
        WHERE aq.AssessmentId = a.AssessmentId
          AND aq.QuestionId = q.QuestionId
    );

    UPDATE aq
    SET
        aq.PhaseId = p.PhaseId,
        aq.DisplayOrder = seed.DisplayOrder,
        aq.Points = seed.Points,
        aq.IsActive = 1
    FROM AssessmentQuestions aq
    INNER JOIN Assessments a ON a.AssessmentId = aq.AssessmentId
    INNER JOIN Questions q ON q.QuestionId = aq.QuestionId
    INNER JOIN @AssessmentQuestionSeed seed
        ON seed.AssessmentTitle = a.Title
       AND seed.QuestionStem = q.Stem
    LEFT JOIN Phases p ON p.Code = seed.PhaseCode;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
