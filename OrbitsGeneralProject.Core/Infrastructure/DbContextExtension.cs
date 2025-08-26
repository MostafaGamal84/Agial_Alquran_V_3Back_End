using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

using Orbits.GeneralProject.Core.Paging;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using DbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace Orbits.GeneralProject.Core.Infrastructure
{


    public static class DbContextExtension
    {
        public static MultipleResultSetWrapper MultipleResults(this DbContext db, string storedProcedure)
        {
            return new MultipleResultSetWrapper(db, storedProcedure);
        }



    }

    /// <summary>
    /// it's reference https://khalidabuhakmeh.com/entity-framework-6-multiple-result-sets-with-stored-procedures
    /// </summary>
    public class MultipleResultSetWrapper
    {
        private DbContext _db;
        private readonly string _storedProcedure;
        public List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>> _resultSets;

        public MultipleResultSetWrapper(DbContext db, string storedProcedure)
        {
            _db = db;
            _storedProcedure = storedProcedure;
            _resultSets = new List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>>();
        }

        public MultipleResultSetWrapper With<TResult>()
        {
            _resultSets.Add((adapter, reader) => adapter.ObjectContext.Translate<TResult>(reader).ToList());
            return this;
        }


        public List<IEnumerable> Execute(List<Type> lstDtos, Hashtable parameters)
        {
            var results = new List<IEnumerable>();

            //using (var connection = _db.Database.GetDbConnection())
            //{
            var connection = _db.Database.GetDbConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandTimeout = 0;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = _storedProcedure;
            if (parameters != null && parameters.Count > 0)
            {
                foreach (DictionaryEntry entry in parameters)
                {
                    DbParameter dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = entry.Key.ToString();
                    dbParameter.Value = entry.Value;
                    command.Parameters.Add(dbParameter);
                }
            }

            using (var reader = command.ExecuteReader())
            {
                foreach (var typ in lstDtos)
                {
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(typ);
                    var instanceOfList = Activator.CreateInstance(constructedListType);

                    while (reader.Read())
                    {
                        var obj = Activator.CreateInstance(typ);
                        foreach (PropertyInfo prop in typ.GetProperties())
                        {

                            //if (!object.Equals(reader[prop.Name], DBNull.Value))
                            //{
                            prop.SetValue(obj, reader[prop.Name]);
                            //}
                        }
                        instanceOfList.GetType().GetMethod("Add").Invoke(instanceOfList, new[] { obj });
                    }
                    results.Add((IEnumerable)instanceOfList);

                    reader.NextResult();
                }

                //}
                connection.Close();
                return results;


            }
        }
    }
}
