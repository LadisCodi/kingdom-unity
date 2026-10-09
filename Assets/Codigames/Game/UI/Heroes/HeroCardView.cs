using System;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Widgets;
using Codigames.Kingdom.Heroes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Heroes
{
    // A hero, 2:3, from painted pieces (the web's .hc): the rarity's face, the art on it, the gilt frame over both, the
    // type's banner from the top-left corner, the stars and the pill at the foot, the HP bar across it when hurt, the
    // rank's brass numeral, the check and the orb. A hero not found yet is the same card on warm stone: a dark
    // silhouette, its fragments in the pill. Asleep: darkened, the Zs rising.
    public class HeroCardView : MonoBehaviour
    {
        private static readonly string[] ROMAN = { "", "I", "II", "III", "IV", "V" };
        private static readonly Color STONE = new Color32(118, 88, 58, 255);
        private static readonly Color PILL_INK = new Color32(255, 246, 224, 255);
        private static readonly Color SILHOUETTE = new(0, 0, 0, 0.5f);
        private static readonly Color ASLEEP = new(0.5f, 0.5f, 0.5f, 1);
        private static readonly Color READY = new Color32(155, 227, 106, 255);

        [SerializeField] private Button _button;
        [SerializeField] private Image _face;
        [SerializeField] private Image _art;
        [SerializeField] private Image _banner;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private RectTransform _foot;
        [SerializeField] private AscensionStars _stars;
        [SerializeField] private Image _pillIcon;
        [SerializeField] private TMP_Text _pill;
        [SerializeField] private ProgressBar _hp;
        [SerializeField] private GameObject _rank;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private GameObject _check;
        [SerializeField] private CtaBadge _cta;
        [SerializeField] private GameObject _zzz;
        [SerializeField] private Sprite _faceCommon;
        [SerializeField] private Sprite _faceRare;
        [SerializeField] private Sprite _faceLegendary;
        [SerializeField] private Sprite _faceStone;
        [SerializeField] private Sprite _bannerWarrior;
        [SerializeField] private Sprite _bannerLancer;
        [SerializeField] private Sprite _bannerArcher;
        [SerializeField] private Sprite _bannerCavalry;
        // The type's mark, in white on its banner.
        [SerializeField] private Sprite _markWarrior;
        [SerializeField] private Sprite _markLancer;
        [SerializeField] private Sprite _markArcher;
        [SerializeField] private Sprite _markCavalry;
        // The foot's height off the card's bottom: over the HP bar when hurt, lower when not, lowest when not found.
        [SerializeField] private float _footHurt = 60;
        [SerializeField] private float _footWhole = 30;
        [SerializeField] private float _footMissing = 36;

        public event Action<string> Tapped;

        public string Id { get; private set; }

        private void Awake()
        {
            if (_button != null) _button.onClick.AddListener(() => Tapped?.Invoke(Id));
        }

        public void Show(HeroCardData hero)
        {
            Id = hero.Id;
            _face.sprite = hero.Missing ? _faceStone : hero.Rarity switch
            {
                HeroRarity.Legendary => _faceLegendary,
                HeroRarity.Rare => _faceRare,
                _ => _faceCommon,
            };
            _face.color = hero.Missing ? STONE : hero.Resting ? ASLEEP : Color.white;
            _art.sprite = hero.Art;
            _art.enabled = hero.Art != null;
            _art.color = hero.Missing ? SILHOUETTE : hero.Resting ? ASLEEP : Color.white;
            _banner.sprite = hero.UnitType switch
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

            _stars.gameObject.SetActive(!hero.Missing && !hero.Resting);
            if (!hero.Missing) _stars.Show(hero.Ascension, hero.StepsPerStar);
            _pill.text = hero.Pill;
            _pill.color = hero.PillReady ? READY : PILL_INK;
            _pillIcon.sprite = hero.PillIcon;
            _pillIcon.gameObject.SetActive(hero.PillIcon != null);

            _hp.gameObject.SetActive(hero.Hp.HasValue);
            if (hero.Hp.HasValue) _hp.Set(hero.Hp.Value, string.Empty, true);
            _foot.anchoredPosition = new Vector2(_foot.anchoredPosition.x,
                hero.Missing ? _footMissing : hero.Hp.HasValue ? _footHurt : _footWhole);

            _rank.SetActive(hero.Rank > 1 && !hero.Picked && !hero.Resting);
            _rankText.text = hero.Rank < ROMAN.Length ? ROMAN[hero.Rank] : hero.Rank.ToString();
            _check.SetActive(hero.Picked);
            _cta.gameObject.SetActive(hero.Cta);
            if (hero.Cta) _cta.Show(1);
            if (_zzz != null) _zzz.SetActive(hero.Resting);
        }
    }
}
