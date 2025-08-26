using System;
using System.Collections.Generic;

namespace Orbits.GeneralProject.BLL.FilterBuilder
{

    /// <summary>   Defines how a property should be filtered. </summary>
    public interface IFilterStatement
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Establishes how this filter statement will connect to the next one. </summary>
        ///
        /// <value> The connector. </value>
        ///-------------------------------------------------------------------------------------------------

        FilterStatementConnector Connector { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Name of the property (or property chain). </summary>
        ///
        /// <value> The name of the property. </value>
        ///-------------------------------------------------------------------------------------------------

        string PropertyName { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Express the interaction between the property and the constant value defined in this
        ///     filter statement.
        /// </summary>
        ///
        /// <value> The operation. </value>
        ///-------------------------------------------------------------------------------------------------

        Operation Operation { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Constant value that will interact with the property defined in this filter statement.
        /// </summary>
        ///
        /// <value> The value. </value>
        ///-------------------------------------------------------------------------------------------------

        object Value { get; set; }
    }
}
