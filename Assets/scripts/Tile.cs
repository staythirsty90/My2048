using System.Collections.Generic;
using UnityEngine;

namespace My2048 {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class Tile : MonoBehaviour {
        public TileData CurrentMove { 
            get { return moves[Mathf.Max(0, moves.Count-1)]; } // return 0 or higher.
            set { 
                if(moves.Count == 0) {
                    moves.Add(value);
                }
                else {
                    moves[Mathf.Max(0, moves.Count-1)] = value; 
                }
            
            }
        }
        
        public uint value;
        public LerpData<Vector2> lerpData;

        public List<TileData> moves = new List<TileData>();

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

            if(t.moves.Count == 0) { // TODO: make this more Robust?
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