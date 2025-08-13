using UnityEngine;

namespace My2048 {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class Tile : MonoBehaviour {
        public TileData CurrentMove;

        public LerpData<Vector2> lerpData;

        Animator animator;
        Transform tf;
        SpriteRenderer sr;
        
        static TwentyFortyEight game;

        void Awake() {
            tf              = transform;
            sr              = GetComponent<SpriteRenderer>();
            animator        = GetComponent<Animator>();
            if(!game) game  = FindObjectOfType<TwentyFortyEight>();
            AnimateSpawn();
        }

        public void Lerp() {
            var timeSinceStarted = Time.time - lerpData.timeStarted;
            lerpData.t           = timeSinceStarted / lerpData.lerpDuration;
            tf.position          = Vector3.Slerp(lerpData.start, lerpData.end, lerpData.t);
        }

        public static void TileEndLerp(Tile t, bool undo = false) {
            if(!t) {
                return;
            }
            
            if(!t.gameObject.activeInHierarchy) {
                return;
            }

            t.transform.position    = t.lerpData.end;
            t.SetSprite();

            if(!undo) {
                if(t.CurrentMove.merged) {
                    t.AnimateMerge();
                }
                else if(t.CurrentMove.removed) {
                    t.gameObject.SetActive(false);
                }
            }
        }

        public static void InitLerp(Tile t, in float tileLerpDuration) {
            if(t == null) return;
            if(!t.gameObject.activeInHierarchy) return; // TODO:
            t.lerpData.timeStarted    = Time.time;
            t.lerpData.start          = t.tf.position;
            t.lerpData.lerpDuration   = tileLerpDuration;
            t.lerpData.t              = 0f;
        }

        public void SetSprite() {
            sr.sprite = game.ValueToTile(CurrentMove.value);
            DebugSetGameObjectName(this);
        }

        public static void DebugSetGameObjectName(in Tile t) {
            if(!t) return;
            if(t.gameObject.activeInHierarchy) {
                var txt = t.CurrentMove.merged ? "M " : "";
                txt += t.CurrentMove.removed ? " R" : "";
                txt += t.CurrentMove.spawnedFromMove ? " S" : "";
                t.GetComponentInChildren<TextMesh>()
                    .text = txt;
                t.name = $"Tile ({t.CurrentMove.index.x},{t.CurrentMove.index.y})({t.CurrentMove.indexEnd.x},{t.CurrentMove.indexEnd.y}) Value: {t.CurrentMove.value} {txt}";
            }
            else {
                t.name = "Tile(Clone)";
            }
        }

        public void AnimateShrink() {
            animator.SetTrigger("shrink");
        }

        public void AnimateMerge() {
            animator.SetTrigger("merge");
        }

        public void AnimateSpawn() {
            animator.SetTrigger("spawn");
        }

        public void Deactivate() {
            //value = 0;
            gameObject.SetActive(false);
        }

        public void OnRemovedFromPool(in bool isUndoing) {
            gameObject.SetActive(true);
            if(!isUndoing) {
                AnimateSpawn();
            }
        }

        void OnShrinkFinished() { // NOTE: Called from the shrink Animation event.
            game.OnSpawnedTileShrunk();
        }
    }
}