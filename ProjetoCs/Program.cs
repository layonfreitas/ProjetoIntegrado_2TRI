using System;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Text.Json;

namespace ProjetoCs
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort port = new SerialPort();

            port.PortName = "COM9";
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;

            port.Open();

            Console.WriteLine("Porta serial aberta.");
            Console.WriteLine("Aguardando dados...\n");

            while (true)
            {
                try
                {
                    string data = port.ReadLine();
                    data = data.Trim();

                    // Formato recebido: 45.32;1
                    string[] partes = data.Split(';');

                    if (partes.Length == 2 &&
                        float.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float valorPot) &&
                        int.TryParse(partes[1], out int filtroAtivo))
                    {
                        Console.WriteLine("Valor: " + valorPot);
                        Console.WriteLine("Filtro: " + filtroAtivo);

                        var leitura = new
                        {
                            valor = valorPot,
                            filtroAtivo = filtroAtivo == 1
                        };

                        string json = JsonSerializer.Serialize(leitura);

                        Console.WriteLine("JSON: " + json);

                        WebClient client = new WebClient();
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";

                        string resposta = client.UploadString(
                            "http://127.0.0.1:3000/leituras", 
                            "POST",
                            json
                        );

                        Console.WriteLine("Resposta da API: " + resposta);
                    }
                    else
                    {
                        Console.WriteLine("Dado inválido: " + data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro: " + ex.Message);
                }
            }
        }
    }
}