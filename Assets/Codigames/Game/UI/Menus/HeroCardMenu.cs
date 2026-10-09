using System;
using System.Collections.Generic;
using Codigames.Game.UI.Heroes;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using Codigames.Kingdom.Heroes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // One hero's card (the web's heroesSheet detail, mockup hero-detail-A), centred over the map: the rarity's ribbon,
    // the name and the type's badge on the plank; the hero on its rarity's stage, edge to edge, with its stars, its
    // stats down the left and the ascension over the foot; then the skill, the boon and the level as parchment trays.
    // A hero not found yet gets the same window: a silhouette, and its fragments where the level was. View only: the
    // HeroCardMenuPresenter fills it.
    public class HeroCardMenu : Menu
    {
        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _ribbon;
        [SerializeField] private TMP_Text _ribbonText;
        [SerializeField] private Image _typeBadge;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private TMP_Text _typeText;

        [SerializeField] private Image _stage;
        [SerializeField] private Image _art;
        [SerializeField] private AscensionStars _stars;
        [SerializeField] private List<Image> _statIcons = new();
        [SerializeField] private List<TMP_Text> _statLabels = new();
        [SerializeField] private List<TMP_Text> _statValues = new();
        [SerializeField] private GameObject _ascendBox;
        [SerializeField] private PriceLabel _ascendPrice;
        [SerializeField] private KitButton _ascend;

        [SerializeField] private TMP_Text _skillName;
        [SerializeField] private List<Image> _pips = new();
        [SerializeField] private TMP_Text _skillSays;
        [SerializeField] private GameObject _skillFoot;
        [SerializeField] private TMP_Text _skillNote;
        [SerializeField] private PriceLabel _skillPrice;
        [SerializeField] private KitButton _skillUpgrade;

        [SerializeField] private GameObject _boonHead;
        [SerializeField] private GameObject _boon;
        [SerializeField] private TMP_Text _boonText;

        [SerializeField] private TMP_Text _read;
        [SerializeField] private ProgressBar _readBar;
        [SerializeField] private GameObject _readNoteBox;
        [SerializeField] private TMP_Text _readNote;
        [SerializeField] private AscensionStars _capStars;
        [SerializeField] private GameObject _readBuyBox;
        [SerializeField] private PriceLabel _readPrice;
        [SerializeField] private KitButton _readButton;

        [SerializeField] private Sprite _ribbonCommon;
        [SerializeField] private Sprite _ribbonRare;
        [SerializeField] private Sprite _ribbonLegendary;
        [SerializeField] private Sprite _stageCommon;
        [SerializeField] private Sprite _stageRare;
        [SerializeField] private Sprite _stageLegendary;
        [SerializeField] private Sprite _bannerWarrior;
        [SerializeField] private Sprite _bannerLancer;
        [SerializeField] private Sprite _bannerArcher;
        [SerializeField] private Sprite _bannerCavalry;
        // The type's mark, in white on its banner.
        [SerializeField] private Sprite _markWarrior;
        [SerializeField] private Sprite _markLancer;
        [SerializeField] private Sprite _markArcher;
        [SerializeField] private Sprite _markCavalry;
        [SerializeField] private Color _pipOn = new Color32(232, 168, 40, 255);
        [SerializeField] private Color _pipOff = new Color32(255, 255, 255, 0);

        public event Action CloseTapped;
        public event Action AscendTapped;
        public event Action SkillTapped;
        public event Action ReadTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_readButton, "hero-level");
            _close.onClick.AddListener(() => CloseTapped?.Invoke());
            _ascend.onClick.AddListener(() => AscendTapped?.Invoke());
            _skillUpgrade.onClick.AddListener(() => SkillTapped?.Invoke());
            _readButton.onClick.AddListener(() => ReadTapped?.Invoke());
        }

        public void Show(HeroSheetData hero)
        {
            _title.text = hero.Name;
            _ribbon.sprite = Pick(hero.Rarity, _ribbonCommon, _ribbonRare, _ribbonLegendary);
            _ribbonText.text = hero.RarityLabel;
            _typeBadge.sprite = hero.UnitType switch
            {
                "Lancer" => _bannerLancer,
                "Archer" => _bannerArcher,
                "Cavalry" => _bannerCavalry,
                _ => _bannerWarrior,
            };
            _typeIcon.sprite = hero.UnitType switch
            {
                "Lancer" => _markLancer,
                "Archer" => _markArcher,
                "Cavalry" => _markCavalry,
                _ => _markWarrior,
            };
            _typeText.text = hero.TypeLabel;

            // Not found yet: the vault turns to warm stone and the hero to a silhouette.
            _stage.sprite = Pick(hero.Rarity, _stageCommon, _stageRare, _stageLegendary);
            _stage.color = hero.Missing ? new Color(0.62f, 0.55f, 0.46f) : Color.white;
            _art.sprite = hero.Art;
            _art.color = hero.Missing ? new Color(0, 0, 0, 0.55f) : Color.white;
            _stars.gameObject.SetActive(!hero.Missing);
            _stars.Show(hero.Ascension, hero.StepsPerStar);
            for (var i = 0; i < _statValues.Count && i < hero.Stats.Count; i++)
            {
                _statLabels[i].text = hero.Stats[i].Label;
                _statValues[i].text = hero.Stats[i].Value;
            }

            Buy(hero.Ascend, _ascendBox, _ascendPrice, _ascend);

            _skillName.text = hero.SkillName;
            for (var i = 0; i < _pips.Count; i++)
            {
                _pips[i].gameObject.SetActive(i < hero.SkillTop);
                _pips[i].color = i < hero.SkillRank ? _pipOn : _pipOff;
            }

            _skillSays.text = hero.SkillSays;
            var note = !string.IsNullOrEmpty(hero.SkillNote);
            _skillFoot.SetActive(note || hero.SkillBuy != null);
            _skillNote.gameObject.SetActive(note);
            _skillNote.text = hero.SkillNote;
            _skillPrice.gameObject.SetActive(hero.SkillBuy != null);
            _skillUpgrade.gameObject.SetActive(hero.SkillBuy != null);
            if (hero.SkillBuy != null) Buy(hero.SkillBuy, null, _skillPrice, _skillUpgrade);

            _boonHead.SetActive(hero.Boon != null);
            _boon.SetActive(hero.Boon != null);
            _boonText.text = hero.Boon;

            _read.text = hero.Read;
            _readBar.Set(hero.ReadShare, string.Empty, true);
            _readNoteBox.SetActive(hero.ReadNote != null);
            _readNote.text = hero.ReadNote;
            _capStars.gameObject.SetActive(hero.CapStars.HasValue);
            if (hero.CapStars.HasValue) _capStars.Show(hero.CapStars.Value, hero.StepsPerStar);
            Buy(hero.ReadBuy, _readBuyBox, _readPrice, _readButton);
        }

        private static void Buy(HeroBuy buy, GameObject box, PriceLabel price, KitButton button)
        {
            if (box != null) box.SetActive(buy != null);
            if (buy == null) return;
            price.Show(buy.Price ?? Array.Empty<Data.PriceTerm>());
            button.Label = buy.Label;
            button.Material = buy.Material;
            button.interactable = buy.Enabled;
        }

        private static Sprite Pick(HeroRarity rarity, Sprite common, Sprite rare, Sprite legendary)
            => rarity switch
            {
                HeroRarity.Legendary => legendary,
                HeroRarity.Rare => rare,
                _ => common,
            };
    }
}
