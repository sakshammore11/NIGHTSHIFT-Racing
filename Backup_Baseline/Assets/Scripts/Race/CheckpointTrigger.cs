using UnityEngine;

/// <summary>
/// NIGHTSHIFT — Checkpoint Trigger
/// Attach to a GameObject with a Trigger Collider.
/// Reports to CheckpointSystem when car enters.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Index in CheckpointSystem.checkpoints array")]
    public int checkpointIndex;

    private CheckpointSystem checkpointSystem;

    void Start()
    {
        checkpointSystem = FindAnyObjectByType<CheckpointSystem>();
        if (checkpointSystem == null)
            Debug.LogError($"[CheckpointTrigger] CheckpointSystem not found! Checkpoint {checkpointIndex} will not work.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            checkpointSystem?.CarEnteredCheckpoint(checkpointIndex);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = checkpointIndex == 0 ? Color.green :
                       GetComponent<CheckpointSystem>()?.RaceFinished == true ? Color.yellow :
                       Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
