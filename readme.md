MARCELO cuiudo e layon macio

using System;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Management;

namespace ProjetoCs
{
    class Program
    {
        static void Main(string[] args)
        {
            bool rodando = true;

            // Pega todas as portas COM disponíveis
            string[] portas = SerialPort.GetPortNames();

            if (portas.Length == 0)
            {
                Console.WriteLine("Nenhuma porta COM encontrada.");
                return;
            }

            // Ordena as portas
            Array.Sort(portas);

            Console.WriteLine("PORTAS COM DISPONÍVEIS");

        for (int i = 0; i < portas.Length; i++)
        {
        string nome = ObterNomeDaPorta(portas[i]);

        Console.WriteLine($"{i + 1} - {portas[i]} - {nome}");
        }

            Console.WriteLine();
            Console.Write("Escolha o número da porta COM: ");

            int escolha;

            while (!int.TryParse(Console.ReadLine(), out escolha) ||
                   escolha < 1 ||
                   escolha > portas.Length)
            {
                Console.Write("Opção inválida. Escolha novamente: ");
            }

            // Pega a porta escolhida
            string portaEscolhida = portas[escolha - 1];

            Console.WriteLine();
            Console.WriteLine("Porta escolhida: " + portaEscolhida);

            SerialPort port = new SerialPort();

            port.PortName = portaEscolhida;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;

            try // tenta fazer tudo que está dentro do try
            {
                port.Open();

                Console.WriteLine(
                    "Porta serial aberta. Estamos conectados na porta "
                    + port.PortName
                    + " com velocidade de "
                    + port.BaudRate
                    + " bps."
                );

                int[] frame = new int[6];

                while (rodando)
                {
                    int b = port.ReadByte();

                    if (b == 0xAA)
                    {
                        frame[0] = 0xAA;
                        frame[1] = port.ReadByte(); // tipo
                        frame[2] = port.ReadByte(); // valor - byte alto
                        frame[3] = port.ReadByte(); // valor - byte baixo
                        frame[4] = port.ReadByte(); // filtro
                        frame[5] = port.ReadByte(); // checksum recebido

                        int checksumCalculado =
                            frame[0] ^
                            frame[1] ^
                            frame[2] ^
                            frame[3] ^
                            frame[4];

                        if (checksumCalculado == frame[5])
                        {
                            int valorEscalado =
                                (frame[2] << 8) | frame[3];

                            float valorPot =
                                (valorEscalado / 4095.0f) * 100.0f;

                            bool filtroAtivo = frame[4] == 1;

                            string filtro;

                            if (filtroAtivo)
                            {
                                filtro = "true";
                            }
                            else
                            {
                                filtro = "false";
                            }

                            string json = "{"
                                + "\"valor\":"
                                + valorPot.ToString(
                                    CultureInfo.InvariantCulture)
                                + ","
                                + "\"filtroAtivo\":"
                                + filtro
                                + "}";

                            Console.WriteLine("JSON gerado: " + json);

                            WebClient client = new WebClient();

                            client.Headers[HttpRequestHeader.ContentType] =
                                "application/json";

                            string resposta = client.UploadString(
                                "http://127.0.0.1:3000/leituras",
                                "POST",
                                json
                            );

                            Console.WriteLine(resposta);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Pacote corrompido, descartado."
                            );
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException) // evita problemas no codigo
            {
                Console.WriteLine(
                    "Não foi possível acessar a porta. " +
                    "Ela pode estar sendo usada por outro programa."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
            finally // independente do que aconteceu, realiza isso no final
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
        }
        static string ObterNomeDaPorta(string porta)
        {
        using (ManagementObjectSearcher searcher =
        new ManagementObjectSearcher(
            "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(" + porta + ")%'"))
        {
        foreach (ManagementObject device in searcher.Get())
        {
            return device["Name"]?.ToString() ?? "Dispositivo desconhecido";
        }
        }

        return "Dispositivo desconhecido";
}
    }
}
