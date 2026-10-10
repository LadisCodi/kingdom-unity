using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Relics
{
    // One relic's card in the Bag (the web's relicCardTile, mockups M72 and M80), two a row: the art — a silhouette in
    // fragments, dimmed under the Zs asleep, lit awake — its level on a blue seal, its name, and under it where it
    // stands: its slots and how many are held, a gold plank with the time left awake, the priced Activate asleep, or
    // one muted line. A world relic's card is rimmed in sky.
    public class RelicCardView : MonoBehaviour
    {
        private static readonly Color SILHOUETTE = new(0, 0, 0, 0.55f);
        private static readonly Color ASLEEP = new(0.82f, 0.8f, 0.76f, 1);
        private static readonly Color RIM = new Color32(0xcf, 0xa8, 0x74, 0xff);
        private static readonly Color WORLD_RIM = new(0.31f, 0.64f, 0.78f, 0.6f);
        private static readonly Color AWAKE_RIM = new Color32(0xb0, 0x7a, 0x1a, 0xff);

        [SerializeField] private Button _button;
        [SerializeField] private Image _rim;
        [SerializeField] private GameObject _glow;
        [SerializeField] private Image _art;
        [SerializeField] private GameObject _zs;
        [SerializeField] private GameObject _seal;
        [SerializeField] private TMP_Text _sealText;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private RelicSlotsView _slots;
        [SerializeField] private TMP_Text _ready;
        [SerializeField] private TMP_Text _held;
        [SerializeField] private GameObject _awake;
        [SerializeField] private TMP_Text _awakeText;
        [SerializeField] private TMP_Text _where;
        [SerializeField] private CostButton _activate;
        [SerializeField] private TMP_Text _effect;
        [SerializeField] private GameObject _hostMark;
        [SerializeField] private GameObject _check;
        [SerializeField] private Image _pickedRim;

        private string _id;

        public event Action<string> Tapped;
        public event Action<string> ActivateTapped;

        private void Awake()
        {
            _button.onClick.AddListener(() => Tapped?.Invoke(_id));
            _activate.Button.onClick.AddListener(() => ActivateTapped?.Invoke(_id));
        }

        public void Show(RelicCardData card)
        {
            _id = card.Id;
            var broken = card.Status == RelicStatus.Broken;
            _rim.color = card.Status == RelicStatus.Awake ? AWAKE_RIM : card.World ? WORLD_RIM : RIM;
            _glow.SetActive(card.Status == RelicStatus.Awake);
            _art.sprite = card.Art;
            _art.color = broken ? SILHOUETTE : card.Status == RelicStatus.Asleep ? ASLEEP : Color.white;
            _zs.SetActive(card.Status == RelicStatus.Asleep);
            _seal.SetActive(card.Level != null);
            _sealText.text = card.Level ?? string.Empty;
            _name.text = card.Name;

            _slots.gameObject.SetActive(broken);
            if (broken) _slots.Show(card.Slots, false);
            _ready.gameObject.SetActive(broken && card.Ready != null);
            _ready.text = card.Ready ?? string.Empty;
            _held.gameObject.SetActive(broken && card.Ready == null);
            _held.text = card.Held;
            _awake.SetActive(card.Status == RelicStatus.Awake);
            _awakeText.text = "<sprite name=\"hourglass\">" + card.Awake;
            _where.gameObject.SetActive(card.Where != null);
            _where.text = card.Where ?? string.Empty;
            _effect.gameObject.SetActive(card.Effect != null);
            _effect.text = card.Effect ?? string.Empty;
            _hostMark.SetActive(card.Hosted);
            _check.SetActive(card.Picked);
            _pickedRim.enabled = card.Picked;
            _activate.gameObject.SetActive(card.Status == RelicStatus.Asleep);
            if (card.Status != RelicStatus.Asleep) return;
            _activate.Button.Label = card.ActivateLabel;
            _activate.Show(card.Activate, true);
        }
    }
}
