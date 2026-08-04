const express = require('express');
const app = express();
app.use(express.json());

app.post('/leituras', (req, res) => {
    const {valor} = req.body;

    // verifica se tem valor e se é um número
    if (valor === undefined || typeof valor !== 'number') {
        return res.status(400).json({ error: 'Valor inválido' });
    }
    
    res.status(201).json({ message: 'Leitura registrada com sucesso' });

});

app.listen(3000, () => {
    console.log("Servidor rodando na porta 3000");
});