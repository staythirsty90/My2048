using System.Collections.Generic;
using UnityEngine;

namespace My2048 {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class Tile : MonoBehaviour {
        public TileData CurrentMove { 
            get { return RingBuffer.Peek(); } // return 0 or higher.
            set { RingBuffer.Set(value); }
        }

        public MyRingBuffer<TileData> RingBuffer = new MyRingBuffer<TileData>(4);

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

        public static void TileEndLerp(Tile t) {
            if(!t) {
                return;
            }
            
            if(!t.gameObject.activeInHierarchy) {
                return;
            }

            t.transform.position = t.lerpData.end;
            t.SetSprite();
            
            if(t.CurrentMove.merged) {
                t.animator.SetTrigger("merge");
            }
            else if(t.CurrentMove.removed) {
                t.gameObject.SetActive(false);
            }

            //t.RingBuffer.Advance();
        }

        public bool FindUndoPoint() {

            var headCopy = (RingBuffer.head - 1) % RingBuffer.capacity;
            var tries = 1; // Increment tries, since we took a step back in the code above.

            while(RingBuffer.buffer[headCopy].value == 0 && tries < RingBuffer.capacity) {
                headCopy = (headCopy - 1) % RingBuffer.capacity;
                tries++;
            }

            if(tries >= RingBuffer.capacity) {
                Debug.Log("Couldn't find an Undo point.");
                // Couldn't find an Undo point.
                return false;
            }
            else {
                // We found an Undo point.
                Debug.Log($"FOUND Undo point. tries: {tries}");
                RingBuffer.buffer[RingBuffer.head] = default; // Wipe current head pointer.
                RingBuffer.head = headCopy; // Set head to the newly found Undo pointer.
            }

            return true;
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
            sr.sprite = game.ValueToTile(value);
            DebugSetGameObjectName(this);
        }

        public static void DebugSetGameObjectName(in Tile t) {
            if(!t) return;
            //t.name = "Tile " + "(" + t.index.x + "," + t.index.y + ")" + " Value: " + t.value;
        }

        public void ResetFlagsAndIndex() {
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
            ResetFlagsAndIndex();
            gameObject.SetActive(true);
            AnimateSpawn();
        }

        public void Undo() {
            if(CurrentMove.merged) {
                // Unmerge
                value /= 2;
                DebugSetGameObjectName(this);
            }
            if(CurrentMove.removed) {
                gameObject.SetActive(true);
            }
        }

        void OnShrinkFinished() { // NOTE: Called from the shrink Animation event.
            game.OnSpawnedTileShrunk();
        }
    }
}