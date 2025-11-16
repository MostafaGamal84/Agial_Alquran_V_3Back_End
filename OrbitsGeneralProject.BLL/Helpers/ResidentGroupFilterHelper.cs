using System;
using System.Collections.Generic;
using System.Linq;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;

namespace Orbits.GeneralProject.BLL.Helpers
{
    public static class ResidentGroupFilterHelper
    {
        public static ResidentGroup? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "all" => null,
                "egyptian" => ResidentGroup.Egyptian,
                "arab" => ResidentGroup.Arab,
                "foreign" => ResidentGroup.Foreign,
                _ => null
            };
        }

        public static List<int>? ResolveResidentIds(IQueryable<Nationality> query, ResidentGroup? group)
        {
            if (!group.HasValue)
                return null;

            var snapshot = query
                .Select(n => new Nationality
                {
                    Id = n.Id,
                    Name = n.Name,
                    TelCode = n.TelCode
                })
                .ToList();

            return snapshot
                .Where(n => NationalityClassificationHelper.ResolveResidentGroup(n) == group.Value)
                .Select(n => n.Id)
                .Distinct()
                .ToList();
        }
    }
}
