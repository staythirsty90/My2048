using UnityEngine;

namespace My2048 {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class Tile : MonoBehaviour {
        public MoveMemory currentMove;
        public Index index;
        public Index otherTileIndex;
        public uint value;
        public Animator animator;
        public LerpData<Vector2> lerpData;

        Transform tf;
        SpriteRenderer sr;
        static TwentyFortyEight game;

        void Awake() {
            tf = transform;
            sr = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            if(!game) game = FindObjectOfType<TwentyFortyEight>();
            Spawn();
        }

        public void MergeWith(Tile other) {
            Merge();
            otherTileIndex = other.index;
            other.otherTileIndex = index;
            other.Remove();
            DebugSetGameObjectName(this);
        }

        public void InitLerp(in float lerpDuration) {
            lerpData.timeStarted    = Time.time;
            lerpData.start          = tf.position;
            lerpData.lerpDuration   = lerpDuration;
            lerpData.t              = 0f;
        }

        public void Lerp() {
            var timeSinceStarted = Time.time - lerpData.timeStarted;
            lerpData.t  = timeSinceStarted / lerpData.lerpDuration;
            tf.position = Vector3.Slerp(lerpData.start, lerpData.end, lerpData.t);
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
            currentMove.merged = false;
            currentMove.removed = false;
            currentMove.spawnedFromMove = false;
            otherTileIndex = Index.Invalid;
        }

        public void Shrink() {
            animator.SetTrigger("shrink");
        }

        public void Spawn() {
            animator.SetTrigger("spawn");
        }

        public void Merge() {
            value += value;
            currentMove.merged = true;
        }

        public void Remove() {
            currentMove.removed = true;
        }

        public void Deactivate() {
            ResetFlagsAndIndex();
            value = 0;
            gameObject.SetActive(false);
        }

        public void OnRemovedFromPool() {
            ResetFlagsAndIndex();
            gameObject.SetActive(true);
            Spawn();
        }

        public void Undo() {
            if(currentMove.merged) {
                // Unmerge
                value /= 2;
                DebugSetGameObjectName(this);
            }
            if(currentMove.removed) {
                gameObject.SetActive(true);
            }
        }

        void OnShrinkFinished() { // NOTE: Called from the shrink Animation event.
            game.OnSpawnedTileShrunk();
        }
    }
}