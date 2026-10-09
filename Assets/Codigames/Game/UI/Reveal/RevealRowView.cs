using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Reveal
{
    // One line of the bag card (the web's bagRow): the hero, a silhouette until recruited, what the batch paid them and
    // their bar; the seal when the bar fills.
    public class RevealRowView : MonoBehaviour
    {
        [SerializeField] private Image _wash;
        [SerializeField] private Image _art;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private Image _countIcon;
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private GameObject _stamp;
        [SerializeField] private Sprite _gold;
        [SerializeField] private Sprite _blue;

        public RevealRowData Data { get; private set; }
        public ProgressBar Bar => _bar;
        public GameObject Stamp => _stamp;

        public void Show(RevealRowData row)
        {
            Data = row;
            _wash.color = row.Wash;
            _art.sprite = row.Art;
            SetMissing(row.Missing);
            _name.text = row.Name;
            _count.text = row.Count;
            _countIcon.sprite = row.CountIcon;
            _bar.gameObject.SetActive(row.Bar != null);
            if (row.Bar != null) SetBar(row.Bar.From, row.Bar.FromText);
            _stamp.SetActive(false);
        }

        public void SetBar(float share, string text) => _bar.Set(share, text, Data.Bar.Gold ? _gold : _blue);

        public void SetMissing(bool missing) => _art.color = missing ? new Color(0, 0, 0, 0.5f) : Color.white;
    }
}
