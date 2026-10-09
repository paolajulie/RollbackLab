namespace RollbackLab.Core;

public class Controlador
{
    private readonly ICorrelacao _correlacao;
    private readonly IImplantador _implantador;
    private readonly IManifesto _manifesto;
    private readonly IFlags _flags;
    private readonly IMetricas _metricas;
    private readonly IRegistry _registry;
    private readonly IPedidos _pedidos;
    private readonly INotificador _notificador;
    private readonly IRelogio? _relogio;

    public Controlador(
        ICorrelacao correlacao,
        IImplantador implantador,
        IManifesto manifesto,
        IFlags flags,
        IMetricas metricas,
        IRegistry registry,
        IPedidos pedidos,
        INotificador notificador,
        IRelogio? relogio = null)
    {
        _correlacao = correlacao ?? throw new ArgumentNullException(nameof(correlacao));
        _implantador = implantador ?? throw new ArgumentNullException(nameof(implantador));
        _manifesto = manifesto ?? throw new ArgumentNullException(nameof(manifesto));
        _flags = flags ?? throw new ArgumentNullException(nameof(flags));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _pedidos = pedidos ?? throw new ArgumentNullException(nameof(pedidos));
        _notificador = notificador ?? throw new ArgumentNullException(nameof(notificador));
        _relogio = relogio;
    }

    public Decisao Executar()
    {
        var acoes = new List<Acao>();

        bool correlaciona = false;
        try
        {
            correlaciona = _correlacao.DegradacaoComecouComODeploy();
        }
        catch
        {
            correlaciona = false;
        }

        if (!correlaciona)
        {
            acoes.Add(Acao.NotificarPlantao);
            SafeNotificarPlantao("Sem correlação comprovada com o deploy; não altera produção.");
            return new Decisao(
                Folha.H,
                ParaHumano: true,
                Motivo: "Sem correlação comprovada com o deploy; não altera produção.",
                Acoes: acoes);
        }

        acoes.Add(Acao.TravarDeploys);
        try
        {
            _implantador.TravarNovosDeploys();
        }
        catch
        {
        }

        Migracao migracao = Migracao.Desconhecida;
        try
        {
            migracao = _manifesto.LerTipoMigracao();
        }
        catch
        {
            migracao = Migracao.Desconhecida;
        }

        if (migracao is Migracao.Destrutiva or Migracao.Desconhecida)
        {
            acoes.Add(Acao.NotificarPlantao);
            SafeNotificarPlantao("Migração destrutiva ou desconhecida; voltar a app quebra a v2.3 e down-migration/restore violaria RPO=0.");
            return new Decisao(
                Folha.D,
                ParaHumano: true,
                Motivo: "Migração destrutiva ou desconhecida; voltar a app quebra a v2.3 e down-migration/restore violaria RPO=0.",
                Acoes: acoes);
        }

        bool temFlag = false;
        try
        {
            temFlag = _flags.ExisteFlagParaRelease();
        }
        catch
        {
            temFlag = false;
        }

        if (temFlag)
        {
            bool desligouComSucesso = false;
            try
            {
                acoes.Add(Acao.DesligarFlag);
                _flags.Desligar();
                desligouComSucesso = true;
            }
            catch
            {
                desligouComSucesso = false;
            }

            if (desligouComSucesso)
            {
                bool flagResolveu = false;
                try
                {
                    flagResolveu = _metricas.SucessoDePedidosNormalizou(TimeSpan.FromMinutes(3));
                }
                catch
                {
                    flagResolveu = false;
                }

                if (flagResolveu)
                {
                    acoes.Add(Acao.AbrirIncidente);
                    SafeAbrirIncidente("Feature flag desativada com sucesso e métricas normalizadas.");
                    return new Decisao(
                        Folha.A,
                        ParaHumano: false,
                        Motivo: "Feature flag desativada com sucesso e métricas normalizadas.",
                        Acoes: acoes);
                }
            }
        }

        bool imagemExiste = false;
        try
        {
            imagemExiste = _registry.DigestAnteriorExiste();
        }
        catch
        {
            imagemExiste = false;
        }

        if (!imagemExiste)
        {
            acoes.Add(Acao.NotificarPlantao);
            SafeNotificarPlantao("Imagem do digest anterior não existe ou não está íntegra no registry; rebuild a quente não auditado é proibido.");
            return new Decisao(
                Folha.E,
                ParaHumano: true,
                Motivo: "Imagem do digest anterior não existe ou não está íntegra no registry; rebuild a quente não auditado é proibido.",
                Acoes: acoes);
        }

        bool pedidosCompativeis = false;
        try
        {
            pedidosCompativeis = _pedidos.PedidosSaoCompativeis();
        }
        catch
        {
            pedidosCompativeis = false;
        }

        if (!pedidosCompativeis)
        {
            acoes.Add(Acao.NotificarPlantao);
            SafeNotificarPlantao("Pedidos gravados pela v2.4 são incompatíveis com a v2.3; rollback tornaria pedidos ilegíveis.");
            return new Decisao(
                Folha.F,
                ParaHumano: true,
                Motivo: "Pedidos gravados pela v2.4 são incompatíveis com a v2.3; rollback tornaria pedidos ilegíveis.",
                Acoes: acoes);
        }

        bool rollbackExecutado = false;
        bool smokePassou = false;
        bool metricasNormalizaram = false;

        try
        {
            acoes.Add(Acao.ExecutarRollback);
            _implantador.RollbackParaDigestAnterior();
            rollbackExecutado = true;
        }
        catch
        {
            rollbackExecutado = false;
        }

        if (rollbackExecutado)
        {
            try
            {
                acoes.Add(Acao.ExecutarSmokeTest);
                smokePassou = _implantador.SmokeTestPassou();
            }
            catch
            {
                smokePassou = false;
            }

            if (smokePassou)
            {
                try
                {
                    metricasNormalizaram = _metricas.SucessoDePedidosNormalizou(TimeSpan.FromMinutes(5));
                }
                catch
                {
                    metricasNormalizaram = false;
                }
            }
        }

        if (!metricasNormalizaram)
        {
            acoes.Add(Acao.NotificarPlantao);
            SafeNotificarPlantao("Rollback executado, mas métricas não normalizaram em até 5 min; causa provavelmente externa, nova tentativa criaria loop.");
            return new Decisao(
                Folha.G,
                ParaHumano: true,
                Motivo: "Rollback executado, mas métricas não normalizaram em até 5 min; causa provavelmente externa, nova tentativa criaria loop.",
                Acoes: acoes);
        }

        try
        {
            acoes.Add(Acao.BloquearVersao);
            _implantador.BloquearVersao();
        }
        catch
        {
        }

        acoes.Add(Acao.AbrirIncidente);
        SafeAbrirIncidente("Rollback automático executado com sucesso e versão bloqueada.");

        if (migracao == Migracao.Aditiva)
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

    private void SafeNotificarPlantao(string motivo)
    {
        try
        {
            _notificador.NotificarPlantao(motivo);
        }
        catch
        {
        }
    }

    private void SafeAbrirIncidente(string resumo)
    {
        try
        {
            _notificador.AbrirIncidente(resumo);
        }
        catch
        {
        }
    }
}
