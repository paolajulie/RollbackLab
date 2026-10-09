namespace RollbackLab.Tests;

using RollbackLab.Core;

public record ConjuntoFakes(
    FakeCorrelacao Correlacao,
    FakeImplantador Implantador,
    FakeManifesto Manifesto,
    FakeFlags Flags,
    FakeMetricas Metricas,
    FakeRegistry Registry,
    FakePedidos Pedidos,
    FakeNotificador Notificador,
    FakeRelogio Relogio);

public static class FabricaDeFakes
{
    public static ConjuntoFakes APartirDe(Estado estado)
    {
        var relogio = new FakeRelogio();
        var correlacao = new FakeCorrelacao(estado.CorrelacionaComDeploy);
        var implantador = new FakeImplantador();
        var manifesto = new FakeManifesto(estado.Migracao);
        var flags = new FakeFlags(estado.TemFlag);
        var metricas = new FakeMetricas(
            normalizaFlag: estado.TemFlag && estado.FlagResolve,
            normalizaRollback: estado.RollbackRecuperou);
        var registry = new FakeRegistry(estado.ImagemExiste);
        var pedidos = new FakePedidos(estado.PedidosCompativeis);
        var notificador = new FakeNotificador();

        return new ConjuntoFakes(
            correlacao,
            implantador,
            manifesto,
            flags,
            metricas,
            registry,
            pedidos,
            notificador,
            relogio);
    }
}

public class FakeCorrelacao : ICorrelacao
{
    public bool Retorno { get; set; }
    public bool LancaExcecao { get; set; }

    public FakeCorrelacao(bool retorno) => Retorno = retorno;

    public bool DegradacaoComecouComODeploy()
    {
        if (LancaExcecao) throw new InvalidOperationException("Falha simulada em ICorrelacao");
        return Retorno;
    }
}

public class FakeManifesto : IManifesto
{
    public Migracao Retorno { get; set; }
    public bool LancaExcecao { get; set; }

    public FakeManifesto(Migracao retorno) => Retorno = retorno;

    public Migracao LerTipoMigracao()
    {
        if (LancaExcecao) throw new InvalidOperationException("Falha simulada em IManifesto");
        return Retorno;
    }
}

public class FakeFlags : IFlags
{
    public bool Existe { get; set; }
    public bool LancaExcecaoNaConsulta { get; set; }
    public bool LancaExcecaoAoDesligar { get; set; }
    public int ChamadasDesligar { get; private set; }

    public FakeFlags(bool existe) => Existe = existe;

    public bool ExisteFlagParaRelease()
    {
        if (LancaExcecaoNaConsulta) throw new InvalidOperationException("Falha simulada na consulta de IFlags");
        return Existe;
    }

    public void Desligar()
    {
        ChamadasDesligar++;
        if (LancaExcecaoAoDesligar) throw new InvalidOperationException("Falha simulada ao desligar IFlags");
    }
}

public class FakeMetricas : IMetricas
{
    public bool NormalizaFlag { get; set; }
    public bool NormalizaRollback { get; set; }
    public bool LancaExcecaoEmFlag { get; set; }
    public bool LancaExcecaoEmRollback { get; set; }

    public FakeMetricas(bool normalizaFlag, bool normalizaRollback)
    {
        NormalizaFlag = normalizaFlag;
        NormalizaRollback = normalizaRollback;
    }

    public bool SucessoDePedidosNormalizou(TimeSpan esperar)
    {
        if (esperar <= TimeSpan.FromMinutes(3))
        {
            if (LancaExcecaoEmFlag) throw new InvalidOperationException("Falha simulada nas métricas de flag");
            return NormalizaFlag;
        }

        if (LancaExcecaoEmRollback) throw new InvalidOperationException("Falha simulada nas métricas de rollback");
        return NormalizaRollback;
    }
}

public class FakeRegistry : IRegistry
{
    public bool Retorno { get; set; }
    public bool LancaExcecao { get; set; }

    public FakeRegistry(bool retorno) => Retorno = retorno;

    public bool DigestAnteriorExiste()
    {
        if (LancaExcecao) throw new TimeoutException("Timeout simulado em IRegistry");
        return Retorno;
    }
}

public class FakePedidos : IPedidos
{
    public bool Retorno { get; set; }
    public bool LancaExcecao { get; set; }
    public int TotalPedidos { get; set; } = 42;

    public FakePedidos(bool retorno) => Retorno = retorno;

    public bool PedidosSaoCompativeis()
    {
        if (LancaExcecao) throw new InvalidOperationException("Falha simulada em IPedidos");
        return Retorno;
    }

    public int ContarDesde(string horario) => TotalPedidos;
}

public class FakeImplantador : IImplantador
{
    public int ChamadasTravar { get; private set; }
    public int ChamadasRollback { get; private set; }
    public int ChamadasSmokeTest { get; private set; }
    public int ChamadasBloquear { get; private set; }

    public bool SmokeTestRetorno { get; set; } = true;
    public bool LancaExcecaoAoTravar { get; set; }
    public bool LancaExcecaoNoRollback { get; set; }
    public bool LancaExcecaoNoSmokeTest { get; set; }
    public bool LancaExcecaoAoBloquear { get; set; }

    public void TravarNovosDeploys()
    {
        ChamadasTravar++;
        if (LancaExcecaoAoTravar) throw new InvalidOperationException("Falha simulada ao travar deploys");
    }

    public void RollbackParaDigestAnterior()
    {
        ChamadasRollback++;
        if (LancaExcecaoNoRollback) throw new InvalidOperationException("Falha simulada no rollback de imagem");
    }

    public bool SmokeTestPassou()
    {
        ChamadasSmokeTest++;
        if (LancaExcecaoNoSmokeTest) throw new InvalidOperationException("Falha simulada no smoke test");
        return SmokeTestRetorno;
    }

    public void BloquearVersao()
    {
        ChamadasBloquear++;
        if (LancaExcecaoAoBloquear) throw new InvalidOperationException("Falha simulada ao bloquear versão");
    }
}

public class FakeNotificador : INotificador
{
    public List<string> NotificacoesPlantao { get; } = new();
    public List<string> IncidentesAbertos { get; } = new();

    public void NotificarPlantao(string motivo) => NotificacoesPlantao.Add(motivo);
    public void AbrirIncidente(string resumo) => IncidentesAbertos.Add(resumo);
}

public class FakeRelogio : IRelogio
{
    private TimeSpan _agora = TimeSpan.Zero;

    public TimeSpan Agora => _agora;

    public void Avancar(TimeSpan duracao)
    {
        _agora += duracao;
    }
}
