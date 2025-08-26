using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.Core.Paging
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Interface for paged list. </summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IPagedList<T> where T : class
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the number of results. </summary>
        ///
        /// <value> The number of results. </value>
        ///-------------------------------------------------------------------------------------------------

        int ResultCount { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the result. </summary>
        ///
        /// <value> The result. </value>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<T> Result { get; set; }
    }

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

        public IEnumerable<T>? Result { get; set; }
    }
}
