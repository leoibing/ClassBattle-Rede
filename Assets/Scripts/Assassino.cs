using UnityEngine;

public class Assassino : Personagem
{
	[Header("Dash")]
	public float distanciaDash = 5f;

	public override void UsarHabilidade()
	{
		// Usa exatamente a direção do PontoDeDisparo
		Vector2 direcao = arma.ObterDirecao();

		if (direcao.sqrMagnitude <= 0.01f)
		{
			Debug.LogWarning("direção inválida.");
			return;
		}

		direcao = direcao.normalized;

		// Faz o Dash na direção em que a arma está apontando
		Vector2 novaPosicao = rb.position + direcao * distanciaDash;
		rb.MovePosition(novaPosicao);

		Debug.Log("Assassino realizou Dash na direção: " + direcao);
	}
}
