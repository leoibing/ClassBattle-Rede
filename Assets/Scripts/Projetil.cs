using UnityEngine;
using Unity.Netcode;

public class Projetil : NetworkBehaviour
{
	[Header("Configurações")]
	public int dano = 20;
	public float velocidade = 12f;
	public float tempoDeVida = 100f;

	private Vector2 direcao;
	private GameObject dono;

	private bool ehEspecial = false;

	public void Configurar(Vector2 Direcao, int Dano, float Velocidade, GameObject Dono)
	{
		direcao = Direcao.normalized;
		dano = Dano;
		velocidade = Velocidade;
		dono = Dono;
	}

	private void Start()
	{
		if (IsServer)
		{
			Invoke(nameof(DespawnarProjetil), tempoDeVida);
		}
	}

	public void MarcarComoEspecial()
	{
		ehEspecial = true;
	}

	private void Update()
	{
		if (!IsServer) return;

		MoverProjetil();
	}

	private void MoverProjetil()
	{
		float distancia = velocidade * Time.deltaTime;
		Vector2 origem = transform.position;

		// Verifica tudo que existe no caminho
		RaycastHit2D[] impactos = Physics2D.RaycastAll(origem, direcao, distancia);

		foreach (RaycastHit2D impacto in impactos)
		{
			if (impacto.collider == null) continue;

			// Ignora disparo em aliados do mesmo time (Se for modo 2v2)
			Personagem pAtingido = impacto.collider.GetComponentInParent<Personagem>();
			Personagem pDono = dono != null ? dono.GetComponent<Personagem>() : null;
			if (pAtingido != null && pDono != null && pAtingido == pDono)
			{
				continue;
			}

			bool ehModo2v2 = LobbyManager.Instancia != null && LobbyManager.Instancia.ObterModoSelecionado() == LobbyManager.ModoDeJogo.Modo2v2;
			if (ehModo2v2 && pAtingido != null && pDono != null && pAtingido.timeId.Value == pDono.timeId.Value)
			{
				continue;
			}

			if (pAtingido != null)
			{
				pAtingido.ReceberDano(dano);
				DespawnarProjetil();
				return;
			}

			//CHÃO
			if (impacto.collider.CompareTag("Chao"))
			{
				Debug.Log("Projétil atingiu o CHÃO.");
				DespawnarProjetil();
				return;
			}

			//POÇÃO, A bala NÃO é destruída pela poção
			if (impacto.collider.GetComponentInParent<PocaoCura>() != null)
			{
				continue;
			}

			//PERSONAGEM
			Personagem personagem = impacto.collider.GetComponentInParent<Personagem>();

			if (personagem != null)
			{
				personagem.ReceberDano(dano);
				Debug.Log("Projétil " + (ehEspecial ? "especial " : "") + "atingiu " + personagem.nome);
				DespawnarProjetil();

				return;
			}

			// Se não acertou nada, continua andando
			transform.position += (Vector3)(direcao * distancia);

		}
	}

	[ClientRpc]
	public void AplicarEspecialClientRpc(float escala)
	{
		transform.localScale *= escala;
		if (TryGetComponent<SpriteRenderer>(out var sprite)) sprite.color = Color.red;
		MarcarComoEspecial();
	}

	private void DespawnarProjetil()
	{
		if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
		{
			NetworkObject.Despawn();
		}
	}
}