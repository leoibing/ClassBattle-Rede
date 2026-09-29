using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
	[Header("Prefabs das Classes")]
	public GameObject[] prefabsClasses; // [0] Assassino, [1] Atirador, [2] Curandeiro, [3] Tanque

	[Header("Spawns")]
	public Transform[] pontosSpawnFFA;
	public Transform[] pontosSpawnTimeA;
	public Transform[] pontosSpawnTimeB;

	private int contagemJogadores = 0;

	// Lista para impedir o erro "Already Spawned" (Impede duplicação de jogadores)
	private readonly HashSet<ulong> jogadoresSpawnados = new();

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		NetworkManager.Singleton.OnClientConnectedCallback += AoConectarCliente;

		// Spawn do Host que já estava conectado ao carregar a cena
		foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
		{
			SpawnarJogador(client.ClientId);
		}
	}

	private void AoConectarCliente(ulong clientId)
	{
		if (IsServer) SpawnarJogador(clientId);
	}

	private void SpawnarJogador(ulong clientId)
	{
		// Trava de segurança: Se o jogador já nasceu, ignora!
		if (jogadoresSpawnados.Contains(clientId)) return;
		jogadoresSpawnados.Add(clientId);

		// 1. Busca a classe do jogador específico no Dicionário
		int indexClasse = 0;

		if (LobbyManager.classePorCliente != null && LobbyManager.classePorCliente.TryGetValue(clientId, out int classeSalva))
		{
			indexClasse = classeSalva;
		}

		// Validação de segurança do índice da classe
		if (prefabsClasses == null || prefabsClasses.Length == 0)
		{
			Debug.LogError("ERRO: O array prefabsClasses está vazio no GameManager!");
			return;
		}

		// Evita estouro de array
		indexClasse = Mathf.Clamp(indexClasse, 0, prefabsClasses.Length - 1);

		// 2. O MODO DE JOGO é lido estritamente do HOST (ignorando a escolha dos clientes)
		LobbyManager.ModoDeJogo modo = LobbyManager.Instancia != null
			? LobbyManager.Instancia.ObterModoSelecionado()
			: LobbyManager.ModoDeJogo.TodosContraTodos;

		Vector3 posicaoSpawn = Vector3.zero;
		int timeDesignado = 0;

		if (modo == LobbyManager.ModoDeJogo.Modo2v2)
		{
			// Alterna times: Jogadores 0 e 2 = Time A | Jogadores 1 e 3 = Time B
			timeDesignado = contagemJogadores % 2;
			Transform[] spawns = (timeDesignado == 0) ? pontosSpawnTimeA : pontosSpawnTimeB;
			if (spawns != null && spawns.Length > 0)
				posicaoSpawn = spawns[Random.Range(0, spawns.Length)].position;
			Debug.Log("Modo 2v2.");
		}
		else // Todos contra todos
		{
			timeDesignado = contagemJogadores; // Cada jogador fica em um time único
			if (pontosSpawnFFA != null && pontosSpawnFFA.Length > 0)
				posicaoSpawn = pontosSpawnFFA[contagemJogadores % pontosSpawnFFA.Length].position;
			Debug.Log("Todos contra todos.");
		}

		contagemJogadores++;

		// 3. Instancia a classe CORRETA do cliente
		GameObject playerObj = Instantiate(prefabsClasses[indexClasse], posicaoSpawn, Quaternion.identity);
		
		if (playerObj.TryGetComponent<NetworkObject>(out var netObj))
		{
			netObj.SpawnWithOwnership(clientId);
		}

		// 4. Define o time no servidor
		Personagem p = playerObj.GetComponent<Personagem>();
		if (p != null) p.timeId.Value = timeDesignado;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();

		if (NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= AoConectarCliente;
		}
	}
}