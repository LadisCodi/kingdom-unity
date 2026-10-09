using System;
using System.Collections.Generic;
using Codigames.Game.UI.Battles;
using Codigames.Game.UI.Kit;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // The attack screen (Docs/proposals/lairs.md §6), top to bottom: the enemy's board and its power, ours and our
    // power, the roster — one tile per troop — and the action box: the price over Attack, Quick deploy beside it, the
    // soldiers the fight will cost as its hint. No rows: where a squad stands is its unit's business. View only.
    public class AttackSheetMenu : Menu
    {
        private static readonly Color SHORT = new(1f, 0.816f, 0.753f);

        [SerializeField] private Button _close;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private HeadPanel _enemy;
        [SerializeField] private RectTransform _enemyCells;
        [SerializeField] private HeadPanel _army;
        [SerializeField] private RectTransform _armyCells;
        [SerializeField] private AttackCell _cellPrefab;
        [SerializeField] private GameObject _heroRow;
        [SerializeField] private RectTransform _heroCells;
        [SerializeField] private HeroSlotView _heroSlotPrefab;
        [SerializeField] private SectionHead _rosterHead;
        [SerializeField] private RectTransform _roster;
        [SerializeField] private AttackTile _tilePrefab;
        [SerializeField] private PriceLabel _price;
        [SerializeField] private KitButton _quick;
        [SerializeField] private KitButton _attack;
        [SerializeField] private TMP_Text _note;

        private readonly List<AttackCell> _enemyShown = new();
        private readonly List<AttackCell> _armyShown = new();
        private readonly List<AttackTile> _tiles = new();
        private readonly List<HeroSlotView> _heroSlots = new();
        private Color _trail;

        public event Action CloseTapped;
        public event Action<int> SlotTapped;
        public event Action<int> TroopTapped;
        public event Action<int> HeroSlotTapped;
        public event Action QuickDeployTapped;
        public event Action AttackTapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_close, "close");
            CoachTarget.Tag(_quick, "battle-deploy");
            CoachTarget.Tag(_attack, "battle-go");
            _trail = Color.white;
        }

        public void Show(AttackSheetData data)
        {
            _title.text = data.Title;
            _enemy.Title = data.EnemyHead;
            _enemy.Trail = data.EnemyPower;
            Cells(_enemyShown, _enemyCells, data.Enemy, data.Enemy.Count, null);

            _army.Title = data.ArmyHead;
            _army.Trail = data.ArmyPower;
            _army.TrailColor = data.Short ? SHORT : _trail;
            Cells(_armyShown, _armyCells, data.Party, data.Slots, i => SlotTapped?.Invoke(i));

            // No hero yet — the first comes from the Tavern's banner — and the row would offer nothing.
            _heroRow.SetActive(data.Heroes.Count > 0);
            for (var i = 0; i < data.Heroes.Count; i++)
            {
                if (i == _heroSlots.Count)
                {
                    var slot = Instantiate(_heroSlotPrefab, _heroCells);
                    var index = i;
                    slot.Tapped += () => HeroSlotTapped?.Invoke(index);
                    _heroSlots.Add(slot);
                }

                _heroSlots[i].gameObject.SetActive(true);
                _heroSlots[i].Show(data.Heroes[i]);
            }

            for (var i = data.Heroes.Count; i < _heroSlots.Count; i++) _heroSlots[i].gameObject.SetActive(false);

            _rosterHead.Title = data.RosterHead;
            for (var i = 0; i < data.Roster.Count; i++)
            {
                if (i == _tiles.Count)
                {
                    var tile = Instantiate(_tilePrefab, _roster);
                    var index = i;
                    tile.Tapped += () => TroopTapped?.Invoke(index);
                    _tiles.Add(tile);
                }

                var t = data.Roster[i];
                _tiles[i].gameObject.SetActive(true);
                _tiles[i].Show(t.Bust, t.Shift, t.Scale, t.Count, t.Rank, t.Name, t.Out, t.Full);
            }

            for (var i = data.Roster.Count; i < _tiles.Count; i++) _tiles[i].gameObject.SetActive(false);

            _price.Show(data.Price);
            _quick.Label = data.QuickDeploy;
            _attack.Label = data.Action;
            _attack.interactable = !data.Blocked;
            _note.text = data.Note;
        }

        private void Cells(List<AttackCell> shown, RectTransform parent, IReadOnlyList<AttackSquadData> squads, int slots, Action<int> tapped)
        {
            for (var i = 0; i < slots; i++)
            {
                if (i == shown.Count)
                {
                    var cell = Instantiate(_cellPrefab, parent);
                    var index = i;
                    if (tapped != null) cell.Tapped += () => tapped(index);
                    shown.Add(cell);
                }

                shown[i].gameObject.SetActive(true);
                shown[i].Tappable = tapped != null && i < squads.Count;
                if (i < squads.Count) shown[i].Show(squads[i].Bust, squads[i].Shift, squads[i].Scale, squads[i].Count, squads[i].Rank);
                else shown[i].ShowEmpty();
            }

            for (var i = slots; i < shown.Count; i++) shown[i].gameObject.SetActive(false);
        }

        protected override void SubscribeToEventsInternal()
        {
            _close.onClick.AddListener(OnClose);
            _quick.onClick.AddListener(OnQuick);
            _attack.onClick.AddListener(OnAttack);
        }

        protected override void UnsubscribeFromEventsInternal()
        {
            _close.onClick.RemoveListener(OnClose);
            _quick.onClick.RemoveListener(OnQuick);
            _attack.onClick.RemoveListener(OnAttack);
        }

        private void OnClose() => CloseTapped?.Invoke();
        private void OnQuick() => QuickDeployTapped?.Invoke();
        private void OnAttack() => AttackTapped?.Invoke();
    }
}
