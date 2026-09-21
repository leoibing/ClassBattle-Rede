using UnityEngine;

public class Curandeiro : Personagem
{
	[Header("Poção")]
	public GameObject prefabPocao;

	public int quantidadeCura = 30;

	public float distanciaDaPocao = 1.5f;

	public override void UsarHabilidade()
	{
		if (prefabPocao == null)
		{
			Debug.LogWarning("Prefab da poção não foi configurado.");

			return;
		}

		// Escolhe aleatoriamente esquerda ou direita.
		int lado = Random.Range(0, 2);

		float direcaoX;

		if (lado == 0)
		{
			direcaoX = -1f;
		}
		else
		{
			direcaoX = 1f;
		}

		Vector2 posicaoPocao = transform.position +
			new Vector3(direcaoX * distanciaDaPocao, 0f, 0f);

		GameObject novaPocao = Instantiate(prefabPocao, posicaoPocao, Quaternion.identity);

		PocaoCura pocao = novaPocao.GetComponent<PocaoCura>();

		if (pocao != null)
		{
			pocao.quantidadeCura =
				quantidadeCura;
		}

		Debug.Log("Curandeiro criou uma poção no lado " + (lado == 0 ? "esquerdo" : "direito")
		);
	}
}