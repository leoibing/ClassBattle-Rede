using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class LobbyManager : MonoBehaviour
{
    public enum ModoDeJogo
    {
        TodosContraTodos = 0,
        Modo2v2 = 1
    }

    [Header("Prefabs das 4 Classes")]
    // [0] Assassino, [1] Atirador, [2] Curandeiro, [3] Tanque
    public GameObject[] prefabsClasses;

    [Header("Componentes de Interface")]
    public TMP_InputField codigoSalaInputField;
    public TMP_Text codigoSalaTexto;
    public TMP_Text statusTexto;

    public GameObject painelMenu;
    public GameObject painelModoDeJogo;

    [Header("Seleção")]
    public int classeSelecionada = 0;
    public ModoDeJogo modoSelecionado =
        ModoDeJogo.TodosContraTodos;

    public static Dictionary<ulong, int> classePorCliente = new();

    public static LobbyManager Instancia { get; private set; }

    private const int MAX_CLIENTES_ADICIONAIS = 3;
    private const string TIPO_CONEXAO_RELAY = "dtls";

    private bool operacaoEmAndamento;
    private bool callbacksConfigurados;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ConfigurarCallbackDeConexao();

        if (painelModoDeJogo != null)
            painelModoDeJogo.SetActive(false);

        if (painelMenu != null)
            painelMenu.SetActive(true);
    }

    private void ConfigurarCallbackDeConexao()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("NetworkManager não encontrado!");
            return;
        }

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = ProcessarConexao;

        if (!callbacksConfigurados)
        {
            nm.OnClientConnectedCallback += AoConectarCliente;
            nm.OnClientDisconnectCallback += AoDesconectarCliente;

            callbacksConfigurados = true;
        }
    }

    private async Task PrepararServicosUnity()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private bool ObterComponentesRede(
        out NetworkManager nm,
        out UnityTransport transport)
    {
        nm = NetworkManager.Singleton;
        transport = null;

        if (nm == null)
        {
            Debug.LogError("NetworkManager não encontrado!");
            return false;
        }

        transport = nm.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("UnityTransport não encontrado!");
            return false;
        }

        return true;
    }

    public void SelecionarClasse(int index)
    {
        if (prefabsClasses == null ||
            index < 0 ||
            index >= prefabsClasses.Length ||
            prefabsClasses[index] == null)
        {
            Debug.LogError("Índice de classe inválido!");
            return;
        }

        classeSelecionada = index;

        Debug.Log(
            "Classe selecionada: " +
            prefabsClasses[index].name
        );
    }

    public void SelecionarModo(int modoIndex)
    {
        if (modoIndex < 0 || modoIndex > 1)
        {
            Debug.LogError("Modo de jogo inválido!");
            return;
        }

        modoSelecionado = (ModoDeJogo)modoIndex;

        Debug.Log("Modo selecionado: " + modoSelecionado);
    }

    // BOTÃO JOGAR DA CENA 0
    // Com código preenchido: entra na sala.
    // Sem código: abre o painel para criar uma sala.
    public async void BotaoJogar()
    {
        if (operacaoEmAndamento)
            return;

        string codigo = codigoSalaInputField != null
            ? codigoSalaInputField.text.Trim()
            : "";

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            await EntrarNaSala(codigo);
            return;
        }

        if (painelMenu != null)
            painelMenu.SetActive(false);

        if (painelModoDeJogo != null)
            painelModoDeJogo.SetActive(true);
    }

    // BOTÃO CRIAR SALA
    public async void BotaoHostIniciarJogo()
    {
        if (operacaoEmAndamento)
            return;

        if (!ObterComponentesRede(out NetworkManager nm, out UnityTransport transport))
            return;

        if (nm.IsListening)
        {
            Debug.LogWarning("Este NetworkManager já está conectado.");
            return;
        }

        if (prefabsClasses == null ||
            classeSelecionada < 0 ||
            classeSelecionada >= prefabsClasses.Length ||
            prefabsClasses[classeSelecionada] == null)
        {
            Debug.LogError("A classe selecionada não é válida.");
            return;
        }

        operacaoEmAndamento = true;
        AtualizarStatus("Criando sala...");

        try
        {
            await PrepararServicosUnity();

            // Host + até 3 jogadores adicionais.
            var allocation =
                await RelayService.Instance.CreateAllocationAsync(
                    MAX_CLIENTES_ADICIONAIS
                );

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(
                    allocation,
                    TIPO_CONEXAO_RELAY
                )
            );

            // Obtém o código que será compartilhado com os clientes.
            string codigo =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId
                );

            EnviarPayload();

            classePorCliente.Clear();

            if (!nm.StartHost())
            {
                throw new Exception("Não foi possível iniciar o Host.");
            }

            // O host também precisa ter sua classe registrada.
            classePorCliente[nm.LocalClientId] = classeSelecionada;

            if (codigoSalaTexto != null)
            {
                codigoSalaTexto.text = "Código da sala: " + codigo;
            }

            AtualizarStatus(
                "Sala criada! Compartilhe o código com os jogadores."
            );

            Debug.Log("Sala Relay criada. Código: " + codigo);
            Debug.Log("Host iniciado. Aguardando jogadores.");
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao criar sala Relay: " + e);
            AtualizarStatus("Erro ao criar sala: " + e.Message);
        }
        finally
        {
            operacaoEmAndamento = false;
        }
    }

    // ENTRAR NA SALA COMO CLIENTE
    private async Task EntrarNaSala(string codigo)
    {
        if (!ObterComponentesRede(out NetworkManager nm, out UnityTransport transport))
            return;

        if (nm.IsListening)
        {
            Debug.LogWarning("Este NetworkManager já está conectado.");
            return;
        }

        if (prefabsClasses == null ||
            classeSelecionada < 0 ||
            classeSelecionada >= prefabsClasses.Length ||
            prefabsClasses[classeSelecionada] == null)
        {
            Debug.LogError("A classe selecionada não é válida.");
            return;
        }

        operacaoEmAndamento = true;
        AtualizarStatus("Conectando à sala...");

        try
        {
            await PrepararServicosUnity();

            var allocation =
                await RelayService.Instance.JoinAllocationAsync(
                    joinCode: codigo.Trim().ToUpperInvariant()
                );

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(
                    allocation,
                    TIPO_CONEXAO_RELAY
                )
            );

            EnviarPayload();

            // O cliente NÃO carrega a cena localmente.
            // Ele será sincronizado com a cena do host pelo NGO.
            if (!nm.StartClient())
            {
                throw new Exception("Não foi possível iniciar o Cliente.");
            }

            AtualizarStatus("Conectando... Aguarde o host iniciar a partida.");

            Debug.Log("Cliente iniciado. Código utilizado: " + codigo);
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao entrar na sala: " + e);
            AtualizarStatus("Erro ao entrar na sala: " + e.Message);
        }
        finally
        {
            operacaoEmAndamento = false;
        }
    }

    // NOVO BOTÃO: INICIAR PARTIDA
    // Deve ser chamado pelo Host depois que os jogadores entrarem.
    public void BotaoIniciarPartida()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsHost)
        {
            Debug.LogWarning("Somente o Host pode iniciar a partida.");
            return;
        }

        if (nm.SceneManager == null)
        {
            Debug.LogError("NetworkSceneManager não está disponível.");
            return;
        }

        AtualizarStatus("Iniciando partida...");

        var resultado = nm.SceneManager.LoadScene(
            "1",
            LoadSceneMode.Single
        );

        if (resultado != SceneEventProgressStatus.Started)
        {
            Debug.LogError(
                "Falha ao iniciar o carregamento da cena 1: " +
                resultado
            );
        }
    }

    private void EnviarPayload()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || nm.NetworkConfig == null)
            return;

        byte[] payload =
            BitConverter.GetBytes(classeSelecionada);

        nm.NetworkConfig.ConnectionData = payload;
    }

    private void ProcessarConexao(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = false;
        response.CreatePlayerObject = false;

        if (request.Payload == null || request.Payload.Length < 4)
        {
            response.Reason = "Dados da classe não recebidos.";
            return;
        }

        int classeIndex =
            BitConverter.ToInt32(request.Payload, 0);

        if (prefabsClasses == null ||
            classeIndex < 0 ||
            classeIndex >= prefabsClasses.Length ||
            prefabsClasses[classeIndex] == null)
        {
            response.Reason = "Classe selecionada inválida.";
            return;
        }

        classePorCliente[request.ClientNetworkId] = classeIndex;

        response.Approved = true;

        Debug.Log(
            "Cliente aprovado: " +
            request.ClientNetworkId +
            " | Classe: " +
            classeIndex
        );
    }

    private void AoConectarCliente(ulong clientId)
    {
        Debug.Log("Cliente conectado: " + clientId);

        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null &&
            clientId == nm.LocalClientId &&
            !nm.IsServer)
        {
            AtualizarStatus("Conectado! Aguardando o host iniciar a partida.");
        }
    }

    private void AoDesconectarCliente(ulong clientId)
    {
        Debug.Log("Cliente desconectado: " + clientId);

        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null && clientId == nm.LocalClientId)
        {
            AtualizarStatus("Você foi desconectado da sala.");
        }
    }

    private void AtualizarStatus(string mensagem)
    {
        Debug.Log(mensagem);

        if (statusTexto != null)
            statusTexto.text = mensagem;
    }

    public ModoDeJogo ObterModoSelecionado()
    {
        return modoSelecionado;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && callbacksConfigurados)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AoConectarCliente;
            NetworkManager.Singleton.OnClientDisconnectCallback -= AoDesconectarCliente;
        }

        if (Instancia == this)
            Instancia = null;
    }
}