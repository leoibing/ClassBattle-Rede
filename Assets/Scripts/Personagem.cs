using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public abstract class Personagem : NetworkBehaviour
{
	[Header("Informações")]
	public string nome = "Personagem";
	public int vidaMaxima = 100;
	public NetworkVariable<int> vidaAtual = new(100);
	public NetworkVariable<int> timeId = new(0);

	[Header("Movimentação")]
	public float velocidade = 5f;
	public float forcaPulo = 7f;

	[Header("Ataque")]
	public Arma arma;

	[Header("Habilidade Especial")]
	public float tempoCooldownEspecial = 5f;

	protected Rigidbody2D rb;
	protected bool estaNoChao = false;
	protected float cooldownAtual = 0f;

	public override void OnNetworkSpawn()
	{
		Rigidbody2D rig = GetComponent<Rigidbody2D>();

		if (IsServer)
		{
			vidaAtual.Value = vidaMaxima;
		}
		else
		{
			// CRUCIAL: Se não for o Host, desativa a física local para não brigar com o servidor
			if (rig != null) rig.bodyType = RigidbodyType2D.Kinematic;
		}

		// Faz a câmera focar no jogador local assim que ele nascer na rede
		if (IsOwner)
		{
			Camera cam = Camera.main;
			if (cam != null)
			{
				if (!cam.TryGetComponent<CameraSeguir>(out var segui)) segui = cam.gameObject.AddComponent<CameraSeguir>();
				segui.alvo = transform;
			}
		}
	}

	protected virtual void Start()
	{
		if (!TryGetComponent<Rigidbody2D>(out rb))
		{
			Debug.LogError("O personagem precisa de um Rigidbody2D!");
		}

		if (arma == null)
		{
			Debug.LogWarning(nome + " não possui uma arma configurada.");
			return;
		}
	}

	protected virtual void Update()
	{
		AtualizarCooldown();

		if (!IsOwner) return;

		if (rb == null) return;

		Movimentacao();

		// ESPAÇO = PULAR
		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
		{
			TentarPularServerRpc();
		}

		// S = ESPECIAL
		if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
		{
			TentarUsarEspecial();
		}
	}

	protected virtual void Movimentacao()
	{
		float horizontal = 0f;

		if (Keyboard.current == null) return;

		// A ou seta esquerda
		if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
		{
			horizontal = -1f;
		}

		// D ou seta direita
		if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
		{
			horizontal = 1f;
		}

		AtualizarVelocidadeServerRpc(horizontal);
	}

	[ServerRpc]
	private void AtualizarVelocidadeServerRpc(float horizontal)
	{
		rb.linearVelocity = new Vector2(horizontal * velocidade, rb.linearVelocity.y);
	}

	[ServerRpc]
	private void TentarPularServerRpc()
	{
		if (!estaNoChao)
		{
			Debug.Log("Não está no chão.");
			return;
		}
		rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
		estaNoChao = false;
	}

	[ServerRpc]
	public void DefinirTimeServerRpc(int time)
	{
		timeId.Value = time;
	}

	protected virtual void AtualizarCooldown()
	{
		if (cooldownAtual > 0f)
		{
			cooldownAtual -= Time.deltaTime;

			if (cooldownAtual < 0f)
				cooldownAtual = 0f;
		}
	}

	private void TentarUsarEspecial()
	{
		if (cooldownAtual > 0f)
		{
			Debug.Log("Especial em cooldown: " + cooldownAtual.ToString("F1") + " segundos.");
			return;
		}

		UsarHabilidade();

		Debug.Log("Usando habilidade especial!");
		cooldownAtual = tempoCooldownEspecial;
	}

	public virtual void ReceberDano(int dano)
	{
		if (!IsServer) return;

		vidaAtual.Value -= dano;

		if (vidaAtual.Value <= 0)
		{
			vidaAtual.Value = 0;
			Morrer();
		}

		Debug.Log(nome +" recebeu " + dano + " de dano. Vida: " + vidaAtual.Value);
	}

	public virtual void Curar(int quantidade)
	{
		if (!IsServer) return;

		vidaAtual.Value += quantidade;

		if (vidaAtual.Value > vidaMaxima)
			vidaAtual.Value = vidaMaxima;

		Debug.Log(nome + " recuperou " + quantidade + " de vida. Vida: " + vidaAtual.Value);
	}

	protected virtual void Morrer()
	{
		if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
		{
			NetworkObject.Despawn();
			Debug.Log(nome + " morreu.");
		}
	}

	protected virtual void OnTriggerEnter2D(Collider2D colisao)
	{
		if (colisao.gameObject.CompareTag("Chao"))
		{
			estaNoChao = true;
		}
	}

	protected virtual void OnTriggerExit2D(Collider2D colisao)
	{
		if (colisao.gameObject.CompareTag("Chao"))
		{
			estaNoChao = false;
		}
	}

	public abstract void UsarHabilidade();
}