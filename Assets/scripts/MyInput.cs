using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace My2048 {
    
    public static class MyInput {

        public static Vector2 GetInputDirection(in TwentyFortyEight game) {
            if(game.IsMoving || game.IsUndoing) {
                return default;
            }

            var swipeDelta = Vector2.zero;
            var startTouch = Vector2.zero;

            #region Swipe Input
            if(Input.GetMouseButtonDown(0)) {
                if(IsTouchOverUIButton()) {
                    return default;
                }
                startTouch = Input.mousePosition;
            }
            else if(Input.GetMouseButtonUp(0)) {
                swipeDelta = (Vector2)Input.mousePosition - startTouch;
                return TrySwipe(startTouch, swipeDelta, game);
            }

            if(Input.touchCount != 0) {
                Touch touch = Input.GetTouch(0);
                if(touch.phase == TouchPhase.Began) {
                    if(IsTouchOverUIButton()) {
                        return default;
                    }
                    startTouch = touch.position;
                }
                else if(touch.phase == TouchPhase.Ended) {
                    swipeDelta = Vector2.zero;
                    if(startTouch != Vector2.zero) {
                        swipeDelta = touch.position - startTouch;
                    }
                    return TrySwipe(startTouch, swipeDelta, game);
                }
            }
            #endregion

#if UNITY_EDITOR
            if(Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow)) {
                return Vector2.up;
            }

            if(Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)) {
                return Vector2.down;
            }

            if(Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow)) {
                return Vector2.right;
            }

            if(Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow)) {
                return Vector2.left;
            }

            if(Input.GetKeyUp(KeyCode.R)) {
                SceneManager.LoadScene(sceneBuildIndex: 0);
            }

            if(Input.GetKeyDown(KeyCode.T)) {
                game.board.SpawnRandomTile();
            }

            if(Input.GetKeyUp(KeyCode.Z) && game.board.spawnedTile) {
                game.board.spawnedTile.AnimateShrink();
            }

            return default;
#endif
        }

        public static Vector2 TrySwipe(Vector2 startTouch, Vector2 swipeDelta, in TwentyFortyEight game) {
            if(startTouch == Vector2.zero) {
                return Vector2.zero;
            }

            if(swipeDelta.magnitude > game.TouchDeadZone && (startTouch.y / Screen.height) < game.TouchMaxHeightPercent) {
                if(IsTouchOverUIButton()) {
                    return Vector2.zero;
                }
                var x = swipeDelta.x;
                var y = swipeDelta.y;
                if(Mathf.Abs(x) > Mathf.Abs(y)) {
                    // left or right
                    return x < 0 ? Vector2.left : Vector2.right;
                }
                else if(y > 0) {
                    // up or down
                    return y > 0 ? Vector2.up : Vector2.down;
                }
            }
            return Vector2.zero;
        }

        static List<RaycastResult> results = new List<RaycastResult>(2);
        public static bool IsTouchOverUIButton() {
            results.Clear();
            var p = new PointerEventData(EventSystem.current) {
                position = Input.mousePosition
            };
            EventSystem.current.RaycastAll(p, results);
            return results.Count > 0;
#if !UNITY_EDITOR && !UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
#else
            return EventSystem.current.IsPointerOverGameObject();
#endif
        }
    }
}