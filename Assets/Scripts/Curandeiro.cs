using UnityEngine;
using Unity.Netcode;

public class Curandeiro : Personagem
{
	[Header("Poção")]
	public GameObject prefabPocao;
	public float distanciaDaPocao = 1.5f;

	public override void UsarHabilidade()
	{
		CriarPocaoServerRpc();
	}

	[ServerRpc]
	private void CriarPocaoServerRpc()
	{
		if (prefabPocao == null)
		{
			Debug.LogWarning("Prefab da poção não foi configurado.");
			return;
		}

		// Escolhe aleatoriamente esquerda ou direita.
		float direcaoX = Random.Range(0, 2) == 0 ? -1f : 1f;
		Vector2 posicaoPocao = transform.position + new Vector3(direcaoX * distanciaDaPocao, 0f, 0f);

		GameObject novaPocao = Instantiate(prefabPocao, posicaoPocao, Quaternion.identity);
		novaPocao.GetComponent<NetworkObject>().Spawn();

		Debug.Log("Curandeiro criou uma poção no lado " + (direcaoX == 0 ? "esquerdo" : "direito"));
	}
}