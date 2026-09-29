using UnityEngine;

public class CameraSeguir : MonoBehaviour
{
	public Transform alvo;
	public float suavizacao = 5f;

	private void LateUpdate()
	{
		if (alvo == null) return;

		Vector3 posicaoDesejada = new(alvo.position.x, alvo.position.y, transform.position.z);
		transform.position = Vector3.Lerp(transform.position, posicaoDesejada, Time.deltaTime * suavizacao);
	}
}