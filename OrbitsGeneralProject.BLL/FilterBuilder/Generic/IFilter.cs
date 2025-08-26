using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Orbits.GeneralProject.BLL.FilterBuilder.Generic
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Defines a filter from which a expression will be built. </summary>
    ///
    /// <typeparam name="TClass">   Type of the class. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IFilter<TClass> where TClass : class
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Group of statements that compose this filter. </summary>
        ///
        /// <value> The statements. </value>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<IFilterStatement> Statements { get; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Adds another statement to this filter. </summary>
        ///
        /// <typeparam name="TPropertyType">    Type of the property type. </typeparam>
        /// <param name="propertyName"> Name of the property that will be filtered. </param>
        /// <param name="operation">    Express the interaction between the property and the constant
        ///                             value. </param>
        /// <param name="value">        Constant value that will interact with the property. </param>
        /// <param name="connector">    (Optional) Establishes how this filter statement will connect to
        ///                             the next one. </param>
        ///
        /// <returns>
        ///     A FilterStatementConnection object that defines how this statement will be connected to
        ///     the next one.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        IFilterStatementConnection<TClass> By<TPropertyType>(string propertyName, Operation operation, TPropertyType value, FilterStatementConnector connector = FilterStatementConnector.And);

        /// <summary>   Removes all statements from this filter. </summary>
        void Clear();

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Builds a LINQ expression based upon the statements included in this filter.
        /// </summary>
        ///
        /// <returns>   An Expression&lt;Func&lt;TClass,bool&gt;&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        Expression<Func<TClass, bool>> BuildExpression();
    }
}
