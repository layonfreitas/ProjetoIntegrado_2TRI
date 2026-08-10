const express = require("express");
const { execFile } = require("child_process");
const util = require("util");
const path = require("path");

const execFileAsync = util.promisify(execFile);

const app = express();
const PORT = 3000;

app.use(express.json());

const historico = [];
const TAMANHO_MAX_HISTORICO = 100;

// Caminho do script Python que faz a classificação (IA)
const PYTHON_SCRIPT = path.join(__dirname, "IA", "classificado.py");
// No Windows, geralmente é "python". No Linux/Mac, geralmente é "python3".
// Se PYTHON_BIN não for definido, cai no valor abaixo.
const PYTHON_BIN = "C:\\Users\\Alunos\\AppData\\Local\\Programs\\Python\\Python313\\python.exe";

app.get("/", (req, res) => {
  res.sendFile(__dirname + "/public/index.html");
});

// Chama o script Python e devolve a classificação prevista
async function classificar(valor) {
  // o classificado.py espera um array de features em JSON como argumento,
  // ex: "[45]" -- no nosso caso só temos 1 feature (o valor do sensor)
  const sample = JSON.stringify([valor]);

  let stdout;
  try {
    ({ stdout } = await execFileAsync(PYTHON_BIN, [PYTHON_SCRIPT, sample]));
  } catch (error) {
    throw new Error(error.stderr?.trim() || error.message);
  }

  let resultado;
  try {
    resultado = JSON.parse(stdout);
  } catch (parseError) {
    throw new Error("Resposta inválida do classificador: " + stdout);
  }

  if (resultado.error) {
    throw new Error(resultado.error);
  }

  return resultado; // { classification, probabilities }
}

app.post("/leituras", async (req, res) => {
  const { valor, filtroAtivo } = req.body;

  if (valor === undefined || typeof valor !== "number") {
    return res.status(400).json({
      erro: "Valor inválido.",
    });
  }

  let resultadoIA;
  try {
    resultadoIA = await classificar(valor);
  } catch (erro) {
    console.error("Erro ao classificar com IA:", erro.message);
    return res.status(502).json({
      erro: "Não foi possível classificar a leitura.",
      detalhes: erro.message,
    });
  }

  const leitura = {
    valor,
    filtroAtivo,
    classificacao: resultadoIA.classification,
    probabilidades: resultadoIA.probabilities,
    horario: new Date().toLocaleString(),
  };

  historico.push(leitura);

  if (historico.length > TAMANHO_MAX_HISTORICO) {
    historico.shift();
  }

  res.status(201).json({
    mensagem: "Leitura recebida.",
    leitura,
  });
});

app.get("/leituras", (req, res) => {
  res.json(historico);
});

app.listen(PORT, () => {
  console.log(`Servidor rodando em http://localhost:${PORT}`);
});
