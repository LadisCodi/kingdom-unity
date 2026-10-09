using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Store
{
    // A call, framed (the web's sth-banner): the common call in nailed wood with a blue plank, the golden call in gold
    // plate with a gold one and a Legendary rising out of its frame; its free calls; its ×1 and ×10.
    public class BannerPanel : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _plank;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Image _hero;
        [SerializeField] private RectTransform _body;
        [SerializeField] private TMP_Text _free;
        [SerializeField] private CostButton _one;
        [SerializeField] private CostButton _ten;
        [SerializeField] private Sprite _woodFrame;
        [SerializeField] private Sprite _goldFrame;
        [SerializeField] private Sprite _bluePlank;
        [SerializeField] private Sprite _goldPlank;
        [SerializeField] private Color _blueInk = new Color32(255, 246, 224, 255);
        [SerializeField] private Color _goldInk = new Color32(90, 58, 18, 255);
        // With a hero: the plank to the right and the words beside the figure.
        [SerializeField] private float _heroInset = 360;

        public event Action OneTapped;
        public event Action TenTapped;

        public CostButton One => _one;

        private void Awake()
        {
            _one.Button.onClick.AddListener(() => OneTapped?.Invoke());
            _ten.Button.onClick.AddListener(() => TenTapped?.Invoke());
        }

        public void Show(BannerPanelData banner)
        {
            _frame.sprite = banner.Golden ? _goldFrame : _woodFrame;
            _plank.sprite = banner.Golden ? _goldPlank : _bluePlank;
            _name.text = banner.Name;
            _name.color = banner.Golden ? _goldInk : _blueInk;
            var plank = _plank.rectTransform;
            var hero = banner.Hero != null;
            plank.anchorMin = plank.anchorMax = plank.pivot = new Vector2(hero ? 1 : 0, 1);
            plank.anchoredPosition = new Vector2(hero ? -11 : 11, plank.anchoredPosition.y);
            _hero.gameObject.SetActive(hero);
            _hero.sprite = banner.Hero;
            _free.margin = new Vector4(hero ? _heroInset : 0, 0, 0, 0);
            _free.text = banner.Free;
            _free.gameObject.SetActive(!string.IsNullOrEmpty(banner.Free) || hero);
            Call(_one, banner.One);
            Call(_ten, banner.Ten);
        }

        private static void Call(CostButton button, CallButtonData call)
        {
            button.Button.Label = call.Label;
            if (call.Note != null) button.ShowNote(call.Note, call.Enabled);
            else button.Show(call.Price, call.Enabled);
        }
    }
}
