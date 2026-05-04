using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Extensions;

namespace Orbits.GeneralProject.Core.Infrastructure
{
    public class ApplicationDbContext : OrbitsContext
    {
        private static readonly JsonSerializerOptions AuditJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly HashSet<string> AuditIgnoredEntityNames = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(AuditLog),
            nameof(AuditLogParticipant),
            nameof(RefreshToken)
        };

        private static readonly HashSet<string> AuditTechnicalProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt",
            "CreatedBy",
            "ModefiedAt",
            "ModefiedBy",
            "IsDeleted",
            "RegisterAt",
            "CodeExpirationTime"
        };

        private static readonly HashSet<string> AuditMaskedProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash"
        };

        private static readonly HashSet<string> UtcCreatedStampProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt",
            "RegisterAt"
        };

        private static readonly HashSet<string> UtcModifiedStampProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "ModefiedAt",
            "ModifiedAt"
        };

        private static readonly HashSet<string> UtcClientDateTimeProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "CreationTime",
            "PaymentDate",
            "PayedAt"
        };

        private static readonly HashSet<string> DateOnlyProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Month",
            "BirthDate",
            "StartDate",
            "EndDate",
            "ScheduleDate",
            "AttendDate"
        };

        private static readonly HashSet<string> UtcSecurityProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "ExpiresOn",
            "CreatedOn",
            "RevokedOn",
            "CodeExpirationTime"
        };

        private static readonly HashSet<string> DateTimeNormalizationIgnoredEntityNames = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(RefreshToken)
        };

        private static readonly Dictionary<string, string> PropertyLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FullName"] = "الاسم",
            ["Name"] = "الاسم",
            ["Email"] = "البريد الإلكتروني",
            ["Mobile"] = "الجوال",
            ["SecondMobile"] = "الجوال الإضافي",
            ["SalaryReceiveMethodId"] = "طريقة استلام الراتب",
            ["ResidentId"] = "الإقامة",
            ["NationalityId"] = "الجنسية",
            ["GovernorateId"] = "المحافظة",
            ["BranchId"] = "الفرع",
            ["UserTypeId"] = "نوع المستخدم",
            ["TeacherId"] = "المعلم",
            ["StudentId"] = "الطالب",
            ["ManagerId"] = "المشرف",
            ["CircleId"] = "الحلقة",
            ["StudentSubscribeId"] = "الباقة",
            ["OldSubscribeId"] = "الباقة السابقة",
            ["NewSubscribeId"] = "الباقة الجديدة",
            ["SubscribeTypeId"] = "نوع الباقة",
            ["StudentSubscribeTypeId"] = "نوع الباقة",
            ["StudentPaymentId"] = "الدفعة",
            ["CircleReportId"] = "التقرير",
            ["Price"] = "السعر",
            ["Minutes"] = "الدقائق",
            ["HourPrice"] = "سعر الساعة",
            ["Amount"] = "المبلغ",
            ["Month"] = "الشهر",
            ["PaymentDate"] = "تاريخ الدفع",
            ["PayedAt"] = "تاريخ السداد",
            ["Inactive"] = "الحالة",
            ["PasswordHash"] = "كلمة المرور",
            ["Other"] = "ملاحظات",
            ["GeneralRate"] = "التقييم العام",
            ["Intonation"] = "أحكام التجويد",
            ["RecentPast"] = "القريب الماضي",
            ["DistantPast"] = "البعيد الماضي",
            ["FarthestPast"] = "الأبعد ماضيًا",
            ["NextCircleOrder"] = "واجب الحلقة القادمة",
            ["ReceiptPath"] = "الإيصال",
            ["Sallary"] = "الراتب",
            ["IsPayed"] = "تم السداد",
            ["PayStatus"] = "حالة السداد",
            ["PayStatue"] = "حالة السداد",
            ["IsCancelled"] = "ملغي"
        };

        private readonly IAuditUserContext _auditUserContext;
        private bool _isSavingAuditLogs;

        public ApplicationDbContext()
            : this(new NullAuditUserContext())
        {
        }

        public ApplicationDbContext(IAuditUserContext auditUserContext)
        {
            _auditUserContext = auditUserContext ?? new NullAuditUserContext();
        }

        public ApplicationDbContext(DbConnection connection)
            : this(new NullAuditUserContext())
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseExceptionProcessor();
            optionsBuilder.UseLazyLoadingProxies();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUtcDateTimeStorage(modelBuilder);

            var jsonvalueMethodInfo = typeof(Json).GetRuntimeMethod(nameof(Json.Value), new[] { typeof(string), typeof(string) });
            var translatevalueMethodInfo = typeof(Translate).GetRuntimeMethod(nameof(Translate.Value), new[] { typeof(int), typeof(string), typeof(int) });

            modelBuilder.HasDbFunction(jsonvalueMethodInfo).HasTranslation(args => SqlFunctionExpression.Create("JSON_VALUE", args, typeof(string), null));
            modelBuilder.HasDbFunction(translatevalueMethodInfo).HasTranslation(args => SqlFunctionExpression.Create("dbo.GetValueTranslate", args, typeof(string), null));

            modelBuilder.HasDbFunction(() => GetDayName(default))
                .HasTranslation(args =>
                {
                    var dateArg = args.First();
                    return new SqlFunctionExpression(
                        "DATENAME",
                        new[]
                        {
                            new SqlFragmentExpression("weekday"),
                            dateArg
                        },
                        true,
                        new[] { false, false },
                        typeof(string),
                        null);
                });

            modelBuilder.HasDbFunction(
                methodInfo: typeof(ApplicationDbContext).GetMethod(nameof(GetHigriDate)),
                builderAction: f => f.HasTranslation(args =>
                {
                    var dateArg = args.First();
                    var formatArg = args.Skip(1).First();

                    return new SqlFunctionExpression(
                        "FORMAT",
                        new[]
                        {
                            dateArg,
                            formatArg,
                            new SqlFragmentExpression("'ar-SA'")
                        },
                        false,
                        new[] { false, false, false },
                        typeof(string),
                        null);
                }));

            modelBuilder.HasDbFunction(() => CastToInt(default))
                .HasTranslation(args =>
                {
                    var dateArg = args.First();
                    return new SqlFunctionExpression(
                        "CAST",
                        new[]
                        {
                            new SqlFragmentExpression($"'{dateArg} AS INTEGER'")
                        },
                        false,
                        new[] { false },
                        typeof(int),
                        null);
                });

            modelBuilder.Entity<RefreshToken>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<ManagerCircle>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<CircleDay>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<Nationality>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<Governorate>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<StudentSubscribe>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<StudentPayment>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<TeacherSallary>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<ManagerSallary>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<ManagerTeacher>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<ManagerStudent>().Ignore(f => f.IsDeleted);

            modelBuilder.ApplyGlobalFilters<IEntityBase>(e => e.IsDeleted == false, "IsDeleted");

            base.OnModelCreating(modelBuilder);
        }

        private static void ConfigureUtcDateTimeStorage(ModelBuilder modelBuilder)
        {
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                value => BusinessDateTime.EnsureUtc(value),
                value => BusinessDateTime.EnsureUtc(value));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                value => value.HasValue ? BusinessDateTime.EnsureUtc(value.Value) : value,
                value => value.HasValue ? BusinessDateTime.EnsureUtc(value.Value) : value);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (DateTimeNormalizationIgnoredEntityNames.Contains(entityType.ClrType.Name))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (propertyType != typeof(DateTime) || DateOnlyProperties.Contains(property.Name))
                    {
                        continue;
                    }

                    property.SetValueConverter(property.ClrType == typeof(DateTime)
                        ? utcConverter
                        : nullableUtcConverter);
                }
            }
        }

        [DbFunction(Schema = "dbo")]
        public static string GetDayName(DateTime dateTime) => throw new NotSupportedException("This method is for database mapping only and should not be called directly.");

        [DbFunction(Schema = "dbo")]
        public static string GetHigriMonth(DateTime dateTime) => throw new NotSupportedException("This method is for database mapping only and should not be called directly.");

        [DbFunction(Schema = "dbo")]
        public static string GetHigriDate(DateTime dateTime, string format) => throw new NotSupportedException("This method is for database mapping only and should not be called directly.");

        [DbFunction(Schema = "dbo")]
        public static string GetHigriDay(DateTime dateTime) => throw new NotSupportedException("This method is for database mapping only and should not be called directly.");

        [DbFunction(Schema = "dbo")]
        public static int CastToInt(string daynum) => throw new NotSupportedException("This method is for database mapping only and should not be called directly.");

        public virtual void Commit()
        {
            SaveChanges();
        }

        public virtual async Task<int> CommitAsync()
        {
            return await SaveChangesAsync();
        }

        public override int SaveChanges()
        {
            return SaveChangesWithAuditAsync(isAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesWithAuditAsync(isAsync: true, cancellationToken);
        }

        private async Task<int> SaveChangesWithAuditAsync(bool isAsync, CancellationToken cancellationToken)
        {
            if (_isSavingAuditLogs)
            {
                return isAsync
                    ? await base.SaveChangesAsync(cancellationToken)
                    : base.SaveChanges();
            }

            ChangeTracker.DetectChanges();
            NormalizeDateTimeValuesForPersistence();
            ChangeTracker.DetectChanges();

            var pendingAuditEntries = PrepareAuditEntries();
            var result = isAsync
                ? await base.SaveChangesAsync(cancellationToken)
                : base.SaveChanges();

            if (pendingAuditEntries.Count == 0)
            {
                return result;
            }

            var auditLogs = await BuildAuditLogsAsync(pendingAuditEntries, cancellationToken);
            if (auditLogs.Count == 0)
            {
                return result;
            }

            try
            {
                _isSavingAuditLogs = true;
                AuditLogs.AddRange(auditLogs);

                if (isAsync)
                {
                    await base.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    base.SaveChanges();
                }
            }
            finally
            {
                _isSavingAuditLogs = false;
            }

            return result;
        }

        private void NormalizeDateTimeValuesForPersistence()
        {
            var utcNow = BusinessDateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                if (DateTimeNormalizationIgnoredEntityNames.Contains(entry.Metadata.ClrType.Name))
                {
                    continue;
                }

                foreach (var property in entry.Properties)
                {
                    if (!IsDateTimeProperty(property) || property.Metadata.IsPrimaryKey())
                    {
                        continue;
                    }

                    var propertyName = property.Metadata.Name;

                    if (UtcSecurityProperties.Contains(propertyName))
                    {
                        continue;
                    }

                    if (UtcCreatedStampProperties.Contains(propertyName))
                    {
                        if (entry.State == EntityState.Added)
                        {
                            var createdValue = TryGetCurrentDateTime(property, out var existingCreatedAt)
                                ? BusinessDateTime.EnsureUtc(existingCreatedAt)
                                : utcNow;

                            SetDateTimeValue(property, createdValue);
                        }

                        continue;
                    }

                    if (UtcModifiedStampProperties.Contains(propertyName))
                    {
                        if (entry.State == EntityState.Modified || property.IsModified)
                        {
                            var modifiedValue = TryGetCurrentDateTime(property, out var existingModifiedAt)
                                ? BusinessDateTime.EnsureUtc(existingModifiedAt)
                                : utcNow;

                            SetDateTimeValue(property, modifiedValue);
                        }

                        continue;
                    }

                    if (!TryGetCurrentDateTime(property, out var currentValue))
                    {
                        continue;
                    }

                    if (DateOnlyProperties.Contains(propertyName))
                    {
                        SetDateTimeValue(property, BusinessDateTime.EnsureUnspecified(currentValue));
                        continue;
                    }

                    if (UtcClientDateTimeProperties.Contains(propertyName))
                    {
                        SetDateTimeValue(property, BusinessDateTime.NormalizeClientDateTimeToUtc(currentValue));
                        continue;
                    }

                    if (entry.State == EntityState.Added || property.IsModified)
                    {
                        SetDateTimeValue(property, BusinessDateTime.EnsureUtc(currentValue));
                    }
                }
            }
        }

        private static bool IsDateTimeProperty(PropertyEntry property)
        {
            var propertyType = Nullable.GetUnderlyingType(property.Metadata.ClrType) ?? property.Metadata.ClrType;
            return propertyType == typeof(DateTime);
        }

        private static bool TryGetCurrentDateTime(PropertyEntry property, out DateTime value)
        {
            value = default;

            if (property.CurrentValue == null)
            {
                return false;
            }

            if (property.CurrentValue is DateTime dateTimeValue)
            {
                if (dateTimeValue == default)
                {
                    return false;
                }

                value = dateTimeValue;
                return true;
            }

            return false;
        }

        private static void SetDateTimeValue(PropertyEntry property, DateTime value)
        {
            property.CurrentValue = Nullable.GetUnderlyingType(property.Metadata.ClrType) == typeof(DateTime)
                ? (DateTime?)value
                : value;
        }

        private List<PendingAuditEntry> PrepareAuditEntries()
        {
            var pendingEntries = new List<PendingAuditEntry>();

            foreach (var entry in ChangeTracker.Entries().Where(ShouldAuditEntry))
            {
                var actionType = ResolveActionType(entry);
                if (actionType == null)
                {
                    continue;
                }

                var pendingEntry = new PendingAuditEntry(entry, actionType);

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.IsPrimaryKey())
                    {
                        if (property.IsTemporary)
                        {
                            pendingEntry.TemporaryProperties.Add(property);
                        }
                        else
                        {
                            pendingEntry.EntityId = TryConvertToInt(property.CurrentValue) ?? TryConvertToInt(property.OriginalValue);
                        }
                    }

                    pendingEntry.OriginalValues[property.Metadata.Name] = NormalizeValue(property.OriginalValue);
                    pendingEntry.CurrentValues[property.Metadata.Name] = NormalizeValue(property.CurrentValue);
                }

                pendingEntry.EntityDisplayName = ResolveEntityDisplayName(pendingEntry);

                if (actionType == "Update")
                {
                    foreach (var property in entry.Properties.Where(p => p.IsModified))
                    {
                        var propertyName = property.Metadata.Name;
                        if (property.Metadata.IsPrimaryKey() || AuditTechnicalProperties.Contains(propertyName))
                        {
                            continue;
                        }

                        var oldValue = NormalizeValue(property.OriginalValue);
                        var newValue = NormalizeValue(property.CurrentValue);
                        if (AreValuesEqual(oldValue, newValue))
                        {
                            continue;
                        }

                        pendingEntry.Changes.Add(new AuditChange
                        {
                            PropertyName = propertyName,
                            PropertyLabel = GetPropertyLabel(propertyName),
                            OldValue = FormatAuditValue(oldValue),
                            NewValue = FormatAuditValue(newValue)
                        });
                    }

                    if (pendingEntry.Changes.Count == 0)
                    {
                        continue;
                    }
                }
                else if (actionType == "Create")
                {
                    foreach (var property in entry.Properties)
                    {
                        var propertyName = property.Metadata.Name;
                        if (property.Metadata.IsPrimaryKey() || AuditTechnicalProperties.Contains(propertyName))
                        {
                            continue;
                        }

                        var newValue = NormalizeValue(property.CurrentValue);
                        if (IsEmptyAuditValue(newValue))
                        {
                            continue;
                        }

                        pendingEntry.Changes.Add(new AuditChange
                        {
                            PropertyName = propertyName,
                            PropertyLabel = GetPropertyLabel(propertyName),
                            OldValue = null,
                            NewValue = FormatAuditValue(newValue)
                        });
                    }
                }
                else if (actionType == "Delete")
                {
                    foreach (var property in entry.Properties)
                    {
                        var propertyName = property.Metadata.Name;
                        if (property.Metadata.IsPrimaryKey() || AuditTechnicalProperties.Contains(propertyName))
                        {
                            continue;
                        }

                        var oldValue = NormalizeValue(property.OriginalValue);
                        if (IsEmptyAuditValue(oldValue))
                        {
                            continue;
                        }

                        pendingEntry.Changes.Add(new AuditChange
                        {
                            PropertyName = propertyName,
                            PropertyLabel = GetPropertyLabel(propertyName),
                            OldValue = FormatAuditValue(oldValue),
                            NewValue = null
                        });
                    }
                }

                pendingEntries.Add(pendingEntry);
            }

            return pendingEntries;
        }

        private async Task<List<AuditLog>> BuildAuditLogsAsync(
            IReadOnlyCollection<PendingAuditEntry> pendingAuditEntries,
            CancellationToken cancellationToken)
        {
            var logs = new List<AuditLog>();
            if (pendingAuditEntries.Count == 0)
            {
                return logs;
            }

            var actorName = await ResolveActorNameAsync(cancellationToken);
            var lookupCache = new AuditLookupCache();
            await EnrichAuditEntriesAsync(pendingAuditEntries, lookupCache, cancellationToken);

            foreach (var pendingEntry in pendingAuditEntries)
            {
                if (!pendingEntry.EntityId.HasValue)
                {
                    foreach (var property in pendingEntry.TemporaryProperties.Where(p => p.Metadata.IsPrimaryKey()))
                    {
                        pendingEntry.EntityId = TryConvertToInt(property.CurrentValue);
                    }
                }

                var auditLog = new AuditLog
                {
                    ActionType = pendingEntry.ActionType,
                    EntityType = pendingEntry.EntityType,
                    EntityId = pendingEntry.EntityId,
                    EntityLabel = ResolveEntityLabel(pendingEntry),
                    EntityDisplayName = pendingEntry.EntityDisplayName,
                    Summary = BuildSummary(pendingEntry, actorName),
                    ActorUserId = _auditUserContext.UserId,
                    ActorName = actorName,
                    ActorRoleId = _auditUserContext.RoleId,
                    SourceScreen = NormalizeAuditText(_auditUserContext.SourceScreen),
                    SourceRoute = NormalizeAuditText(_auditUserContext.SourceRoute),
                    RequestPath = NormalizeAuditText(_auditUserContext.RequestPath),
                    HttpMethod = NormalizeAuditText(_auditUserContext.HttpMethod),
                    CreatedAt = BusinessDateTime.UtcNow,
                    ChangesJson = pendingEntry.Changes.Count == 0
                        ? null
                        : JsonSerializer.Serialize(pendingEntry.Changes, AuditJsonOptions),
                    IsDeleted = false
                };

                AddParticipant(auditLog, "User", _auditUserContext.UserId, "Actor", actorName);
                AddParticipant(
                    auditLog,
                    GetUserParticipantType(_auditUserContext.RoleId),
                    _auditUserContext.UserId,
                    GetUserEntityLabel(_auditUserContext.RoleId),
                    actorName);
                await AddEntityParticipantsAsync(auditLog, pendingEntry, lookupCache, cancellationToken);

                logs.Add(auditLog);
            }

            return logs;
        }

        private async Task EnrichAuditEntriesAsync(
            IReadOnlyCollection<PendingAuditEntry> pendingAuditEntries,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            foreach (var pendingEntry in pendingAuditEntries)
            {
                pendingEntry.EntityDisplayName = await ResolveEntityDisplayNameAsync(pendingEntry, lookupCache, cancellationToken);

                foreach (var change in pendingEntry.Changes)
                {
                    change.PropertyLabel = GetPropertyLabel(change.PropertyName);
                    change.OldValue = await ResolveAuditChangeValueAsync(change.PropertyName, change.OldValue, lookupCache, cancellationToken);
                    change.NewValue = await ResolveAuditChangeValueAsync(change.PropertyName, change.NewValue, lookupCache, cancellationToken);
                }
            }
        }

        private async Task<string?> ResolveAuditChangeValueAsync(
            string propertyName,
            string? value,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (normalizedValue == null)
            {
                return null;
            }

            switch (propertyName)
            {
                case "TeacherId":
                case "StudentId":
                case "ManagerId":
                    return await ResolveUserDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "ResidentId":
                case "NationalityId":
                    return await ResolveNationalityDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "GovernorateId":
                    return await ResolveGovernorateDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "BranchId":
                    return ResolveBranchDisplayName(TryConvertToInt(normalizedValue)) ?? normalizedValue;
                case "CircleId":
                    return await ResolveCircleDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "StudentSubscribeId":
                case "OldSubscribeId":
                case "NewSubscribeId":
                    return await ResolveSubscribeDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "SubscribeTypeId":
                case "StudentSubscribeTypeId":
                    return await ResolveSubscribeTypeDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "StudentPaymentId":
                    return await ResolveStudentPaymentDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "CircleReportId":
                    return await ResolveCircleReportDisplayNameAsync(TryConvertToInt(normalizedValue), lookupCache, cancellationToken) ?? normalizedValue;
                case "UserTypeId":
                    return GetUserEntityLabel(TryConvertToInt(normalizedValue));
            }

            if (bool.TryParse(normalizedValue, out var boolValue))
            {
                return boolValue ? "نعم" : "لا";
            }

            return normalizedValue;
        }

        private async Task<string?> ResolveEntityDisplayNameAsync(
            PendingAuditEntry pendingEntry,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            var fallbackDisplayName = ResolveEntityDisplayName(pendingEntry);

            switch (pendingEntry.EntityType)
            {
                case nameof(User):
                    return fallbackDisplayName;
                case nameof(Circle):
                    return fallbackDisplayName?.StartsWith("#", StringComparison.Ordinal) == true
                        ? await ResolveCircleDisplayNameAsync(pendingEntry.EntityId, lookupCache, cancellationToken)
                        : fallbackDisplayName;
                case nameof(Subscribe):
                    return fallbackDisplayName?.StartsWith("#", StringComparison.Ordinal) == true
                        ? await ResolveSubscribeDisplayNameAsync(pendingEntry.EntityId, lookupCache, cancellationToken)
                        : fallbackDisplayName;
                case nameof(SubscribeType):
                    return fallbackDisplayName?.StartsWith("#", StringComparison.Ordinal) == true
                        ? await ResolveSubscribeTypeDisplayNameAsync(pendingEntry.EntityId, lookupCache, cancellationToken)
                        : fallbackDisplayName;
                case nameof(StudentPayment):
                    return BuildCompositeDisplayName(
                        await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentId")), lookupCache, cancellationToken),
                        await ResolveSubscribeDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentSubscribeId")), lookupCache, cancellationToken),
                        FormatDisplayDate(GetSnapshotValue(pendingEntry, "PaymentDate")))
                        ?? await ResolveStudentPaymentDisplayNameAsync(pendingEntry.EntityId, lookupCache, cancellationToken)
                        ?? fallbackDisplayName;
                case nameof(StudentSubscribe):
                    return BuildCompositeDisplayName(
                        await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentId")), lookupCache, cancellationToken),
                        await ResolveSubscribeDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentSubscribeId")), lookupCache, cancellationToken),
                        await ResolveSubscribeTypeDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentSubscribeTypeId")), lookupCache, cancellationToken))
                        ?? fallbackDisplayName;
                case nameof(TeacherSallary):
                    return BuildCompositeDisplayName(
                        await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "TeacherId")), lookupCache, cancellationToken),
                        FormatDisplayDate(GetSnapshotValue(pendingEntry, "Month")))
                        ?? fallbackDisplayName;
                case nameof(ManagerStudent):
                    return BuildCompositeDisplayName(
                        $"المشرف: {await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "ManagerId")), lookupCache, cancellationToken)}",
                        $"الطالب: {await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentId")), lookupCache, cancellationToken)}")
                        ?? fallbackDisplayName;
                case nameof(ManagerTeacher):
                    return BuildCompositeDisplayName(
                        $"المشرف: {await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "ManagerId")), lookupCache, cancellationToken)}",
                        $"المعلم: {await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "TeacherId")), lookupCache, cancellationToken)}")
                        ?? fallbackDisplayName;
                case nameof(ManagerCircle):
                    return BuildCompositeDisplayName(
                        $"المشرف: {await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "ManagerId")), lookupCache, cancellationToken)}",
                        $"الحلقة: {await ResolveCircleDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "CircleId")), lookupCache, cancellationToken)}")
                        ?? fallbackDisplayName;
                case nameof(CircleReport):
                    return BuildCompositeDisplayName(
                        await ResolveCircleDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "CircleId")), lookupCache, cancellationToken),
                        await ResolveUserDisplayNameAsync(TryConvertToInt(GetSnapshotValue(pendingEntry, "StudentId")), lookupCache, cancellationToken),
                        FormatDisplayDate(GetSnapshotValue(pendingEntry, "CreationTime") ?? GetSnapshotValue(pendingEntry, "CreatedAt")))
                        ?? await ResolveCircleReportDisplayNameAsync(pendingEntry.EntityId, lookupCache, cancellationToken)
                        ?? fallbackDisplayName;
                default:
                    return fallbackDisplayName;
            }
        }

        private async Task<string?> ResolveParticipantDisplayNameAsync(
            string participantType,
            int participantId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            return participantType switch
            {
                "User" or "Admin" or "BranchLeader" or "Manager" or "Teacher" or "Student"
                    => await ResolveUserDisplayNameAsync(participantId, lookupCache, cancellationToken),
                "Circle"
                    => await ResolveCircleDisplayNameAsync(participantId, lookupCache, cancellationToken),
                "Subscribe"
                    => await ResolveSubscribeDisplayNameAsync(participantId, lookupCache, cancellationToken),
                "SubscribeType"
                    => await ResolveSubscribeTypeDisplayNameAsync(participantId, lookupCache, cancellationToken),
                "StudentPayment"
                    => await ResolveStudentPaymentDisplayNameAsync(participantId, lookupCache, cancellationToken),
                "CircleReport"
                    => await ResolveCircleReportDisplayNameAsync(participantId, lookupCache, cancellationToken),
                _ => null
            };
        }

        private async Task<string?> ResolveUserDisplayNameAsync(
            int? userId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.UserNames.TryGetValue(userId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.UserNames[userId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{userId.Value}" : displayName;
            return lookupCache.UserNames[userId.Value];
        }

        private async Task<string?> ResolveCircleDisplayNameAsync(
            int? circleId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!circleId.HasValue || circleId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.CircleNames.TryGetValue(circleId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await Circles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Id == circleId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.CircleNames[circleId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{circleId.Value}" : displayName;
            return lookupCache.CircleNames[circleId.Value];
        }

        private async Task<string?> ResolveSubscribeDisplayNameAsync(
            int? subscribeId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!subscribeId.HasValue || subscribeId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.SubscribeNames.TryGetValue(subscribeId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await Subscribes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.Id == subscribeId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.SubscribeNames[subscribeId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{subscribeId.Value}" : displayName;
            return lookupCache.SubscribeNames[subscribeId.Value];
        }

        private async Task<string?> ResolveSubscribeTypeDisplayNameAsync(
            int? subscribeTypeId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!subscribeTypeId.HasValue || subscribeTypeId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.SubscribeTypeNames.TryGetValue(subscribeTypeId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await SubscribeTypes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.Id == subscribeTypeId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.SubscribeTypeNames[subscribeTypeId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{subscribeTypeId.Value}" : displayName;
            return lookupCache.SubscribeTypeNames[subscribeTypeId.Value];
        }

        private async Task<string?> ResolveNationalityDisplayNameAsync(
            int? nationalityId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!nationalityId.HasValue || nationalityId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.NationalityNames.TryGetValue(nationalityId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await Nationalities
                .AsNoTracking()
                .Where(n => n.Id == nationalityId.Value)
                .Select(n => n.Name)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.NationalityNames[nationalityId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{nationalityId.Value}" : displayName;
            return lookupCache.NationalityNames[nationalityId.Value];
        }

        private async Task<string?> ResolveGovernorateDisplayNameAsync(
            int? governorateId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!governorateId.HasValue || governorateId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.GovernorateNames.TryGetValue(governorateId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var displayName = await Governorates
                .AsNoTracking()
                .Where(g => g.Id == governorateId.Value)
                .Select(g => g.Name)
                .FirstOrDefaultAsync(cancellationToken);

            lookupCache.GovernorateNames[governorateId.Value] = string.IsNullOrWhiteSpace(displayName) ? $"#{governorateId.Value}" : displayName;
            return lookupCache.GovernorateNames[governorateId.Value];
        }

        private async Task<string?> ResolveStudentPaymentDisplayNameAsync(
            int? studentPaymentId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!studentPaymentId.HasValue || studentPaymentId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.StudentPaymentNames.TryGetValue(studentPaymentId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var paymentData = await StudentPayments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.Id == studentPaymentId.Value)
                .Select(p => new
                {
                    StudentName = p.Student != null ? p.Student.FullName : null,
                    SubscribeName = p.StudentSubscribe != null ? p.StudentSubscribe.Name : null,
                    p.PaymentDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            var displayName = paymentData == null
                ? $"#{studentPaymentId.Value}"
                : BuildCompositeDisplayName(paymentData.StudentName, paymentData.SubscribeName, FormatDisplayDate(paymentData.PaymentDate));

            lookupCache.StudentPaymentNames[studentPaymentId.Value] = string.IsNullOrWhiteSpace(displayName)
                ? $"#{studentPaymentId.Value}"
                : displayName;
            return lookupCache.StudentPaymentNames[studentPaymentId.Value];
        }

        private async Task<string?> ResolveCircleReportDisplayNameAsync(
            int? circleReportId,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (!circleReportId.HasValue || circleReportId.Value <= 0)
            {
                return null;
            }

            if (lookupCache.CircleReportNames.TryGetValue(circleReportId.Value, out var cachedValue))
            {
                return cachedValue;
            }

            var reportData = await CircleReports
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.Id == circleReportId.Value)
                .Select(r => new
                {
                    CircleName = r.Circle != null ? r.Circle.Name : null,
                    StudentName = r.Student != null ? r.Student.FullName : null,
                    r.CreationTime
                })
                .FirstOrDefaultAsync(cancellationToken);

            var displayName = reportData == null
                ? $"#{circleReportId.Value}"
                : BuildCompositeDisplayName(reportData.CircleName, reportData.StudentName, FormatDisplayDate(reportData.CreationTime));

            lookupCache.CircleReportNames[circleReportId.Value] = string.IsNullOrWhiteSpace(displayName)
                ? $"#{circleReportId.Value}"
                : displayName;
            return lookupCache.CircleReportNames[circleReportId.Value];
        }

        private static string? BuildCompositeDisplayName(params string?[] parts)
        {
            var normalizedParts = parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim())
                .ToList();

            return normalizedParts.Count == 0
                ? null
                : string.Join(" - ", normalizedParts);
        }

        private static string? FormatDisplayDate(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsedDate))
            {
                return parsedDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static string? ResolveBranchDisplayName(int? branchId)
        {
            return branchId switch
            {
                1 => "الرجال",
                2 => "النساء",
                _ => null
            };
        }

        private static bool ShouldAuditEntry(EntityEntry entry)
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                return false;
            }

            if (entry.Entity == null)
            {
                return false;
            }

            return !AuditIgnoredEntityNames.Contains(entry.Metadata.ClrType.Name);
        }

        private static string? ResolveActionType(EntityEntry entry)
        {
            if (entry.State == EntityState.Added)
            {
                return "Create";
            }

            if (entry.State == EntityState.Deleted)
            {
                return "Delete";
            }

            if (entry.State != EntityState.Modified)
            {
                return null;
            }

            var isDeletedProperty = entry.Properties.FirstOrDefault(p =>
                p.Metadata.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) && p.IsModified);

            if (isDeletedProperty != null)
            {
                var oldDeletedValue = TryConvertToBool(isDeletedProperty.OriginalValue);
                var newDeletedValue = TryConvertToBool(isDeletedProperty.CurrentValue);

                if (oldDeletedValue == false && newDeletedValue == true)
                {
                    return "Delete";
                }

                if (oldDeletedValue == true && newDeletedValue == false)
                {
                    return "Restore";
                }
            }

            return "Update";
        }

        private string ResolveEntityLabel(PendingAuditEntry pendingEntry)
        {
            if (pendingEntry.EntityType.Equals(nameof(User), StringComparison.OrdinalIgnoreCase))
            {
                var roleId = TryConvertToInt(GetSnapshotValue(pendingEntry, "UserTypeId"));
                return GetUserEntityLabel(roleId);
            }

            return pendingEntry.EntityType switch
            {
                nameof(Circle) => "حلقة",
                nameof(CircleReport) => "تقرير حلقة",
                nameof(Subscribe) => "باقة",
                nameof(SubscribeType) => "نوع باقة",
                nameof(StudentSubscribe) => "اشتراك طالب",
                nameof(StudentPayment) => "دفعة طالب",
                nameof(TeacherSallary) => "راتب معلم",
                nameof(ManagerTeacher) => "ربط مشرف بمعلم",
                nameof(ManagerStudent) => "ربط مشرف بطالب",
                nameof(ManagerCircle) => "ربط مشرف بحلقة",
                _ => pendingEntry.EntityType
            };
        }

        private string? ResolveEntityDisplayName(PendingAuditEntry pendingEntry)
        {
            var preferredKeys = new[]
            {
                "FullName",
                "Name",
                "Email",
                "Mobile"
            };

            foreach (var key in preferredKeys)
            {
                var value = GetSnapshotValue(pendingEntry, key)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return pendingEntry.EntityId.HasValue ? $"#{pendingEntry.EntityId.Value}" : null;
        }

        private string BuildSummary(PendingAuditEntry pendingEntry, string? actorName)
        {
            var actor = string.IsNullOrWhiteSpace(actorName) ? "النظام" : actorName;
            var entityLabel = ResolveEntityLabel(pendingEntry);
            var entityName = string.IsNullOrWhiteSpace(pendingEntry.EntityDisplayName)
                ? entityLabel
                : $"{entityLabel} {pendingEntry.EntityDisplayName}";

            if (pendingEntry.ActionType == "Update" && pendingEntry.Changes.Count > 0)
            {
                var visibleChanges = pendingEntry.Changes
                    .Take(3)
                    .Select(BuildChangeSummary)
                    .Where(summary => !string.IsNullOrWhiteSpace(summary))
                    .ToList();

                if (visibleChanges.Count == 0)
                {
                    return $"{actor} عدّل {entityName}";
                }

                var hiddenChangesCount = pendingEntry.Changes.Count - visibleChanges.Count;
                var changesSummary = string.Join("، ", visibleChanges);

                if (hiddenChangesCount > 0)
                {
                    changesSummary = $"{changesSummary}، و{hiddenChangesCount} تغييرات أخرى";
                }

                return $"{actor} عدّل {entityName}: {changesSummary}";
            }

            return pendingEntry.ActionType switch
            {
                "Create" => $"{actor} أضاف {entityName}",
                "Delete" => $"{actor} حذف {entityName}",
                "Restore" => $"{actor} استعاد {entityName}",
                _ => $"{actor} غيّر {entityName}"
            };
        }

        private static string BuildChangeSummary(AuditChange change)
        {
            var label = string.IsNullOrWhiteSpace(change.PropertyLabel)
                ? change.PropertyName
                : change.PropertyLabel;

            var oldValue = string.IsNullOrWhiteSpace(change.OldValue) ? "فارغ" : change.OldValue.Trim();
            var newValue = string.IsNullOrWhiteSpace(change.NewValue) ? "فارغ" : change.NewValue.Trim();

            return $"{label} من {oldValue} إلى {newValue}";
        }

        private async Task<string?> ResolveActorNameAsync(CancellationToken cancellationToken)
        {
            if (!_auditUserContext.UserId.HasValue)
            {
                return null;
            }

            return await Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == _auditUserContext.UserId.Value)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static void AddParticipant(
            AuditLog auditLog,
            string participantType,
            int? participantId,
            string participantLabel,
            string? displayName)
        {
            if (!participantId.HasValue || participantId.Value <= 0)
            {
                return;
            }

            if (auditLog.Participants.Any(p => p.ParticipantType == participantType && p.ParticipantId == participantId))
            {
                return;
            }

            auditLog.Participants.Add(new AuditLogParticipant
            {
                ParticipantType = participantType,
                ParticipantId = participantId,
                ParticipantLabel = participantLabel,
                DisplayName = displayName,
                IsDeleted = false
            });
        }

        private async Task AddEntityParticipantsAsync(
            AuditLog auditLog,
            PendingAuditEntry pendingEntry,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            if (pendingEntry.EntityType.Equals(nameof(User), StringComparison.OrdinalIgnoreCase))
            {
                var roleId = TryConvertToInt(GetSnapshotValue(pendingEntry, "UserTypeId"));
                AddParticipant(auditLog, "User", pendingEntry.EntityId, "مستخدم", pendingEntry.EntityDisplayName);
                AddParticipant(auditLog, GetUserParticipantType(roleId), pendingEntry.EntityId, ResolveEntityLabel(pendingEntry), pendingEntry.EntityDisplayName);
            }
            else
            {
                var selfParticipantType = GetEntityParticipantType(pendingEntry.EntityType);
                if (!string.IsNullOrWhiteSpace(selfParticipantType))
                {
                    AddParticipant(auditLog, selfParticipantType, pendingEntry.EntityId, ResolveEntityLabel(pendingEntry), pendingEntry.EntityDisplayName);
                }
            }

            await AddIdParticipantAsync(auditLog, pendingEntry, "StudentId", "Student", "الطالب", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "TeacherId", "Teacher", "المعلم", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "ManagerId", "Manager", "المشرف", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "CircleId", "Circle", "الحلقة", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "StudentSubscribeId", "Subscribe", "الباقة", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "SubscribeTypeId", "SubscribeType", "نوع الباقة", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "StudentSubscribeTypeId", "SubscribeType", "نوع الباقة", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "StudentPaymentId", "StudentPayment", "الدفعة", lookupCache, cancellationToken);
            await AddIdParticipantAsync(auditLog, pendingEntry, "CircleReportId", "CircleReport", "التقرير", lookupCache, cancellationToken);
        }

        private async Task AddIdParticipantAsync(
            AuditLog auditLog,
            PendingAuditEntry pendingEntry,
            string propertyName,
            string participantType,
            string participantLabel,
            AuditLookupCache lookupCache,
            CancellationToken cancellationToken)
        {
            var id = TryConvertToInt(GetSnapshotValue(pendingEntry, propertyName));
            if (!id.HasValue)
            {
                return;
            }

            var displayName = await ResolveParticipantDisplayNameAsync(participantType, id.Value, lookupCache, cancellationToken);
            AddParticipant(auditLog, participantType, id, participantLabel, displayName ?? $"#{id.Value}");
        }

        private static object? GetSnapshotValue(PendingAuditEntry pendingEntry, string propertyName)
        {
            if (pendingEntry.CurrentValues.TryGetValue(propertyName, out var currentValue) && currentValue != null)
            {
                return currentValue;
            }

            return pendingEntry.OriginalValues.TryGetValue(propertyName, out var originalValue)
                ? originalValue
                : null;
        }

        private static object? NormalizeValue(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue.Trim();
            }

            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.UtcDateTime;
            }

            return value;
        }

        private static string? FormatAuditValue(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string? NormalizeAuditText(string? value)
        {
            return NormalizeValue(value) as string;
        }

        private static bool IsEmptyAuditValue(object? value)
        {
            return value == null || string.IsNullOrWhiteSpace(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static bool AreValuesEqual(object? oldValue, object? newValue)
        {
            if (oldValue == null && newValue == null)
            {
                return true;
            }

            if (oldValue == null || newValue == null)
            {
                return false;
            }

            return string.Equals(
                Convert.ToString(oldValue, CultureInfo.InvariantCulture),
                Convert.ToString(newValue, CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }

        private static int? TryConvertToInt(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static bool? TryConvertToBool(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static string GetPropertyLabel(string propertyName)
        {
            return PropertyLabels.TryGetValue(propertyName, out var label)
                ? label
                : propertyName;
        }

        private static string GetUserEntityLabel(int? roleId)
        {
            return roleId switch
            {
                1 => "إدمن",
                2 => "قائد فرع",
                3 => "مشرف",
                4 => "معلم",
                5 => "طالب",
                _ => "مستخدم"
            };
        }

        private static string GetUserParticipantType(int? roleId)
        {
            return roleId switch
            {
                1 => "Admin",
                2 => "BranchLeader",
                3 => "Manager",
                4 => "Teacher",
                5 => "Student",
                _ => "User"
            };
        }

        private static string? GetEntityParticipantType(string entityType)
        {
            return entityType switch
            {
                nameof(Circle) => "Circle",
                nameof(CircleReport) => "CircleReport",
                nameof(Subscribe) => "Subscribe",
                nameof(SubscribeType) => "SubscribeType",
                nameof(StudentPayment) => "StudentPayment",
                _ => null
            };
        }

        private sealed class PendingAuditEntry
        {
            public PendingAuditEntry(EntityEntry entry, string actionType)
            {
                Entry = entry;
                ActionType = actionType;
                EntityType = entry.Metadata.ClrType.Name;
            }

            public EntityEntry Entry { get; }
            public string ActionType { get; }
            public string EntityType { get; }
            public int? EntityId { get; set; }
            public string? EntityDisplayName { get; set; }
            public Dictionary<string, object?> OriginalValues { get; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, object?> CurrentValues { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<PropertyEntry> TemporaryProperties { get; } = new();
            public List<AuditChange> Changes { get; } = new();
        }

        private sealed class AuditChange
        {
            public string PropertyName { get; set; } = string.Empty;
            public string PropertyLabel { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
        }

        private sealed class AuditLookupCache
        {
            public Dictionary<int, string> UserNames { get; } = new();
            public Dictionary<int, string> CircleNames { get; } = new();
            public Dictionary<int, string> SubscribeNames { get; } = new();
            public Dictionary<int, string> SubscribeTypeNames { get; } = new();
            public Dictionary<int, string> NationalityNames { get; } = new();
            public Dictionary<int, string> GovernorateNames { get; } = new();
            public Dictionary<int, string> StudentPaymentNames { get; } = new();
            public Dictionary<int, string> CircleReportNames { get; } = new();
        }
    }
}
