using System;
using System.Threading;
namespace TPProj {
  class Program {

    static void standartStart() {
      double lm, mu;
      int count;
      Console.Write("input lambda: ");
      double.TryParse(Console.ReadLine(), out lm);
      Console.Write("input mu: ");
      double.TryParse(Console.ReadLine(), out mu);
      Console.Write("input threads count: ");
      int.TryParse(Console.ReadLine(), out count);
      Console.WriteLine((int)(1000 / lm));
      Console.WriteLine((int)(1000 / mu));
      Server server = new Server(1000 / mu, count);
      Client client = new Client(server);
      for (int id = 1; id <= 100; id++) {
        client.send(id);
        Thread.Sleep((int)(1000 / lm));
      }
      Console.WriteLine("Всего заявок: {0}", server.requestCount);
      Console.WriteLine("Обработано заявок: {0}", server.processedCount);
      Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
    }
    static void statisticStart(double lambda, double mu, int count) {
      double P0, Pn, Q, A, k, p, sum; 
      p = lambda / mu;
      sum = 0;
      for (int j = 0; j < count; j++)
        sum += Math.Pow(p, j) / Factorial(j);
      P0 = 1L / sum;
      Pn = P0 * Math.Pow(p, count) / Factorial(count);
      Q = 1 - Pn;
      A = lambda * Q;
      k = A / mu;

      Console.WriteLine(P0);
      Console.WriteLine(Pn);
      Console.WriteLine(Q);
      Console.WriteLine(A);
      Console.WriteLine(k);

      Server server = new Server(1000 / mu, count);
      Client client = new Client(server);
      for (int id = 1; id <= 100; id++) {
        client.send(id);
        Thread.Sleep((int)(1000 / lambda));
      }

      Console.WriteLine("Всего заявок: {0}", server.requestCount);
      Console.WriteLine("Обработано заявок: {0}", server.processedCount);
      Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
      Console.WriteLine("Вероятность отказа: {0}", server.rejectedCount / server.requestCount);
      Console.WriteLine("Относительная пропусканая способность: {0}", server.processedCount / server.requestCount);
      Console.WriteLine("Абсолютная пропусканая способность: {0}", lambda * server.processedCount / server.requestCount);
    }
    static double Factorial(int x) => x == 0 ? 1 : x * Factorial(x - 1);
    static void Main() {
      //standartStart();
      statisticStart(4, 3, 5);
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