namespace RollbackLab.Simulador;

using RollbackLab.Core;

public class RelogioVirtual : IRelogio
{
    private TimeSpan _tempo = TimeSpan.Zero;

    public TimeSpan Agora => _tempo;

    public void Avancar(TimeSpan duracao)
    {
        _tempo += duracao;
    }

    public void Resetar()
    {
        _tempo = TimeSpan.Zero;
    }

    public string Formatar() => $"{_tempo:mm\\:ss}";
}
