using System;
using Codigames.Game.UI.Kit;
using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Menus
{
    // The stand-in rewarded video (the web's adScreen): full-bleed and inescapable, an unmistakable placeholder, its
    // countdown, and Claim once it has played. View only: the AdScreenPresenter fills it.
    public class AdScreen : Menu
    {
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private KitButton _claim;

        public event Action ClaimTapped;

        protected override void InitializeInternal() => _claim.onClick.AddListener(() => ClaimTapped?.Invoke());

        public void ShowLeft(string left, bool ready)
        {
            _countdown.gameObject.SetActive(!ready);
            _countdown.text = left;
            _claim.gameObject.SetActive(ready);
        }
    }
}
