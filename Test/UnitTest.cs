using Orbits.GeneralProject.Core.Entities;

namespace Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void ManagerIntersection_WithTwoManagers_ReturnsOnlyCommonTeachers()
        {
            var managerIds = new[] { 1106, 1107 };
            var links = new List<ManagerTeacher>
            {
                new() { ManagerId = 1106, TeacherId = 1 },
                new() { ManagerId = 1106, TeacherId = 2 },
                new() { ManagerId = 1107, TeacherId = 2 },
                new() { ManagerId = 1107, TeacherId = 3 }
            }.AsQueryable();

            var teacherIdsCommon = links
                .Where(mt => mt.ManagerId.HasValue
                             && managerIds.Contains(mt.ManagerId.Value)
                             && mt.TeacherId.HasValue)
                .GroupBy(mt => mt.TeacherId!.Value)
                .Where(g => g.Select(x => x.ManagerId!.Value).Distinct().Count() == managerIds.Length)
                .Select(g => g.Key)
                .ToList();

            CollectionAssert.AreEquivalent(new List<int> { 2 }, teacherIdsCommon);
        }

        [TestMethod]
        public void ManagerIntersection_WithSingleManager_MatchesSingleManagerBehavior()
        {
            int managerId = 1106;
            var managerIds = new[] { managerId };
            var links = new List<ManagerTeacher>
            {
                new() { ManagerId = 1106, TeacherId = 1 },
                new() { ManagerId = 1106, TeacherId = 2 },
                new() { ManagerId = 1107, TeacherId = 2 },
            }.AsQueryable();

            var oldBehavior = links
                .Where(mt => mt.ManagerId == managerId && mt.TeacherId.HasValue)
                .Select(mt => mt.TeacherId!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var newBehaviorSingle = links
                .Where(mt => mt.ManagerId.HasValue
                             && managerIds.Contains(mt.ManagerId.Value)
                             && mt.TeacherId.HasValue)
                .GroupBy(mt => mt.TeacherId!.Value)
                .Where(g => g.Select(x => x.ManagerId!.Value).Distinct().Count() == managerIds.Length)
                .Select(g => g.Key)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            CollectionAssert.AreEqual(oldBehavior, newBehaviorSingle);
        }

        [TestMethod]
        public void ManagerIntersection_WithNoManagers_AllTeachersPassAndPagingWorks()
        {
            var teachers = Enumerable.Range(1, 5).ToList();
            var hasManagersFilter = false;
            var commonTeacherIds = new List<int>();

            var filtered = teachers
                .Where(id => !hasManagersFilter || commonTeacherIds.Contains(id))
                .OrderByDescending(id => id)
                .Skip(1)
                .Take(2)
                .ToList();

            CollectionAssert.AreEqual(new List<int> { 4, 3 }, filtered);
        }
    }
}
