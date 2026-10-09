namespace RollbackLab.Simulador;

public class Program
{
    public static int Main(string[] args)
    {
        bool gerarCsv = args.Any(a => string.Equals(a, "--csv", StringComparison.OrdinalIgnoreCase));
        string arquivoCsv = "resultados.csv";

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--csv", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                arquivoCsv = args[i + 1];
            }
        }

        var resultados = SimuladorCenarios.ExecutarTodos();

        SimuladorCenarios.ImprimirRelatorio(resultados, Console.Out);

        if (gerarCsv)
        {
            try
            {
                SimuladorCenarios.GravarCsv(resultados, arquivoCsv);
                Console.WriteLine();
                Console.WriteLine($"[INFO] Resultados exportados com sucesso para o arquivo CSV: {Path.GetFullPath(arquivoCsv)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERRO] Falha ao gravar arquivo CSV: {ex.Message}");
                return 1;
            }
        }

        return 0;
    }
}
