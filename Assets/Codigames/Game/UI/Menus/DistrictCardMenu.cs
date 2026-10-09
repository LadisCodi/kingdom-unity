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

        [Header("Crew")]
        [SerializeField] private SectionHead _crewHead;
        [SerializeField] private GameObject _crew;
        [SerializeField] private KitButton _crewMinus;
        [SerializeField] private UnitPortrait _crewFace;
        [SerializeField] private TMP_Text _crewCount;
        [SerializeField] private TMP_Text _crewLimit;
        [SerializeField] private KitButton _crewPlus;

        [SerializeField] private RectTransform _window;

        public event Action CloseTapped;
        public event Action MoveTapped;
        public event Action UpgradeTapped;
        public event Action FinishWorkTapped;
        public event Action AmountTapped;
        public event Action TrainTapped;
        public event Action FinishTrainingTapped;
        public event Action CrewMinusTapped;
        public event Action CrewPlusTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close", "card:close");
            CoachTarget.Tag(_move, "card:move");
            CoachTarget.Tag(_upgrade, "card:upgrade");
            CoachTarget.Tag(_train.Button, "card:train");
            CoachTarget.Tag(_finishTraining.Button, "card:finish-training");
            CoachTarget.Tag(_crewPlus, "card:workers");
        }

        public void Show(DistrictCardData card)
        {
            _title.text = card.Title;
            _move.gameObject.SetActive(card.Movable);
            _portrait.Show(card.Art);
            _what.text = card.What;

            var work = card.Work;
            _workBar.gameObject.SetActive(work != null);
            _doing.gameObject.SetActive(work != null);
            _finishWork.gameObject.SetActive(work != null);
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
            _batchWhat.text = batch.Doing;
            // Training runs green, as the web's batch bar does.
            _batchBar.Set(batch.Progress, batch.Left, done: true);
            _batchTotal.text = batch.Total;
            _finishTraining.Show(batch.Finish, batch.CanFinish);
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
            _amount.onClick.AddListener(OnAmount);
            _train.Button.onClick.AddListener(OnTrain);
            _finishTraining.Button.onClick.AddListener(OnFinishTraining);
            _crewMinus.onClick.AddListener(OnCrewMinus);
            _crewPlus.onClick.AddListener(OnCrewPlus);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _move.onClick.RemoveListener(OnMove);
            _upgrade.onClick.RemoveListener(OnUpgrade);
            _finishWork.Button.onClick.RemoveListener(OnFinishWork);
            _amount.onClick.RemoveListener(OnAmount);
            _train.Button.onClick.RemoveListener(OnTrain);
            _finishTraining.Button.onClick.RemoveListener(OnFinishTraining);
            _crewMinus.onClick.RemoveListener(OnCrewMinus);
            _crewPlus.onClick.RemoveListener(OnCrewPlus);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnMove() => MoveTapped?.Invoke();
        private void OnUpgrade() => UpgradeTapped?.Invoke();
        private void OnFinishWork() => FinishWorkTapped?.Invoke();
        private void OnAmount() => AmountTapped?.Invoke();
        private void OnTrain() => TrainTapped?.Invoke();
        private void OnFinishTraining() => FinishTrainingTapped?.Invoke();
        private void OnCrewMinus() => CrewMinusTapped?.Invoke();
        private void OnCrewPlus() => CrewPlusTapped?.Invoke();
    }
}
