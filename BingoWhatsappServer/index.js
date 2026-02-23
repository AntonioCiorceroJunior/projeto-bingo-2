const wppconnect = require('@wppconnect-team/wppconnect');
const express = require('express');
const cors = require('cors');
require('dotenv').config();

const app = express();
app.use(express.json());
app.use(cors());

// Variáveis de Estado
let wppClient = null;
let currentQrCode = ''; // Base64 image
let currentStatus = 'disconnected'; // disconnected, waiting_qr, connecting, connected
let lastError = '';

// ==========================================
// API REST (Para o BingoAdmin C# consumir)
// ==========================================

// 1. Status da Conexão e QR Code
app.get('/status', (req, res) => {
    res.json({
        status: currentStatus,
        qrCode: currentQrCode || null,
        error: lastError
    });
});

// 2. Enviar Mensagem (C# -> WhatsApp)
app.post('/send-text', async (req, res) => {
    if (!wppClient || currentStatus !== 'connected' && currentStatus !== 'inChat') {
        return res.status(400).json({ error: 'WhatsApp não conectado' });
    }

    const { phone, message } = req.body;
    
    // Tratamento básico de número
    let formattedPhone = phone.replace(/\D/g, ''); 
    // Assumindo BR se não tiver DDI
    if (formattedPhone.length <= 11) formattedPhone = '55' + formattedPhone; 
    
    // Adiciona sufixo WPP
    if (!formattedPhone.includes('@c.us')) formattedPhone += '@c.us';

    try {
        await wppClient.sendText(formattedPhone, message);
        console.log(`Mensagem enviada para ${formattedPhone}`);
        res.json({ success: true });
    } catch (err) {
        console.error('Erro ao enviar mensagem:', err);
        res.status(500).json({ error: err.message });
    }
});

// ==========================================
// LÓGICA DO ROBÔ WPPCONNECT
// ==========================================

function startWpp() {
    console.log('Iniciando WPPConnect...');
    currentStatus = 'starting';
    
    wppconnect.create({
        session: 'bingo-admin-bot',
        headless: true, // "Invisible" browser
        debug: false,
        logQR: false, // Vamos pegar o QR via callback para API
        catchQR: (base64Qr, asciiQR) => {
            console.log('📷 Novo QR Code gerado (Disponível na API /status)');
            currentQrCode = base64Qr;
            currentStatus = 'waiting_qr';
        },
        statusFind: (statusSession, session) => {
            console.log('Status Session:', statusSession);
            currentStatus = statusSession; // isLogged, notLogged, browserClose, qrReadSuccess, etc.
            
            if (statusSession === 'inChat' || statusSession === 'isLogged') {
                currentQrCode = ''; // Limpa QR após conectar
            }
        },
    })
    .then((client) => {
        wppClient = client;
        currentStatus = 'connected';
        start(client);
    })
    .catch((error) => {
        console.error('Erro Fatal WPP:', error);
        lastError = error.message;
        currentStatus = 'error';
    });
}

// Controle de Estado da Conversa (Memória Volátil por enquanto)
const UserStates = {
    MENU: 'MENU',
    COMPRANDO: 'COMPRANDO',
    AGUARDANDO_PAGAMENTO: 'AGUARDANDO_PAGAMENTO'
};
const conversationState = {};

function start(client) {
  console.log('✅ Robô do Bingo Conectado e Pronto!');

  client.onMessage(async (message) => {
    // Ignora status
    if (message.from === 'status@broadcast') return;

    // 1. Lógica de GRUPO
    if (message.isGroupMsg) {
        // Por enquanto, apenas observa. Se alguém mandar !ajuda, responde marcando.
        if (message.body?.toLowerCase() === '!ajuda') {
            await client.reply(message.from, 'Olá! Para comprar cartelas ou tirar dúvidas, me chame no privado! 🎱', message.id);
        }
        return;
    }

    // 2. Lógica de PRIVADO (Funil de Vendas)
    const userId = message.from;
    const userState = conversationState[userId] || { step: UserStates.MENU };

    try {
        // Reset manual
        if (message.body?.toLowerCase() === 'menu' || message.body?.toLowerCase() === 'oi') {
            userState.step = UserStates.MENU;
            conversationState[userId] = userState;
        }

        switch (userState.step) {
            case UserStates.MENU:
                await enviarMenuPrincipal(client, userId);
                conversationState[userId] = { step: UserStates.COMPRANDO }; // Avança estado
                break;

            case UserStates.COMPRANDO:
                // Simulação de fluxo
                if (message.body === '1') {
                    await client.sendText(userId, 'Ótimo! Quantas cartelas você deseja comprar? (Digite apenas o número)');
                    userState.step = UserStates.AGUARDANDO_PAGAMENTO;
                } else if (message.body === '2') {
                    await client.sendText(userId, 'Seus números da sorte são: [Consultando Sistema...]');
                    // Aqui chamaria a API do C#
                    userState.step = UserStates.MENU; // Volta pro menu
                } else {
                    await client.sendText(userId, 'Opção inválida. Digite 1 para Comprar ou 2 para Meus Jogos.');
                }
                break;
            
            case UserStates.AGUARDANDO_PAGAMENTO:
                // Aqui entraria a geração do Pix via API Banco Inter
                const qtd = parseInt(message.body);
                if (!isNaN(qtd) && qtd > 0) {
                    const valor = qtd * 10; // R$ 10,00 por cartela (exemplo)
                    await client.sendText(userId, `Entendido! ${qtd} cartelas ficam R$ ${valor},00.\n\nEstou gerando seu Pix Copia e Cola... ⏳`);
                    
                    // TODO: Chamar API do Inter aqui (via C# ou direto)
                    // Por enquanto só simula
                    setTimeout(async () => {
                         await client.sendText(userId, `pix-copia-e-cola-falso-para-teste-123456789\n\nAssim que pagar, me envie o comprovante ou aguarde a aprovação automática!`);
                         userState.step = UserStates.MENU; 
                    }, 2000);
                } else {
                    await client.sendText(userId, 'Por favor, digite um número válido de cartelas.');
                }
                break;
        }
        
        // Salva estado atualizado
        conversationState[userId] = userState;

    } catch (e) {
        console.error('Erro no fluxo:', e);
    }
  });
}

async function enviarMenuPrincipal(client, to) {
    const menu =  
`🎰 *BINGO AUTOMÁTICO* 🎰
Bem-vindo! Eu sou o assistente virtual do Bingo.

Como posso te ajudar hoje?
1️⃣ - *Comprar Cartelas* (Rodada Principal)
2️⃣ - *Ver Meus Jogos*
3️⃣ - *Falar com Admin*

_Digite o número da opção:_`;
    await client.sendText(to, menu);
}

// Inicia o Servidor API HTTP
const PORT = 3000;
app.listen(PORT, () => {
    console.log(`🚀 API do Robô rodando em http://localhost:${PORT}`);
    // Inicia o WPPConnect
    startWpp();
});
