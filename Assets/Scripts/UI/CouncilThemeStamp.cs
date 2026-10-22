using UnityEngine;

namespace SkullKing.UI
{
    /// <summary>One-time authoring receipt. No runtime layout changes or automatic restyling.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class CouncilThemeStamp : MonoBehaviour
    {
        [SerializeField, HideInInspector] private int _revision;
        [SerializeField, HideInInspector] private int _settingsControlsRevision;
        [SerializeField, HideInInspector] private int _gameVisualsRevision;
        [SerializeField, HideInInspector] private int _celestialRigRevision;
        [SerializeField, HideInInspector] private int _titleUtilityIconsRevision;
        public int Revision => _revision;
        public int SettingsControlsRevision => _settingsControlsRevision;
        public int GameVisualsRevision => _gameVisualsRevision;
        public int CelestialRigRevision => _celestialRigRevision;
        public int TitleUtilityIconsRevision => _titleUtilityIconsRevision;
    }
}
