namespace CSharpExercises.Linq;

public record Employee(string Name, string Department, decimal Salary, int Age);

/// <summary>
/// Querying, grouping and aggregating data with LINQ.
/// </summary>
public static class LinqExercises
{
    public static IEnumerable<Employee> HighEarners(IEnumerable<Employee> employees, decimal threshold) =>
        employees.Where(e => e.Salary > threshold).OrderByDescending(e => e.Salary);

    public static Dictionary<string, decimal> AverageSalaryByDepartment(IEnumerable<Employee> employees) =>
        employees.GroupBy(e => e.Department)
                 .ToDictionary(g => g.Key, g => Math.Round(g.Average(e => e.Salary), 2));

    public static Employee? OldestInDepartment(IEnumerable<Employee> employees, string department) =>
        employees.Where(e => e.Department == department).MaxBy(e => e.Age);

    public static IEnumerable<string> DepartmentsWithAtLeast(IEnumerable<Employee> employees, int minCount) =>
        employees.CountBy(e => e.Department)
                 .Where(kv => kv.Value >= minCount)
                 .Select(kv => kv.Key)
                 .Order();

    public static IEnumerable<int[]> Paginate(IEnumerable<int> source, int pageSize) =>
        source.Chunk(pageSize);
}
