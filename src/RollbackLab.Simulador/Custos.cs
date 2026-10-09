namespace RollbackLab.Simulador;

public static class Custos
{
    public static readonly TimeSpan Decisao = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan EsperaFlag = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan ConferenciaFlag = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Rollback = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan SmokeTest = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Estabilizacao = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ValidacaoPedidos = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Notificacao = TimeSpan.FromSeconds(30);
}
