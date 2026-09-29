using UnityEngine;
using Unity.Netcode;

public class Tanque : Personagem
{
	[Header("Escudo")]
	public GameObject objetoEscudoVisual;
	public float duracaoEscudo = 3f;
	private readonly NetworkVariable<bool> escudoAtivo = new(false);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		escudoAtivo.OnValueChanged += AoMudarEscudo;
		AtualizarVisualEscudo(escudoAtivo.Value);
	}

	private void AoMudarEscudo(bool valorAntigo, bool valorNovo)
	{
		AtualizarVisualEscudo(valorNovo);
	}
	private void AtualizarVisualEscudo(bool ativo)
	{
		if (objetoEscudoVisual != null)
		{
			objetoEscudoVisual.SetActive(ativo);
		}
	}

	public override void UsarHabilidade()
	{
		AtivarEscudoServerRpc();
	}

	[ServerRpc]
	private void AtivarEscudoServerRpc()
	{
		if (escudoAtivo.Value) return;

		Debug.Log("Tanque ativou o escudo!");
		escudoAtivo.Value = true;
		Invoke(nameof(DesativarEscudo), duracaoEscudo);
	}

	private void DesativarEscudo()
	{
		if (IsServer)
		{
			escudoAtivo.Value = false;
		}

		Debug.Log("Escudo do Tanque terminou.");
	}

	public override void ReceberDano(int dano)
	{
		if (escudoAtivo.Value) return;

		Debug.Log("O escudo bloqueou o dano!");
		base.ReceberDano(dano);
	}
}