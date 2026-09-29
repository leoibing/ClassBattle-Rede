using UnityEngine;
using Unity.Netcode;

public class Atirador : Personagem
{
	[Header("Especial do Atirador")]
	public int danoEspecial = 60;
	public float velocidadeProjetilEspecial = 18f;

	[Header("Especial")]
	public float tamanhoEspecial = 2f;

	public override void UsarHabilidade()
	{
		AtirarEspecialServerRpc(arma.ObterDirecao(), arma.ObterPosicaoDeDisparo());
	}

	[ServerRpc]
	private void AtirarEspecialServerRpc(Vector2 direcao, Vector2 posicao)
	{
		Projetil novoProjetil = Instantiate(arma.projetilPrefab, posicao, Quaternion.identity);
		novoProjetil.Configurar(direcao, danoEspecial, velocidadeProjetilEspecial, gameObject);

		NetworkObject netObj = novoProjetil.GetComponent<NetworkObject>();
		netObj.Spawn();

		// Avisa os clientes para mudarem o visual da bala especial
		novoProjetil.AplicarEspecialClientRpc(tamanhoEspecial);

		Debug.Log("Atirador utilizou o tiro especial!");
	}
}