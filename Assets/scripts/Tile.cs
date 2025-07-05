using UnityEngine;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class Tile : MonoBehaviour {
    public MoveMemory currentMove;
    public Index index;
    public Index otherTileIndex;
    public uint value;
    public Vector2 nextPosition;
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

    public static Tile operator +(Tile a, Tile b) {
        a.Merge();
        a.otherTileIndex = b.index;
        b.otherTileIndex = a.index;
        b.Remove();
        DebugSetGameObjectName(a);
        return a;
    }

    public void Lerp() {
        float timeSinceStarted = Time.time - lerpData.timeStarted;
        lerpData.t = timeSinceStarted / lerpData.lerpDuration;
        tf.position = Vector3.Slerp(lerpData.start, lerpData.end, lerpData.t);
    }

    public void MoveToPos(Vector2 pos) {
        tf.position = pos;
    }

    public void SetSprite() {
        sr.sprite = game.ValueToTile(value);
        DebugSetGameObjectName(this);
    }

    public static void DebugSetGameObjectName(Tile t) {
        if (!t) return;
        //t.name = "Tile " + "(" + t.index.x + "," + t.index.y + ")" + " Value: " + t.value;
    }

    public void Clean() {
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

    public void Unmerge() {
        value /= 2;
        DebugSetGameObjectName(this);
    }

    public void Remove() {
        currentMove.removed = true;
    }

    public void Reset() {
        Clean();
        value = 0;
        nextPosition = new Vector2(-1, -1);
        gameObject.SetActive(false);
        animator.enabled = false;
    }

    public void OnRemovedFromPool() {
        Clean();
        animator.enabled = true;
        gameObject.SetActive(true);
        Spawn();
    }

    public void Undo() {
        if (currentMove.merged) {
            Unmerge();
        }
        if (currentMove.removed) {
            gameObject.SetActive(true);
        }
    }

    void OnShrinkFinished() { // NOTE: Called from the shrink Animation event.
        game.OnSpawnedTileShrunk();
    }
}