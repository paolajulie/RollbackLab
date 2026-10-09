namespace RollbackLab.Core;

public enum Migracao { Nenhuma, Aditiva, Destrutiva, Desconhecida }

public enum Folha { A, B, C, D, E, F, G, H }

public enum Acao
{
    TravarDeploys,
    DesligarFlag,
    ExecutarRollback,
    ExecutarSmokeTest,
    BloquearVersao,
    AbrirIncidente,
    NotificarPlantao
}

public record Estado(
    bool CorrelacionaComDeploy,
    Migracao Migracao,
    bool TemFlag,
    bool FlagResolve,
    bool ImagemExiste,
    bool PedidosCompativeis,
    bool RollbackRecuperou);

public record Decisao(
    Folha Folha,
    bool ParaHumano,
    string Motivo,
    IReadOnlyList<Acao> Acoes);
