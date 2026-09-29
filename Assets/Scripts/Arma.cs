using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Arma : NetworkBehaviour
{
	[Header("Projétil")]
	public Projetil projetilPrefab;
	public Transform pontoDeDisparo;

	[Header("Configurações")]
	public int dano = 20;
	public float velocidadeProjetil = 12f;
	public float intervaloEntreTiros = 0.3f;

	[Header("Posição do disparo")]
	public float distanciaDoPontoDeDisparo = 0.8f;

	private float proximoTiro = 0f;

	private Camera cameraPrincipal;

	// Guarda o último ângulo para evitar flood na rede e o flicker visual
	private float ultimoAnguloEnviado = -999f;

	private void Start()
	{
		cameraPrincipal = Camera.main;

		if (cameraPrincipal == null)
		{
			Debug.LogError("Nenhuma câmera com a tag MainCamera foi encontrada!");
		}

		if (pontoDeDisparo == null)
		{
			Debug.LogError("O PontoDeDisparo não foi configurado na arma!");
		}

		if (projetilPrefab == null)
		{
			Debug.LogError("O ProjetilPrefab não foi configurado na arma!");
		}
	}

	private void Update()
	{
		if (!IsOwner) return;

		ApontarParaMouse();

		Atirar();
	}

	private void ApontarParaMouse()
	{
		Vector2 direcao = ObterDirecao();
		float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

		transform.rotation = Quaternion.Euler(0f, 0f, angulo);

		if (pontoDeDisparo != null)
		{
			pontoDeDisparo.localPosition = Vector3.right * distanciaDoPontoDeDisparo;
		}

		// Só envia pacote para a rede se a mira moveu significativamente
		if (Mathf.Abs(Mathf.DeltaAngle(ultimoAnguloEnviado, angulo)) > 1f)
		{
			ultimoAnguloEnviado = angulo;
			RotacionarArmaNaRedeServerRpc(angulo);
		}
	}

	[ServerRpc]
	private void RotacionarArmaNaRedeServerRpc(float angulo)
	{
		RotacionarArmaClientRpc(angulo);
	}

	[ClientRpc]
	private void RotacionarArmaClientRpc(float angulo)
	{
		if (!IsOwner)
		{
			transform.rotation = Quaternion.Euler(0f, 0f, angulo);
			if (pontoDeDisparo != null)
			{
				pontoDeDisparo.localPosition = Vector3.right * distanciaDoPontoDeDisparo;
			}
		}
	}

	public Vector2 ObterDirecao()
	{
		if (cameraPrincipal == null) cameraPrincipal = Camera.main;
		if (cameraPrincipal == null || Mouse.current == null) return Vector2.right;

		Vector2 mouseTela = Mouse.current.position.ReadValue();
		Vector2 mouseMundo = cameraPrincipal.ScreenToWorldPoint(mouseTela);

		Vector2 origem = transform.root.position;
		Vector2 direcao = mouseMundo - origem;

		return direcao.sqrMagnitude <= 0.01f ? Vector2.right : direcao.normalized;
	}

	public Vector2 ObterPosicaoDeDisparo()
	{
		if (pontoDeDisparo == null)
			return transform.position;
		return pontoDeDisparo.position;
	}

	private void Atirar()
	{
		// Clique esquerdo
		if (Mouse.current == null || !Mouse.current.leftButton.isPressed) return;

		if (Time.time < proximoTiro) return;

		if (projetilPrefab == null) return;

		if (pontoDeDisparo == null) return;

		proximoTiro = Time.time + intervaloEntreTiros;

		// Pede para o servidor criar a bala, Manda pro servidor APENAS a direção! A posição é decidida pelo servidor.
		AtirarServerRpc(ObterDirecao());
	}

	[ServerRpc]
	private void AtirarServerRpc(Vector2 direcao)
	{
		// O SERVIDOR pega a posição DELE. Isso conserta as balas nascendo no spawn vazio!
		Vector2 posicaoDisparo = pontoDeDisparo != null ? (Vector2)pontoDeDisparo.position : (Vector2)transform.position;

		// O SERVIDOR instancia a bala na cena dele
		Projetil novoProjetil = Instantiate(projetilPrefab, posicaoDisparo, Quaternion.identity);
		novoProjetil.Configurar(direcao, dano, velocidadeProjetil, transform.root.gameObject);

		// O SERVIDOR avisa todos os outros jogadores para criarem essa bala
		novoProjetil.GetComponent<NetworkObject>().Spawn();
	}
}
