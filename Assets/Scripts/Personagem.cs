using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Personagem : MonoBehaviour
{
	[Header("Informações")]
	public string nome = "Personagem";
	public int vidaMaxima = 100;
	public int vidaAtual = 100;

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

	protected virtual void Start()
	{
		rb = GetComponent<Rigidbody2D>();

		if (rb == null)
		{
			Debug.LogError("O personagem precisa de um Rigidbody2D!");
		}

		if (arma == null)
		{
			Debug.LogWarning(nome + " não possui uma arma configurada.");
			return;
		}

		vidaAtual = vidaMaxima;
	}

	protected virtual void Update()
	{
		if (rb == null)
			return;

		AtualizarCooldown();
		Movimentacao();

		// ESPAÇO = PULAR
		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
		{
			Pular();
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

		if (Keyboard.current == null)
			return;

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

		rb.linearVelocity = new Vector2(horizontal * velocidade, rb.linearVelocity.y);
	}

	protected virtual void Pular()
	{
		if (!estaNoChao)
		{
			Debug.Log("Não está no chão.");
			return;
		}

		rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);

		estaNoChao = false;
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

		Debug.Log("Usando habilidade especial!");

		UsarHabilidade();

		cooldownAtual = tempoCooldownEspecial;
	}

	public float ObterCooldownAtual()
	{
		return cooldownAtual;
	}

	public virtual void ReceberDano(int dano)
	{
		vidaAtual -= dano;

		if (vidaAtual < 0)
			vidaAtual = 0;

		Debug.Log(nome +" recebeu " + dano + " de dano. Vida: " + vidaAtual);

		if (vidaAtual <= 0)
		{
			Morrer();
		}
	}

	public virtual void Curar(int quantidade)
	{
		vidaAtual += quantidade;

		if (vidaAtual > vidaMaxima)
			vidaAtual = vidaMaxima;

		Debug.Log(nome + " recuperou " + quantidade + " de vida. Vida: " + vidaAtual);
	}

	protected virtual void Morrer()
	{
		Debug.Log(nome + " morreu.");

		gameObject.SetActive(false);
	}

	protected virtual void OnCollisionEnter2D(Collision2D colisao)
	{
		if (colisao.gameObject.CompareTag("Chao"))
		{
			estaNoChao = true;
		}
	}

	protected virtual void OnCollisionExit2D(Collision2D colisao)
	{
		if (colisao.gameObject.CompareTag("Chao"))
		{
			estaNoChao = false;
		}
	}

	public abstract void UsarHabilidade();
}
