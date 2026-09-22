using UnityEngine;
using UnityEngine.InputSystem;

public class Assassino : Personagem
{
	[Header("Dash")]
	public float distanciaDash = 5f;

	public override void UsarHabilidade()
	{
		float direcaoX = 0f;

		// Verifica se o jogador está apertando as setas/teclas de movimento
		if (Keyboard.current != null)
		{
			if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
				direcaoX = -1f;
			else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
				direcaoX = 1f;
		}

		// Garante que o dash seja puramente horizontal
		Vector2 direcaoDash = new(direcaoX, 0f);

		Vector2 novaPosicao = rb.position + direcaoDash * distanciaDash;
		rb.MovePosition(novaPosicao);

		Debug.Log("Assassino realizou Dash na direção: " + direcaoDash);
	}
}
