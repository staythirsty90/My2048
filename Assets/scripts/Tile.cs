using UnityEngine;

namespace My2048 {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class Tile : MonoBehaviour {
        public TileData CurrentMove;

        public uint value;
        public LerpData<Vector2> lerpData;

        Animator animator;
        Transform tf;
        SpriteRenderer sr;
        
        static TwentyFortyEight game;

        void Awake() {
            tf = transform;
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            if(!game) game = FindObjectOfType<TwentyFortyEight>();
            AnimateSpawn();
        }

        public void Lerp() {
            var timeSinceStarted = Time.time - lerpData.timeStarted;
            lerpData.t  = timeSinceStarted / lerpData.lerpDuration;
            tf.position = Vector3.Slerp(lerpData.start, lerpData.end, lerpData.t);
        }

        public static void TileEndLerp(Tile t, bool undo = false) {
            if(!t) {
                return;
            }
            
            if(!t.gameObject.activeInHierarchy) {
                return;
            }

            t.value = t.CurrentMove.value;

            t.transform.position = t.lerpData.end;
            t.SetSprite();

            if(!undo) {
                if(t.CurrentMove.merged) {
                    t.animator.SetTrigger("merge");
                }
                else if(t.CurrentMove.removed) {
                    t.gameObject.SetActive(false);
                }
            }
            else {
                if(t.CurrentMove.merged) {
                    t.value /= 2;
                    t.CurrentMove = new TileData {
                        value = t.value,
                        index = t.CurrentMove.index,
                    };
                    t.SetSprite();
                }
                else if(t.CurrentMove.removed) {
                    t.CurrentMove = new TileData {
                        value = t.value,
                        removed = false,
                        //index = t.CurrentMove.indexEnd,
                    };
                }
            }
        }

        //public bool Undo() {
        //    if(RingBuffer.head == -1) {
        //        // Skip unplayed?
        //        return false;
        //    }

        //    if(RingBuffer.head == 0) {
        //        // Skip tiles that cannot undo.
        //        return false;
        //    }

        //    if(FindUndoPoint()) {
        //        value = CurrentMove.value;

        //        if(value == 0) { // Check if the tile should be deactivated / removed.
        //            gameObject.SetActive(false);
        //        }
        //        else {
        //            gameObject.SetActive(true);
        //        }

        //        SetSprite();

        //        if(CurrentMove.removed) {
        //            // Use the RemovedIndex here.
        //            // Flip the Lerp Start and End positions.
        //            lerpData.end   = new Vector2 (CurrentMove.removedIndex.x, CurrentMove.removedIndex.y);
        //            lerpData.start = new Vector2 (CurrentMove.index.x, CurrentMove.index.y);
        //            transform.position = lerpData.start; // Instantly move the position to avoid Lerping
        //                                                           // from a possibly newly spawned position.
        //            // HACK
        //            // Force undo again...
        //            //RingBuffer.Undo();
        //        }
        //        else{
        //            // Flip the Lerp Start and End positions.
        //            lerpData.end   = new Vector2 (CurrentMove.index.x, CurrentMove.index.y);
        //            lerpData.start = transform.position;
        //        }

        //        return true;
        //    }
        //    return false;
        //}

        public static void InitLerp(Tile t, in float tileLerpDuration) {
            if(t == null) return;
            if(!t.gameObject.activeInHierarchy) return; // TODO:
            t.lerpData.timeStarted    = Time.time;
            t.lerpData.start          = t.tf.position;
            t.lerpData.lerpDuration   = tileLerpDuration;
            t.lerpData.t              = 0f;
        }

        public void SetSprite() {
            sr.sprite = game.ValueToTile(value);
            DebugSetGameObjectName(this);
        }

        public static void DebugSetGameObjectName(in Tile t) {
            if(!t) return;
            //t.name = "Tile " + "(" + t.index.x + "," + t.index.y + ")" + " Value: " + t.value;
        }

        public void ResetFlagsAndIndex() {
            //RingBuffer = new MyRingBuffer<TileData>();
            //CurrentMove.merged = false;
            //CurrentMove.removed = false;
            //CurrentMove.spawnedFromMove = false;

            //CurrentMove = new TileData();
        }

        public void AnimateShrink() {
            animator.SetTrigger("shrink");
        }

        public void AnimateSpawn() {
            animator.SetTrigger("spawn");
        }

        public void Deactivate() {
            ResetFlagsAndIndex();
            value = 0;
            gameObject.SetActive(false);
        }

        public void OnRemovedFromPool() {
            //ResetFlagsAndIndex();
            gameObject.SetActive(true);
            AnimateSpawn();
        }

        void OnShrinkFinished() { // NOTE: Called from the shrink Animation event.
            game.OnSpawnedTileShrunk();
        }
    }
}