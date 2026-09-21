using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Arma : MonoBehaviour
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
		ApontarParaMouse();

		Atirar();
	}

	private void ApontarParaMouse()
	{
		Vector2 direcao = ObterDirecao();

		if (direcao.sqrMagnitude <= 0.001f)
		{
			Debug.LogWarning("direção inválida.");
			return;
		}

		float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

		transform.rotation = Quaternion.Euler(0f, 0f, angulo);

		AtualizarPosicaoDoPontoDeDisparo();

	}

	private void AtualizarPosicaoDoPontoDeDisparo()
	{
		if (pontoDeDisparo == null) return;

		pontoDeDisparo.localPosition = Vector3.right * distanciaDoPontoDeDisparo;
		pontoDeDisparo.localRotation = Quaternion.identity;
	}

	public Vector2 ObterDirecao()
	{
		if (cameraPrincipal == null) cameraPrincipal = Camera.main;
		if (cameraPrincipal == null) return Vector2.right;
		if (Mouse.current == null) return Vector2.right;

		Vector2 mouseTela = Mouse.current.position.ReadValue();
		Vector2 mouseMundo = cameraPrincipal.ScreenToWorldPoint(mouseTela);

		Vector2 origem = transform.root.position;
		Vector2 direcao = mouseMundo - origem;

		if (direcao.sqrMagnitude <= 0.001f) return Vector2.right;

		return direcao.normalized;
	}

	public Vector2 ObterPosicaoDeDisparo()
	{
		if (pontoDeDisparo == null)
			return transform.position;
		AtualizarPosicaoDoPontoDeDisparo();
		return pontoDeDisparo.position;
	}

	private void Atirar()
	{
		if (Mouse.current == null)
			return;

		// Clique esquerdo
		if (!Mouse.current.leftButton.isPressed)
			return;

		if (Time.time < proximoTiro)
			return;

		if (projetilPrefab == null)
			return;

		if (pontoDeDisparo == null)
			return;

		proximoTiro = Time.time + intervaloEntreTiros;

		Vector2 direcao = ObterDirecao();

		Projetil novoProjetil =
			Instantiate(projetilPrefab, pontoDeDisparo.position, Quaternion.identity);

		GameObject dono = transform.root.gameObject;

		novoProjetil.Configurar(direcao, dano, velocidadeProjetil, dono);
	}
}
