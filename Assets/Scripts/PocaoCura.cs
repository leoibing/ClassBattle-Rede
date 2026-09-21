using UnityEngine;

public class PocaoCura : MonoBehaviour
{
	public int quantidadeCura = 30;

	private void OnTriggerEnter2D(Collider2D colisao)
	{
		Personagem personagem = colisao.GetComponentInParent<Personagem>();

		if (personagem == null)
			return;

		personagem.Curar(quantidadeCura);

		Debug.Log(personagem.nome + " pegou a poção e recuperou " + quantidadeCura + " de vida.");

		Destroy(gameObject);
	}
}