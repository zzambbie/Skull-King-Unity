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
            [TextArea(2, 3)] public string summary;
            [TextArea(12, 30)] public string body;
            public Sprite illustration;
            [TextArea(2, 3)] public string illustrationCaption;
            public string exampleA;
            public string exampleB;
            public string exampleC;
            public string exampleResult;
        }
        public bool illustratedLayout;
        public Page[] pages;
    }
}
