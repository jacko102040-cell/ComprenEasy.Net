using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ReadingAdaptive.Infrastructure.Persistence.Entities;

namespace ReadingAdaptive.Infrastructure.Persistence;

public partial class ReadingAdaptiveDbContext : DbContext
{
    public ReadingAdaptiveDbContext(DbContextOptions<ReadingAdaptiveDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdaptiveRecommendation> AdaptiveRecommendations { get; set; }

    public virtual DbSet<Assessment> Assessments { get; set; }

    public virtual DbSet<AssessmentAttempt> AssessmentAttempts { get; set; }

    public virtual DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }

    public virtual DbSet<AttemptAnswer> AttemptAnswers { get; set; }

    public virtual DbSet<AttemptPhaseProgress> AttemptPhaseProgresses { get; set; }

    public virtual DbSet<Badge> Badges { get; set; }

    public virtual DbSet<CommentTag> CommentTags { get; set; }

    public virtual DbSet<DifficultyLevel> DifficultyLevels { get; set; }

    public virtual DbSet<Dimension> Dimensions { get; set; }

    public virtual DbSet<FeedbackLog> FeedbackLogs { get; set; }

    public virtual DbSet<MlPrediction> MlPredictions { get; set; }

    public virtual DbSet<PasswordResetLog> PasswordResetLogs { get; set; }

    public virtual DbSet<Phase> Phases { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<Reading> Readings { get; set; }

    public virtual DbSet<ReadingPhase> ReadingPhases { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentBadge> StudentBadges { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherComment> TeacherComments { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdaptiveRecommendation>(entity =>
        {
            entity.HasKey(e => e.RecommendationId);

            entity.HasIndex(e => e.RecommendedAssessmentId, "IX_AdaptiveRecommendations_RecommendedAssessmentId");

            entity.HasIndex(e => e.SourceAttemptId, "IX_AdaptiveRecommendations_SourceAttemptId");

            entity.HasIndex(e => e.StudentId, "IX_AdaptiveRecommendations_StudentId");

            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.EngineType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PredictedAction)
                .HasMaxLength(25)
                .IsUnicode(false);

            entity.HasOne(d => d.CurrentDifficultyLevel).WithMany(p => p.AdaptiveRecommendationCurrentDifficultyLevels)
                .HasForeignKey(d => d.CurrentDifficultyLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdaptiveRecommendations_CurrentDifficulty");

            entity.HasOne(d => d.RecommendedAssessment).WithMany(p => p.AdaptiveRecommendations)
                .HasForeignKey(d => d.RecommendedAssessmentId)
                .HasConstraintName("FK_AdaptiveRecommendations_Assessments");

            entity.HasOne(d => d.RecommendedDifficultyLevel).WithMany(p => p.AdaptiveRecommendationRecommendedDifficultyLevels)
                .HasForeignKey(d => d.RecommendedDifficultyLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdaptiveRecommendations_RecommendedDifficulty");

            entity.HasOne(d => d.SourceAttempt).WithMany(p => p.AdaptiveRecommendations)
                .HasForeignKey(d => d.SourceAttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdaptiveRecommendations_AssessmentAttempts");

            entity.HasOne(d => d.Student).WithMany(p => p.AdaptiveRecommendations)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdaptiveRecommendations_Students");
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasIndex(e => e.AssessmentType, "IX_Assessments_AssessmentType");

            entity.HasIndex(e => e.ReadingId, "IX_Assessments_ReadingId");

            entity.Property(e => e.AssessmentType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.DifficultyLevel).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.DifficultyLevelId)
                .HasConstraintName("FK_Assessments_DifficultyLevels");

            entity.HasOne(d => d.Reading).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.ReadingId)
                .HasConstraintName("FK_Assessments_Readings");
        });

        modelBuilder.Entity<AssessmentAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId);

            entity.HasIndex(e => e.AssessmentId, "IX_AssessmentAttempts_AssessmentId");

            entity.HasIndex(e => e.StartedAt, "IX_AssessmentAttempts_StartedAt");

            entity.HasIndex(e => e.Status, "IX_AssessmentAttempts_Status");

            entity.HasIndex(e => e.StudentId, "IX_AssessmentAttempts_StudentId");

            entity.Property(e => e.AttemptNumber).HasDefaultValue((byte)1);
            entity.Property(e => e.CompletionPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CriticalScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.InferentialScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.LiteralScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("InProgress");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Assessment).WithMany(p => p.AssessmentAttempts)
                .HasForeignKey(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssessmentAttempts_Assessments");

            entity.HasOne(d => d.Student).WithMany(p => p.AssessmentAttempts)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssessmentAttempts_Students");
        });

        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.HasIndex(e => e.AssessmentId, "IX_AssessmentQuestions_AssessmentId");

            entity.HasIndex(e => e.PhaseId, "IX_AssessmentQuestions_PhaseId");

            entity.HasIndex(e => e.QuestionId, "IX_AssessmentQuestions_QuestionId");

            entity.HasIndex(e => new { e.AssessmentId, e.DisplayOrder }, "UQ_AssessmentQuestions_AssessmentId_DisplayOrder").IsUnique();

            entity.HasIndex(e => new { e.AssessmentId, e.QuestionId }, "UQ_AssessmentQuestions_AssessmentId_QuestionId").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Points)
                .HasDefaultValue(100m)
                .HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Assessment).WithMany(p => p.AssessmentQuestions)
                .HasForeignKey(d => d.AssessmentId)
                .HasConstraintName("FK_AssessmentQuestions_Assessments");

            entity.HasOne(d => d.Phase).WithMany(p => p.AssessmentQuestions)
                .HasForeignKey(d => d.PhaseId)
                .HasConstraintName("FK_AssessmentQuestions_Phases");

            entity.HasOne(d => d.Question).WithMany(p => p.AssessmentQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssessmentQuestions_Questions");
        });

        modelBuilder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasIndex(e => e.AttemptId, "IX_AttemptAnswers_AttemptId");

            entity.HasIndex(e => e.QuestionId, "IX_AttemptAnswers_QuestionId");

            entity.HasIndex(e => e.SelectedOptionId, "IX_AttemptAnswers_SelectedOptionId");

            entity.HasIndex(e => new { e.AttemptId, e.QuestionId }, "UQ_AttemptAnswers_AttemptId_QuestionId").IsUnique();

            entity.Property(e => e.ScoreObtained).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Attempt).WithMany(p => p.AttemptAnswers)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("FK_AttemptAnswers_AssessmentAttempts");

            entity.HasOne(d => d.Question).WithMany(p => p.AttemptAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptAnswers_Questions");

            entity.HasOne(d => d.SelectedOption).WithMany(p => p.AttemptAnswers)
                .HasForeignKey(d => d.SelectedOptionId)
                .HasConstraintName("FK_AttemptAnswers_QuestionOptions");
        });

        modelBuilder.Entity<AttemptPhaseProgress>(entity =>
        {
            entity.ToTable("AttemptPhaseProgress");

            entity.HasIndex(e => e.AttemptId, "IX_AttemptPhaseProgress_AttemptId");

            entity.HasIndex(e => e.PhaseId, "IX_AttemptPhaseProgress_PhaseId");

            entity.HasIndex(e => new { e.AttemptId, e.PhaseId }, "UQ_AttemptPhaseProgress_AttemptId_PhaseId").IsUnique();

            entity.HasIndex(e => new { e.AttemptId, e.SequenceOrder }, "UQ_AttemptPhaseProgress_AttemptId_SequenceOrder").IsUnique();

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UnlockReason).HasMaxLength(200);

            entity.HasOne(d => d.Attempt).WithMany(p => p.AttemptPhaseProgresses)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("FK_AttemptPhaseProgress_AssessmentAttempts");

            entity.HasOne(d => d.Phase).WithMany(p => p.AttemptPhaseProgresses)
                .HasForeignKey(d => d.PhaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttemptPhaseProgress_Phases");
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasIndex(e => e.CriteriaCode, "UQ_Badges_CriteriaCode").IsUnique();

            entity.HasIndex(e => e.Name, "UQ_Badges_Name").IsUnique();

            entity.Property(e => e.CriteriaCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IconKey)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(60);
        });

        modelBuilder.Entity<CommentTag>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_CommentTags_Name").IsUnique();

            entity.Property(e => e.CommentTagId).ValueGeneratedOnAdd();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DifficultyLevel>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_DifficultyLevels_Name").IsUnique();

            entity.HasIndex(e => e.RankOrder, "UQ_DifficultyLevels_RankOrder").IsUnique();

            entity.Property(e => e.DifficultyLevelId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Dimension>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Dimensions_Name").IsUnique();

            entity.Property(e => e.DimensionId).ValueGeneratedOnAdd();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FeedbackLog>(entity =>
        {
            entity.HasIndex(e => e.AttemptId, "IX_FeedbackLogs_AttemptId");

            entity.HasIndex(e => e.PhaseId, "IX_FeedbackLogs_PhaseId");

            entity.HasIndex(e => e.QuestionId, "IX_FeedbackLogs_QuestionId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FeedbackType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Message).HasMaxLength(300);
            entity.Property(e => e.Severity)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Info");

            entity.HasOne(d => d.Attempt).WithMany(p => p.FeedbackLogs)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("FK_FeedbackLogs_AssessmentAttempts");

            entity.HasOne(d => d.Phase).WithMany(p => p.FeedbackLogs)
                .HasForeignKey(d => d.PhaseId)
                .HasConstraintName("FK_FeedbackLogs_Phases");

            entity.HasOne(d => d.Question).WithMany(p => p.FeedbackLogs)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_FeedbackLogs_Questions");
        });

        modelBuilder.Entity<MlPrediction>(entity =>
        {
            entity.HasIndex(e => e.RecommendationId, "UQ_MlPredictions_RecommendationId").IsUnique();

            entity.Property(e => e.AvgResponseTimeSeconds).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CriticalScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.InferentialScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.LiteralScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ModelVersion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PreviousProgressDelta).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RawOutput).HasMaxLength(200);

            entity.HasOne(d => d.Recommendation).WithOne(p => p.MlPrediction)
                .HasForeignKey<MlPrediction>(d => d.RecommendationId)
                .HasConstraintName("FK_MlPredictions_AdaptiveRecommendations");
        });

        modelBuilder.Entity<PasswordResetLog>(entity =>
        {
            entity.HasIndex(e => e.ResetByUserId, "IX_PasswordResetLogs_ResetByUserId");

            entity.HasIndex(e => e.StudentId, "IX_PasswordResetLogs_StudentId");

            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.ResetAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.ResetByUser).WithMany(p => p.PasswordResetLogs)
                .HasForeignKey(d => d.ResetByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetLogs_Users");

            entity.HasOne(d => d.Student).WithMany(p => p.PasswordResetLogs)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetLogs_Students");
        });

        modelBuilder.Entity<Phase>(entity =>
        {
            entity.HasIndex(e => e.Code, "UQ_Phases_Code").IsUnique();

            entity.HasIndex(e => e.DefaultOrder, "UQ_Phases_DefaultOrder").IsUnique();

            entity.Property(e => e.PhaseId).ValueGeneratedOnAdd();
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DisplayName)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasIndex(e => e.DifficultyLevelId, "IX_Questions_DifficultyLevelId");

            entity.HasIndex(e => e.DimensionId, "IX_Questions_DimensionId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Explanation).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.QuestionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("MultipleChoice");
            entity.Property(e => e.Stem).HasMaxLength(500);

            entity.HasOne(d => d.DifficultyLevel).WithMany(p => p.Questions)
                .HasForeignKey(d => d.DifficultyLevelId)
                .HasConstraintName("FK_Questions_DifficultyLevels");

            entity.HasOne(d => d.Dimension).WithMany(p => p.Questions)
                .HasForeignKey(d => d.DimensionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Questions_Dimensions");
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.OptionId);

            entity.HasIndex(e => e.QuestionId, "IX_QuestionOptions_QuestionId");

            entity.HasIndex(e => new { e.QuestionId, e.DisplayOrder }, "UQ_QuestionOptions_QuestionId_DisplayOrder").IsUnique();

            entity.HasIndex(e => e.QuestionId, "UX_QuestionOptions_OneCorrectOption")
                .IsUnique()
                .HasFilter("([IsCorrect]=(1))");

            entity.Property(e => e.OptionText).HasMaxLength(300);

            entity.HasOne(d => d.Question).WithOne(p => p.QuestionOption)
                .HasForeignKey<QuestionOption>(d => d.QuestionId)
                .HasConstraintName("FK_QuestionOptions_Questions");
        });

        modelBuilder.Entity<Reading>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_Readings_CreatedByUserId");

            entity.HasIndex(e => e.DifficultyLevelId, "IX_Readings_DifficultyLevelId");

            entity.HasIndex(e => e.IsActive, "IX_Readings_IsActive");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ImageUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Summary).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Readings)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Readings_Users");

            entity.HasOne(d => d.DifficultyLevel).WithMany(p => p.Readings)
                .HasForeignKey(d => d.DifficultyLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Readings_DifficultyLevels");
        });

        modelBuilder.Entity<ReadingPhase>(entity =>
        {
            entity.HasIndex(e => new { e.ReadingId, e.PhaseId }, "UQ_ReadingPhases_ReadingId_PhaseId").IsUnique();

            entity.Property(e => e.GuidanceText).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);

            entity.HasOne(d => d.Phase).WithMany(p => p.ReadingPhases)
                .HasForeignKey(d => d.PhaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReadingPhases_Phases");

            entity.HasOne(d => d.Reading).WithMany(p => p.ReadingPhases)
                .HasForeignKey(d => d.ReadingId)
                .HasConstraintName("FK_ReadingPhases_Readings");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.Grade).HasDefaultValue((byte)1);
            entity.Property(e => e.IsEnabledForTest).HasDefaultValue(true);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.Section)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.StudentNavigation).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<StudentBadge>(entity =>
        {
            entity.HasIndex(e => e.BadgeId, "IX_StudentBadges_BadgeId");

            entity.HasIndex(e => e.StudentId, "IX_StudentBadges_StudentId");

            entity.HasIndex(e => new { e.StudentId, e.BadgeId }, "UQ_StudentBadges_StudentId_BadgeId").IsUnique();

            entity.Property(e => e.EarnedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Attempt).WithMany(p => p.StudentBadges)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("FK_StudentBadges_AssessmentAttempts");

            entity.HasOne(d => d.Badge).WithMany(p => p.StudentBadges)
                .HasForeignKey(d => d.BadgeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentBadges_Badges");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentBadges)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentBadges_Students");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.Property(e => e.TeacherId).ValueGeneratedNever();
            entity.Property(e => e.CanResetPasswords).HasDefaultValue(true);

            entity.HasOne(d => d.TeacherNavigation).WithOne(p => p.Teacher)
                .HasForeignKey<Teacher>(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teachers_Users");
        });

        modelBuilder.Entity<TeacherComment>(entity =>
        {
            entity.HasIndex(e => e.AttemptId, "IX_TeacherComments_AttemptId");

            entity.HasIndex(e => e.StudentId, "IX_TeacherComments_StudentId");

            entity.HasIndex(e => e.TeacherId, "IX_TeacherComments_TeacherId");

            entity.Property(e => e.CommentText).HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Attempt).WithMany(p => p.TeacherComments)
                .HasForeignKey(d => d.AttemptId)
                .HasConstraintName("FK_TeacherComments_AssessmentAttempts");

            entity.HasOne(d => d.CommentTag).WithMany(p => p.TeacherComments)
                .HasForeignKey(d => d.CommentTagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherComments_CommentTags");

            entity.HasOne(d => d.Student).WithMany(p => p.TeacherComments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherComments_Students");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherComments)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeacherComments_Teachers");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_Users_IsActive");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
