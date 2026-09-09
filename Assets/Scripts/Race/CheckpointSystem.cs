using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NIGHTSHIFT — Checkpoint System
/// Tracks sequential checkpoint passing. Player must hit all in order.
/// </summary>
public class CheckpointSystem : MonoBehaviour
{
    [System.Serializable]
    public class Checkpoint
    {
        public Transform transform;
        public bool isPassed;
        public bool isFinishLine;
    }

    [Header("Checkpoints")]
    public Checkpoint[] checkpoints;

    [Header("Events")]
    public UnityEvent<int, int> onCheckpointPassed;   // (current, total)
    public UnityEvent onFinishReached;
    public UnityEvent onWrongCheckpoint;

    private int nextCheckpointIndex = 0;
    private bool raceFinished = false;

    public int TotalCheckpoints => checkpoints != null ? checkpoints.Length : 0;
    public int PassedCheckpoints => nextCheckpointIndex;
    public bool RaceFinished => raceFinished;

    void Start()
    {
        ResetCheckpoints();
    }

    public void ResetCheckpoints()
    {
        nextCheckpointIndex = 0;
        raceFinished = false;
        if (checkpoints != null)
        {
            foreach (var cp in checkpoints)
                cp.isPassed = false;
        }
    }

    /// <summary>
    /// Called by CheckpointTrigger when the car enters a trigger zone.
    /// </summary>
    public void CarEnteredCheckpoint(int checkpointIndex)
    {
        if (raceFinished) return;

        if (checkpointIndex == nextCheckpointIndex)
        {
            checkpoints[checkpointIndex].isPassed = true;
            nextCheckpointIndex++;

            if (checkpoints[checkpointIndex].isFinishLine)
            {
                raceFinished = true;
                onFinishReached?.Invoke();
            }
            else
            {
                onCheckpointPassed?.Invoke(nextCheckpointIndex, TotalCheckpoints);
            }
        }
        else if (checkpointIndex < nextCheckpointIndex)
        {
            // Already passed — ignore
        }
        else
        {
            // Wrong order
            onWrongCheckpoint?.Invoke();
        }
    }
}
