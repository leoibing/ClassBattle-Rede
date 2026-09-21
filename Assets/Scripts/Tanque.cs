using UnityEngine;

public class Tanque : Personagem
{
	[Header("Escudo")]
	public GameObject prefabEscudo;

	public float duracaoEscudo = 3f;

	private GameObject escudoAtual;

	public override void UsarHabilidade()
	{
		if (escudoAtual != null)
			return;

		if (prefabEscudo == null)
		{
			Debug.LogWarning(
				"Prefab do escudo não foi configurado."
			);

			return;
		}

		escudoAtual = Instantiate(prefabEscudo, transform.position, Quaternion.identity, transform );

		Debug.Log("Tanque ativou o escudo!");

		Invoke(nameof(DesativarEscudo), duracaoEscudo);
	}

	private void DesativarEscudo()
	{
		if (escudoAtual != null)
		{
			Destroy(escudoAtual);
			escudoAtual = null;
		}

		Debug.Log("Escudo do Tanque terminou.");
	}

	public override void ReceberDano(int dano)
	{
		if (escudoAtual != null)
		{
			Debug.Log("O escudo bloqueou o dano!");

			return;
		}

		base.ReceberDano(dano);
	}
}