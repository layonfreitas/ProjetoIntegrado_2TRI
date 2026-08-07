const express = require("express");

const app = express();
const PORT = 3000;

app.use(express.json());

const historico = [];
const TAMANHO_MAX_HISTORICO = 100;

app.get("/", (req, res) => {
      res.sendFile(__dirname + '/public/index.html');
})

app.post("/leituras", (req, res) => {
    const { valor, filtroAtivo } = req.body;

    if (valor === undefined || typeof valor !== "number") {
        return res.status(400).json({
            erro: "Valor inválido."
        });
    }

    const leitura = {
        valor,
        filtroAtivo,
        horario: new Date().toLocaleString()
    };

    historico.push(leitura);

    if (historico.length > TAMANHO_MAX_HISTORICO) {
        historico.shift();
    }


    res.status(201).json({
        mensagem: "Leitura recebida.",
        leitura
    });
});

app.get("/leituras", (req, res) => {
    res.json(historico);
});

app.listen(PORT, () => {
    console.log(`Servidor rodando em http://localhost:${PORT}`);
});