using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.DBQueriesDto
{
    public class CheckDto
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the has children. </summary>
        ///
        /// <value> If 0 entity doesn't have Children, else entity has Children. </value>
        ///-------------------------------------------------------------------------------------------------

        public int HasChildren { get; set; }
    }
}
