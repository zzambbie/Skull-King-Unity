using System;
using UnityEngine;

namespace SkullKing.UI
{
    [CreateAssetMenu(menuName = "CROSSION/Rulebook Content")]
    public sealed class RulebookContent : ScriptableObject
    {
        [Serializable]
        public sealed class Page
        {
            public string title;
            [TextArea(12, 30)] public string body;
        }
        public Page[] pages;
    }
}
