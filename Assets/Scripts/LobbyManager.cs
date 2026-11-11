using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;

public class LobbyManager : MonoBehaviour
{
	public enum ModoDeJogo { TodosContraTodos = 0, Modo2v2 = 1 }

	[Header("Prefabs das 4 Classes")]
	// Arraste no Inspector: [0] Assassino, [1] Atirador, [2] Curandeiro, [3] Tanque
	public GameObject[] prefabsClasses;

	[Header("Componentes de Interface (UI)")]
	public TMP_InputField ipInputField;
	public GameObject painelMenu;
	public GameObject painelModoDeJogo;
	public int classeSelecionada = 0; // Padrão: 0 (Assassino)
	public ModoDeJogo modoSelecionado = ModoDeJogo.TodosContraTodos;

	// Dicionário no Servidor para armazenar a classe de cada Cliente pelo ClientId
	public static Dictionary<ulong, int> classePorCliente = new();

	public static LobbyManager Instancia { get; private set; }

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

		// Garante que ao iniciar a Cena 0, o painel de modo de jogo começa fechado
		if (painelModoDeJogo != null) painelModoDeJogo.SetActive(false);
		if (painelMenu != null) painelMenu.SetActive(true);
	}

	private void ConfigurarCallbackDeConexao()
	{
		// Ativa a verificação/aprovação de conexões no Servidor
		if (NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
			NetworkManager.Singleton.ConnectionApprovalCallback = ProcessarConexao;
		}
	}

	// Chamado ao clicar nos botões das classes na UI
	public void SelecionarClasse(int index)
	{
		classeSelecionada = index;
		Debug.Log("Classe selecionada: " + prefabsClasses[index].name);
	}

	public void SelecionarModo(int modoIndex)
	{
		modoSelecionado = (ModoDeJogo)modoIndex;
		Debug.Log("Modo selecionado: " + modoSelecionado);
	}

	// BOTÃO JOGAR DA CENA 0 (Menu principal para todos)
	public void BotaoJogar()
	{
		if (NetworkManager.Singleton == null)
		{
			Debug.LogError("ERRO: NetworkManager não encontrado na cena!");
			return;
		}

		if (!NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
		{
			Debug.LogError("ERRO: Componente UnityTransport não encontrado!");
			return;
		}

		// Lê o IP digitado (se houver)
		string ip = (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text)) ? ipInputField.text.Trim() : "";

		// Se digitou IP ou se já existe um Host na rede, ele é CLIENTE
		bool ehCliente = !string.IsNullOrEmpty(ip) && ip != "127.0.0.1" && ip != "localhost";
		if (!ehCliente && HostJaExisteNaPorta(7777))
		{
			ehCliente = true;
			ip = "127.0.0.1";
		}

		if (ehCliente)
		{
			// É CLIENTE: Configura conexão e vai direto para a Cena 1 (Jogo)
			if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
			transport.SetConnectionData(ip, 7777);
			EnviarPayload();

			Debug.Log("Entrando como Cliente...");
			NetworkManager.Singleton.StartClient();
			SceneManager.LoadScene("1"); // Vai direto para a Cena do jogo
		}
		else
		{
			Debug.Log("Nenhum Host encontrado. Você é o Host! Abrindo painel de modo de jogo...");
			if (painelMenu != null) painelMenu.SetActive(false);
			if (painelModoDeJogo != null) painelModoDeJogo.SetActive(true);
		}
	}

	// CLICOU NO BOTÃO "JOGAR AGORA" DO PAINEL DE HOST
	public void BotaoHostIniciarJogo()
	{
		if (NetworkManager.Singleton == null)
		{
			Debug.LogError("ERRO: NetworkManager não encontrado na cena!");
			return;
		}

		if (!NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
		{
			Debug.LogError("ERRO: UnityTransport não encontrado!");
			return;
		}

		// 2. Se não digitou IP (ou é local), verifica se a porta 7777 já está sendo usada por um Host nesta máquina
		if (HostJaExisteNaPorta(7777))
		{
			Debug.LogWarning("Já existe um Host ativo na rede! Convertendo este jogador em Cliente...");
			transport.SetConnectionData("127.0.0.1", 7777);
			EnviarPayload();

			NetworkManager.Singleton.StartClient();
			SceneManager.LoadScene("1");
			return;
		}

		transport.SetConnectionData("127.0.0.1", 7777);
		EnviarPayload();

		Debug.Log("Iniciando Host com o modo: " + modoSelecionado);
		if (NetworkManager.Singleton.StartHost())
		{
			if (NetworkManager.Singleton.SceneManager != null)
			{
				// Carrega a Cena do Jogo
				NetworkManager.Singleton.SceneManager.LoadScene("1", LoadSceneMode.Single);
			}
			else
			{
				SceneManager.LoadScene("1");
			}
		}
		else
		{
			Debug.LogWarning("Falha ao iniciar o Host. Convertendo em Cliente...");
			NetworkManager.Singleton.StartClient();
			SceneManager.LoadScene("1");
		}
	}

	private bool HostJaExisteNaPorta(int porta)
	{
		try
		{
			IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] listeners = properties.GetActiveUdpListeners();

			foreach (IPEndPoint listener in listeners)
			{
				if (listener.Port == porta)
				{
					return true; // Encontrou a porta 7777 em uso (Host ativo)
				}
			}
		}
		catch
		{
			// Caso ocorra restrição de permissão de rede no SO
		}
		return false;
	}
	
	private void EnviarPayload()
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
		{
			// Cria um payload fixo de 4 bytes para sempre dar Match na configuração
			byte[] payload = new byte[4];
			System.BitConverter.GetBytes(classeSelecionada).CopyTo(payload, 0);
			NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;
		}
	}

	private void ProcessarConexao(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
	{
		// Lê a classe enviada no payload do cliente que está se conectando
		int classeIndex = 0;

		if (request.Payload != null && request.Payload.Length >= 4)
		{
			classeIndex = System.BitConverter.ToInt32(request.Payload, 0);
		}

		// Salva a classe referente ao ID desse cliente específico
		classePorCliente ??= new Dictionary<ulong, int>();
		classePorCliente[request.ClientNetworkId] = classeIndex;

		response.Approved = true;
		response.CreatePlayerObject = false; // O GameManager da Cena 1 vai instanciar
	}

	public ModoDeJogo ObterModoSelecionado() => modoSelecionado;
}