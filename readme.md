# Projeto Integrado 2 TRI - Sistema Inteligente de Monitoramento de Luminosidade

Sistema distribuído de aquisição, processamento e visualização de dados, simulando uma aplicação de Internet das Coisas (IoT) de monitoramento de luminosidade: um microcontrolador STM32 lê um sensor analógico (potenciômetro, que simula um sensor de luminosidade, como um LDR), uma aplicação em C# recebe essa leitura pela porta serial e a envia para um servidor Web, que por sua vez encaminha o valor para um modelo de Inteligência Artificial responsável por classificar o nível de luminosidade da medição. O resultado final é exibido em tempo real em uma interface Web.

## Integrantes do grupo

- Layon Rubens Motta de Freitas 
- Marcelo Vitor Pereira 

# Arquitetura do sistema

```
[Potenciômetro] --> [STM32F103C8T6] --USB CDC / Serial--> [App C#]
                                                              |
                                                       HTTP POST (JSON)
                                                              v
                                                    [API REST - Node.js/Express]
                                                              |
                                                    chama o classificador
                                                              v
                                                   [Modelo IA - Python/scikit-learn]
                                                              |
                                                     classificação + probabilidades
                                                              v
                                                    [API armazena o histórico]
                                                              |
                                                       HTTP GET (polling)
                                                              v
                                                    [Interface Web - Dashboard]
```

O fluxo de dados é sempre unidirecional, de ponta a ponta: sensor -> microcontrolador -> aplicação intermediária -> API -> IA -> API -> interface Web.

## Estrutura de pastas

```
ProjetoIntegrado_2TRI/
├── ProjetoIntregrado2/       Firmware do STM32 (STM32CubeIDE)
│   ├── Core/Src/main.c       Lógica principal: leitura ADC, filtro e envio serial
│   └── USB_DEVICE/           Configuração do USB CDC (porta COM virtual)
│
├── ProjetoCs/                Aplicação intermediária em C#
│   └── Program.cs            Leitura da porta serial e envio HTTP para a API
│
├── API/                      Servidor Web (Node.js + Express)
│   ├── api.js                API REST, histórico de leituras e chamada da IA
│   ├── IA/
│   │   ├── classificado.py   Script Python que treina e executa o classificador
│   │   └── dataset.csv       Conjunto de dados usado para treinar o modelo
│   └── public/
│       └── index.html        Interface Web (dashboard de monitoramento)
│
└── README.md
```

## Como o sistema funciona

### 1. Aquisição de dados (STM32)

O STM32 realiza a leitura contínua de um potenciômetro conectado a um canal do ADC. O potenciômetro simula um sensor de luminosidade (como um LDR), permitindo variar manualmente o "nível de luz" percebido pelo sistema. Um botão ligado a um pino GPIO (`Filtro_Pin`) permite ativar ou desativar, em tempo real, um filtro de média móvel de 5 amostras sobre a leitura do sensor.

A cada 1 segundo, o microcontrolador monta um pacote de 6 bytes e o transmite pela porta serial virtual (USB CDC), no seguinte formato:

| Byte | Conteúdo |
|---|---|
| 0 | Cabeçalho fixo `0xAA` |
| 1 | Tipo do pacote (`0x01`) |
| 2 | Valor do ADC - byte alto |
| 3 | Valor do ADC - byte baixo |
| 4 | Estado do filtro (0 = desligado, 1 = ligado) |
| 5 | Checksum (XOR dos bytes 0 a 4) |

### 2. Comunicação com o servidor (C#)

A aplicação em C# (`ProjetoCs/Program.cs`) lista as portas COM disponíveis, permite que o usuário escolha a porta correta e abre a comunicação serial a 115200 bps. Ao receber um pacote, ela:

1. valida o pacote pelo checksum, descartando pacotes corrompidos;
2. converte o valor bruto do ADC (0 a 4095) para uma escala percentual de 0 a 100, representando o nível de luminosidade;
3. monta um objeto JSON com o valor convertido e o estado do filtro;
4. envia o JSON via requisição HTTP POST para o endpoint `/leituras` da API.

### 3. API REST (Node.js/Express)

A API (`API/api.js`) expõe os seguintes endpoints:

- `POST /leituras`: recebe a leitura enviada pela aplicação C#, chama o script de classificação em Python, armazena o resultado em um histórico em memória (últimas 100 leituras) e retorna a leitura processada.
- `GET /leituras`: retorna o histórico completo de leituras já classificadas, consumido pela interface Web.
- `GET /`: serve a página do dashboard (`public/index.html`).

### 4. Classificação automática (IA)

O script `API/IA/classificado.py` é chamado pela API a cada nova leitura. Ele carrega o conjunto de dados `dataset.csv` (valores do sensor associados a rótulos), treina um modelo de **Árvore de Decisão** (`DecisionTreeClassifier` do scikit-learn) e classifica o nível de luminosidade da leitura recebida em uma das três categorias:

- **Pouca** (ambiente com baixa luminosidade)
- **Media** (nível de iluminação ideal)
- **Muita** (excesso de luminosidade)

Além da classificação, o script também retorna as probabilidades associadas a cada categoria, usadas na interface Web para exibir a confiança da IA na previsão.

### 5. Interface Web (Dashboard)

A página (`API/public/index.html`), intitulada "Sistema Inteligente de Monitoramento de Luminosidade", consulta a API a cada segundo e exibe:

- valor atual da luminosidade (%) e horário da última atualização;
- classificação retornada pela IA e a confiança da previsão;
- estado atual do filtro (ligado ou desligado);
- gráfico com a evolução das medições ao longo do tempo (Chart.js);
- estatísticas em tempo real: média, máximo e mínimo;
- indicação de tendência de luminosidade (crescendo, diminuindo ou estável);
- alertas visuais quando a luminosidade atinge um estado de atenção (muito iluminado ou muito escuro);
- tabela com o histórico das últimas leituras recebidas.

## Decisões de projeto

- O potenciômetro foi usado para simular um sensor de luminosidade (como um LDR), já que ambos entregam uma variação analógica contínua proporcional à grandeza medida, permitindo simular diferentes níveis de luz de forma controlada durante os testes.
- O protocolo serial usa um cabeçalho fixo e checksum simples (XOR) para garantir a integridade dos pacotes recebidos pela aplicação C#, descartando pacotes corrompidos.
- O filtro de média móvel foi escolhido por ser adequado a variáveis físicas contínuas e de variação lenta, como a luminosidade, suavizando ruídos da leitura do ADC.
- O modelo de IA é uma Árvore de Decisão, treinada a cada requisição a partir do `dataset.csv`. A escolha por um modelo simples e interpretável facilita a explicação das decisões de classificação durante a arguição.
- O histórico de leituras é mantido em memória na própria API (sem banco de dados), limitado às últimas 100 leituras, o que é suficiente para o escopo do projeto.
- A interface Web atualiza os dados por polling (requisição HTTP a cada 1 segundo), mantendo a solução simples e sem a necessidade de WebSockets.

## Vídeo de apresentação

https://youtu.be/AaEtk6dAJRc

## Tecnologias utilizadas

- **Firmware**: STM32CubeIDE, HAL, USB Device Library (CDC)
- **Aplicação intermediária**: C# (.NET), System.IO.Ports
- **Servidor**: Node.js, Express
- **Inteligência Artificial**: Python, scikit-learn
- **Interface Web**: HTML, CSS, JavaScript, Chart.js
