

using DG.Tweening;
using UnityEngine;

public class Window : MonoBehaviour
{
	[SerializeField] private Transform _background;
	//[SerializeField] private Transform _header;
	//[SerializeField] private Transform _content;

	private Sequence _sequence;

	private readonly Vector3 _closedBackgroundScale = new Vector3(0.01f, 1, 1);
	
	public void Open()
	{
		Debug.Log("Opened");

		_sequence?.Kill();
		_sequence = DOTween.Sequence();

		_background.gameObject.SetActive(true);
		_sequence.Append(_background.DOScale(Vector3.one, 0.2f).OnComplete(() =>
		{
			//_header.gameObject.SetActive(true);
			//_content.gameObject.SetActive(true);
		}));
		//_sequence.Append(_header.DOScale(Vector3.one, 0.2f));
		//_sequence.Join(_content.DOScale(Vector3.one, 0.2f));
	}
	
	public void Close()
	{
		Debug.Log("Closed");

		_sequence?.Kill();
		_sequence = DOTween.Sequence();

		//_sequence.Append(_content.DOScale(Vector3.one * 0.01f, 0.2f).OnComplete(() =>
		//{
			//_content.gameObject.SetActive(false);
		//}));
		//_sequence.Join(_header.DOScale(Vector3.one * 0.01f, 0.2f).OnComplete(() =>
		//{
			//_header.gameObject.SetActive(false);
		//}));
		_sequence.Append(_background.DOScale(_closedBackgroundScale , 0.2f).OnComplete(() =>
		{
			_background.gameObject.SetActive(false);
		}));
	}
}
