﻿using System;
using System.Globalization;
using System.IO.Ports;
using System.Management;
using System.Net.Http;
using System.Text;

namespace ProjetoCs
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Sistema de leitura STM32");
            Console.WriteLine();

            string? portaEscolhida = SelecionarPorta();

            if (portaEscolhida == null)
            {
                Console.WriteLine("Nenhuma porta foi selecionada.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Porta escolhida: {portaEscolhida}");
            Console.WriteLine();

            SerialPort port = new SerialPort();

            port.PortName = portaEscolhida;
            port.BaudRate = 115200;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            port.ReadTimeout = 1000;

            try
            {
                port.Open();

                Console.WriteLine(
                    $"Porta {port.PortName} aberta com sucesso."
                );

                Console.WriteLine(
                    $"Velocidade: {port.BaudRate} bps"
                );

                Console.WriteLine();
                Console.WriteLine("Aguardando dados do STM32...");
                Console.WriteLine();

                while (true)
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

                    if (b != 0xAA)
                    {
                        continue;
                    }

                    int[] frame = new int[6];

                    frame[0] = 0xAA;

                    try
                    {
                        frame[1] = port.ReadByte();
                        frame[2] = port.ReadByte();
                        frame[3] = port.ReadByte();
                        frame[4] = port.ReadByte();
                        frame[5] = port.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Frame incompleto.");
                        continue;
                    }

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

                    int valorEscalado =
                        (frame[2] << 8) | frame[3];

                    float valorPot =
                        (valorEscalado / 4095.0f) * 100.0f;

                    bool filtroAtivo = frame[4] == 1;

                    string json =
                        "{"
                        + "\"valor\":"
                        + valorPot.ToString(
                            CultureInfo.InvariantCulture)
                        + ","
                        + "\"filtroAtivo\":"
                        + filtroAtivo.ToString().ToLowerInvariant()
                        + "}";

                    Console.WriteLine(
                        $"{DateTime.Now:HH:mm:ss.fff} | " +
                        $"ADC: {valorEscalado} | " +
                        $"Pot: {valorPot:F1}% | " +
                        $"Filtro: {filtroAtivo}"
                    );

                    Console.WriteLine(
                        "JSON: " + json
                    );

                    EnviarParaAPI(json);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(
                    "Não foi possível acessar a porta."
                );

                Console.WriteLine(
                    "Ela pode estar sendo usada por outro programa."
                );
            }
            catch (IOException)
            {
                Console.WriteLine(
                    "A porta foi desconectada ou deixou de estar disponível."
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

        static string? SelecionarPorta()
        {
            string[] portas = SerialPort.GetPortNames();

            if (portas.Length == 0)
            {
                Console.WriteLine(
                    "Nenhuma porta COM foi encontrada."
                );

                return null;
            }

            Array.Sort(portas);

            Console.WriteLine("Portas COM disponíveis");
            Console.WriteLine();

            for (int i = 0; i < portas.Length; i++)
            {
                string? nome =
                    ObterNomeCompletoDaPorta(portas[i]);

                if (string.IsNullOrEmpty(nome))
                {
                    nome = "Dispositivo desconhecido";
                }

                Console.WriteLine(
                    $"{i + 1} - {nome}"
                );
            }

            Console.WriteLine();

            while (true)
            {
                Console.Write(
                    "Escolha a porta COM (0 para sair): "
                );

                string? entrada = Console.ReadLine();

                if (!int.TryParse(entrada, out int escolha))
                {
                    Console.WriteLine(
                        "Digite um número válido."
                    );

                    continue;
                }

                if (escolha == 0)
                {
                    return null;
                }

                if (escolha < 1 || escolha > portas.Length)
                {
                    Console.WriteLine(
                        "Opção inválida."
                    );

                    continue;
                }

                return portas[escolha - 1];
            }
        }

        static void EnviarParaAPI(string json)
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
                    client.PostAsync(
                        "http://127.0.0.1:3000/leituras",
                        content
                    ).Result;

                string textoResposta =
                    resposta.Content
                        .ReadAsStringAsync()
                        .Result;

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

        static string? ObterNomeCompletoDaPorta(string porta)
        {
            try
            {
                using ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher(
                        "SELECT Name FROM Win32_PnPEntity"
                    );

                foreach (ManagementObject device in searcher.Get())
                {
                    string? nome =
                        device["Name"] as string;

                    if (string.IsNullOrEmpty(nome))
                    {
                        continue;
                    }

                    if (nome.IndexOf(
                        "(" + porta + ")",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                    {
                        return nome;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Erro ao consultar dispositivos: "
                    + ex.Message
                );
            }

            return null;
        }
    }
}