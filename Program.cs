using System;
using System.Threading;
using ScottPlot;
namespace TPProj {
  class Program {

    static void standartStart() {
      double lambda, mu;
      int count;
      Console.Write("input lambda: ");
      double.TryParse(Console.ReadLine(), out lambda);
      Console.Write("input mu: ");
      double.TryParse(Console.ReadLine(), out mu);
      Console.Write("input threads count: ");
      int.TryParse(Console.ReadLine(), out count);
      Console.WriteLine((int)(1000 / lambda));
      Console.WriteLine((int)(1000 / mu));
      Server server = new Server(1000 / mu, count);
      Client client = new Client(server);
      for (int id = 1; id <= 100; id++) {
        client.send(id);
        Thread.Sleep((int)(1000 / lambda));
      }
      Console.WriteLine("Всего заявок: {0}", server.requestCount);
      Console.WriteLine("Обработано заявок: {0}", server.processedCount);
      Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
    }
    static void statisticStart(double lambda, double mu, int count, string fileName) {
      double P0, Pn, Q, A, k, p, sum;
      double P0exp, Pnexp, Qexp, Aexp, kexp;
      p = lambda / mu;
      sum = 0;
      for (int j = 0; j < count; j++)
        sum += Math.Pow(p, j) / factorial(j);
      P0 = 1L / sum;
      Pn = P0 * Math.Pow(p, count) / factorial(count);
      Q = 1 - Pn;
      A = lambda * Q;
      k = A / mu;

      Server server = new Server(1000 / mu, count);
      server.StartMonitoring();
      Client client = new Client(server);
      for (int id = 1; id <= 100; id++) {
        client.send(id);
        Thread.Sleep((int)(1000 / lambda));
      }

      server.StopMonitoring();

      P0exp = (double)server.unbusyChecks / server.totalChecks;
      Pnexp = (double)server.rejectedCount / server.requestCount;
      Qexp = (double)server.processedCount / server.requestCount;
      Aexp = lambda * (double)server.processedCount / server.requestCount;
      kexp = (double)server.sumBusyPools / server.totalChecks;

      Console.WriteLine("Всего заявок: {0}", server.requestCount);
      Console.WriteLine("Обработано заявок: {0}", server.processedCount);
      Console.WriteLine("Отклонено заявок: {0}\n", server.rejectedCount);
      Console.WriteLine("Вероятность простоя: {0} (теор.: {1})", P0exp, P0);
      Console.WriteLine("Вероятность отказа: {0} (теор.: {1})", Pnexp, Pn);
      Console.WriteLine("Относительная пропусканая способность: {0} (теор.: {1})", Qexp, Q);
      Console.WriteLine("Абсолютная пропусканая способность: {0} (теор.: {1})", Aexp, A);
      Console.WriteLine("Среднее число занятых каналов: {0} (теор.: {1})", kexp, k);

      if (File.Exists(fileName)) {
        string data = string.Format("\n{0:F5}; {1:F5}; {2:F5} - {3:F5}; {4:F5} - {5:F5}; {6:F5} - {7:F5}; {8:F5} - {9:F5}; {10:F5} - {11:F5}", lambda, mu, P0exp, P0, Pnexp, Pn, Qexp, Q, Aexp, A, kexp, k);
        File.AppendAllText(fileName, data);
      }
      else {
        throw new Exception($"File {fileName} not found");
      }
    }
    static double factorial(int x) => x == 0 ? 1 : x * factorial(x - 1);
    static void createGraphicData(string inputFile, string outputFile, string targetMu) {
      var lines = File.ReadAllLines(inputFile);
      var filteredLines = lines.Where(line => {
        var parts = line.Split(';');
        if (parts.Length >= 2) {
          var muPart = parts[1].Trim();
          return muPart.Equals(targetMu);
        }
        return false;
      }).ToList();

      File.WriteAllLines(outputFile, filteredLines);
      Console.WriteLine("Created successfully");
    }
    static void createGraphic(string inputFile, string outputFile, int dataIndex, string dataName) {
      string[] lines = File.ReadAllLines(inputFile);

      double[] lambdas = new double[lines.Length];
      double[] experimental = new double[lines.Length];
      double[] theoretical = new double[lines.Length];

      for (int i = 0; i < lines.Length; i++) {
        string[] parts = lines[i].Split(';');
        if (parts.Length >= 3) {
          lambdas[i] = double.Parse(parts[0].Trim().Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

          string[] dataParts = parts[dataIndex + 2].Trim().Split('-');
          experimental[i] = double.Parse(dataParts[0].Trim().Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
          theoretical[i] = double.Parse(dataParts[1].Trim().Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
        }
      }

      var plot = new Plot();
      plot.Title($"Зависимость {dataName} от λ (μ = 1.0)", size: 16);
      plot.XLabel("λ (интенсивность потока заявок)");
      plot.YLabel(dataName);

      var expScatter = plot.Add.Scatter(lambdas, experimental);
      expScatter.LegendText = "Экспериментальные данные";
      expScatter.Color = Colors.Blue;
      expScatter.MarkerSize = 7;
      expScatter.LineWidth = 2;

      var theoryScatter = plot.Add.Scatter(lambdas, theoretical);
      theoryScatter.LegendText = "Теоретические данные";
      theoryScatter.Color = Colors.Red;
      theoryScatter.MarkerSize = 7;
      theoryScatter.LineWidth = 2;

      plot.ShowLegend();
      plot.SavePng(outputFile, 1200, 800);

      Console.WriteLine("Graphic created successfully");
    }
    static void Main() {
      string basePath = AppContext.BaseDirectory;
      string projectPath = Path.GetFullPath(Path.Combine(basePath, "../../.."));
      string resultPath = Path.Combine(projectPath, "result/");
      string resultsPath = Path.Combine(projectPath, "results.txt");
      string dataPath = Path.Combine(projectPath, "data.txt");
      double[] lambdas = { 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5, 5.5, 6 };
      double[] mus = { 0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5, 5.5, 6 };
      string[] names = { "P0 (Вероятность простоя)", "Pn (Вероятность отказа)", "Q (Относительная пропусканая способность)", "A (Абсолютная пропусканая способность)", "k (Среднее число занятых каналов)" };

      foreach (double lambda in lambdas)
        foreach (double mu in mus)
          statisticStart(lambda, mu, 5, resultsPath);

      createGraphicData(resultsPath, dataPath, "1,00000");
      for (int i = 0; i < 5; i++) {
        string graphPath = Path.Combine(resultPath, $"p-{i + 1}.png");
        createGraphic(dataPath, graphPath, i, names[i]);
      }
      //standartStart();
      //statisticStart(2, 3, 5);
    }
  }
  struct PoolRecord {
    public Thread thread;
    public bool in_use;
  }
  class Server {
    private PoolRecord[] pool;
    private double T;
    private object threadLock = new object();
    public int requestCount = 0;
    public int processedCount = 0;
    public int rejectedCount = 0;

    public int totalChecks = 0;
    public int sumBusyPools = 0;
    public int unbusyChecks = 0;
    private bool isMonitoring = true;
    public Server(double T, int poolSize) {
      pool = new PoolRecord[poolSize];
      this.T = T;
    }
    public void proc(object sender, procEventArgs e) {
      lock (threadLock) {
        Console.WriteLine("Заявка с номером: {0}", e.id);
        requestCount++;
        for (int i = 0; i < pool.Length; i++) {
          if (!pool[i].in_use) {
            pool[i].in_use = true;
            pool[i].thread = new Thread(new ParameterizedThreadStart(Answer!));
            pool[i].thread.Start(e.id);
            processedCount++;
            return;
          }
        }
        rejectedCount++;
      }
    }
    public void Answer(object arg) {
      int id = (int)arg;
      Console.WriteLine("Обработка заявки: {0}", id);
      DateTime startProcessing = DateTime.Now;
      Thread.Sleep((int)T);
      for (int i = 0; i < pool.Length; i++)
        if (pool[i].thread == Thread.CurrentThread)
          pool[i].in_use = false;
    }
    public void StartMonitoring() {
      new Thread(() => {
        while (isMonitoring) {
          totalChecks++;
          if (unbusy())
            unbusyChecks++;
          sumBusyPools += busyPoolsCount();
          Thread.Sleep(10);
        }
      }).Start();
    }

    public void StopMonitoring() {
      isMonitoring = false;
    }
    private bool unbusy() {
      foreach (var record in pool)
        if (record.in_use)
          return false;
      return true;
    }
    private int busyPoolsCount() {
      int count = 0;
      foreach (var record in pool)
        if (record.in_use)
          count++;
      return count;
    }
  }
  class Client {
    private Server server;
    public Client(Server server) {
      this.server = server;
      this.request += server.proc!;
    }
    public void send(int id) {
      procEventArgs args = new procEventArgs();
      args.id = id;
      OnProc(args);
    }
    protected virtual void OnProc(procEventArgs e) {
      EventHandler<procEventArgs> handler = request;
      if (handler != null) {
        handler(this, e);
      }
    }
    public event EventHandler<procEventArgs> request;
  }
  public class procEventArgs : EventArgs {
    public int id { get; set; }
  }
}