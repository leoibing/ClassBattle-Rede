using UnityEngine;
using Unity.Netcode;

public class PocaoCura : NetworkBehaviour
{
	public int quantidadeCura = 30;

	private void OnTriggerEnter2D(Collider2D colisao)
	{
		if (!IsServer) return;

		Personagem personagem = colisao.GetComponentInParent<Personagem>();

		if (personagem == null) return;

		personagem.Curar(quantidadeCura);

		Debug.Log(personagem.nome + " pegou a poção e recuperou " + quantidadeCura + " de vida.");

		NetworkObject.Despawn();
	}
}