using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCell : MonoBehaviour
{
    [HideInInspector] public bool IsFilled; // Indicates if the pipe is filled
    [HideInInspector] public int PipeType; // Type of the pipe

    public int PipeData => PipeType + rotation * 10; // Data representation of the pipe

    [SerializeField] private Transform[] _pipePrefabs; // Array of pipe prefabs

    private Transform currentPipe; // The currently instantiated pipe
    private int rotation; // Current rotation of the pipe

    private SpriteRenderer emptySprite; // Sprite for the empty state
    private SpriteRenderer filledSprite; // Sprite for the filled state
    private List<Transform> connectBoxes; // Connection points for the pipe

    private const int minRotation = 0; // Minimum rotation value
    private const int maxRotation = 3; // Maximum rotation value
    private const int rotationMultiplier = 90; // Degrees per rotation

    public void Init(int pipe)
    {
        // Destroy the current pipe if it exists
        if (currentPipe != null)
        {
            Destroy(currentPipe.gameObject);
        }

        // Set the pipe type
        PipeType = pipe % 10;
        currentPipe = Instantiate(_pipePrefabs[PipeType], transform);
        currentPipe.transform.localPosition = Vector3.zero;

        // Set rotation based on pipe type
        if (PipeType == 1 || PipeType == 2)
        {
            rotation = pipe / 10; // Use the provided rotation for certain pipe types
        }
        else
        {
            rotation = Random.Range(minRotation, maxRotation + 1); // Random rotation for others
        }

        // Apply rotation
        currentPipe.transform.eulerAngles = new Vector3(0, 0, rotation * rotationMultiplier);

        // Set filled state based on pipe type
        IsFilled = (PipeType == 0 || PipeType == 1);

        // If the pipe type is 0, exit early
        if (PipeType == 0)
        {
            return;
        }

        // Get the sprite renderers for the empty and filled states
        emptySprite = currentPipe.GetChild(0).GetComponent<SpriteRenderer>();
        filledSprite = currentPipe.GetChild(1).GetComponent<SpriteRenderer>();

        // Update visibility based on the filled state
        UpdateFilled();

        // Initialize connection boxes
        connectBoxes = new List<Transform>();
        for (int i = 2; i < currentPipe.childCount; i++)
        {
            connectBoxes.Add(currentPipe.GetChild(i));
        }
    }

    public void UpdateInput()
    {
        // If the pipe type is 0, do nothing
        if (PipeType == 0)
        {
            return;
        }

        // Rotate the pipe
        rotation = (rotation + 1) % (maxRotation + 1);
        currentPipe.transform.eulerAngles = new Vector3(0, 0, rotation * rotationMultiplier);
    }

    public void UpdateFilled()
    {
        // If the pipe type is 0, do nothing
        if (PipeType == 0) return;

        // Update the visibility of the sprites based on the filled state
        emptySprite.gameObject.SetActive(!IsFilled);
        filledSprite.gameObject.SetActive(IsFilled);
    }

    public List<SpawnCell> ConnectedPipes()
    {
        List<SpawnCell> result = new List<SpawnCell>();

        // Check for connected pipes using raycasting
        foreach (var box in connectBoxes)
        {
            RaycastHit2D[] hit = Physics2D.RaycastAll(box.transform.position, Vector2.zero, 0.1f);
            foreach (var h in hit)
            {
                SpawnCell connectedPipe = h.collider.transform.parent.parent.GetComponent<SpawnCell>();
                if (connectedPipe != null)
                {
                    result.Add(connectedPipe);
                }
            }
        }

        return result;
    }
}