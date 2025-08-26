using Orbits.GeneralProject.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Core.Infrastructure
{
    public interface IUnitOfWork
    {
        
        /// <summary>   Commits this object. </summary>
        void Commit();
        Task SaveChanges();

        Task<int> CommitAsync();
       
    }
}
