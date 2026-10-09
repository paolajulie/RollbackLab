namespace RollbackLab.Tests;

using RollbackLab.Core;

public class CenariosTests
{
    public static IEnumerable<object[]> ObterCenarios()
    {
        yield return new object[]
        {
            "Cenário A",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Nenhuma,
                TemFlag: true,
                FlagResolve: true,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: false),
            Folha.A,
            false
        };

        yield return new object[]
        {
            "Cenário B",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Nenhuma,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: true),
            Folha.B,
            false
        };

        yield return new object[]
        {
            "Cenário C",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Aditiva,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: true),
            Folha.C,
            false
        };

        yield return new object[]
        {
            "Cenário D",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Destrutiva,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: false),
            Folha.D,
            true
        };

        yield return new object[]
        {
            "Cenário E",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Nenhuma,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: false,
                PedidosCompativeis: true,
                RollbackRecuperou: false),
            Folha.E,
            true
        };

        yield return new object[]
        {
            "Cenário F",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Nenhuma,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: false,
                RollbackRecuperou: false),
            Folha.F,
            true
        };

        yield return new object[]
        {
            "Cenário G",
            new Estado(
                CorrelacionaComDeploy: true,
                Migracao: Migracao.Nenhuma,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: false),
            Folha.G,
            true
        };

        yield return new object[]
        {
            "Cenário H",
            new Estado(
                CorrelacionaComDeploy: false,
                Migracao: Migracao.Nenhuma,
                TemFlag: false,
                FlagResolve: false,
                ImagemExiste: true,
                PedidosCompativeis: true,
                RollbackRecuperou: false),
            Folha.H,
            true
        };
    }

    [Theory]
    [MemberData(nameof(ObterCenarios))]
    public void ValidarCenario(string nomeCenario, Estado estado, Folha folhaEsperada, bool paraHumanoEsperado)
    {
        Assert.False(string.IsNullOrWhiteSpace(nomeCenario));

        var decisaoArvore = ArvoreDecisao.Decidir(estado);
        Assert.True(folhaEsperada == decisaoArvore.Folha, $"{nomeCenario}: esperado {folhaEsperada}, obtido {decisaoArvore.Folha}");
        Assert.Equal(paraHumanoEsperado, decisaoArvore.ParaHumano);
        Assert.False(string.IsNullOrWhiteSpace(decisaoArvore.Motivo));

        var fakes = FabricaDeFakes.APartirDe(estado);
        var controlador = new Controlador(
            fakes.Correlacao,
            fakes.Implantador,
            fakes.Manifesto,
            fakes.Flags,
            fakes.Metricas,
            fakes.Registry,
            fakes.Pedidos,
            fakes.Notificador,
            fakes.Relogio);

        var decisaoControlador = controlador.Executar();
        Assert.True(folhaEsperada == decisaoControlador.Folha, $"{nomeCenario}: controlador esperado {folhaEsperada}, obtido {decisaoControlador.Folha}");
        Assert.Equal(paraHumanoEsperado, decisaoControlador.ParaHumano);
    }
}
