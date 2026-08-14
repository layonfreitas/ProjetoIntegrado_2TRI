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
        // Um único HttpClient para todo o programa
        static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            bool rodando = true;

            // =====================================================
            // PEGA TODAS AS PORTAS COM DISPONÍVEIS
            // =====================================================

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

            while (!int.TryParse(Console.ReadLine(), out escolha) ||
                   escolha < 1 ||
                   escolha > portas.Length)
            {
                Console.Write("Opção inválida. Escolha novamente: ");
            }

            // =====================================================
            // PORTA ESCOLHIDA
            // =====================================================

            string portaEscolhida = portas[escolha - 1];

            Console.WriteLine();
            Console.WriteLine(
                "Porta escolhida: " + portaEscolhida
            );

            // =====================================================
            // CONFIGURA SERIAL
            // =====================================================

            SerialPort port = new SerialPort();

            port.PortName = portaEscolhida;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;

            // Evita ficar bloqueado indefinidamente
            port.ReadTimeout = 1000;

            try
            {
                port.Open();

                Console.WriteLine();
                Console.WriteLine(
                    "Porta serial aberta. Estamos conectados na porta "
                    + port.PortName
                    + " com velocidade de "
                    + port.BaudRate
                    + " bps."
                );

                Console.WriteLine();
                Console.WriteLine(
                    "Aguardando dados do STM32..."
                );

                Console.WriteLine();

                // =================================================
                // LOOP DE LEITURA
                // =================================================

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

                    // =================================================
                    // RECEBE FRAME DE 6 BYTES
                    // =================================================

                    int[] frame = new int[6];

                    frame[0] = 0xAA;

                    try
                    {
                        frame[1] = port.ReadByte(); // tipo
                        frame[2] = port.ReadByte(); // byte alto
                        frame[3] = port.ReadByte(); // byte baixo
                        frame[4] = port.ReadByte(); // filtro
                        frame[5] = port.ReadByte(); // checksum
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine(
                            "Frame incompleto."
                        );

                        continue;
                    }

                    // =================================================
                    // CALCULA CHECKSUM
                    // =================================================

                    int checksumCalculado =
                        frame[0] ^
                        frame[1] ^
                        frame[2] ^
                        frame[3] ^
                        frame[4];

                    if (checksumCalculado != frame[5])
                    {
                        Console.WriteLine(
                            "Pacote corrompido, descartado."
                        );

                        continue;
                    }

                    // =================================================
                    // RECONSTRÓI VALOR DO ADC
                    // =================================================

                    int valorEscalado =
                        (frame[2] << 8) | frame[3];

                    // =================================================
                    // CONVERTE ADC PARA PORCENTAGEM
                    // =================================================

                    float valorPot =
                        (valorEscalado / 4095.0f) * 100.0f;

                    // =================================================
                    // ESTADO DO FILTRO
                    // =================================================

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

                    // =================================================
                    // MONTA JSON
                    // =================================================

                    string json =
                        "{"
                        + "\"valor\":"
                        + valorPot.ToString(
                            CultureInfo.InvariantCulture)
                        + ","
                        + "\"filtroAtivo\":"
                        + filtro
                        + "}";

                    // =================================================
                    // MOSTRA IMEDIATAMENTE NO CONSOLE
                    // =================================================

                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff} | " +
                        $"ADC: {valorEscalado} | " +
                        $"Pot: {valorPot:F1}% | " +
                        $"Filtro: {filtro}"
                    );

                    Console.WriteLine(
                        "JSON gerado: " + json
                    );

                    // =================================================
                    // ENVIA PARA API SEM BLOQUEAR A SERIAL
                    // =================================================

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

        // =========================================================
        // ENVIA PARA API
        // =========================================================

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

        // =========================================================
        // OBTÉM NOME DO DISPOSITIVO DA PORTA COM
        // =========================================================

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