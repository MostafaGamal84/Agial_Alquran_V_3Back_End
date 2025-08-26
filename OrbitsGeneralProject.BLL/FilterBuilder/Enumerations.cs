namespace Orbits.GeneralProject.BLL.FilterBuilder
{
    /// <summary>   Values that represent filter statement connectors. </summary>
    public enum FilterStatementConnector
    { And, Or }

    /// <summary>   Values that represent operations. </summary>
    public enum Operation
    {
        /// <summary>   An enum constant representing the equals option. </summary>
        Equals,

        /// <summary>   An enum constant representing the contains option. </summary>
        Contains,

        /// <summary>   An enum constant representing the not contains option. </summary>
        NotContains,

        /// <summary>   An enum constant representing the starts with option. </summary>
        StartsWith,

        /// <summary>   An enum constant representing the ends with option. </summary>
        EndsWith,

        /// <summary>   An enum constant representing the not equals option. </summary>
        NotEquals,

        /// <summary>   An enum constant representing the greater than option. </summary>
        GreaterThan,

        /// <summary>   An enum constant representing the greater than or equals option. </summary>
        GreaterThanOrEquals,

        /// <summary>   An enum constant representing the less than option. </summary>
        LessThan,

        /// <summary>   An enum constant representing the less than or equals option. </summary>
        LessThanOrEquals
    }
}