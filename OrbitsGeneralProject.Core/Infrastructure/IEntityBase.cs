using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Core.Infrastructure
{
    public interface IEntityBase
    {
        //public int? TranslationHeaderId { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets a value indicating whether the inactive. </summary>
        ///
        /// <value> True if inactive, false if not. </value>
        ///-------------------------------------------------------------------------------------------------
       


        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets a value indicating whether this object is deleted. </summary>
        ///
        /// <value> True if this object is deleted, false if not. </value>
        ///-------------------------------------------------------------------------------------------------
        bool IsDeleted { get; set; }
    }
}
