using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.BLL.FilterBuilder.Generic
{

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   A filter. </summary>
    ///
    /// <typeparam name="TClass">   Type of the class. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public class Filter<TClass> : IFilter<TClass> where TClass : class
    {
        /// <summary>   The statements. </summary>
        private readonly List<IFilterStatement> _statements;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Group of statements that compose this filter. </summary>
        ///
        /// <value> The statements. </value>
        ///-------------------------------------------------------------------------------------------------

        public IEnumerable<IFilterStatement> Statements
        {
            get
            {
                return _statements.ToArray();
            }
        }

        /// <summary>   Default constructor. </summary>
        public Filter()
        {
            _statements = new List<IFilterStatement>();
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Adds another statement to this filter. </summary>
        ///
        /// <typeparam name="TPropertyType">    Type of the property type. </typeparam>
        /// <param name="propertyName"> Name of the property that will be filtered. </param>
        /// <param name="operation">    Express the interaction between the property and the constant
        ///                                                          value. </param>
        /// <param name="value">        Constant value that will interact with the property. </param>
        /// <param name="connector">    (Optional) Establishes how this filter statement will connect to
        ///                                                          the next one. </param>
        ///
        /// <returns>
        ///     A FilterStatementConnection object that defines how this statement will be connected to
        ///     the next one.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        public IFilterStatementConnection<TClass> By<TPropertyType>(string propertyName, Operation operation, TPropertyType value, FilterStatementConnector connector = FilterStatementConnector.And)
        {
            IFilterStatement statement = null;
            statement = new FilterStatement<TPropertyType>(propertyName, operation, value, connector);
            _statements.Add(statement);
            return new FilterStatementConnection<TClass>(this, statement);
        }

        /// <summary>   Removes all statements from this filter. </summary>
        public void Clear()
        {
            _statements.Clear();
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Builds a LINQ expression based upon the statements included in this filter.
        /// </summary>
        ///
        /// <returns>   An Expression&lt;Func&lt;TClass,bool&gt;&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        public System.Linq.Expressions.Expression<Func<TClass, bool>> BuildExpression()
        {
            return Builder.GetExpression<TClass>(this);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ///-------------------------------------------------------------------------------------------------

        public override string ToString()
        {
            var result = "";
            FilterStatementConnector lastConector = FilterStatementConnector.And;
            foreach (var statement in _statements)
            {
                if (!string.IsNullOrWhiteSpace(result)) result += " " + lastConector + " ";
                result += statement.ToString();
                lastConector = statement.Connector;
            }

            return result.Trim();
        }
    }
}
