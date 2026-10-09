namespace RollbackLab.Tests;

using RollbackLab.Core;
using Xunit.Abstractions;

public class ArvoreDecisaoTests
{
    private readonly ITestOutputHelper _output;

    public ArvoreDecisaoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<Estado> Obter256Estados()
    {
        var bools = new[] { false, true };
        var migracoes = (Migracao[])Enum.GetValues(typeof(Migracao));

        foreach (var correlaciona in bools)
        foreach (var migracao in migracoes)
        foreach (var temFlag in bools)
        foreach (var flagResolve in bools)
        foreach (var imagemExiste in bools)
        foreach (var pedidosCompativeis in bools)
        foreach (var rollbackRecuperou in bools)
        {
            yield return new Estado(
                correlaciona,
                migracao,
                temFlag,
                flagResolve,
                imagemExiste,
                pedidosCompativeis,
                rollbackRecuperou);
        }
    }

    [Fact]
    public void ValidarInvariantesCompletos256Combinacoes()
    {
        var estados = Obter256Estados().ToList();
        Assert.Equal(256, estados.Count);

        var contagemPorFolha = new Dictionary<Folha, int>();
        foreach (Folha f in Enum.GetValues(typeof(Folha)))
        {
            contagemPorFolha[f] = 0;
        }

        foreach (var e in estados)
        {
            var d = ArvoreDecisao.Decidir(e);
            contagemPorFolha[d.Folha]++;

            Assert.NotNull(d);
            Assert.False(string.IsNullOrWhiteSpace(d.Motivo), $"Motivo não pode ser vazio para estado: {e}");

            bool condicaoBC = e.CorrelacionaComDeploy
                && (e.Migracao == Migracao.Nenhuma || e.Migracao == Migracao.Aditiva)
                && e.ImagemExiste
                && e.PedidosCompativeis
                && e.RollbackRecuperou
                && !(e.TemFlag && e.FlagResolve);

            if (d.Folha is Folha.B or Folha.C)
            {
                Assert.True(condicaoBC, $"Folha {d.Folha} ocorreu sem atender à condição necessária: {e}");
            }
            if (condicaoBC)
            {
                Assert.True(d.Folha is Folha.B or Folha.C, $"Estado atendia a BC mas resultou em folha {d.Folha}: {e}");
            }

            if (e.CorrelacionaComDeploy && e.Migracao is Migracao.Destrutiva or Migracao.Desconhecida)
            {
                Assert.Equal(Folha.D, d.Folha);
            }

            if (!e.CorrelacionaComDeploy)
            {
                Assert.Equal(Folha.H, d.Folha);
            }

            if (d.Folha is Folha.D or Folha.E or Folha.F or Folha.G or Folha.H)
            {
                Assert.True(d.ParaHumano, $"Folha {d.Folha} deveria ser ParaHumano=true");
            }
            else
            {
                Assert.False(d.ParaHumano, $"Folha {d.Folha} deveria ser ParaHumano=false");
            }

            if (d.Folha == Folha.B)
            {
                Assert.Equal(Migracao.Nenhuma, e.Migracao);
            }
            if (d.Folha == Folha.C)
            {
                Assert.Equal(Migracao.Aditiva, e.Migracao);
            }

            if (d.Acoes.Contains(Acao.ExecutarRollback))
            {
                Assert.True(d.Folha is Folha.B or Folha.C or Folha.G, $"ExecutarRollback não permitido na folha {d.Folha}");
            }
            if (d.Folha is Folha.D or Folha.E or Folha.F or Folha.H)
            {
                Assert.DoesNotContain(Acao.ExecutarRollback, d.Acoes);
            }
        }

        foreach (Folha f in Enum.GetValues(typeof(Folha)))
        {
            Assert.True(contagemPorFolha[f] > 0, $"A folha {f} não apareceu em nenhuma combinação.");
        }

        _output.WriteLine("=================================================");
        _output.WriteLine($"Total de combinações avaliadas: {estados.Count}");
        _output.WriteLine("Contagem de decisões por folha:");
        foreach (Folha f in Enum.GetValues(typeof(Folha)).Cast<Folha>().OrderBy(x => x.ToString()))
        {
            _output.WriteLine($"  Folha {f}: {contagemPorFolha[f]} combinações");
        }
        _output.WriteLine("=================================================");
    }
}
