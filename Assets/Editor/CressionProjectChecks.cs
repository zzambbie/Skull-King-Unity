#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SkullKing.Editor
{
    public static class CressionProjectChecks
    {
        [MenuItem("CROSSION/Run Project Checks (Read Only)")]
        public static void Run()
        {
            int history = CressionTrickHistoryChecks.RunChecks();
            int whale = CressionWhalePresentationChecks.RunChecks();
            int network = CressionNetworkChecks.RunSnapshotChecks(true);
            int styling = CressionCouncilInterfaceSetup.RunStyleSafetyChecks();
            int cardEffect = CressionCardEffectChecks.RunChecks();
            int rulebook = CressionRulebookChecks.RunChecks();
            Debug.Log("[CROSSION Checks] 기록 " + history + ", 특수 판정 " + whale + ", 네트워크 " + network
                + ", UI 보호 " + styling + ", 카드 연출 영역 " + cardEffect + ", 그림 규칙 " + rulebook + "개 검사 통과. 씬/프리팹은 변경하지 않았습니다.");
        }
    }
}
#endif
