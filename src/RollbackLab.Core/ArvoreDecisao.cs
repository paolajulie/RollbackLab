namespace RollbackLab.Core;

public static class ArvoreDecisao
{
    public static Decisao Decidir(Estado e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (!e.CorrelacionaComDeploy)
        {
            return new Decisao(
                Folha.H,
                ParaHumano: true,
                Motivo: "Sem correlação comprovada com o deploy; não altera produção.",
                Acoes: new[] { Acao.NotificarPlantao });
        }

        if (e.Migracao is Migracao.Destrutiva or Migracao.Desconhecida)
        {
            return new Decisao(
                Folha.D,
                ParaHumano: true,
                Motivo: "Migração destrutiva ou desconhecida; voltar a app quebra a v2.3 e down-migration/restore violaria RPO=0.",
                Acoes: new[] { Acao.TravarDeploys, Acao.NotificarPlantao });
        }

        if (e.TemFlag && e.FlagResolve)
        {
            return new Decisao(
                Folha.A,
                ParaHumano: false,
                Motivo: "Feature flag desativada com sucesso e métricas normalizadas.",
                Acoes: new[] { Acao.TravarDeploys, Acao.DesligarFlag, Acao.AbrirIncidente });
        }

        var acoesBase = new List<Acao> { Acao.TravarDeploys };
        if (e.TemFlag)
        {
            acoesBase.Add(Acao.DesligarFlag);
        }

        if (!e.ImagemExiste)
        {
            var acoes = new List<Acao>(acoesBase) { Acao.NotificarPlantao };
            return new Decisao(
                Folha.E,
                ParaHumano: true,
                Motivo: "Imagem do digest anterior não existe ou não está íntegra no registry; rebuild a quente não auditado é proibido.",
                Acoes: acoes);
        }

        if (!e.PedidosCompativeis)
        {
            var acoes = new List<Acao>(acoesBase) { Acao.NotificarPlantao };
            return new Decisao(
                Folha.F,
                ParaHumano: true,
                Motivo: "Pedidos gravados pela v2.4 são incompatíveis com a v2.3; rollback tornaria pedidos ilegíveis.",
                Acoes: acoes);
        }

        if (!e.RollbackRecuperou)
        {
            var acoes = new List<Acao>(acoesBase)
            {
                Acao.ExecutarRollback,
                Acao.ExecutarSmokeTest,
                Acao.NotificarPlantao
            };
            return new Decisao(
                Folha.G,
                ParaHumano: true,
                Motivo: "Rollback executado, mas métricas não normalizaram em até 5 min; causa provavelmente externa, nova tentativa criaria loop.",
                Acoes: acoes);
        }

        {
            var acoes = new List<Acao>(acoesBase)
            {
                Acao.ExecutarRollback,
                Acao.ExecutarSmokeTest,
                Acao.BloquearVersao,
                Acao.AbrirIncidente
            };

            if (e.Migracao == Migracao.Aditiva)
            {
                return new Decisao(
                    Folha.C,
                    ParaHumano: false,
                    Motivo: "Rollback automático executado com sucesso com migração aditiva; versão bloqueada e incidente registrado.",
                    Acoes: acoes);
            }

            return new Decisao(
                Folha.B,
                ParaHumano: false,
                Motivo: "Rollback automático executado com sucesso sem migração de banco; versão bloqueada e incidente registrado.",
                Acoes: acoes);
        }
    }
}
