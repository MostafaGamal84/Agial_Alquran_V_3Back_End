using Orbits.GeneralProject.Repositroy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Repositroy.Extentions
{
    public static class ExceptionSqlHandler
    {
        public static string FormatSqlErrorMessage(this string errorMessage, SqlExceptionEnum sqlExceptionEnum)
        {
            var resmessage = "";
            switch (sqlExceptionEnum)
            {
                case SqlExceptionEnum.ReferenceNotFound:
                    var column = errorMessage.Substring(errorMessage.IndexOf("column '") + 8, errorMessage.IndexOf("'", errorMessage.IndexOf("column '") + 9) - errorMessage.IndexOf("column '") - 8);
                    var table = errorMessage.Substring(errorMessage.IndexOf("table \"") + 7, errorMessage.IndexOf("\"", errorMessage.IndexOf("table \"") + 8) - errorMessage.IndexOf("table \"") - 7);
                    table = table.IndexOf('.')>-1? table.Split('.')[1]: table;
                    resmessage =string.Format("Failed :the submitted {0} {1} value has no refrence record on table  {0} .",  table, column);
                    break;
                default:
                    break;
            }
            return resmessage;
        }

    }
}


