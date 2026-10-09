namespace RollbackLab.Tests;

using RollbackLab.Core;

public class ConsistenciaTests
{
    [Fact]
    public void ValidarConsistenciaControladorComArvoreEmTodas256Combinacoes()
    {
        var estados = ArvoreDecisaoTests.Obter256Estados().ToList();
        Assert.Equal(256, estados.Count);

        foreach (var estado in estados)
        {
            var decisaoArvore = ArvoreDecisao.Decidir(estado);

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

            Assert.Equal(decisaoArvore.Folha, decisaoControlador.Folha);
            Assert.Equal(decisaoArvore.ParaHumano, decisaoControlador.ParaHumano);
            Assert.True(fakes.Implantador.ChamadasRollback <= 1,
                $"Rollback executou {fakes.Implantador.ChamadasRollback} vezes no estado {estado}");
            Assert.True(fakes.Flags.ChamadasDesligar <= 1,
                $"Desligar flag executou {fakes.Flags.ChamadasDesligar} vezes no estado {estado}");

            int travamentoEsperado = estado.CorrelacionaComDeploy ? 1 : 0;
            Assert.Equal(travamentoEsperado, fakes.Implantador.ChamadasTravar);

            Assert.All(decisaoControlador.Acoes, acao =>
            {
                Assert.True(Enum.IsDefined(typeof(Acao), acao), $"Ação inválida detectada: {acao}");
            });

            Assert.Equal(decisaoArvore.Acoes, decisaoControlador.Acoes);
        }
    }
}
