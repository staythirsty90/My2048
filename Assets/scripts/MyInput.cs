using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TwentyFortyEight))]
public class MyInput : MonoBehaviour {
    TwentyFortyEight game;
    
    Vector2 startTouch;
    Vector2 swipeDelta;

    void Awake() {
        game = GetComponent<TwentyFortyEight>();
    }

    public void HandleInput() {
        if(game.IsMoving || game.IsUndoing) {
            return;
        }
        #region Swipe Input
        if(Input.GetMouseButtonDown(0)) {
            if(IsTouchOverUIButton()) {
                return;
            }
            startTouch = Input.mousePosition;
        }
        else if(Input.GetMouseButtonUp(0)) {
            swipeDelta = (Vector2)Input.mousePosition - startTouch;
            TrySwipe();
        }

        if(Input.touchCount != 0) {
            Touch touch = Input.GetTouch(0);
            if(touch.phase == TouchPhase.Began) {
                if(IsTouchOverUIButton()) {
                    return;
                }
                startTouch = touch.position;
            }
            else if(touch.phase == TouchPhase.Ended) {
                swipeDelta = Vector2.zero;
                if(startTouch != Vector2.zero) {
                    swipeDelta = touch.position - startTouch;
                }
                TrySwipe();
                startTouch = swipeDelta = Vector2.zero;
            }
        }
        #endregion

#if UNITY_EDITOR
        if(Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow)) {
            game.MoveTiles(MoveData.Up);
        }

        if(Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow)) {
            game.MoveTiles(MoveData.Down);
        }

        if(Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow)) {
            game.MoveTiles(MoveData.Right);
        }

        if(Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow)) {
            game.MoveTiles(MoveData.Left);
        }

        if(Input.GetKeyUp(KeyCode.R)) {
            SceneManager.LoadScene(sceneBuildIndex: 0);
        }

        if(Input.GetKeyDown(KeyCode.T)) {
            game.board.SpawnRandomTile();
        }

        if(Input.GetKeyUp(KeyCode.Z) && game.board.spawnedTile) {
            game.board.spawnedTile.Shrink();
        }
#endif
    }

    public void TrySwipe() {
        if(startTouch == Vector2.zero) {
            return;
        }

        if(swipeDelta.magnitude > game.TouchDeadZone && (startTouch.y / Screen.height) < game.TouchMaxHeightPercent) {
            if(IsTouchOverUIButton()) {
                return;
            }
            float x = swipeDelta.x;
            float y = swipeDelta.y;
            if(Mathf.Abs(x) > Mathf.Abs(y)) {
                // left or right
                if(x < 0) {
                    game.MoveTiles(MoveData.Left);
                }
                else {
                    game.MoveTiles(MoveData.Right);
                }
            }
            else if(y > 0) {
                // up or down
                game.MoveTiles(MoveData.Up);
            }
            else {
                game.MoveTiles(MoveData.Down);
            }
            startTouch = swipeDelta = Vector2.zero;
        }
    }

    List<RaycastResult> results = new List<RaycastResult>(2);
    public bool IsTouchOverUIButton() {
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