
using System.Linq;

namespace Orbits.GeneralProject.Repositroy.Base
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   A list paged. </summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public class ListPaged<T> : List<T> where T : class
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the number of results. </summary>
        ///
        /// <value> The number of results. </value>
        ///-------------------------------------------------------------------------------------------------

        public int ResultCount { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the result. </summary>
        ///
        /// <value> The result. </value>
        ///-------------------------------------------------------------------------------------------------

        public IEnumerable<T> Result { get; set; }
    }


    public class QueryPaged<T>  where T : class
    {

        public int ResultCount { get; set; }

   
        public IQueryable<T> Query { get; set; }
    }
}
