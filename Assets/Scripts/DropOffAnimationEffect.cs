using UnityEngine;

public class DropOffAnimationEffect : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float fps = 12f;

    private float frameTime;
    private int currentFrame = 0;
    private bool isPlaying = false;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    public void PlayAnimation(Sprite[] frames, float animationFPS)
    {
        if (frames == null || frames.Length == 0)
        {
            DebugHelper.LogWarning("DropOffAnimationEffect: no animation frames!");
            return;
        }

        animationFrames = frames;
        fps = animationFPS;
        frameTime = 0f;
        currentFrame = 0;
        isPlaying = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = animationFrames[0];
        }
    }

    public void StopAnimation()
    {
        isPlaying = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (!isPlaying || animationFrames == null || animationFrames.Length == 0)
            return;

        frameTime += Time.deltaTime;

        float timePerFrame = 1f / fps;

        if (frameTime >= timePerFrame)
        {
            frameTime -= timePerFrame;
            currentFrame = (currentFrame + 1) % animationFrames.Length;

            if (spriteRenderer != null && currentFrame < animationFrames.Length)
            {
                spriteRenderer.sprite = animationFrames[currentFrame];
            }
        }
    }

    public bool IsPlaying() => isPlaying;
}