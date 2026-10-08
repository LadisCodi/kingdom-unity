using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codigames.Modules.UI
{
    // Every menu prefab the game can open, and which menus form a group (opening one closes the other).
    [CreateAssetMenu(fileName = "MenuCatalog", menuName = "Codigames/UI/Menu Catalog")]
    public class MenuCatalog : ScriptableObject
    {
        [SerializeField] private List<Menu> _menus = new();
        [SerializeField] private List<MenuGroup> _groups = new();

        public Menu GetPrefab(Type menuType)
        {
            foreach (var menu in _menus)
            {
                if (menu != null && menu.GetType() == menuType) return menu;
            }

            return null;
        }

        public bool AreGrouped(Type a, Type b)
        {
            foreach (var group in _groups)
            {
                if (group.Contains(a) && group.Contains(b)) return true;
            }

            return false;
        }

        [Serializable]
        private class MenuGroup
        {
            [SerializeField] private List<Menu> _menus = new();

            public bool Contains(Type menuType)
            {
                foreach (var menu in _menus)
                {
                    if (menu != null && menu.GetType() == menuType) return true;
                }

                return false;
            }
        }
    }
}
