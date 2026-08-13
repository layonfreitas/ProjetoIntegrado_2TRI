﻿using System;
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
            bool rodando = true;

            SerialPort port = new SerialPort();
            port.PortName = "COM5"; // Caso seja necessário, altere para a porta correta
            port.BaudRate = 115200; // Configura a velocidade de transmissão
            port.DataBits = 8; // Quantidade de bits de dados
            port.Parity = Parity.None; // Tipo de paridade para detecção de erros
            port.StopBits = StopBits.One; // Quantidade de bits de parada

            port.Open();
            Console.WriteLine("Porta serial aberta. Estamos conectados na porta " + port.PortName + " com velocidade de " + port.BaudRate + " bps.");

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

                    int checksumCalculado = frame[0] ^ frame[1] ^ frame[2] ^ frame[3] ^ frame[4];

                    if (checksumCalculado == frame[5])
                    {
                        int valorEscalado = (frame[2] << 8) | frame[3]; // remonta byte alto + byte baixo
                        float valorPot = (valorEscalado / 4095.0f) * 100.0f; // converte ADC bruto para %

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
                         + "\"valor\":" + valorPot.ToString(CultureInfo.InvariantCulture)
                         + ","
                         + "\"filtroAtivo\":" + filtro
                         + "}";
                        Console.WriteLine("JSON gerado: " + json);

                        WebClient client = new WebClient();
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";

                        string resposta = client.UploadString(
                            "http://127.0.0.1:3000/leituras",
                            "POST",
                            json
                        );

                        Console.WriteLine(resposta);
                    }
                    else
                    {
                        Console.WriteLine("Pacote corrompido, descartado.");
                    }
                }
            }
        }
    }
}