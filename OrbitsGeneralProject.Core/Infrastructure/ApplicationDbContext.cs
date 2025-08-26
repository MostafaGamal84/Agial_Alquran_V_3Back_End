using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Extensions;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

using System.Data.Common;
using System.Reflection;
using Orbits.GeneralProject.Core.Entities;
using System.Drawing;
using System.Data.Entity.SqlServer;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Orbits.GeneralProject.Core.Infrastructure
{
    public class ApplicationDbContext : OrbitsContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbConnection connection)
        {
            // this.Database.SetDbConnection(connection);
        }

        //public ApplicationDbContext( DbContextOptions<MyDexefContext> options ) : base(options)
        //{
        //}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.AddInterceptors(new AuditLogInterceptor());
            //optionsBuilder.UseLoggerFactory()
            optionsBuilder.UseExceptionProcessor();
            optionsBuilder.UseLazyLoadingProxies();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var jsonvalueMethodInfo = typeof(Json).GetRuntimeMethod(nameof(Json.Value), new[] { typeof(string), typeof(string) });
            var translatevalueMethodInfo = typeof(Translate).GetRuntimeMethod(nameof(Translate.Value), new[] { typeof(int), typeof(string), typeof(int) });

            modelBuilder.HasDbFunction(jsonvalueMethodInfo).HasTranslation(args => SqlFunctionExpression.Create("JSON_VALUE", args, typeof(string), null));
            modelBuilder.HasDbFunction(translatevalueMethodInfo).HasTranslation(args => SqlFunctionExpression.Create("dbo.GetValueTranslate", args, typeof(string), null));

            modelBuilder.HasDbFunction(() => GetDayName(default))
             .HasTranslation(args =>
             {
                 var dateArg = args.First();
                 return new SqlFunctionExpression("DATENAME",
                     new[]
                     {
                        new SqlFragmentExpression("weekday"), // datepart
                        dateArg
                     },
                     true,
                     new[] { false, false },
                     typeof(string),
                     null
                 );
             });

            //FORMAT
            /*modelBuilder.HasDbFunction(() => GetHigriMonth(default))
             .HasTranslation(args =>
             {
                 var dateArg = args.First();
                 return new SqlFunctionExpression("FORMAT",
                     new[]
                     {
                        dateArg,
                        new SqlFragmentExpression("'MM'"),
                        new SqlFragmentExpression ("'ar-SA'")
                     },
                     false,
                     new[] { false, false, false },
                     typeof(string),
                     null
                 );
             });*/

            /*modelBuilder.HasDbFunction(() => GetHigriDay(default))
             .HasTranslation(args =>
             {
                 var dateArg = args.First();
                 return new SqlFunctionExpression("FORMAT",
                     new[]
                     {
                        dateArg,
                        new SqlFragmentExpression("'dd'"),
                        new SqlFragmentExpression ("'ar-SA'")
                     },
                     false,
                     new[] { false, false, false },
                     typeof(string),
                     null
                 );
             });*/

            modelBuilder.HasDbFunction(
            methodInfo: typeof(ApplicationDbContext)
            .GetMethod(nameof(ApplicationDbContext.GetHigriDate)),
            builderAction: f => f.HasTranslation(args =>
            {
                var dateArg = args.First();
                var formatArg = args.Skip(1).First();

                return new SqlFunctionExpression("FORMAT",
                     new[]
                     {
                        dateArg,
                        formatArg,
                        new SqlFragmentExpression ("'ar-SA'")
                     },
                     false,
                     new[] { false, false, false },
                     typeof(string),
                     null
                 );
            }));

            modelBuilder.HasDbFunction(() => CastToInt(default))
            .HasTranslation(args =>
            {
                var dateArg = args.First();
                return new SqlFunctionExpression("CAST",
                    new[]
                    {
                        new SqlFragmentExpression($"'{dateArg} AS INTEGER'"),
                    },
                    false,
                    new[] { false},
                    typeof(int),
                    null
                );
            });




            #region Ignore Dynamic Variables for not existing fields


            modelBuilder.Entity<Student>().Ignore(f => f.IsDeleted); 
            modelBuilder.Entity<Teacher>().Ignore(f => f.IsDeleted);
            modelBuilder.Entity<RefreshToken>().Ignore(f => f.IsDeleted);



            #endregion Ignore Dynamic Variables for not existing fields
            #region Apply Global Filters

            //if (!Database.GetDbConnection().GetType().Name.StartsWith("Effort", StringComparison.Ordinal))
            //{
            //TODO:UnComment this

            modelBuilder.ApplyGlobalFilters<IEntityBase>(e => e.IsDeleted == false, "IsDeleted");
            //}

            #endregion

            base.OnModelCreating(modelBuilder);
        }

        [DbFunction(Schema = "dbo")]
        public static string GetDayName(DateTime dateTime)
        {
            // The implementation here doesn't contain the logic for getting the day name
            // as it will be executed in the database.
            throw new NotSupportedException("This method is for database mapping only and should not be called directly.");
            //return "DUMMY";
        }

        [DbFunction(Schema = "dbo")]
        public static string GetHigriMonth(DateTime dateTime)
        {
            // The implementation here doesn't contain the logic for getting the day name
            // as it will be executed in the database.
            throw new NotSupportedException("This method is for database mapping only and should not be called directly.");
            //return "DUMMY";
        }
        
        [DbFunction(Schema = "dbo")]
        public static string GetHigriDate(DateTime dateTime, string format)
        {
            // The implementation here doesn't contain the logic for getting the day name
            // as it will be executed in the database.
            throw new NotSupportedException("This method is for database mapping only and should not be called directly.");
            //return "DUMMY";
        }

        [DbFunction(Schema = "dbo")]
        public static string GetHigriDay(DateTime dateTime)
        {
            // The implementation here doesn't contain the logic for getting the day name
            // as it will be executed in the database.
            throw new NotSupportedException("This method is for database mapping only and should not be called directly.");
            //return "DUMMY";
        }

        [DbFunction(Schema = "dbo")]
        public static int CastToInt(string daynum)
        {
            // The implementation here doesn't contain the logic for getting the day name
            // as it will be executed in the database.
            throw new NotSupportedException("This method is for database mapping only and should not be called directly.");
            //return "DUMMY";
        }

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
            return base.SaveChanges();
        }
    }
}
