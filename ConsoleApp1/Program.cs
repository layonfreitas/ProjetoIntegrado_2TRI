using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjetoCs
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            string url = "http://127.0.0.1:3000/leituras";

            int quantidadeDeEnvios = 1000;
            int intervaloEmMilissegundos = 100;

            Random random = new Random();

            Console.WriteLine($"Iniciando o envio de {quantidadeDeEnvios} leituras...");

            for (int i = 1; i <= quantidadeDeEnvios; i++)
            {
                var leitura = new Leitura
                {
                    Valor = (float)(random.NextDouble() * 100),
                    Filtro = random.Next(0, 2) == 1
                };

                try
                {
                    string json = JsonSerializer.Serialize(leitura);

                    using var conteudo = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                    HttpResponseMessage resposta = await client.PostAsync(
                        url,
                        conteudo
                    );

                    string respostaTexto =
                        await resposta.Content.ReadAsStringAsync();

                    Console.WriteLine(
                        $"[{i}/{quantidadeDeEnvios}] " +
                        $"Enviado: {json} | " +
                        $"Status: {(int)resposta.StatusCode} | " +
                        $"Resposta: {respostaTexto}"
                    );
                }
                catch (HttpRequestException erro)
                {
                    Console.WriteLine(
                        $"[{i}/{quantidadeDeEnvios}] " +
                        $"Erro ao enviar: {erro.Message}"
                    );
                }

                await Task.Delay(intervaloEmMilissegundos);
            }

            Console.WriteLine("Envio finalizado.");
        }
    }

    public class Leitura
    {
        public float Valor { get; set; }
        public bool Filtro { get; set; }
    }
}