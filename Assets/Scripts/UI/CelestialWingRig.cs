using UnityEngine;
using UnityEngine.UI;

namespace SkullKing.UI
{
    public sealed class CelestialWingRig : MonoBehaviour
    {
        [SerializeField] private CelestialWingGraphic[] _wings;
        [SerializeField] private float _span=1.55f;
        [SerializeField] private float _rootOffset=.42f;
        [SerializeField] private float _featherLag=.055f;
        [Header("Painted anatomical poses (spread to embrace)")]
        [SerializeField] private Sprite[] _poseFrames;
        [SerializeField] private Image _poseFrom;
        [SerializeField] private Image _poseTo;
        [SerializeField,Min(1)] private float _poseScale=2.25f;
        [Header("Continuous silhouette animation")]
        [SerializeField] private CelestialPoseContour[] _contours;
        [SerializeField] private CelestialPoseMorphGraphic _morphFrom;
        [SerializeField] private CelestialPoseMorphGraphic _morphTo;
        private bool MorphReady => _contours!=null&&_contours.Length==6&&
            System.Array.TrueForAll(_contours,c=>c!=null&&c.Valid)&&_morphFrom!=null&&_morphTo!=null;
        private bool PaintedReady => _poseFrames!=null&&_poseFrames.Length==6&&
            System.Array.TrueForAll(_poseFrames,s=>s!=null)&&_poseFrom!=null&&_poseTo!=null;
        public bool Ready => MorphReady||PaintedReady||(_wings!=null&&_wings.Length==6&&System.Array.TrueForAll(_wings,w=>w!=null));
        public void Pose(Vector2 card,float phase,float alpha,bool revealing)
        {
            if(!Ready)return;
            if(MorphReady||PaintedReady){PosePainted(card,phase,alpha,revealing);return;}
            for(int i=0;i<_wings.Length;i++)
            {
                int layer=i%3;bool right=i>=3;
                var wing=_wings[i];
                // Keep legacy serialized references, but display exactly one wing on each side.
                bool visible=layer==1;
                if(wing.gameObject.activeSelf!=visible)wing.gameObject.SetActive(visible);
                if(!visible)continue;
                float t=Mathf.Clamp01(phase-(right?_featherLag:0));
                float spread=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.34f,t));
                float fold=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.47f,1,t));
                // Normalize the delayed side so both reach the same closed pose before the swap.
                fold=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.47f,1-(right?_featherLag:0),t));
                if(revealing){spread=1;fold=1-Mathf.SmoothStep(0,1,phase);}
                float flutter=Mathf.PI*2*t;
                var rect=wing.rectTransform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(1,.52f);
                rect.anchoredPosition=new Vector2((right?1:-1)*card.x*_rootOffset,card.y*.19f);
                rect.sizeDelta=new Vector2(card.x*_span,card.x*_span*2/3);
                rect.localScale=new Vector3(right?-1:1,1,1);
                // Lay two whole wings diagonally over the card, like closing palms; no twisting fan.
                float fan=Mathf.Lerp(-8,-12,fold)+Mathf.Sin(flutter)*2*(1-fold);
                rect.localRotation=Quaternion.Euler(0,0,right?-fan:fan);
                wing.color=new Color(1,.98f,.91f,alpha);
                wing.SetPose(spread,fold,flutter);
            }
        }
        private void PosePainted(Vector2 card,float phase,float alpha,bool revealing)
        {
            // Artist-drawn shoulder/elbow folding, never a rotated/mirrored flat-wing substitute.
            if(_wings!=null)foreach(var wing in _wings)
                if(wing!=null&&wing.gameObject.activeSelf)wing.gameObject.SetActive(false);
            float progress=revealing?Mathf.Lerp(1,.32f,Mathf.SmoothStep(0,1,phase)):
                Mathf.InverseLerp(.12f,1,phase);
            float frame=progress*(_poseFrames.Length-1);
            int from=Mathf.FloorToInt(frame),to=Mathf.Min(from+1,_poseFrames.Length-1);
            // No per-frame easing/pauses: outline vertices travel throughout each interval.
            float mix=frame-from;
            float size=Mathf.Max(card.x*_poseScale,card.y*1.05f);
            if(MorphReady)
            {
                if(_poseFrom!=null)_poseFrom.gameObject.SetActive(false);
                if(_poseTo!=null)_poseTo.gameObject.SetActive(false);
                SetMorph(_morphFrom,_contours[from],_contours[from],_contours[to],mix,size,
                    alpha*(1-mix)/Mathf.Max(.0001f,1-alpha*mix));
                SetMorph(_morphTo,_contours[to],_contours[from],_contours[to],mix,size,alpha*mix);
                return;
            }
            SetFrame(_poseFrom,_poseFrames[from],size,alpha*(1-mix)/Mathf.Max(.0001f,1-alpha*mix));
            SetFrame(_poseTo,_poseFrames[to],size,alpha*mix);
        }
        private static void SetMorph(CelestialPoseMorphGraphic graphic,CelestialPoseContour source,
            CelestialPoseContour from,CelestialPoseContour to,float mix,float size,float alpha)
        {
            if(!graphic.gameObject.activeSelf)graphic.gameObject.SetActive(true);
            var rect=graphic.rectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.one*size;
            rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
            graphic.color=new Color(1,1,1,Mathf.Clamp01(alpha));
            graphic.SetPose(source,from,to,mix);
        }
        private static void SetFrame(Image image,Sprite sprite,float size,float alpha)
        {
            if(!image.gameObject.activeSelf)image.gameObject.SetActive(true);
            var rect=image.rectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.one*size;
            rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
            image.sprite=sprite;image.color=new Color(1,1,1,Mathf.Clamp01(alpha));
            image.raycastTarget=false;image.preserveAspect=true;
        }
    }
}
