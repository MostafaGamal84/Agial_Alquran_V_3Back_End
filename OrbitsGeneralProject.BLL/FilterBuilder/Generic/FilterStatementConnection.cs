using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.BLL.FilterBuilder.Generic
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   A filter statement connection. </summary>
    ///
    /// <typeparam name="TClass">   Type of the class. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public class FilterStatementConnection<TClass> : IFilterStatementConnection<TClass> where TClass : class
    {
        /// <summary>   Specifies the filter. </summary>
        private readonly IFilter<TClass> _filter;

        /// <summary>   The statement. </summary>
        private readonly IFilterStatement _statement;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Constructor. </summary>
        ///
        /// <param name="filter">       Specifies the filter. </param>
        /// <param name="statement">    The statement. </param>
        ///-------------------------------------------------------------------------------------------------

        public FilterStatementConnection(IFilter<TClass> filter, IFilterStatement statement)
        {
            _filter = filter;
            _statement = statement;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Defines that the last filter statement will connect to the next one using the 'AND'
        ///     logical operator.
        /// </summary>
        ///
        /// <value> The and. </value>
        ///-------------------------------------------------------------------------------------------------

        public IFilter<TClass> And
        {
            get
            {
                _statement.Connector = FilterStatementConnector.And;
                return _filter;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Defines that the last filter statement will connect to the next one using the 'OR'
        ///     logical operator.
        /// </summary>
        ///
        /// <value> The or. </value>
        ///-------------------------------------------------------------------------------------------------

        public IFilter<TClass> Or
        {
            get
            {
                _statement.Connector = FilterStatementConnector.Or;
                return _filter;
            }
        }
    }
}
