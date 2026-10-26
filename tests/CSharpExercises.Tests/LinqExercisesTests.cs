using CSharpExercises.Linq;

namespace CSharpExercises.Tests;

public class LinqExercisesTests
{
    private static readonly Employee[] Employees =
    [
        new("Ana", "Engineering", 5200, 34),
        new("Marko", "Engineering", 4800, 41),
        new("Elena", "Sales", 3100, 29),
        new("Ivan", "Sales", 3500, 52),
        new("Sara", "HR", 2900, 38)
    ];

    [Fact]
    public void HighEarners_AreSortedDescending() =>
        Assert.Equal(["Ana", "Marko", "Ivan"], LinqExercises.HighEarners(Employees, 3200).Select(e => e.Name));

    [Fact]
    public void AverageSalaryByDepartment()
    {
        var result = LinqExercises.AverageSalaryByDepartment(Employees);

        Assert.Equal(5000m, result["Engineering"]);
        Assert.Equal(3300m, result["Sales"]);
        Assert.Equal(2900m, result["HR"]);
    }

    [Fact]
    public void OldestInDepartment()
    {
        Assert.Equal("Ivan", LinqExercises.OldestInDepartment(Employees, "Sales")?.Name);
        Assert.Null(LinqExercises.OldestInDepartment(Employees, "Marketing"));
    }

    [Fact]
    public void DepartmentsWithAtLeast() =>
        Assert.Equal(["Engineering", "Sales"], LinqExercises.DepartmentsWithAtLeast(Employees, 2));

    [Fact]
    public void Paginate_LastPageMayBeShorter()
    {
        var pages = LinqExercises.Paginate(Enumerable.Range(1, 7), 3).ToList();

        Assert.Equal(3, pages.Count);
        Assert.Equal([7], pages[^1]);
    }
}
