using CSharpExercises.Chemistry;

namespace CSharpExercises.Tests;

public class ChemistryTests
{
    [Fact]
    public void Formula_ParsesGroupsAndHydrates()
    {
        Assert.Equal(new Dictionary<string, int> { ["Ca"] = 1, ["O"] = 2, ["H"] = 2 }, ChemicalFormula.Parse("Ca(OH)2"));
        Assert.Equal(new Dictionary<string, int> { ["K"] = 4, ["Fe"] = 1, ["C"] = 6, ["N"] = 6 }, ChemicalFormula.Parse("K4[Fe(CN)6]"));
        Assert.Equal(9, ChemicalFormula.Parse("CuSO4·5H2O")["O"]);
        Assert.Equal(ChemicalFormula.Parse("CuSO4·5H2O"), ChemicalFormula.Parse("CuSO4*5H2O"));
    }

    [Fact]
    public void Formula_CaseMatters()
    {
        Assert.Equal(["Co"], ChemicalFormula.Parse("Co").Keys);
        Assert.Equal(["C", "O"], ChemicalFormula.Parse("CO").Keys);
        Assert.Throws<FormatException>(() => ChemicalFormula.Parse("Xx2"));
        Assert.Throws<FormatException>(() => ChemicalFormula.Parse("Ca(OH2"));
        Assert.Throws<FormatException>(() => ChemicalFormula.Parse("H2O)"));
    }

    [Theory]
    [InlineData("H2O", 18.015)]
    [InlineData("C6H12O6", 180.156)]
    [InlineData("NaCl", 58.44)]
    [InlineData("CuSO4·5H2O", 249.677)]
    public void Formula_MolarMass(string formula, double grams)
    {
        Assert.Equal(grams, ChemicalFormula.MolarMass(formula), 2);
    }

    [Fact]
    public void Formula_MassFractionsSumToOne()
    {
        var water = ChemicalFormula.MassFractions("H2O");

        Assert.Equal(0.888, water["O"], 3);
        Assert.Equal(1, water.Values.Sum(), 9);
    }

    [Theory]
    [InlineData("H2 + O2 -> H2O", "2H2 + O2 -> 2H2O")]
    [InlineData("CH4 + O2 -> CO2 + H2O", "CH4 + 2O2 -> CO2 + 2H2O")]
    [InlineData("Fe + O2 -> Fe2O3", "4Fe + 3O2 -> 2Fe2O3")]
    [InlineData("C3H8 + O2 -> CO2 + H2O", "C3H8 + 5O2 -> 3CO2 + 4H2O")]
    [InlineData("KMnO4 + HCl -> KCl + MnCl2 + H2O + Cl2", "2KMnO4 + 16HCl -> 2KCl + 2MnCl2 + 8H2O + 5Cl2")]
    [InlineData("Ca(OH)2 + H3PO4 -> Ca3(PO4)2 + H2O", "3Ca(OH)2 + 2H3PO4 -> Ca3(PO4)2 + 6H2O")]
    public void Balancer_FindsTheSmallestWholeNumbers(string equation, string balanced)
    {
        Assert.Equal(balanced, EquationBalancer.Balance(equation));
    }

    [Fact]
    public void Balancer_RejectsImpossibleAndAmbiguousEquations()
    {
        Assert.Throws<InvalidOperationException>(() => EquationBalancer.Balance("H2 -> O2"));
        Assert.Throws<InvalidOperationException>(() => EquationBalancer.Balance("H2 + O2 -> H2O + H2O2"));
        Assert.Throws<FormatException>(() => EquationBalancer.Balance("H2 + O2 = H2O"));
    }
}
