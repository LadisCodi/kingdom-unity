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
    // Placing a building: a small window across the bottom, because the map is the screen here. Its band
    // carries the building's name and the close, which is the cancel; its body the picture, what it does and how
    // long it takes, and the priced Build. View only: the PlacementMenuPresenter fills it.
    public class PlacementMenu : Menu
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _close;
        [SerializeField] private BuildingPortrait _portrait;
        [SerializeField] private TMP_Text _promise;
        [SerializeField] private TMP_Text _wait;
        [SerializeField] private TMP_Text _reason;
        [SerializeField] private CostButton _build;
        [SerializeField] private Color _ordinalColor = new Color32(0xf4, 0xe4, 0xc1, 0xcc);


        public event Action CloseTapped;
        public event Action BuildTapped;

        // What the tutorial's lines call its controls.
        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_build.Button, "place-confirm");
        }

        public void Show(PlacementPanelData panel)
        {
            _title.text = string.IsNullOrEmpty(panel.Ordinal)
                ? panel.Name
                : $"{panel.Name}<size=75%><color=#{ColorUtility.ToHtmlStringRGBA(_ordinalColor)}> {panel.Ordinal}</color></size>";
            _portrait.Show(panel.Art);
            _promise.text = panel.Promise;
            _wait.text = "<sprite name=\"hourglass\">" + panel.Wait;
            _wait.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(panel.Wait));
            _build.Button.Label = panel.Verb;
            _reason.text = "<sprite name=\"padlock\">" + panel.Reason;
            _reason.gameObject.SetActive(!string.IsNullOrEmpty(panel.Reason));
            _build.Show(panel.Price, panel.CanBuild);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _build.Button.onClick.AddListener(OnBuild);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _build.Button.onClick.RemoveListener(OnBuild);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnBuild() => BuildTapped?.Invoke();
    }
}
