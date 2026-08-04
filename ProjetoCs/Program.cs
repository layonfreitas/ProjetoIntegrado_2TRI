using System;
using System.IO.Ports;
using System.Text.Json;

namespace ProjetoCs
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort port = new SerialPort();
            port.PortName = "COM3"; // Caso seja necessário, altere para a porta correta
            port.BaudRate = 115200; // Configura a velocidade de transmissão
            port.DataBits = 8; // Quantidade de bits de dados
            port.Parity = Parity.None; // Tipo de paridade para detecção de erros
            port.StopBits = StopBits.One; // Quantidade de bits de parada

            port.Open();
            Console.WriteLine("Porta serial aberta. Estamos conectados na porta " + port.PortName + " com velocidade de " + port.BaudRate + " bps.");

            while (true)
            {
                try
                {
                    string data = port.ReadLine();
                    data = data.Trim(); // remove \r, \n e espaços que vêm junto na serial

                    if (int.TryParse(data, out int valorPot))
                    {
                        Console.WriteLine("Valor do potenciômetro: " + valorPot);

                        var leitura = new { valor = valorPot };
                        string json = JsonSerializer.Serialize(leitura);
                        Console.WriteLine("JSON gerado: " + json);
                    }
                    else
                    {
                        Console.WriteLine("Dado inválido recebido, ignorando: " + data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao ler dados da porta serial: " + ex.Message);
                }
            }
        }
    }
}