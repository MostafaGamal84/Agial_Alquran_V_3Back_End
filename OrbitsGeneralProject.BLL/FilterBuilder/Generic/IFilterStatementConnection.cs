using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.BLL.FilterBuilder.Generic
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Interface for filter statement connection. </summary>
    ///
    /// <typeparam name="TClass">   Type of the class. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IFilterStatementConnection<TClass> where TClass : class
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Defines that the last filter statement will connect to the next one using the 'AND'
        ///     logical operator.
        /// </summary>
        ///
        /// <value> The and. </value>
        ///-------------------------------------------------------------------------------------------------

        IFilter<TClass> And { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Defines that the last filter statement will connect to the next one using the 'OR'
        ///     logical operator.
        /// </summary>
        ///
        /// <value> The or. </value>
        ///-------------------------------------------------------------------------------------------------

        IFilter<TClass> Or { get; }
    }
}
