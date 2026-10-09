namespace RollbackLab.Tests;

using RollbackLab.Core;

public class FailSafeTests
{
    private static Estado CriarEstadoPadraoSucesso() => new(
        CorrelacionaComDeploy: true,
        Migracao: Migracao.Nenhuma,
        TemFlag: false,
        FlagResolve: false,
        ImagemExiste: true,
        PedidosCompativeis: true,
        RollbackRecuperou: true);

    [Fact]
    public void FalhaEmCorrelacao_DeveResultarEmFolhaH()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Correlacao.LancaExcecao = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.H, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Equal(0, fakes.Implantador.ChamadasTravar);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaEmManifesto_DeveResultarEmFolhaD()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Manifesto.LancaExcecao = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.D, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Equal(1, fakes.Implantador.ChamadasTravar);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaNaConsultaDeFlags_DeveTratarComoSemFlagESeguirParaRegistry()
    {
        var estado = new Estado(
            CorrelacionaComDeploy: true,
            Migracao: Migracao.Nenhuma,
            TemFlag: true,
            FlagResolve: true,
            ImagemExiste: true,
            PedidosCompativeis: true,
            RollbackRecuperou: true);

        var fakes = FabricaDeFakes.APartirDe(estado);
        fakes.Flags.LancaExcecaoNaConsulta = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.B, decisao.Folha);
        Assert.False(decisao.ParaHumano);
        Assert.Equal(0, fakes.Flags.ChamadasDesligar);
    }

    [Fact]
    public void FalhaAoDesligarFlag_DeveSeguirParaRegistry()
    {
        var estado = new Estado(
            CorrelacionaComDeploy: true,
            Migracao: Migracao.Nenhuma,
            TemFlag: true,
            FlagResolve: true,
            ImagemExiste: true,
            PedidosCompativeis: true,
            RollbackRecuperou: true);

        var fakes = FabricaDeFakes.APartirDe(estado);
        fakes.Flags.LancaExcecaoAoDesligar = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.B, decisao.Folha);
        Assert.False(decisao.ParaHumano);
    }

    [Fact]
    public void FalhaEmRegistry_DeveResultarEmFolhaE()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Registry.LancaExcecao = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.E, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaEmPedidos_DeveResultarEmFolhaF()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Pedidos.LancaExcecao = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.F, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaNoRollbackDeImagem_DeveResultarEmFolhaG()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Implantador.LancaExcecaoNoRollback = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.G, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaNoSmokeTest_DeveResultarEmFolhaG()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Implantador.LancaExcecaoNoSmokeTest = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.G, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }

    [Fact]
    public void FalhaNasMetricasAposRollback_DeveResultarEmFolhaG()
    {
        var fakes = FabricaDeFakes.APartirDe(CriarEstadoPadraoSucesso());
        fakes.Metricas.LancaExcecaoEmRollback = true;

        var controlador = new Controlador(
            fakes.Correlacao, fakes.Implantador, fakes.Manifesto, fakes.Flags,
            fakes.Metricas, fakes.Registry, fakes.Pedidos, fakes.Notificador, fakes.Relogio);

        var decisao = controlador.Executar();

        Assert.Equal(Folha.G, decisao.Folha);
        Assert.True(decisao.ParaHumano);
        Assert.Contains(Acao.NotificarPlantao, decisao.Acoes);
    }
}
