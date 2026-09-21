using UnityEngine;

public class Projetil : MonoBehaviour
{
	[Header("Configurações")]
	public int dano = 20;
	public float velocidade = 12f;
	public float tempoDeVida = 10f;

	private Vector2 direcao;
	private GameObject dono;

	private bool ehEspecial = false;

	public void Configurar(Vector2 Direcao, int Dano, float Velocidade, GameObject Dono)
	{
		direcao = Direcao.normalized;
		dano = Dano;
		velocidade = Velocidade;
		dono = Dono;
	}

	private void Start()
	{
		Destroy(gameObject, tempoDeVida);
	}

	public void MarcarComoEspecial()
	{
		ehEspecial = true;
	}

	private void Update()
	{
		MoverProjetil();
	}

	private void MoverProjetil()
	{
		float distancia = velocidade * Time.deltaTime;
		Vector2 origem = transform.position;

		// Verifica tudo que existe no caminho
		RaycastHit2D[] impactos = Physics2D.RaycastAll(origem, direcao, distancia);

		foreach (RaycastHit2D impacto in impactos)
		{
			if (impacto.collider == null) continue;

			// Ignora o próprio jogador
			if (dono != null && impacto.collider.transform.root.gameObject == dono)
			{
				continue;
			}

			//CHÃO
			if (impacto.collider.CompareTag("Chao"))
			{
				Debug.Log("Projétil atingiu o CHÃO.");
				Destroy(gameObject);
				return;
			}

			//POÇÃO
			if (impacto.collider.GetComponentInParent<PocaoCura>() != null)
			{
				// A bala NÃO é destruída pela poção
				continue;
			}

			//PERSONAGEM
			Personagem personagem = impacto.collider.GetComponentInParent<Personagem>();

			if (personagem != null)
			{
				personagem.ReceberDano(dano);

				Debug.Log("Projétil " + (ehEspecial ? "especial " : "") + "atingiu " + personagem.nome);

				Destroy(gameObject);

				return;
			}

			// Se não acertou nada, continua andando
			transform.position += (Vector3)(direcao * distancia);

		}
	}
}
