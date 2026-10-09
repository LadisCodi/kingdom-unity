using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // One building's card: a window across the bottom with its name and level on the band, its art and what it
    // does, and then either the work under way or the priced Upgrade. View only: the DistrictCardMenuPresenter
    // fills it.
    public class DistrictCardMenu : Menu
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField] private Image _art;
        [SerializeField] private AspectRatioFitter _artFit;
        [SerializeField] private TMP_Text _level;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private TMP_Text _store;
        [SerializeField] private Color _storeColor = new Color32(0x7a, 0x5c, 0x3e, 0xff);
        [SerializeField] private Color _storeFullColor = new Color32(0xd4, 0x55, 0x3e, 0xff);
        [SerializeField] private GameObject _workRow;
        [SerializeField] private TMP_Text _work;
        [SerializeField] private Image _workFill;
        [SerializeField] private GameObject _upgradeRow;
        [SerializeField] private GameObject _nextRow;
        [SerializeField] private TMP_Text _next;
        [SerializeField] private TMP_Text _reason;
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private Button _upgrade;
        [Header("Training")]
        [SerializeField] private RectTransform _window;
        [SerializeField] private float _windowHeight = 470;
        [SerializeField] private float _trainingHeight = 190;
        [SerializeField] private GameObject _trainingRow;
        [SerializeField] private TMP_Text _villagers;
        [SerializeField] private TMP_Text _onTheWay;
        [SerializeField] private RectTransform _trainPrice;
        [SerializeField] private Button _train;
        [Header("Crew")]
        [SerializeField] private GameObject _crewRow;
        [SerializeField] private TMP_Text _crewCount;
        [SerializeField] private TMP_Text _crewNote;
        [SerializeField] private Button _crewMinus;
        [SerializeField] private Button _crewPlus;
        [SerializeField] private Color _ordinalColor = new Color32(0xf4, 0xe4, 0xc1, 0xcc);

        private readonly List<CostChip> _chips = new();
        private readonly List<CostChip> _trainChips = new();

        public event Action CloseTapped;
        public event Action UpgradeTapped;
        public event Action TrainTapped;
        public event Action CrewMinusTapped;
        public event Action CrewPlusTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close", "card:close");
            CoachTarget.Tag(_upgrade, "card:upgrade", "upgrade-go");
            CoachTarget.Tag(_train, "card:train");
            CoachTarget.Tag(_crewPlus, "card:workers");
        }

        public void Show(DistrictCardData card)
        {
            _title.text = string.IsNullOrEmpty(card.Ordinal)
                ? card.Name
                : $"{card.Name}<size=75%><color=#{ColorUtility.ToHtmlStringRGBA(_ordinalColor)}> {card.Ordinal}</color></size>";
            _art.sprite = card.Art;
            _art.enabled = card.Art != null;
            if (card.Art != null) _artFit.aspectRatio = card.Art.rect.width / card.Art.rect.height;
            _level.text = card.Level;
            _promise.text = card.Promise;
            _store.text = card.Store;
            _store.color = card.StoreFull ? _storeFullColor : _storeColor;
            _store.gameObject.SetActive(!string.IsNullOrEmpty(card.Store));

            _workRow.SetActive(card.Working);
            _work.text = card.Work;
            _workFill.fillAmount = card.Progress;

            _upgradeRow.SetActive(!card.Working);
            _nextRow.SetActive(!card.Working && !string.IsNullOrEmpty(card.Next));
            _next.text = card.Next;
            _reason.text = card.Reason;
            _reason.gameObject.SetActive(!string.IsNullOrEmpty(card.Reason));
            _upgrade.gameObject.SetActive(card.Price.Count > 0);
            _upgrade.interactable = card.CanUpgrade;

            ShowChips(_chips, _price, card.Price);

            var crew = card.Crew;
            _crewRow.SetActive(crew != null);
            if (crew != null)
            {
                _crewCount.text = crew.Count;
                _crewNote.text = crew.Note;
                _crewMinus.interactable = crew.CanRemove;
                _crewPlus.interactable = crew.CanAdd;
            }

            var training = card.Training;
            _trainingRow.SetActive(training != null);
            var strip = training != null || crew != null;
            _window.sizeDelta = new Vector2(_window.sizeDelta.x, _windowHeight + (strip ? _trainingHeight : 0));
            if (training == null) return;

            _villagers.text = training.Villagers;
            _onTheWay.text = string.IsNullOrEmpty(training.OnTheWay) ? training.Reason : training.OnTheWay;
            _train.interactable = training.CanTrain;
            ShowChips(_trainChips, _trainPrice, training.Price);
        }

        private void ShowChips(List<CostChip> chips, RectTransform parent, IReadOnlyList<CostChipData> price)
        {
            for (var i = 0; i < price.Count; i++)
            {
                if (i == chips.Count) chips.Add(Instantiate(_chipPrefab, parent));
                chips[i].gameObject.SetActive(true);
                chips[i].Show(price[i]);
            }

            for (var i = price.Count; i < chips.Count; i++) chips[i].gameObject.SetActive(false);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _upgrade.onClick.AddListener(OnUpgrade);
            _train.onClick.AddListener(OnTrain);
            _crewMinus.onClick.AddListener(OnCrewMinus);
            _crewPlus.onClick.AddListener(OnCrewPlus);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _upgrade.onClick.RemoveListener(OnUpgrade);
            _train.onClick.RemoveListener(OnTrain);
            _crewMinus.onClick.RemoveListener(OnCrewMinus);
            _crewPlus.onClick.RemoveListener(OnCrewPlus);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnUpgrade() => UpgradeTapped?.Invoke();
        private void OnTrain() => TrainTapped?.Invoke();
        private void OnCrewMinus() => CrewMinusTapped?.Invoke();
        private void OnCrewPlus() => CrewPlusTapped?.Invoke();
    }
}
