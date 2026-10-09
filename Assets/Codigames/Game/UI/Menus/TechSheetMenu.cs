using System;
using System.Collections.Generic;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Data.Research;
using Codigames.Game.UI.Research;
using Codigames.Game.UI.Widgets;
using Codigames.Kingdom.Research;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // A technology, opened: a loose research page over the dimmed book, read top to bottom — what it is; then
    // its requirements while locked, or its Knowledge (with the pours) and the Research block while it can be
    // worked on. Always the same size. View only: the TechSheetMenuPresenter fills it.
    public class TechSheetMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private Button _scrim;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _emblem;
        [SerializeField] private TMP_Text _says;
        [SerializeField] private GameObject _planned;

        [Header("Researched")]
        [SerializeField] private GameObject _done;

        [Header("Locked")]
        [SerializeField] private GameObject _locked;
        [SerializeField] private RectTransform _requirements;
        [SerializeField] private RequirementRowView _requirementPrefab;

        [Header("In progress")]
        [SerializeField] private GameObject _progress;
        [SerializeField] private GameObject _knowledge;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private GameObject _pours;
        [SerializeField] private Button _buyWithGems;
        [SerializeField] private TMP_Text _gemsPrice;
        [SerializeField] private Button _pourOne;
        [SerializeField] private Button _pourMost;
        [SerializeField] private TMP_Text _pourMostLabel;
        [SerializeField] private GameObject _filled;
        [SerializeField] private RectTransform _price;
        [SerializeField] private CostChip _chipPrefab;
        [SerializeField] private Button _research;
        [SerializeField] private GameObject _researchPadlock;
        [SerializeField] private TMP_Text _note;

        private readonly List<RequirementRowView> _rows = new();
        private readonly List<CostChip> _chips = new();

        public event Action CloseTapped;
        public event Action BuyWithGemsTapped;
        public event Action PourOneTapped;
        public event Action PourMostTapped;
        public event Action ResearchTapped;

        public void Show(TechSheetData sheet)
        {
            _title.text = sheet.Name;
            _emblem.sprite = sheet.Icon;
            _says.text = sheet.Says;
            _planned.SetActive(sheet.Planned);

            _done.SetActive(sheet.State == TechState.Done);
            _locked.SetActive(sheet.State == TechState.Locked);
            _progress.SetActive(sheet.State == TechState.Progress);

            if (sheet.State == TechState.Locked) ShowRequirements(sheet.Requirements);
            if (sheet.State == TechState.Progress) ShowProgress(sheet);
        }

        private void ShowRequirements(IReadOnlyList<RequirementData> requirements)
        {
            for (var i = 0; i < requirements.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(Instantiate(_requirementPrefab, _requirements));
                _rows[i].gameObject.SetActive(true);
                _rows[i].Show(requirements[i]);
            }

            for (var i = requirements.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
        }

        private void ShowProgress(TechSheetData sheet)
        {
            _knowledge.SetActive(sheet.NeedsKnowledge);
            _bar.Set(sheet.Fraction, sheet.Bar);
            _pours.SetActive(!sheet.Filled);
            _filled.SetActive(sheet.Filled);
            _gemsPrice.text = sheet.GemsPrice;
            _buyWithGems.interactable = sheet.CanBuyWithGems;
            _pourOne.interactable = sheet.CanPour;
            _pourMost.interactable = sheet.CanPour;
            _pourMostLabel.text = sheet.PourMost;

            for (var i = 0; i < sheet.Price.Count; i++)
            {
                if (i == _chips.Count) _chips.Add(Instantiate(_chipPrefab, _price));
                _chips[i].gameObject.SetActive(true);
                _chips[i].Show(sheet.Price[i]);
            }

            for (var i = sheet.Price.Count; i < _chips.Count; i++) _chips[i].gameObject.SetActive(false);

            _research.interactable = sheet.CanResearch;
            _researchPadlock.SetActive(!sheet.Filled);
            _note.text = sheet.Note;
            _note.gameObject.SetActive(!string.IsNullOrEmpty(sheet.Note));
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _scrim.onClick.AddListener(OnClose);
            _buyWithGems.onClick.AddListener(OnBuyWithGems);
            _pourOne.onClick.AddListener(OnPourOne);
            _pourMost.onClick.AddListener(OnPourMost);
            _research.onClick.AddListener(OnResearch);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _scrim.onClick.RemoveListener(OnClose);
            _buyWithGems.onClick.RemoveListener(OnBuyWithGems);
            _pourOne.onClick.RemoveListener(OnPourOne);
            _pourMost.onClick.RemoveListener(OnPourMost);
            _research.onClick.RemoveListener(OnResearch);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnBuyWithGems() => BuyWithGemsTapped?.Invoke();
        private void OnPourOne() => PourOneTapped?.Invoke();
        private void OnPourMost() => PourMostTapped?.Invoke();
        private void OnResearch() => ResearchTapped?.Invoke();
    }
}
