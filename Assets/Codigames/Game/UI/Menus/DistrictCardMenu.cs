using System;
using Codigames.Game.UI.Data;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // One building's card (the web's districtCard): a window across the bottom sized to what it holds. The band
    // carries the name and level, Move and Close. One row: the portrait, what the building is, and the one thing
    // bought for it — Upgrade (its call to action when it is ready), or the gem Finish while it is being built, the
    // bar riding on the portrait's foot. Then the band of what it is worth now, and its blocks: the training panel,
    // the crew stepper. View only: the DistrictCardMenuPresenter fills it.
    public class DistrictCardMenu : Menu
    {
        [Header("Band")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField, Tooltip("Left of Close: it moves the building; hidden for one that never moves.")] private Button _move;

        [Header("Head row")]
        [SerializeField] private BuildingPortrait _portrait;
        [SerializeField] private ProgressBar _workBar;
        [SerializeField] private TMP_Text _what;
        [SerializeField] private TMP_Text _doing;
        [SerializeField] private KitButton _upgrade;
        [SerializeField] private CtaBadge _upgradeCta;
        [SerializeField] private CostButton _finishWork;
        [SerializeField] private KitButton _speedUpWork;

        [Header("Stats")]
        [SerializeField] private StatBand _stats;

        [Header("Training")]
        [SerializeField] private SectionHead _trainingHead;
        [SerializeField] private GameObject _training;
        [SerializeField] private UnitPortrait _trainee;
        [SerializeField] private Tag _tag;
        [SerializeField] private TMP_Text _traineeLine;
        [SerializeField] private KitButton _amount;
        [SerializeField] private CostButton _train;
        [SerializeField] private TMP_Text _batchEmpty;
        [SerializeField] private GameObject _batch;
        [SerializeField] private UnitPortrait _batchFace;
        [SerializeField] private TMP_Text _batchWhat;
        [SerializeField] private ProgressBar _batchBar;
        [SerializeField] private TMP_Text _batchTotal;
        [SerializeField] private CostButton _finishTraining;
        [SerializeField] private KitButton _speedUpTraining;

        [Header("Crew")]
        [SerializeField] private SectionHead _crewHead;
        [SerializeField] private GameObject _crew;
        [SerializeField] private KitButton _crewMinus;
        [SerializeField] private UnitPortrait _crewFace;
        [SerializeField] private TMP_Text _crewCount;
        [SerializeField] private TMP_Text _crewLimit;
        [SerializeField] private KitButton _crewPlus;

        [Header("A hall's troops")]
        [SerializeField] private RectTransform _troopStats;
        [SerializeField] private Buildings.TroopStatTile _troopStatPrefab;
        [SerializeField, Tooltip("The portrait as a button: the hall's ranks.")] private Button _pick;
        [SerializeField] private GameObject _caret;
        [SerializeField] private GameObject _rankMenu;
        [SerializeField] private RectTransform _rankRows;
        [SerializeField] private Buildings.RankRow _rankRowPrefab;

        [Header("Ward")]
        [SerializeField] private GameObject _wardBlock;
        [SerializeField] private SectionHead _wardHead;
        [SerializeField] private TMP_Text _wardBeds;
        [SerializeField] private TMP_Text _wardEmpty;
        [SerializeField] private RectTransform _wardRows;
        [SerializeField] private Buildings.WardRow _wardRowPrefab;

        private readonly System.Collections.Generic.List<Buildings.TroopStatTile> _troopStatViews = new();
        private readonly System.Collections.Generic.List<Buildings.RankRow> _rankRowViews = new();
        private readonly System.Collections.Generic.List<Buildings.WardRow> _wardRowViews = new();

        [Header("Tavern")]
        [SerializeField] private GameObject _tavernBlock;
        [SerializeField] private SectionHead _tavernHead;
        [SerializeField] private KitButton _tavernHeroes;
        [SerializeField] private KitButton _tavernCall;

        [Header("Shrine")]
        [SerializeField] private GameObject _shrineBlock;
        [SerializeField] private SectionHead _shrineHead;
        [SerializeField] private Buildings.ShrinePanel _shrine;

        [Header("Workshop")]
        [SerializeField] private GameObject _workshopBlock;
        [SerializeField] private SectionHead _workshopHead;
        [SerializeField] private Buildings.WorkshopPanel _workshop;

        [Header("Harmony and neighbours")]
        [SerializeField] private GameObject _harmonyBlock;
        [SerializeField] private SectionHead _harmonyHead;
        [SerializeField] private TMP_Text _harmonyLine;
        [SerializeField] private TMP_Text _harmonyNote;
        [SerializeField] private GameObject _neighboursBlock;
        [SerializeField] private SectionHead _neighboursHead;
        [SerializeField] private RectTransform _badges;
        [SerializeField] private ToneBadge _badgePrefab;

        private readonly System.Collections.Generic.List<ToneBadge> _badgeViews = new();

        [SerializeField] private RectTransform _window;

        [SerializeField, Tooltip("Its window: where its top edge stands, for the camera.")] private MapCard _card;

        // The window's top edge, as a share of the screen's height from the bottom.
        public float CardTop() => _card.ViewportTop();

        public event Action CloseTapped;
        public event Action MoveTapped;
        public event Action UpgradeTapped;
        public event Action FinishWorkTapped;
        public event Action SpeedUpWorkTapped;
        public event Action SpeedUpTrainingTapped;
        public event Action MakeTapped;
        public event Action<string> RankPicked;
        public event Action<string> HealTapped;
        public event Action<int> CancelGoodTapped;
        public event Action FinishGoodTapped;
        public event Action SpeedUpGoodTapped;
        public event Action AmountTapped;
        public event Action TrainTapped;
        public event Action FinishTrainingTapped;
        public event Action CrewMinusTapped;
        public event Action CrewPlusTapped;
        public event Action TavernHeroesTapped;
        public event Action TavernCallTapped;
        public event Action ShrinePickTapped;
        public event Action ShrineActivateTapped;
        public event Action ShrineFlaskTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close", "card:close");
            CoachTarget.Tag(_move, "card:move");
            CoachTarget.Tag(_upgrade, "card:upgrade");
            CoachTarget.Tag(_train.Button, "card:train");
            CoachTarget.Tag(_finishTraining.Button, "card:finish-training");
            CoachTarget.Tag(_crewPlus, "card:workers");
            CoachTarget.Tag(_tavernCall, "card:call");
            CoachTarget.Tag(_speedUpWork, "speedup");
        }

        public void Show(DistrictCardData card)
        {
            _title.text = card.Title;
            _move.gameObject.SetActive(card.Movable);
            _portrait.Show(card.Art);
            _portrait.SetWorking(card.Work != null ? card.Id : null);
            _what.text = card.What;

            var work = card.Work;
            _workBar.gameObject.SetActive(work != null);
            _doing.gameObject.SetActive(work != null);
            _finishWork.gameObject.SetActive(work != null && !work.SpeedUp);
            _speedUpWork.gameObject.SetActive(work != null && work.SpeedUp);
            _speedUpWork.Label = card.SpeedUp;
            _speedUpTraining.Label = card.SpeedUp;
            _upgrade.gameObject.SetActive(work == null && card.Upgradable);
            _upgradeCta.Show(work == null && card.UpgradeReady ? 1 : 0);
            if (work != null)
            {
                _workBar.Set(work.Progress, work.Left);
                _doing.text = work.Doing;
                _finishWork.Show(work.Finish, work.CanFinish);
            }

            _stats.Show(card.Stats);
            ShowTraining(card);
            ShowCrew(card);
            _workshopBlock.SetActive(card.Workshop != null);
            if (card.Workshop != null)
            {
                _workshopHead.Title = card.WorkshopHead;
                _workshop.Show(card.Workshop);
            }

            _tavernBlock.SetActive(card.TavernHead != null);
            if (card.TavernHead != null)
            {
                _tavernHead.Title = card.TavernHead;
                _tavernHeroes.Label = card.TavernHeroes;
                _tavernCall.Label = card.TavernCall;
            }

            _shrineBlock.SetActive(card.Shrine != null);
            if (card.Shrine != null)
            {
                _shrineHead.Title = card.ShrineHead;
                _shrine.Show(card.Shrine);
            }

            ShowWard(card);
            ShowHarmony(card);
            ShowNeighbours(card);
            LayoutRebuilder.MarkLayoutForRebuild(_window);
        }

        private void ShowTraining(DistrictCardData card)
        {
            var training = card.Training;
            _trainingHead.gameObject.SetActive(training != null);
            _training.SetActive(training != null);
            if (training == null) return;

            _trainingHead.Title = card.TrainingHead;
            _trainee.Show(training.Bust, training.BustShift, training.BustScale, training.Owned);
            _trainee.ShowRank(training.Rank);
            ShowTroop(training);
            _tag.Label = training.Tag;
            _traineeLine.text = training.Description;
            _amount.Label = training.Amount;
            if (string.IsNullOrEmpty(training.Gate)) _train.Show(training.Price, training.CanTrain);
            else _train.ShowGate(training.Gate);

            var batch = training.Batch;
            _batch.SetActive(batch != null);
            _batchEmpty.gameObject.SetActive(batch == null);
            _batchEmpty.text = training.Empty;
            if (batch == null) return;

            _batchFace.Show(training.Bust, training.BustShift, training.BustScale, batch.Count);
            _batchFace.ShowRank(training.Rank);
            _batchWhat.text = batch.Doing;
            // Training runs green, as the web's batch bar does.
            _batchBar.Set(batch.Progress, batch.Left, done: true);
            _batchTotal.text = batch.Total;
            _finishTraining.gameObject.SetActive(!batch.SpeedUp);
            _speedUpTraining.gameObject.SetActive(batch.SpeedUp);
            _finishTraining.Show(batch.Finish, batch.CanFinish);
        }

        // A troop's numbers under its line, and its hall's ranks behind its portrait.
        private void ShowTroop(TrainingPanelData training)
        {
            _troopStats.gameObject.SetActive(training.Stats.Count > 0);
            for (var i = 0; i < training.Stats.Count; i++)
            {
                if (i == _troopStatViews.Count) _troopStatViews.Add(Instantiate(_troopStatPrefab, _troopStats));
                _troopStatViews[i].gameObject.SetActive(true);
                _troopStatViews[i].Show(training.Stats[i]);
            }

            for (var i = training.Stats.Count; i < _troopStatViews.Count; i++) _troopStatViews[i].gameObject.SetActive(false);

            _pick.interactable = training.Ranks.Count > 1;
            _caret.SetActive(training.Ranks.Count > 1);
            if (training.Ranks.Count <= 1) _rankMenu.SetActive(false);
            for (var i = 0; i < training.Ranks.Count; i++)
            {
                if (i == _rankRowViews.Count)
                {
                    var row = Instantiate(_rankRowPrefab, _rankRows);
                    row.Tapped += troop => RankPicked?.Invoke(troop);
                    _rankRowViews.Add(row);
                }

                _rankRowViews[i].gameObject.SetActive(true);
                _rankRowViews[i].Show(training.Ranks[i]);
            }

            for (var i = training.Ranks.Count; i < _rankRowViews.Count; i++) _rankRowViews[i].gameObject.SetActive(false);
        }

        public void CloseRanks() => _rankMenu.SetActive(false);

        private void OnPick() => _rankMenu.SetActive(!_rankMenu.activeSelf);

        private void ShowWard(DistrictCardData card)
        {
            _wardBlock.SetActive(card.Ward != null);
            if (card.Ward == null) return;
            _wardHead.Title = card.WardHead;
            _wardBeds.text = card.Ward.Beds;
            _wardEmpty.gameObject.SetActive(card.Ward.Rows.Count == 0);
            _wardEmpty.text = card.Ward.Empty;
            for (var i = 0; i < card.Ward.Rows.Count; i++)
            {
                if (i == _wardRowViews.Count)
                {
                    var row = Instantiate(_wardRowPrefab, _wardRows);
                    row.HealTapped += troop => HealTapped?.Invoke(troop);
                    _wardRowViews.Add(row);
                }

                _wardRowViews[i].gameObject.SetActive(true);
                _wardRowViews[i].Show(card.Ward.Rows[i]);
            }

            for (var i = card.Ward.Rows.Count; i < _wardRowViews.Count; i++) _wardRowViews[i].gameObject.SetActive(false);
        }

        private void ShowHarmony(DistrictCardData card)
        {
            _harmonyBlock.SetActive(card.HarmonyLine != null);
            if (card.HarmonyLine == null) return;
            _harmonyHead.Title = card.HarmonyHead;
            _harmonyLine.text = card.HarmonyLine;
            _harmonyNote.text = card.HarmonyNote;
        }

        private void ShowNeighbours(DistrictCardData card)
        {
            _neighboursBlock.SetActive(card.Neighbours.Count > 0);
            _neighboursHead.gameObject.SetActive(!string.IsNullOrEmpty(card.NeighboursHead));
            _neighboursHead.Title = card.NeighboursHead;
            for (var i = 0; i < card.Neighbours.Count; i++)
            {
                if (i == _badgeViews.Count) _badgeViews.Add(Instantiate(_badgePrefab, _badges));
                _badgeViews[i].gameObject.SetActive(true);
                _badgeViews[i].Show(card.Neighbours[i].Text, card.Neighbours[i].Good);
            }

            for (var i = card.Neighbours.Count; i < _badgeViews.Count; i++) _badgeViews[i].gameObject.SetActive(false);
        }

        private void ShowCrew(DistrictCardData card)
        {
            var crew = card.Crew;
            _crewHead.gameObject.SetActive(crew != null);
            _crew.SetActive(crew != null);
            if (crew == null) return;

            _crewHead.Title = card.CrewHead;
            _crewFace.Show(crew.Bust, crew.BustShift, crew.BustScale, string.Empty);
            _crewCount.text = crew.Count;
            _crewLimit.text = crew.Limit;
            _crewMinus.interactable = crew.CanRemove;
            _crewPlus.interactable = crew.CanAdd;
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _move.onClick.AddListener(OnMove);
            _upgrade.onClick.AddListener(OnUpgrade);
            _finishWork.Button.onClick.AddListener(OnFinishWork);
            _speedUpWork.onClick.AddListener(OnSpeedUpWork);
            _speedUpTraining.onClick.AddListener(OnSpeedUpTraining);
            _workshop.MakeTapped += OnMake;
            _pick.onClick.AddListener(OnPick);
            _workshop.CancelTapped += OnCancelGood;
            _workshop.FinishTapped += OnFinishGood;
            _workshop.SpeedUpTapped += OnSpeedUpGood;
            _amount.onClick.AddListener(OnAmount);
            _train.Button.onClick.AddListener(OnTrain);
            _finishTraining.Button.onClick.AddListener(OnFinishTraining);
            _crewMinus.onClick.AddListener(OnCrewMinus);
            _crewPlus.onClick.AddListener(OnCrewPlus);
            _tavernHeroes.onClick.AddListener(OnTavernHeroes);
            _tavernCall.onClick.AddListener(OnTavernCall);
            _shrine.PickTapped += OnShrinePick;
            _shrine.ActivateTapped += OnShrineActivate;
            _shrine.FlaskTapped += OnShrineFlask;
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _move.onClick.RemoveListener(OnMove);
            _upgrade.onClick.RemoveListener(OnUpgrade);
            _finishWork.Button.onClick.RemoveListener(OnFinishWork);
            _speedUpWork.onClick.RemoveListener(OnSpeedUpWork);
            _speedUpTraining.onClick.RemoveListener(OnSpeedUpTraining);
            _workshop.MakeTapped -= OnMake;
            _pick.onClick.RemoveListener(OnPick);
            _workshop.CancelTapped -= OnCancelGood;
            _workshop.FinishTapped -= OnFinishGood;
            _workshop.SpeedUpTapped -= OnSpeedUpGood;
            _amount.onClick.RemoveListener(OnAmount);
            _train.Button.onClick.RemoveListener(OnTrain);
            _finishTraining.Button.onClick.RemoveListener(OnFinishTraining);
            _crewMinus.onClick.RemoveListener(OnCrewMinus);
            _crewPlus.onClick.RemoveListener(OnCrewPlus);
            _tavernHeroes.onClick.RemoveListener(OnTavernHeroes);
            _tavernCall.onClick.RemoveListener(OnTavernCall);
            _shrine.PickTapped -= OnShrinePick;
            _shrine.ActivateTapped -= OnShrineActivate;
            _shrine.FlaskTapped -= OnShrineFlask;
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnMove() => MoveTapped?.Invoke();
        private void OnUpgrade() => UpgradeTapped?.Invoke();
        private void OnFinishWork() => FinishWorkTapped?.Invoke();
        private void OnSpeedUpWork() => SpeedUpWorkTapped?.Invoke();
        private void OnSpeedUpTraining() => SpeedUpTrainingTapped?.Invoke();
        private void OnMake() => MakeTapped?.Invoke();
        private void OnCancelGood(int index) => CancelGoodTapped?.Invoke(index);
        private void OnFinishGood() => FinishGoodTapped?.Invoke();
        private void OnSpeedUpGood() => SpeedUpGoodTapped?.Invoke();
        private void OnAmount() => AmountTapped?.Invoke();
        private void OnTrain() => TrainTapped?.Invoke();
        private void OnFinishTraining() => FinishTrainingTapped?.Invoke();
        private void OnCrewMinus() => CrewMinusTapped?.Invoke();
        private void OnCrewPlus() => CrewPlusTapped?.Invoke();
        private void OnTavernHeroes() => TavernHeroesTapped?.Invoke();
        private void OnTavernCall() => TavernCallTapped?.Invoke();
        private void OnShrinePick() => ShrinePickTapped?.Invoke();
        private void OnShrineActivate() => ShrineActivateTapped?.Invoke();
        private void OnShrineFlask() => ShrineFlaskTapped?.Invoke();
    }
}
