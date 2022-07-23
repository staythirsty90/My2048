using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Index {
    public int x;
    public int y;

    public Index(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public override string ToString() {
        return x.ToString() + " : " + y.ToString();
    }
}
[System.Serializable]
public struct LerpData<T> {
    public float lerpDuration;// = 0.25f;
    public float timeStarted;// = Time.time;
    public T start;// = tf.position;
    public T end;// = pos;
    public float t;
}

[System.Serializable]
public struct moveMemory {
    public bool merged;// = false;
    public bool removed;// = false;
    public bool spawnedFromMove;// = false;
    public Index index;
}


public class Tile : MonoBehaviour {
    public List<moveMemory> memory = new List<moveMemory>(100);
    public moveMemory currentMove;
    public Index index;
    public Index otherTileIndex;
    public uint value;
    public Vector2 nextPosition;
    Transform tf;
    public Animator animator;
    SpriteRenderer sr;
    static TwentyFortyEight game;
    public LerpData<Vector2> lerpData;

    private void Awake() {
        tf = transform;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if(!game) game = FindObjectOfType<TwentyFortyEight>();
        spawn();
    }

    public static Tile operator +(Tile a, Tile b) {
        a.merge();
        a.otherTileIndex = b.index;
        b.otherTileIndex = a.index;
        b.remove();
        DebugSetGameObjectName(a);
        return a;
    }

    public void lerp() {
        float timeSinceStarted = Time.time - lerpData.timeStarted;
        lerpData.t = timeSinceStarted / lerpData.lerpDuration;
        tf.position = Vector3.Slerp(lerpData.start, lerpData.end, lerpData.t);
    }

    public void moveToPos(Vector2 pos) {
        tf.position = pos;
    }

    public void setSprite() {
        //index = new Index(x, y);
        sr.sprite = game.valueToTile(value);
        //sr.sprite = s;
        DebugSetGameObjectName(this);
    }

    public static void DebugSetGameObjectName(Tile t) {
        if (!t) return;
        //t.name = "Tile " + "(" + t.index.x + "," + t.index.y + ")" + " Value: " + t.value;
    }

    public void clean() {
        currentMove.merged = false;
        currentMove.removed = false;
        currentMove.spawnedFromMove = false;
        otherTileIndex = new Index(-1,-1);
    }

    public void shrink() {
        animator.SetTrigger("shrink");
    }

    public void spawn() {
        animator.SetTrigger("spawn");
    }

    public void merge() {
        value += value;
        currentMove.merged = true;
    }

    public void unmerge() {
        value = value / 2;
        DebugSetGameObjectName(this);
    }

    public void remove() {
        currentMove.removed = true;
    }

    public void reset() {
        clean();
        value = 0;
        nextPosition = new Vector2(-1, -1);
        gameObject.SetActive(false);
        animator.enabled = false;
    }

    public void onRemovedFromPool() {
        clean();
        animator.enabled = true;
        gameObject.SetActive(true);
        spawn();
    }

    public void undo() {
        if (currentMove.merged) {
            unmerge();
        }
        if (currentMove.removed) {
            gameObject.SetActive(true);
        }
    }

    void onShrinkFinished() {
        game.onSpawnedTileShrunk();
    }
}
