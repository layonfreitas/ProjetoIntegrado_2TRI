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

            while (rodando)
            {
                string data = port.ReadLine();
                data = data.Trim();

                string[] partes = data.Split(',');

                if (partes.Length == 2 &&
                    float.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float valorPot) &&
                    int.TryParse(partes[1], out int filtroAtivo)) // Verifica se a leitura é válida
                {

                    string filtro;

                    if (filtroAtivo == 1)
                    {
                        filtro = "true";
                    }
                    else
                    {
                        filtro = "false";
                    }



                    string json = "{" // cria a variável json, e abre o objeto JSON
                     + "\"valor\":" + valorPot.ToString(CultureInfo.InvariantCulture) // Adiciona o valor da leitura ao JSON e faz que o numero seja escrito com ponto ao invés de vírgula, para que seja aceito pelo JSON
                     + "," // Adiciona uma vírgula para separar os campos do JSON
                     + "\"filtroAtivo\":" + filtro // Adiciona o valor do filtro ao JSON true ou false
                     + "}"; // Fecha o objeto JSON
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
            }

        }
    }
}