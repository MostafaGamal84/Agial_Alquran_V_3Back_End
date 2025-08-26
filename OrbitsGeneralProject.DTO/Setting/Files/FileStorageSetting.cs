using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.DTO.Setting.Files
{
    public class FileStorageSetting
    {
        public string CompanyPath { get; set; }
        public string OrganizationPathPrefix { get; set; }
        public string BranchPath { get; set; }  
       public string OrganizationsContainerName { get; set; }

        public string BaseCompanyPath { get; set; }
        public string TaxPath { get; set; }
        public string FileStorageBaseUrlPath { get; set; }
    }
}
