namespace RollbackLab.Tests;

using RollbackLab.Core;
using RollbackLab.Simulador;

public class SimuladorTests
{
    [Fact]
    public void SimuladorDeveExecutar8CenariosComFolhasCorretasETemposSimulados()
    {
        var resultados = SimuladorCenarios.ExecutarTodos();

        Assert.Equal(8, resultados.Count);

        var cA = resultados.Single(r => r.Cenario == "A");
        Assert.Equal(Folha.A, cA.Folha);
        Assert.False(cA.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(4), cA.TempoSimulado);

        var cB = resultados.Single(r => r.Cenario == "B");
        Assert.Equal(Folha.B, cB.Folha);
        Assert.False(cB.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(8), cB.TempoSimulado);

        var cC = resultados.Single(r => r.Cenario == "C");
        Assert.Equal(Folha.C, cC.Folha);
        Assert.False(cC.ParaHumano);
        Assert.Equal(TimeSpan.FromSeconds(510), cC.TempoSimulado);

        var cD = resultados.Single(r => r.Cenario == "D");
        Assert.Equal(Folha.D, cD.Folha);
        Assert.True(cD.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(1), cD.TempoSimulado);

        var cE = resultados.Single(r => r.Cenario == "E");
        Assert.Equal(Folha.E, cE.Folha);
        Assert.True(cE.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(1), cE.TempoSimulado);

        var cF = resultados.Single(r => r.Cenario == "F");
        Assert.Equal(Folha.F, cF.Folha);
        Assert.True(cF.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(1), cF.TempoSimulado);

        var cG = resultados.Single(r => r.Cenario == "G");
        Assert.Equal(Folha.G, cG.Folha);
        Assert.True(cG.ParaHumano);
        Assert.Equal(TimeSpan.FromSeconds(510), cG.TempoSimulado);

        var cH = resultados.Single(r => r.Cenario == "H");
        Assert.Equal(Folha.H, cH.Folha);
        Assert.True(cH.ParaHumano);
        Assert.Equal(TimeSpan.FromMinutes(1), cH.TempoSimulado);
    }

    [Fact]
    public void SimuladorDeveExportarCsvValido()
    {
        var resultados = SimuladorCenarios.ExecutarTodos();
        string tempCsv = Path.Combine(Path.GetTempPath(), $"teste_resultados_{Guid.NewGuid():N}.csv");

        try
        {
            SimuladorCenarios.GravarCsv(resultados, tempCsv);

            Assert.True(File.Exists(tempCsv));
            var linhas = File.ReadAllLines(tempCsv);
            Assert.Equal(9, linhas.Length);
            Assert.StartsWith("Cenario,Folha,Tipo", linhas[0]);
        }
        finally
        {
            if (File.Exists(tempCsv))
            {
                File.Delete(tempCsv);
            }
        }
    }
}
