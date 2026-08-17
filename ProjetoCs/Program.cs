﻿using System;
using System.Globalization;
using System.IO.Ports;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace ProjetoCs
{
    class Program
    {
        static readonly HttpClient client = new HttpClient(); // client HTTP para enviar dados para a API


        static async Task Main(string[] args)
        {
            bool rodando = true;


           
            // PEGA TODAS AS PORTAS COM DISPONÍVEIS
            string[] portas = SerialPort.GetPortNames();


            if (portas.Length == 0)
            {
                Console.WriteLine("Nenhuma porta COM encontrada.");
                return;
            }


            Array.Sort(portas);


            Console.WriteLine("PORTAS COM DISPONÍVEIS");
            Console.WriteLine();


            for (int i = 0; i < portas.Length; i++)
            {
                string nome = ObterNomeDaPorta(portas[i]);


                Console.WriteLine(
                    $"{i + 1} - {portas[i]} - {nome}"
                );
            }


            Console.WriteLine();
            Console.Write("Escolha o número da porta COM: ");


            int escolha;


            while (!int.TryParse(Console.ReadLine(), out escolha) ||  escolha < 1 || escolha > portas.Length)
            {
                Console.Write("Opção inválida. Escolha novamente: ");
            }
            // PORTA ESCOLHIDA
            string portaEscolhida = portas[escolha - 1];


            Console.WriteLine();
            Console.WriteLine("Porta escolhida: " + portaEscolhida);

            // CONFIGURA SERIAL
            SerialPort port = new SerialPort(); // Cria o objeto que representa a comunicação com a COM


            port.PortName = portaEscolhida; // Define a porta COM escolhida
            port.BaudRate = 115200; // Define a velocidade de comunicação
            port.DataBits = 8; // Define o número de bits de dados
            port.Parity = Parity.None; // Define a paridade (nenhuma)
            port.StopBits = StopBits.One; // Define o número de bits de parada


            // Evita ficar bloqueado indefinidamente
            port.ReadTimeout = 1000;


            try
            {
                port.Open(); // Abre a porta serial para comunicação


                Console.WriteLine();
                Console.WriteLine("Porta serial aberta. Estamos conectados na porta " + port.PortName + " com velocidade de " + port.BaudRate + " bps.");
                Console.WriteLine();
                Console.WriteLine("Aguardando dados do STM32...");


                Console.WriteLine();

                // LOOP DE LEITURA
                while (rodando)
                {
                    int b;


                    try
                    {
                        b = port.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }


                    // Procura pelo início do protocolo
                    if (b != 0xAA)
                    {
                        continue;
                    }

                    // RECEBE FRAME DE 5 BYTES
                    int[] frame = new int[5];


                    frame[0] = 0xAA;


                    try
                    {
                        frame[1] = port.ReadByte(); // byte alto
                        frame[2] = port.ReadByte(); // byte baixo
                        frame[3] = port.ReadByte(); // filtro
                        frame[4] = port.ReadByte(); // checksum
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Frame incompleto.");


                        continue;
                    }

                    // CALCULA CHECKSUM
                    int checksumCalculado = frame[0] ^ frame[1] ^ frame[2] ^ frame[3];

                    if (checksumCalculado != frame[4])
                    {
                        Console.WriteLine("Pacote corrompido, descartado.");
                        continue;
                    }

                    // RECONSTRÓI VALOR DO ADC
                    int valorEscalado = (frame[1] << 8) | frame[2];


                    // CONVERTE ADC PARA PORCENTAGEM
                    float valorPot = (valorEscalado / 4095.0f) * 100.0f;

                    // ESTADO DO FILTRO
                    bool filtroAtivo = frame[3] == 1;

                    string filtro;


                    if (filtroAtivo)
                    {
                        filtro = "true";
                    }
                    else
                    {
                        filtro = "false";
                    }

                    // MONTA JSON
                    string json =
                        "{"
                        + "\"valor\":"
                        + valorPot.ToString(CultureInfo.InvariantCulture)
                        + ","
                        + "\"filtroAtivo\":"
                        + filtro
                        + "}";

                    // MOSTRA IMEDIATAMENTE NO CONSOLE
                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff} | " +
                        $"ADC: {valorEscalado} | " +
                        $"Pot: {valorPot:F1}% | " +
                        $"Filtro: {filtro}"
                    );


                    Console.WriteLine(
                        "JSON gerado: " + json
                    );

                    // ENVIA PARA API
                    _ = EnviarParaAPI(json);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(
                    "Não foi possível acessar a porta. " +
                    "Ela pode estar sendo usada por outro programa."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Erro: " + ex.Message
                );
            }
            finally
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
        }

        // ENVIA PARA API
        static async Task EnviarParaAPI(string json)
        {
            try
            {
                using StringContent content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );


                HttpResponseMessage resposta =
                    await client.PostAsync(
                        "http://127.0.0.1:3000/leituras",
                        content
                    );


                string textoResposta =
                    await resposta.Content.ReadAsStringAsync();


                Console.WriteLine(
                    $"API: {resposta.StatusCode} | {textoResposta}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Erro ao enviar para API: "
                    + ex.Message
                );
            }
        }

        // OBTÉM NOME DO DISPOSITIVO DA PORTA COM
        static string ObterNomeDaPorta(string porta)
        {
            using (ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%("
                    + porta
                    + ")%'"))
            {
                foreach (ManagementObject device in searcher.Get())
                {
                    return device["Name"]?.ToString()
                           ?? "Dispositivo desconhecido";
                }
            }


            return "Dispositivo desconhecido";
        }
    }
}