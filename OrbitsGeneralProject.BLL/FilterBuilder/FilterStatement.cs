using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.BLL.FilterBuilder
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   A filter statement. </summary>
    ///
    /// <typeparam name="TPropertyType">    Type of the property type. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public class FilterStatement<TPropertyType> : IFilterStatement
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Establishes how this filter statement will connect to the next one. </summary>
        ///
        /// <value> The connector. </value>
        ///-------------------------------------------------------------------------------------------------

        public FilterStatementConnector Connector { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Name of the property (or property chain). </summary>
        ///
        /// <value> The name of the property. </value>
        ///-------------------------------------------------------------------------------------------------

        public string PropertyName { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Express the interaction between the property and the constant value defined in this
        ///     filter statement.
        /// </summary>
        ///
        /// <value> The operation. </value>
        ///-------------------------------------------------------------------------------------------------

        public Operation Operation { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Constant value that will interact with the property defined in this filter statement.
        /// </summary>
        ///
        /// <value> The value. </value>
        ///-------------------------------------------------------------------------------------------------

        public object Value { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Constructor. </summary>
        ///
        /// <exception cref="ArgumentException">    Thrown when one or more arguments have unsupported or
        ///                                         illegal values. </exception>
        ///
        /// <param name="propertyName"> Name of the property. </param>
        /// <param name="operation">    The operation. </param>
        /// <param name="value">        The value. </param>
        /// <param name="connector">    (Optional) The connector. </param>
        ///-------------------------------------------------------------------------------------------------

        public FilterStatement(string propertyName, Operation operation, TPropertyType value, FilterStatementConnector connector = FilterStatementConnector.And)
        {
            PropertyName = propertyName;
            Connector = connector;
            Operation = operation;
            if (typeof(TPropertyType).IsArray)
            {
                if (operation != Operation.Contains) throw new ArgumentException("Only 'Operacao.Contains' supports arrays as parameters.");
                var listType = typeof(List<>);
                var constructedListType = listType.MakeGenericType(typeof(TPropertyType).GetElementType());
                Value = Activator.CreateInstance(constructedListType, value);
            }
            else
            {
                Value = value;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ///-------------------------------------------------------------------------------------------------

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", PropertyName, Operation, Value);
        }
    }
}
