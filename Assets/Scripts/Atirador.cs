using UnityEngine;

public class Atirador : Personagem
{
	[Header("Especial do Atirador")]
	public int danoEspecial = 60;
	public float velocidadeProjetilEspecial = 18f;

	[Header("Especial")]
	public float tamanhoEspecial = 2f;

	public override void UsarHabilidade()
	{
		// Usa exatamente a direção do PontoDeDisparo
		Vector2 direcao = arma.ObterDirecao();

		if (direcao.sqrMagnitude <= 0.01f)
		{
			Debug.LogWarning("Atirador: direção inválida.");
			return;
		}

		direcao = direcao.normalized;

		// Posição do PontoDeDisparo
		Vector2 posicao = arma.ObterPosicaoDeDisparo();

		Projetil novoProjetil = Instantiate(arma.projetilPrefab, posicao, Quaternion.identity);

		novoProjetil.Configurar(direcao, danoEspecial, velocidadeProjetilEspecial, gameObject);

		// Torna o especial maior
		novoProjetil.transform.localScale = novoProjetil.transform.localScale * tamanhoEspecial;

		// Torna o especial vermelho
		SpriteRenderer sprite = novoProjetil.GetComponent<SpriteRenderer>();

		if (sprite != null)
		{
			sprite.color = Color.red;
		}

		// Informa que esse projétil é um especial
		novoProjetil.MarcarComoEspecial();

		Debug.Log("Atirador utilizou o tiro especial!");
	}
}
