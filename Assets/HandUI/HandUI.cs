
using DG.Tweening;
using System;
using UnityEngine;


	public class HandUI : MonoBehaviour
	{
        #region Editor
        [SerializeField] private Transform attachPoint;
        
        [Space]
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _panel;
        [SerializeField] private Window _window;

        [Space]
        /*[SerializeField] private HologramButton openButton;
        [SerializeField] private HologramButton closeButton;
        [SerializeField] private HologramCheckbox checkbox1;
        [SerializeField] private HologramCheckbox checkbox2;
        [SerializeField] private HologramButton menuButton;
        [SerializeField] private HologramButton modeButton;
        [SerializeField] private HologramButton inventoryButton;*/
        #endregion

        #region Private
        [SerializeField] private Transform _centerEyeAnchor;

        private bool _isFacing = false;
        private bool _panelOpened = false;
        private bool _panelMoving = false;

        private Vector3 _panelInitialScale;
        private Vector3 _buttonOpenInitialScale;

        private Sequence _panelSequence;
        private Tweener _buttonOpenTweener;
        
        private readonly Vector3 _openRotation = new Vector3(20, 0, 0);
        #endregion

        private void Awake()
		{
		}

		private void Start()
		{
			Initialize();
		}

		private void Initialize()
		{
			InitializePanel();
			InitializeButtons();
			
			transform.parent = attachPoint;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}

		private void Update()
		{
			CheckFacing();

			/*if (_isFacing && !_panelOpened && !_panelMoving)
			{
				OpenPanel();
			}

			if (!_isFacing && _panelOpened && !_panelMoving)
			{
				ClosePanel();
			}*/
		}

		private void CheckFacing()
		{
			Vector3 forward = transform.forward;
			Vector3 toOther = (_centerEyeAnchor.position - transform.position).normalized;

			// check if hand palm is facing towards head
			_isFacing = Vector3.Dot(forward, toOther) > 0.1f;
		}

		private void InitializePanel()
		{
			_panel.gameObject.SetActive(false);
			_pivot.localRotation = Quaternion.Euler(0, 0, 0);
			_panelInitialScale = _panel.localScale;
			_panel.localScale = _panelInitialScale * 0.01f;
			//_buttonOpenInitialScale = openButton.transform.localScale;
			_window.Close();
		}

		public void OpenPanel()
		{
		_window.Open();
			_panelMoving = true;
			_panel.gameObject.SetActive(true);
			_panelSequence?.Kill();
			_panelSequence = DOTween.Sequence();
			_panelSequence.Append(_pivot.DOLocalRotate(_openRotation, 0.25f))
                .SetEase(Ease.InOutQuad)
				.OnComplete(() =>
				{
					_panelMoving = false;
					_panelOpened = true;
				});
			_panelSequence.Join(_panel.DOScale(_panelInitialScale, 0.2f))
                .SetEase(Ease.InOutQuad);
		}

	public void ClosePanel()
		{
			_window.Close();
			_buttonOpenTweener?.Kill();
			//_buttonOpenTweener = openButton.transform.
                /*DOScale(_buttonOpenInitialScale, 0.1f).OnComplete(() =>
			{
				//openButton.gameObject.SetActive(true);
			});*/
			
			_panelMoving = true;
			_panelSequence?.Kill();
			_panelSequence = DOTween.Sequence();
			_panelSequence.Append(
                _pivot.DOLocalRotate(Vector3.zero, 0.1f)).
                SetEase(Ease.InOutQuad).OnComplete(
				() =>
				{
					_panel.gameObject.SetActive(false);
					_panelMoving = false;
					_panelOpened = false;
				});
			_panelSequence.Join(_panel.DOScale(_panelInitialScale * 0.01f, 0.1f))
                .SetEase(Ease.InOutQuad);
		}
		
		private void InitializeButtons()
		{
            // OPEN MENU
           /* openButton.OnPress.Subscribe(b =>
			{
				_window.Open();
				_buttonOpenTweener?.Kill();
				_buttonOpenTweener = openButton.transform.DOScale(_buttonOpenInitialScale * 0.01f, 0.1f)
					.OnComplete(() => { openButton.gameObject.SetActive(false); });
				openButton.DeactivateFor(1);
				Debug.Log("OPEN MENU");
			});*/

			// CLOSE HAND UI
			/*closeButton.OnPress.Subscribe(b =>
			{
				_window.Close();
				closeButton.DeactivateFor(1);
				openButton.gameObject.SetActive(true);

				Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(t =>
				{
					_buttonOpenTweener?.Kill();
					_buttonOpenTweener = openButton.transform.DOScale(_buttonOpenInitialScale, 0.1f).OnComplete(() =>
					{
						openButton.gameObject.SetActive(true);
					});
				});
			});
*/
			//menuButton.OnPress.Subscribe(b => { _controlPanel.Activate(true); });

            //_buttonMode.SetText(_viralSettings.EngineerModeActive.Value? _operatorModeText : _engineerModeText);
			/*modeButton.OnPress.Subscribe(_ =>
			{
				//_viralSettings.EngineerModeActive.Value = !_viralSettings.EngineerModeActive.Value;
				Debug.Log("SWITCH MODE: " + (_viralSettings.EngineerModeActive.Value? "ENGINEER" : "OPERATOR"));
			});*/
		}
	}
