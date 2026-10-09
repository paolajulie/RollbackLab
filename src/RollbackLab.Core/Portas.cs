namespace RollbackLab.Core;

public interface IManifesto
{
    Migracao LerTipoMigracao();
}

public interface ICorrelacao
{
    bool DegradacaoComecouComODeploy();
}

public interface IFlags
{
    bool ExisteFlagParaRelease();
    void Desligar();
}

public interface IMetricas
{
    bool SucessoDePedidosNormalizou(TimeSpan esperar);
}

public interface IRegistry
{
    bool DigestAnteriorExiste();
}

public interface IPedidos
{
    bool PedidosSaoCompativeis();
    int ContarDesde(string horario);
}

public interface IImplantador
{
    void TravarNovosDeploys();
    void RollbackParaDigestAnterior();
    bool SmokeTestPassou();
    void BloquearVersao();
}

public interface INotificador
{
    void NotificarPlantao(string motivo);
    void AbrirIncidente(string resumo);
}

public interface IRelogio
{
    void Avancar(TimeSpan duracao);
    TimeSpan Agora { get; }
}
