namespace RollbackLab.Simulador;

using RollbackLab.Core;

public record PassoLinhaDoTempo(TimeSpan Tempo, string Descricao, TimeSpan? Duracao = null);

public record ResultadoCenario(
    string Cenario,
    string Titulo,
    Estado Estado,
    Folha Folha,
    bool ParaHumano,
    TimeSpan TempoSimulado,
    string ReferenciaDoc,
    string Divergencia,
    string Composicao,
    IReadOnlyList<PassoLinhaDoTempo> LinhaDoTempo,
    Decisao Decisao)
{
    public string Tipo => ParaHumano ? "Humano" : "Automático";
    public string TempoFormatado => $"{TempoSimulado:mm\\:ss} ({TempoSimulado.TotalMinutes:F1} min)";
}

public class SimuladorCenarios
{
    public static List<ResultadoCenario> ExecutarTodos()
    {
        var cenarios = new List<(string Id, string Titulo, Estado Estado, string RefDoc, string Divergencia)>
        {
            ("A", "Feature Flag resolve degradação",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Nenhuma, TemFlag: true, FlagResolve: true, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: false),
             "≈ 4,0 min", "Nenhuma"),

            ("B", "Rollback automático sem migração de banco",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Nenhuma, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: true),
             "≈ 8,0 min", "Nenhuma"),

            ("C", "Rollback automático com migração aditiva compatível",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Aditiva, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: true),
             "≈ 8,5 min", "Nenhuma"),

            ("D", "Parada para humano por migração destrutiva (RPO=0)",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Destrutiva, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: false),
             "≈ 1,0 min", "Nenhuma"),

            ("E", "Parada para humano por digest anterior ausente no registry",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Nenhuma, TemFlag: false, FlagResolve: false, ImagemExiste: false, PedidosCompativeis: true, RollbackRecuperou: false),
             "≈ 1,0 min", "Nenhuma"),

            ("F", "Parada para humano por incompatibilidade retroativa de pedidos",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Nenhuma, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: false, RollbackRecuperou: false),
             "≈ 1,0 min", "Nenhuma"),

            ("G", "Parada para humano após rollback sem normalização de métricas",
             new Estado(CorrelacionaComDeploy: true, Migracao: Migracao.Nenhuma, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: false),
             "≈ 8,0 min", "+0,5 min (esperada)"),

            ("H", "Parada para humano por ausência de correlação com o deploy",
             new Estado(CorrelacionaComDeploy: false, Migracao: Migracao.Nenhuma, TemFlag: false, FlagResolve: false, ImagemExiste: true, PedidosCompativeis: true, RollbackRecuperou: false),
             "≈ 1,0 min", "Nenhuma")
        };

        var resultados = new List<ResultadoCenario>();

        foreach (var c in cenarios)
        {
            var relogio = new RelogioVirtual();
            var passos = new List<PassoLinhaDoTempo>();

            passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Alerta de degradação disparado em produção"));

            relogio.Avancar(Custos.Decisao);
            passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Decisão inicial e consultas concluídas (manifesto, registry, correlação)", Custos.Decisao));

            var decisao = ArvoreDecisao.Decidir(c.Estado);
            string composicao = "";

            switch (decisao.Folha)
            {
                case Folha.H:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Análise: Sem correlação comprovada com o deploy (produção intocada)"));
                    relogio.Avancar(Custos.Notificacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Plantão notificado para investigação humana", Custos.Notificacao));
                    composicao = "Decisao (30s) + Notificacao (30s)";
                    break;

                case Folha.D:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Análise: Migração destrutiva/desconhecida detectada (RPO=0 proíbe down-migration)"));
                    relogio.Avancar(Custos.Notificacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Plantão notificado para intervenção manual com DBA", Custos.Notificacao));
                    composicao = "Decisao (30s) + Notificacao (30s)";
                    break;

                case Folha.A:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag do release desativada"));
                    relogio.Avancar(Custos.EsperaFlag);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Janela de amostragem estatística de telemetria concluída", Custos.EsperaFlag));
                    relogio.Avancar(Custos.ConferenciaFlag);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Conferência de métricas: taxa de pedidos normalizada!", Custos.ConferenciaFlag));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Incidente aberto para pós-morte (Folha A - Automática)"));
                    composicao = "Decisao (30s) + EsperaFlag (3m) + ConferenciaFlag (30s)";
                    break;

                case Folha.E:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    if (c.Estado.TemFlag)
                        passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag desativada (não resolveu)"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Análise: Imagem do digest anterior inexistente ou corrompida no registry"));
                    relogio.Avancar(Custos.Notificacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Plantão notificado para verificação de imagem/auditoria", Custos.Notificacao));
                    composicao = "Decisao (30s) + Notificacao (30s)";
                    break;

                case Folha.F:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    if (c.Estado.TemFlag)
                        passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag desativada (não resolveu)"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Análise: Pedidos gravados pela versão v2.4 são incompatíveis com v2.3"));
                    relogio.Avancar(Custos.Notificacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Plantão notificado: rollback impedido para não corromper pedidos", Custos.Notificacao));
                    composicao = "Decisao (30s) + Notificacao (30s)";
                    break;

                case Folha.G:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    if (c.Estado.TemFlag)
                        passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag desativada (não resolveu)"));
                    relogio.Avancar(Custos.Rollback);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Rollback de pods para digest v2.3 concluído", Custos.Rollback));
                    relogio.Avancar(Custos.SmokeTest);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Smoke test sintético de rotas aprovado", Custos.SmokeTest));
                    relogio.Avancar(Custos.Estabilizacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Janela de estabilização pós-rollback: métricas NÃO normalizaram", Custos.Estabilizacao));
                    relogio.Avancar(Custos.Notificacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Plantão notificado: causa externa provável, sem nova tentativa", Custos.Notificacao));
                    composicao = "Decisao (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m) + Notificacao (30s)";
                    break;

                case Folha.B:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    if (c.Estado.TemFlag)
                        passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag desativada (não resolveu)"));
                    relogio.Avancar(Custos.Rollback);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Rollback de pods para digest v2.3 concluído", Custos.Rollback));
                    relogio.Avancar(Custos.SmokeTest);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Smoke test sintético de rotas aprovado", Custos.SmokeTest));
                    relogio.Avancar(Custos.Estabilizacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Janela de estabilização pós-rollback: sucesso de pedidos recuperado!", Custos.Estabilizacao));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Versão defeituosa v2.4 bloqueada na esteira"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Incidente aberto para pós-morte (Folha B - Automática)"));
                    composicao = "Decisao (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m)";
                    break;

                case Folha.C:
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Travar novos deploys na esteira"));
                    if (c.Estado.TemFlag)
                        passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Feature flag desativada (não resolveu)"));
                    relogio.Avancar(Custos.ValidacaoPedidos);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Validação de retrocompatibilidade de pedidos concluída com sucesso", Custos.ValidacaoPedidos));
                    relogio.Avancar(Custos.Rollback);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Rollback de pods para digest v2.3 concluído", Custos.Rollback));
                    relogio.Avancar(Custos.SmokeTest);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Smoke test sintético de rotas aprovado", Custos.SmokeTest));
                    relogio.Avancar(Custos.Estabilizacao);
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Janela de estabilização pós-rollback: sucesso de pedidos recuperado!", Custos.Estabilizacao));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Versão defeituosa v2.4 bloqueada na esteira"));
                    passos.Add(new PassoLinhaDoTempo(relogio.Agora, "Ação executada: Incidente aberto para pós-morte (Folha C - Automática)"));
                    composicao = "Decisao (30s) + ValidacaoPedidos (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m)";
                    break;
            }

            resultados.Add(new ResultadoCenario(
                c.Id,
                c.Titulo,
                c.Estado,
                decisao.Folha,
                decisao.ParaHumano,
                relogio.Agora,
                c.RefDoc,
                c.Divergencia,
                composicao,
                passos,
                decisao));
        }

        return resultados;
    }

    public static void GravarCsv(IEnumerable<ResultadoCenario> resultados, string caminhoArquivo)
    {
        using var writer = new StreamWriter(caminhoArquivo, append: false, System.Text.Encoding.UTF8);
        writer.WriteLine("Cenario,Folha,Tipo,TempoSimuladoFormatado,TempoSimuladoSegundos,ReferenciaDocumento,Divergencia,Composicao");
        foreach (var r in resultados)
        {
            writer.WriteLine($"\"{r.Cenario}\",\"{r.Folha}\",\"{r.Tipo}\",\"{r.TempoFormatado}\",{(int)r.TempoSimulado.TotalSeconds},\"{r.ReferenciaDoc}\",\"{r.Divergencia}\",\"{r.Composicao}\"");
        }
    }

    public static void ImprimirRelatorio(IEnumerable<ResultadoCenario> resultados, TextWriter writer)
    {
        writer.WriteLine("====================================================================================================");
        writer.WriteLine("              LABORATÓRIO LOCAL DE AUTOMAÇÃO DE ROLLBACK - EXECUÇÃO DOS 8 CENÁRIOS                  ");
        writer.WriteLine("====================================================================================================");
        writer.WriteLine();

        foreach (var r in resultados)
        {
            writer.WriteLine("----------------------------------------------------------------------------------------------------");
            writer.WriteLine($"CENÁRIO {r.Cenario}: {r.Titulo}");
            writer.WriteLine($"Classificação: Folha {r.Folha} | Modo: {r.Tipo.ToUpper()} | Tempo Total: {r.TempoFormatado}");
            writer.WriteLine($"Motivo da Decisão: {r.Decisao.Motivo}");
            writer.WriteLine("Linha do tempo:");
            foreach (var passo in r.LinhaDoTempo)
            {
                string delta = passo.Duracao.HasValue ? $" (+{passo.Duracao.Value.TotalSeconds}s)" : "";
                writer.WriteLine($"  [t={passo.Tempo:mm\\:ss}] {passo.Descricao}{delta}");
            }
            writer.WriteLine($"Composição do tempo: {r.Composicao}");
            writer.WriteLine("----------------------------------------------------------------------------------------------------");
            writer.WriteLine();
        }

        writer.WriteLine("========================================================================================================================");
        writer.WriteLine("                                           TABELA CONSOLIDADA DOS 8 CENÁRIOS                                            ");
        writer.WriteLine("========================================================================================================================");
        writer.WriteLine(string.Format("{0,-7} | {1,-5} | {2,-10} | {3,-15} | {4,-14} | {5,-12} | {6}",
            "Cenário", "Folha", "Tipo", "Tempo Simulado", "Ref. Documento", "Divergência", "Composição"));
        writer.WriteLine(new string('-', 120));

        foreach (var r in resultados)
        {
            writer.WriteLine(string.Format("{0,-7} | {1,-5} | {2,-10} | {3,-15} | {4,-14} | {5,-12} | {6}",
                r.Cenario,
                r.Folha,
                r.Tipo,
                r.TempoFormatado,
                r.ReferenciaDoc,
                r.Divergencia,
                r.Composicao));
        }

        writer.WriteLine(new string('=', 120));
        writer.WriteLine();
        writer.WriteLine("OBSERVAÇÕES E ANÁLISE DE TEMPOS SIMULADOS:");
        writer.WriteLine("- Cenários D, E, F e H: ~1,0 min (30s de análise inicial + 30s para acionamento/notificação de plantão).");
        writer.WriteLine("- Cenário A: 4,0 min (30s de análise inicial + 3min de amostragem de flag + 30s de conferência de telemetria).");
        writer.WriteLine("- Cenário B: 8,0 min (30s decisão + 2min rollback de imagem + 30s smoke test + 5min estabilização pós-rollback).");
        writer.WriteLine("- Cenário C: 8,5 min (30s decisão + 30s validação retroativa de pedidos + 2min rollback + 30s smoke + 5min estabilização).");
        writer.WriteLine("- Cenário G: 8,5 min (30s decisão + 2min rollback + 30s smoke + 5min estabilização sem sucesso + 30s notificação plantão).");
    }
}
