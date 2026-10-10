using System;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Relics;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Buildings
{
    // The Shrine's chapel on its card (the web's dc-chapel): the painting is the press — a tap anywhere on it opens the
    // relic picker. Waiting, the cradle calls with rays turning out of its +. Held, the relic sits on the altar, dim
    // under the Zs asleep and lit awake, its name and effect on the painting's dark top band, Activate (or the window
    // running down) on its calm floor — presses of their own over the painting.
    public class ShrinePanel : MonoBehaviour
    {
        private static readonly Color ASLEEP = new(0.82f, 0.8f, 0.76f, 1);

        [SerializeField] private Button _press;
        [SerializeField] private Image _painting;
        [SerializeField] private GameObject _call;
        [SerializeField] private CtaBadge _cta;
        [SerializeField] private GameObject _glow;
        [SerializeField] private Image _relic;
        [SerializeField] private GameObject _zs;
        [SerializeField] private GameObject _head;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _effect;
        [SerializeField] private GameObject _foot;
        [SerializeField] private CostButton _activate;
        [SerializeField] private CostButton _flask;
        [SerializeField] private GameObject _awake;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private Sprite _barFill;

        public event Action PickTapped;
        public event Action ActivateTapped;
        public event Action FlaskTapped;

        private void Awake()
        {
            _press.onClick.AddListener(() => PickTapped?.Invoke());
            _activate.Button.onClick.AddListener(() => ActivateTapped?.Invoke());
            _flask.Button.onClick.AddListener(() => FlaskTapped?.Invoke());
        }

        public void Show(ShrinePanelData shrine)
        {
            _painting.sprite = shrine.Painting;
            _call.SetActive(shrine.Empty);
            _cta.gameObject.SetActive(shrine.Empty && shrine.Placeable);
            if (shrine.Empty && shrine.Placeable) _cta.Show(1);
            var held = !shrine.Empty;
            _relic.gameObject.SetActive(held);
            _head.SetActive(held);
            _glow.SetActive(held && shrine.Status == RelicStatus.Awake);
            _zs.SetActive(held && shrine.Status == RelicStatus.Asleep);
            _foot.SetActive(held && shrine.Status == RelicStatus.Asleep);
            _awake.SetActive(held && shrine.Status == RelicStatus.Awake);
            if (!held) return;
            _relic.sprite = shrine.Relic;
            _relic.color = shrine.Status == RelicStatus.Asleep ? ASLEEP : Color.white;
            _name.text = shrine.Head;
            _effect.text = shrine.Effect;
            if (shrine.Status == RelicStatus.Awake) _bar.Set(shrine.AwakeFraction, shrine.AwakeLeft, _barFill);
            if (shrine.Status != RelicStatus.Asleep) return;
            _activate.Button.Label = shrine.ActivateLabel;
            _activate.Show(shrine.ActivatePrice, true);
            _flask.gameObject.SetActive(shrine.FlaskNote != null);
            if (shrine.FlaskNote == null) return;
            _flask.Button.Label = shrine.FlaskLabel;
            _flask.ShowNote(shrine.FlaskNote, true);
        }
    }
}
